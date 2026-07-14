// ABOUTME: Disabled-by-default hosted loop for durable provider-publication dispatch.
// ABOUTME: Delegates bounded claims and fenced provider I/O to the infrastructure drain boundary.

using Explore.Application.Contracts.Webhooks;
using Explore.Infrastructure.Webhooks;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class WebhookProviderPublicationProcessor(
    IServiceProvider serviceProvider,
    IOptions<WebhookProviderPublicationProcessorSettings> settings,
    ILogger<WebhookProviderPublicationProcessor> logger) : BackgroundService
{
    private readonly WebhookProviderPublicationProcessorSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Provider publication processor is disabled");
            return;
        }

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds));
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = serviceProvider.CreateAsyncScope();
                var drainService = scope.ServiceProvider
                    .GetRequiredService<IWebhookProviderPublicationDrainService>();
                await drainService.ProcessBatchAsync(stoppingToken);
                await drainService.ProcessReconciliationBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(
                    "Provider publication processor cycle failed. FailureType={FailureType}",
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
