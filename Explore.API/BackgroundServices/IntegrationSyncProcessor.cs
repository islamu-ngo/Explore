// ABOUTME: Hosted timer wrapper that triggers native integration sync outbox processing.
// ABOUTME: Lets Listmonk subscriber synchronization run independently of registration requests.

using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class IntegrationSyncProcessor(
    IntegrationSyncHostedDrainRunner drainRunner,
    IOptions<IntegrationSyncProcessorSettings> settings,
    ILogger<IntegrationSyncProcessor> logger) : BackgroundService
{
    private readonly IntegrationSyncProcessorSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Integration sync processor is disabled");
            return;
        }

        logger.LogInformation(
            "Integration sync processor starting with {Interval}s interval and batch size {BatchSize}",
            _settings.PollingIntervalSeconds,
            _settings.BatchSize);

        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await drainRunner.RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in integration sync processor loop");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(_settings.PollingIntervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Integration sync processor stopped");
    }
}
