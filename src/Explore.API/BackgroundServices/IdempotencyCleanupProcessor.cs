// ABOUTME: Hosted timer that triggers expired idempotency replay-cache cleanup.
// ABOUTME: Keeps scheduling in the API host while scoped cleanup logic lives in Infrastructure.

using Explore.Application.Contracts.Services;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

public sealed class IdempotencyCleanupProcessor(
    IServiceProvider serviceProvider,
    IOptions<IdempotencyCleanupSettings> settings,
    ILogger<IdempotencyCleanupProcessor> logger) : BackgroundService
{
    private readonly IdempotencyCleanupSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("Idempotency cleanup processor is disabled");
            return;
        }

        logger.LogInformation(
            "Idempotency cleanup processor starting with {Interval} minute interval, batch size {BatchSize}, dry-run {DryRun}",
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
                logger.LogError(ex, "Error in idempotency cleanup processor loop");
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

        logger.LogInformation("Idempotency cleanup processor stopped");
    }

    private async Task ProcessBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();
        var cleanupService = scope.ServiceProvider.GetRequiredService<IIdempotencyCleanupService>();
        await cleanupService.CleanupExpiredAsync(DateTime.UtcNow, stoppingToken);
    }
}
