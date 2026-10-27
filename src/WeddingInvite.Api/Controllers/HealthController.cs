using Microsoft.AspNetCore.Mvc;
using WeddingInvite.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace WeddingInvite.Api.Controllers;

[ApiController]
[Route("health")]
[ApiExplorerSettings(IgnoreApi = true)]
public sealed class HealthController(WeddingInviteDbContext dbContext, IMemoryCache memoryCache) : ControllerBase
{
    [HttpHead]
    public async Task<IActionResult> Head(CancellationToken cancellationToken)
    {
        const string key = "health:db";
        if (!memoryCache.TryGetValue(key, out _))
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
                memoryCache.Set(key, true, TimeSpan.FromDays(7));
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
        return Ok();
    }

    [HttpGet("live")]
    public IActionResult Live() => Ok(new { status = "Alive" });

    [HttpGet("ready")]
    public async Task<IActionResult> Ready(CancellationToken cancellationToken)
    {
        const string key = "health:db";
        if (!memoryCache.TryGetValue(key, out _))
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                await dbContext.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
                memoryCache.Set(key, true, TimeSpan.FromDays(7));
            }
            catch
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable);
            }
        }
        return Ok(new { status = "Ready", database = "Connected" });
    }
}
