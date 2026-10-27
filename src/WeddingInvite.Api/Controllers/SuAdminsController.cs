using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeddingInvite.Application.DTOs;
using WeddingInvite.Application.Interfaces;

namespace WeddingInvite.Api.Controllers;

[Authorize(Roles = "SuperUser")]
[ApiController]
[Route("api/v1/su/admins")]
public sealed class SuAdminsController(ITenantAdminService tenantAdminService) : ControllerBase
{
    [HttpPost]
    public async Task<TenantAdminResponse> CreateAdmin(CreateTenantAdminRequest request, CancellationToken cancellationToken)
    {
        return await tenantAdminService.CreateAdminAsync(request, cancellationToken);
    }

    [HttpGet]
    public Task<List<TenantAdminResponse>> GetAdmins(CancellationToken cancellationToken) =>
        tenantAdminService.GetAdminsAsync(cancellationToken);

    [HttpPost("{adminId}/frontend-urls")]
    public async Task<IActionResult> AddFrontendUrl(Guid adminId, AddFrontendUrlRequest request, CancellationToken cancellationToken)
    {
        await tenantAdminService.AddFrontendUrlAsync(adminId, request, cancellationToken);
        return NoContent();
    }

    [HttpGet("{adminId}/frontend-urls")]
    public Task<List<FrontendUrlResponse>> GetFrontendUrls(Guid adminId, CancellationToken cancellationToken) =>
        tenantAdminService.GetFrontendUrlsAsync(adminId, cancellationToken);

    [HttpDelete("{adminId}/frontend-urls/{urlId}")]
    public async Task<IActionResult> RemoveFrontendUrl(Guid adminId, Guid urlId, CancellationToken cancellationToken)
    {
        await tenantAdminService.RemoveFrontendUrlAsync(adminId, urlId, cancellationToken);
        return NoContent();
    }

    [HttpPut("{adminId}/frontend-urls/{urlId}/toggle")]
    public async Task<IActionResult> ToggleFrontendUrl(Guid adminId, Guid urlId, CancellationToken cancellationToken)
    {
        await tenantAdminService.ToggleFrontendUrlAsync(adminId, urlId, cancellationToken);
        return NoContent();
    }
}
