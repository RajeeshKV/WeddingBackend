using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;
using WeddingInvite.Application.Options;
using WeddingInvite.Domain.Entities;
using WeddingInvite.Infrastructure.Persistence;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using Task = System.Threading.Tasks.Task;

namespace WeddingInvite.Infrastructure.Authentication;

public sealed class AuthService(
    WeddingInviteDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    IOptions<BrevoOptions> brevoOptions,
    ILogger<AuthService> logger,
    IHttpContextAccessor httpContextAccessor) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly BrevoOptions _brevo = brevoOptions.Value;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task CreateAdminAsync(CreateAdminRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.AdminUsers.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Admin user already exists.");
        }

        var admin = new AdminUser
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            SecurityVersion = 1
        };

        dbContext.AdminUsers.Add(admin);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var lookup = request.UsernameOrEmail.Trim().ToLowerInvariant();
        var admin = await dbContext.AdminUsers.SingleOrDefaultAsync(
            x => x.Username.ToLower() == lookup || x.Email.ToLower() == lookup,
            cancellationToken);

        if (admin is null || !admin.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, admin.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var origin = httpContext?.Request.Headers.Origin.ToString();

        if (!string.IsNullOrWhiteSpace(origin))
        {
            var normalized = origin.TrimEnd('/');
            var isAllowed = await dbContext.AdminUserFrontendUrls
                .AnyAsync(x => x.AdminUserId == admin.Id && x.IsActive && x.Url == normalized, cancellationToken);

            if (!isAllowed)
            {
                throw new UnauthorizedAccessException("Invalid username/email or password.");
            }
        }
        else
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        return await IssueTokenPairAsync(admin, cancellationToken);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var token = await dbContext.RefreshTokens
            .Include(x => x.AdminUser)
            .SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (token is null || !token.IsActive || token.AdminUser is null || !token.AdminUser.IsActive)
        {
            throw new UnauthorizedAccessException("Refresh token is invalid or expired.");
        }

        if (token.RevokedAtUtc.HasValue && token.ReplacedByTokenId.HasValue)
        {
            await RevokeTokenFamilyAsync(token.FamilyId, "reuse_detected", cancellationToken);
            throw new UnauthorizedAccessException("Refresh token reuse detected.");
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevocationReason = "rotated";
            var response = await IssueTokenPairAsync(token.AdminUser, cancellationToken, token.FamilyId);
            var newTokenHash = HashToken(response.RefreshToken);
            var newToken = await dbContext.RefreshTokens.SingleAsync(x => x.TokenHash == newTokenHash && x.UserId == token.UserId, cancellationToken);
            token.ReplacedByTokenId = newToken.Id;
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new TokenResponse(response.AccessToken, response.RefreshToken, response.ExpiresIn, token.AdminUser.SecurityVersion);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var token = await dbContext.RefreshTokens.SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (token is not null && token.RevokedAtUtc is null)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevocationReason = "logout";
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task LogoutAllAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens
            .Where(x => x.UserId == adminUserId && x.UserType == "Admin" && x.RevokedAtUtc == null)
            .ToListAsync(cancellationToken);

        foreach (var token in tokens)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevocationReason = "logout_all";
        }

        var admin = await dbContext.AdminUsers.FindAsync(new object[] { adminUserId }, cancellationToken);
        if (admin != null)
        {
            admin.SecurityVersion += 1;
            admin.UpdatedAt = DateTimeOffset.UtcNow;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminMeResponse> GetMeAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        var admin = await dbContext.AdminUsers.FindAsync(new object[] { adminUserId }, cancellationToken);
        if (admin is null) throw new UnauthorizedAccessException();
        return new AdminMeResponse(admin.Id, admin.Username, admin.Email, admin.SecurityVersion, admin.FullName);
    }

    public async Task ChangePasswordAsync(Guid adminUserId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var admin = await dbContext.AdminUsers.FindAsync(new object[] { adminUserId }, cancellationToken);
        if (admin is null || !BCrypt.Net.BCrypt.Verify(request.CurrentPassword, admin.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid current password.");
        }

        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        admin.SecurityVersion += 1;
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        admin.MustChangePassword = false;

        await RevokeAllTokensAsync(adminUserId, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        await SendPasswordChangedEmailAsync(admin.Email, cancellationToken);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await dbContext.AdminUsers.SingleOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);
        if (admin is null) return;

        var otp = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var otpHash = HashToken(otp);

        dbContext.PasswordResetOtps.Add(new PasswordResetOtp
        {
            AdminUserId = admin.Id,
            OtpHash = otpHash,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddMinutes(10)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Password reset OTP requested for admin {AdminId}", admin.Id);
    }

    public async Task VerifyResetOtpAsync(VerifyResetOtpRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await dbContext.AdminUsers.SingleOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);
        if (admin is null) throw new UnauthorizedAccessException("Invalid OTP.");

        var otpHash = HashToken(request.Otp);
        var otp = await dbContext.PasswordResetOtps
            .Where(x => x.AdminUserId == admin.Id && x.OtpHash == otpHash)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || !otp.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid or expired OTP.");
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var admin = await dbContext.AdminUsers.SingleOrDefaultAsync(x => x.Email == email && x.IsActive, cancellationToken);
        if (admin is null) throw new UnauthorizedAccessException("Invalid OTP.");

        var otpHash = HashToken(request.Otp);
        var otp = await dbContext.PasswordResetOtps
            .Where(x => x.AdminUserId == admin.Id && x.OtpHash == otpHash)
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null || !otp.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid or expired OTP.");
        }

        otp.UsedAtUtc = DateTimeOffset.UtcNow;
        admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        admin.SecurityVersion += 1;
        admin.UpdatedAt = DateTimeOffset.UtcNow;
        admin.MustChangePassword = false;

        await RevokeAllTokensAsync(admin.Id, cancellationToken);
        var otps = await dbContext.PasswordResetOtps.Where(x => x.AdminUserId == admin.Id && x.UsedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var o in otps) o.UsedAtUtc = DateTimeOffset.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);
        await SendPasswordChangedEmailAsync(admin.Email, cancellationToken);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(AdminUser admin, CancellationToken cancellationToken, Guid? familyId = null)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var accessToken = CreateAccessToken(admin, expiresAt);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var fid = familyId ?? Guid.NewGuid();

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            UserId = admin.Id,
            UserType = "Admin",
            AdminUser = admin,
            TokenHash = HashToken(refreshToken),
            FamilyId = fid,
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new TokenResponse(accessToken, refreshToken, _jwt.AccessTokenMinutes * 60);
    }

    private string CreateAccessToken(AdminUser admin, DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, admin.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, admin.Email),
            new Claim(ClaimTypes.Name, admin.Username),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("tenantId", admin.Id.ToString()),
            new Claim("security_version", admin.SecurityVersion.ToString())
        };

        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task RevokeAllTokensAsync(Guid adminUserId, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens.Where(x => x.UserId == adminUserId && x.UserType == "Admin" && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevocationReason = "session_invalidated";
        }
    }

    private async Task RevokeTokenFamilyAsync(Guid familyId, string reason, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens.Where(x => x.FamilyId == familyId && x.RevokedAtUtc == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAtUtc = DateTimeOffset.UtcNow;
            token.RevocationReason = reason;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SendPasswordChangedEmailAsync(string email, CancellationToken cancellationToken)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_brevo.ApiKey)) return;
            Configuration.Default.AddApiKey("api-key", _brevo.ApiKey);
            var apiInstance = new TransactionalEmailsApi();
            var sendSmtpEmail = new SendSmtpEmail(
                sender: new SendSmtpEmailSender(_brevo.SenderName, _brevo.SenderEmail),
                to: new List<SendSmtpEmailTo> { new SendSmtpEmailTo(email) },
                subject: "Password Changed",
                textContent: "Your admin password has been changed. Please sign in again."
            );
            await apiInstance.SendTransacEmailAsync(sendSmtpEmail);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send password changed email");
        }
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
