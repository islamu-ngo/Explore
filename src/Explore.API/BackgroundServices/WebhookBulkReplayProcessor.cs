// ABOUTME: Hosted scheduler for durable bounded webhook bulk replay operations.
// ABOUTME: Keeps timer lifecycle in API while atomic replay execution and audit remain infrastructure-owned.

using Explore.Application.Contracts.Webhooks;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class WebhookBulkReplayProcessor(
    IServiceProvider serviceProvider,
    IOptions<WebhookBulkReplaySettings> settings,
    ILogger<WebhookBulkReplayProcessor> logger) : BackgroundService
{
    private readonly WebhookBulkReplaySettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Webhook bulk replay processor is disabled.");
            return;
        }

        if (_settings.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.InitialDelaySeconds), stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var replayService = scope.ServiceProvider.GetRequiredService<IWebhookBulkReplayService>();
                var result = await replayService.ProcessQueuedAsync(stoppingToken);
                if (result.CompletedOperations > 0 || result.FailedOperations > 0)
                {
                    logger.LogInformation(
                        "Webhook bulk replay pass completed. Completed={CompletedOperationCount}, Scheduled={ScheduledTargetCount}, Failed={FailedOperationCount}.",
                        result.CompletedOperations,
                        result.ScheduledTargets,
                        result.FailedOperations);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Webhook bulk replay pass failed. FailureType={FailureType}",
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
