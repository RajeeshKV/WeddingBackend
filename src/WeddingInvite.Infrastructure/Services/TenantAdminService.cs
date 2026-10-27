using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;
using WeddingInvite.Application.Options;
using WeddingInvite.Domain.Entities;
using WeddingInvite.Infrastructure.Persistence;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using Task = System.Threading.Tasks.Task;

namespace WeddingInvite.Infrastructure.Services;

public sealed class TenantAdminService(
    WeddingInviteDbContext dbContext,
    IOptions<BrevoOptions> brevoOptions,
    ILogger<TenantAdminService> logger) : ITenantAdminService
{
    private readonly BrevoOptions _brevo = brevoOptions.Value;

    public async Task<TenantAdminResponse> CreateAdminAsync(CreateTenantAdminRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var username = request.Username.Trim().ToLowerInvariant();
        if (await dbContext.AdminUsers.AnyAsync(x => x.Email == email || x.Username == username, cancellationToken))
        {
            throw new InvalidOperationException("Admin with this email or username already exists.");
        }

        var tempPassword = GeneratePassword();
        var slug = GenerateSlug(request.FullName);
        var admin = new AdminUser
        {
            FullName = request.FullName.Trim(),
            Username = username,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(tempPassword),
            MustChangePassword = true,
            SecurityVersion = 1,
            IsActive = true
        };

        dbContext.AdminUsers.Add(admin);
        var wedding = new Wedding
        {
            AdminUserId = admin.Id,
            Name = request.FullName.Trim(),
            Slug = slug,
            IsPublished = true,
            RsvpEnabled = true,
            GuestUploadsEnabled = true,
            AutoApprovePhotos = false
        };
        dbContext.Weddings.Add(wedding);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SendCredentialsEmailAsync(admin.Email, admin.Username, tempPassword, cancellationToken);
        return new TenantAdminResponse(admin.Id, admin.FullName, admin.Username, admin.Email, admin.IsActive, admin.MustChangePassword, slug);
    }

    public async Task<List<TenantAdminResponse>> GetAdminsAsync(CancellationToken cancellationToken)
    {
        var admins = await dbContext.AdminUsers.ToListAsync(cancellationToken);
        var result = new List<TenantAdminResponse>();
        foreach (var a in admins)
        {
            var slug = await dbContext.Weddings.Where(w => w.AdminUserId == a.Id).OrderBy(w => w.CreatedAtUtc).Select(w => w.Slug).FirstOrDefaultAsync(cancellationToken);
            result.Add(new TenantAdminResponse(a.Id, a.FullName, a.Username, a.Email, a.IsActive, a.MustChangePassword, slug));
        }
        return result;
    }

    public async Task AddFrontendUrlAsync(Guid adminId, AddFrontendUrlRequest request, CancellationToken cancellationToken)
    {
        var url = NormalizeUrl(request.Url);
        if (string.IsNullOrWhiteSpace(url)) throw new InvalidOperationException("Invalid URL.");
        var exists = await dbContext.AdminUserFrontendUrls.AnyAsync(x => x.AdminUserId == adminId && x.Url == url, cancellationToken);
        if (exists) throw new InvalidOperationException("URL already exists.");
        dbContext.AdminUserFrontendUrls.Add(new AdminUserFrontendUrl { AdminUserId = adminId, Url = url, IsActive = true });
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<FrontendUrlResponse>> GetFrontendUrlsAsync(Guid adminId, CancellationToken cancellationToken)
    {
        return await dbContext.AdminUserFrontendUrls
            .Where(x => x.AdminUserId == adminId)
            .Select(x => new FrontendUrlResponse(x.Id, x.Url, x.IsActive, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task RemoveFrontendUrlAsync(Guid adminId, Guid urlId, CancellationToken cancellationToken)
    {
        var url = await dbContext.AdminUserFrontendUrls.FirstOrDefaultAsync(x => x.Id == urlId && x.AdminUserId == adminId, cancellationToken);
        if (url is null) return;
        dbContext.AdminUserFrontendUrls.Remove(url);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ToggleFrontendUrlAsync(Guid adminId, Guid urlId, CancellationToken cancellationToken)
    {
        var url = await dbContext.AdminUserFrontendUrls.FirstOrDefaultAsync(x => x.Id == urlId && x.AdminUserId == adminId, cancellationToken);
        if (url is null) return;
        url.IsActive = !url.IsActive;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SendCredentialsEmailAsync(string email, string username, string password, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_brevo.ApiKey)) 
            {
                logger.LogWarning("Brevo API key not configured. Admin credentials not emailed.");
                return;
            }
            Configuration.Default.AddApiKey("api-key", _brevo.ApiKey);
            var apiInstance = new TransactionalEmailsApi();
            var sendSmtpEmail = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(_brevo.SenderName, _brevo.SenderEmail),
                to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(email) },
                subject: "Your Admin Account Credentials",
                textContent: $"Username/Email: {username}\nTemporary Password: {password}\n\nPlease login and reset your password."
            );
            await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send admin credentials email");
        }
    }

    private static string GeneratePassword()
    {
        var bytes = RandomNumberGenerator.GetBytes(12);
        return Convert.ToBase64String(bytes).Replace("+", "").Replace("/", "").Replace("=", "")[..12];
    }

    private static string GenerateSlug(string name)
    {
        var baseSlug = new string(name.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || char.IsWhiteSpace(c)).ToArray())
            .Trim().Replace(" ", "-");
        var suffix = RandomNumberGenerator.GetHexString(6).ToLower();
        return $"{baseSlug}-{suffix}";
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri)) return string.Empty;
        if (uri.Scheme != "http" && uri.Scheme != "https") return string.Empty;
        return uri.Scheme + "://" + uri.Authority;
    }
}
