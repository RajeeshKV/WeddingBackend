using Microsoft.AspNetCore.Mvc;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;

namespace WeddingInvite.Api.Controllers;

[ApiController]
[Route("api/v1/su/bootstrap")]
public sealed class SuBootstrapController(ISuperUserAuthService superUserAuthService) : ControllerBase
{
    [HttpPost("create-su")]
    public async Task<IActionResult> CreateSuperUser(CreateSuRequest request, CancellationToken cancellationToken)
    {
        await superUserAuthService.CreateSuperUserAsync(request, cancellationToken);
        return NoContent();
    }
}
