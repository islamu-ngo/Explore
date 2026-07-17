// ABOUTME: Hosted polling loop for durable Coop incoming-webhook effect pointers.
// ABOUTME: Delegates bounded claiming, lease renewal, and tenant execution to the effect drain service.

using Explore.Application.Contracts.Webhooks;
using Explore.Application.Services.Webhooks;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class IncomingWebhookEffectProcessor(
    IIncomingWebhookEffectDrainService drainService,
    IOptions<IncomingWebhookProcessingSettings> settings,
    ILogger<IncomingWebhookEffectProcessor> logger) : BackgroundService
{
    private readonly IncomingWebhookProcessingSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            return;
        }

        logger.LogInformation("Incoming Coop effect processor started");
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
                    "Incoming Coop effect processor cycle failed. FailureType={FailureType}",
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

        logger.LogInformation("Incoming Coop effect processor stopped");
    }
}
