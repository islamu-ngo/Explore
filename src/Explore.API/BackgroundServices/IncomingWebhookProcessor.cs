// ABOUTME: Hosted polling loop for durable incoming webhook claim processing.
// ABOUTME: Delegates bounded claims, lease renewal, and tenant-isolated execution to the drain service.

using Explore.Application.Contracts.Webhooks;
using Explore.Application.Services.Webhooks;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class IncomingWebhookProcessor(
    IIncomingWebhookDrainService drainService,
    IOptions<IncomingWebhookProcessingSettings> settings,
    ILogger<IncomingWebhookProcessor> logger) : BackgroundService
{
    private readonly IncomingWebhookProcessingSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Incoming webhook processor is disabled");
            return;
        }

        logger.LogInformation(
            "Incoming webhook processor starting with {IntervalSeconds}s interval, batch size {BatchSize}, and concurrency {Concurrency}",
            _settings.PollIntervalSeconds,
            _settings.BatchSize,
            _settings.MaxConcurrentItems);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await drainService.ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Incoming webhook processor cycle failed. FailureType={FailureType}",
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

        logger.LogInformation("Incoming webhook processor stopped");
    }
}
