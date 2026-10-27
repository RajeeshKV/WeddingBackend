using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeddingInvite.Api.Helpers;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;

namespace WeddingInvite.Api.Controllers;

[ApiController]
[Route("api/v1/admin/auth")]
public sealed class AdminAuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        authService.LoginAsync(request, cancellationToken);

    [HttpPost("refresh")]
    public Task<TokenResponse> Refresh(RefreshTokenRequest request, CancellationToken cancellationToken) =>
        authService.RefreshAsync(request, cancellationToken);

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var adminId = JwtHelper.GetTenantId(HttpContext);
        await authService.LogoutAllAsync(adminId, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("me")]
    public async Task<AdminMeResponse> Me(CancellationToken cancellationToken)
    {
        var adminId = JwtHelper.GetTenantId(HttpContext);
        return await authService.GetMeAsync(adminId, cancellationToken);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var adminId = JwtHelper.GetTenantId(HttpContext);
        await authService.ChangePasswordAsync(adminId, request, cancellationToken);
        return NoContent();
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ForgotPasswordAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("verify-reset-otp")]
    public async Task<IActionResult> VerifyResetOtp(VerifyResetOtpRequest request, CancellationToken cancellationToken)
    {
        await authService.VerifyResetOtpAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword(ResetPasswordRequest request, CancellationToken cancellationToken)
    {
        await authService.ResetPasswordAsync(request, cancellationToken);
        return NoContent();
    }
}
