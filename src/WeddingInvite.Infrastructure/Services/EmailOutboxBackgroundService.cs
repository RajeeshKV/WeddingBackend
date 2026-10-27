using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WeddingInvite.Infrastructure.Services;

public sealed class EmailOutboxBackgroundService(ILogger<EmailOutboxBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogDebug("Email outbox worker running");
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
