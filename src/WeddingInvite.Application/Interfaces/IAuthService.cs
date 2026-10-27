using WeddingInvite.Application.DTOs;

namespace WeddingInvite.Application.Interfaces;

public interface IAuthService
{
    Task CreateAdminAsync(CreateAdminRequest request, CancellationToken cancellationToken);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task LogoutAllAsync(Guid adminUserId, CancellationToken cancellationToken);
    Task<AdminMeResponse> GetMeAsync(Guid adminUserId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(Guid adminUserId, ChangePasswordRequest request, CancellationToken cancellationToken);
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken);
    Task VerifyResetOtpAsync(VerifyResetOtpRequest request, CancellationToken cancellationToken);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken);
}

public interface ISuperUserAuthService
{
    Task CreateSuperUserAsync(CreateSuRequest request, CancellationToken cancellationToken);
    Task<TokenResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<TokenResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);
    Task LogoutAllAsync(Guid superUserId, CancellationToken cancellationToken);
}

public interface ITenantAdminService
{
    Task<TenantAdminResponse> CreateAdminAsync(CreateTenantAdminRequest request, CancellationToken cancellationToken);
    Task<List<TenantAdminResponse>> GetAdminsAsync(CancellationToken cancellationToken);
    Task AddFrontendUrlAsync(Guid adminId, AddFrontendUrlRequest request, CancellationToken cancellationToken);
    Task<List<FrontendUrlResponse>> GetFrontendUrlsAsync(Guid adminId, CancellationToken cancellationToken);
    Task RemoveFrontendUrlAsync(Guid adminId, Guid urlId, CancellationToken cancellationToken);
    Task ToggleFrontendUrlAsync(Guid adminId, Guid urlId, CancellationToken cancellationToken);
}
