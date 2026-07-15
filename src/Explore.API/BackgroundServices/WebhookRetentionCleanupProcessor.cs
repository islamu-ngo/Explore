// ABOUTME: Hosted scheduler for bounded tenant-scoped webhook retention cleanup.
// ABOUTME: Keeps timer lifecycle in API while cleanup, audit, and telemetry remain infrastructure-owned.

using Explore.Application.Contracts.Webhooks;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class WebhookRetentionCleanupProcessor(
    IServiceProvider serviceProvider,
    IOptions<WebhookRetentionSettings> settings,
    ILogger<WebhookRetentionCleanupProcessor> logger) : BackgroundService
{
    private readonly WebhookRetentionSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Webhook retention cleanup processor is disabled.");
            return;
        }

        if (_settings.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.InitialDelaySeconds), stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(_settings.PollingIntervalMinutes));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<IWebhookRetentionCleanupService>();
                await cleanup.CleanupAllTenantsAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Webhook retention cleanup pass failed. FailureType={FailureType}",
                    exception.GetType().Name);
            }

            try
            {
                if (!await timer.WaitForNextTickAsync(stoppingToken))
                {
                    break;
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }
    }
}
