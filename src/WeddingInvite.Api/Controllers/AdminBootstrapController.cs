using Microsoft.AspNetCore.Mvc;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;

namespace WeddingInvite.Api.Controllers;

[ApiController]
[Route("api/v1/admin/bootstrap")]
public sealed class AdminBootstrapController(IAuthService authService) : ControllerBase
{
    [HttpPost("create-admin")]
    public async Task<IActionResult> CreateAdmin(CreateAdminRequest request, CancellationToken cancellationToken)
    {
        await authService.CreateAdminAsync(request, cancellationToken);
        return NoContent();
    }
}
