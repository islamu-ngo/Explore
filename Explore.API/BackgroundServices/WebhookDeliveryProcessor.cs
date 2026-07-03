// ABOUTME: Hosted timer wrapper that drains LocalProvider webhook delivery attempts.
// ABOUTME: Keeps API scheduling separate from Infrastructure-owned HTTP delivery state transitions.

using Explore.Application.Contracts.Services;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class WebhookDeliveryProcessor(
    IServiceProvider serviceProvider,
    IOptions<WebhookDeliveryProcessorSettings> settings,
    ILogger<WebhookDeliveryProcessor> logger) : BackgroundService
{
    private readonly WebhookDeliveryProcessorSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Webhook delivery processor is disabled");
            return;
        }

        logger.LogInformation(
            "Webhook delivery processor starting with {Interval}s interval and batch size {BatchSize}",
            _settings.PollingIntervalSeconds,
            _settings.BatchSize);

        if (_settings.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.InitialDelaySeconds), stoppingToken);
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in webhook delivery processor loop");
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

        logger.LogInformation("Webhook delivery processor stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var drainService = scope.ServiceProvider.GetRequiredService<IWebhookDeliveryDrainService>();
        await drainService.RecoverStaleProcessingAsync(stoppingToken);
        await drainService.ProcessBatchAsync(stoppingToken);
    }
}
