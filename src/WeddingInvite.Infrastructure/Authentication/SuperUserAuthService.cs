using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;
using WeddingInvite.Application.Options;
using WeddingInvite.Domain.Entities;
using WeddingInvite.Infrastructure.Persistence;
using Task = System.Threading.Tasks.Task;

namespace WeddingInvite.Infrastructure.Authentication;

public sealed class SuperUserAuthService(
    WeddingInviteDbContext dbContext,
    IOptions<JwtOptions> jwtOptions,
    IHttpContextAccessor httpContextAccessor) : ISuperUserAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task CreateSuperUserAsync(CreateSuRequest request, CancellationToken cancellationToken)
    {
        if (await dbContext.SuperUsers.AnyAsync(cancellationToken))
        {
            throw new InvalidOperationException("Super user already exists.");
        }

        var su = new SuperUser
        {
            Username = request.Username.Trim(),
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            SecurityVersion = 1
        };

        dbContext.SuperUsers.Add(su);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var lookup = request.UsernameOrEmail.Trim().ToLowerInvariant();
        var su = await dbContext.SuperUsers.SingleOrDefaultAsync(
            x => x.Username.ToLower() == lookup || x.Email.ToLower() == lookup,
            cancellationToken);

        if (su is null || !su.IsActive || !BCrypt.Net.BCrypt.Verify(request.Password, su.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var origin = httpContext?.Request.Headers.Origin.ToString();

        if (!string.IsNullOrWhiteSpace(origin))
        {
            var normalized = origin.TrimEnd('/');
            var allowedByCors = await dbContext.AdminUserFrontendUrls.AnyAsync(x => x.IsActive && x.Url == normalized, cancellationToken);
            if (!allowedByCors)
            {
                throw new UnauthorizedAccessException("Invalid username/email or password.");
            }
        }
        else
        {
            throw new UnauthorizedAccessException("Invalid username/email or password.");
        }

        return await IssueTokenPairAsync(su, cancellationToken);
    }

    public async Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        throw new NotSupportedException("Use su auth flow");
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

    public async Task LogoutAllAsync(Guid superUserId, CancellationToken cancellationToken)
    {
        var su = await dbContext.SuperUsers.FindAsync(new object[] { superUserId }, cancellationToken);
        if (su != null)
        {
            su.SecurityVersion += 1;
            su.UpdatedAt = DateTimeOffset.UtcNow;
        }
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<TokenResponse> IssueTokenPairAsync(SuperUser su, CancellationToken cancellationToken)
    {
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_jwt.AccessTokenMinutes);
        var accessToken = CreateAccessToken(su, expiresAt);
        var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

        dbContext.RefreshTokens.Add(new RefreshToken
        {
            AdminUserId = su.Id,
            TokenHash = HashToken(refreshToken),
            FamilyId = Guid.NewGuid(),
            ExpiresAtUtc = DateTimeOffset.UtcNow.AddDays(_jwt.RefreshTokenDays)
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        return new TokenResponse(accessToken, refreshToken, _jwt.AccessTokenMinutes * 60);
    }

    private string CreateAccessToken(SuperUser su, DateTimeOffset expiresAt)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Key));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, su.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, su.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, su.Email),
            new Claim(ClaimTypes.Name, su.Username),
            new Claim(ClaimTypes.Role, "SuperUser"),
            new Claim("security_version", su.SecurityVersion.ToString())
        };

        var token = new JwtSecurityToken(_jwt.Issuer, _jwt.Audience, claims, expires: expiresAt.UtcDateTime, signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }
}
