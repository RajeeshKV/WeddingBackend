namespace WeddingInvite.Application.DTOs;

public sealed record LoginRequest(string UsernameOrEmail, string Password);
public sealed record TokenResponse(string AccessToken, string RefreshToken, int ExpiresIn, int? SecurityVersion = null);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record CreateAdminRequest(string Username, string Email, string Password);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record ForgotPasswordRequest(string Email);
public sealed record VerifyResetOtpRequest(string Email, string Otp);
public sealed record ResetPasswordRequest(string Email, string Otp, string NewPassword);
public sealed record AdminMeResponse(Guid Id, string Username, string Email, int SecurityVersion, string? FullName = null);
public sealed record CreateSuRequest(string Username, string Email, string Password);
public sealed record CreateTenantAdminRequest(string FullName, string Username, string Email);
public sealed record TenantAdminResponse(Guid Id, string FullName, string Username, string Email, bool IsActive, bool MustChangePassword, string? DefaultWeddingSlug = null);
public sealed record AddFrontendUrlRequest(string Url);
public sealed record FrontendUrlResponse(Guid Id, string Url, bool IsActive, DateTimeOffset CreatedAtUtc);
