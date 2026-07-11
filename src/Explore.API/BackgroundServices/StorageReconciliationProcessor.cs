// ABOUTME: Hosted timer that triggers bounded storage reconciliation passes.
// ABOUTME: Keeps API scheduling separate from scoped reconciliation logic in Infrastructure.

using Explore.Application.Contracts.Services;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class StorageReconciliationProcessor(
    IServiceProvider serviceProvider,
    IOptions<StorageReconciliationSettings> settings,
    ILogger<StorageReconciliationProcessor> logger) : BackgroundService
{
    private readonly StorageReconciliationSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Storage reconciliation processor is disabled.");
            return;
        }

        logger.LogInformation(
            "Storage reconciliation processor starting with {Interval} minute interval, batch size {BatchSize}, dry-run {DryRun}.",
            _settings.PollingIntervalMinutes,
            _settings.BatchSize,
            _settings.DryRun);

        if (_settings.InitialDelaySeconds > 0)
        {
            await Task.Delay(TimeSpan.FromSeconds(_settings.InitialDelaySeconds), stoppingToken);
        }

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
                logger.LogError(ex, "Error in storage reconciliation processor loop.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(_settings.PollingIntervalMinutes), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        logger.LogInformation("Storage reconciliation processor stopped.");
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var reconciliationService = scope.ServiceProvider.GetRequiredService<IStorageReconciliationService>();
        await reconciliationService.ReconcileAsync(DateTime.UtcNow, stoppingToken);
    }
}
