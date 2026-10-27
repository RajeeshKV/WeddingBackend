using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;
using System.Security.Claims;

namespace WeddingInvite.Api.Controllers;

[ApiController]
[Route("api/v1/su/auth")]
public sealed class SuAuthController(ISuperUserAuthService superUserAuthService) : ControllerBase
{
    [HttpPost("login")]
    public Task<TokenResponse> Login(LoginRequest request, CancellationToken cancellationToken) =>
        superUserAuthService.LoginAsync(request, cancellationToken);

    [Authorize(Roles = "SuperUser")]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(LogoutRequest request, CancellationToken cancellationToken)
    {
        await superUserAuthService.LogoutAsync(request, cancellationToken);
        return NoContent();
    }

    [Authorize(Roles = "SuperUser")]
    [HttpPost("logout-all")]
    public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
    {
        var suId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)!);
        await superUserAuthService.LogoutAllAsync(suId, cancellationToken);
        return NoContent();
    }
}
