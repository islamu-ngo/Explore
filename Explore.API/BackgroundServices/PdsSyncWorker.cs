// ABOUTME: Background worker service that processes PDS synchronization outbox entries.
// ABOUTME: Implements polling-based outbox pattern with exponential backoff for reliable AT Protocol sync.

using Explore.Application.Contracts.Infrastructure;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Federation;
using Explore.Infrastructure;
using Microsoft.Extensions.Options;

namespace Explore.API.BackgroundServices;

/// <summary>
/// Background worker that processes PDS synchronization outbox entries.
/// Polls the outbox table and syncs records with AT Protocol PDS servers.
/// </summary>
public sealed class PdsSyncWorker(
    IServiceProvider serviceProvider,
    IOptions<PdsSyncSettings> settings,
    ILogger<PdsSyncWorker> logger) : BackgroundService
{
    private readonly PdsSyncSettings _settings = settings.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            logger.LogInformation("PDS sync worker is disabled");
            return;
        }

        logger.LogInformation("PDS sync worker starting with {Interval}s polling interval, batch size {BatchSize}",
            _settings.PollingIntervalSeconds, _settings.BatchSize);

        // Initial delay to let the application fully start
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxBatchAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected during shutdown
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in PDS sync worker loop");
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

        logger.LogInformation("PDS sync worker stopped");
    }

    private async Task ProcessOutboxBatchAsync(CancellationToken stoppingToken)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var outboxRepository = scope.ServiceProvider.GetRequiredService<IPdsSyncOutboxRepository>();
        var pdsService = scope.ServiceProvider.GetRequiredService<IPdsService>();

        // Check if PDS service is available
        if (!await pdsService.IsAvailableAsync(stoppingToken))
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("PDS service not available, skipping batch");
            }
            return;
        }

        // Get pending entries
        var pendingEntries = await outboxRepository.GetPendingBatch(_settings.BatchSize);

        if (pendingEntries.Count == 0)
        {
            if (_settings.VerboseLogging)
            {
                logger.LogDebug("No pending outbox entries");
            }
            return;
        }

        logger.LogInformation("Processing {Count} PDS sync outbox entries", pendingEntries.Count);

        foreach (var entry in pendingEntries)
        {
            if (stoppingToken.IsCancellationRequested)
                break;

            await ProcessSingleEntryAsync(entry, outboxRepository, pdsService, stoppingToken);
        }
    }

    private async Task ProcessSingleEntryAsync(
        PdsSyncOutbox entry,
        IPdsSyncOutboxRepository outboxRepository,
        IPdsService pdsService,
        CancellationToken stoppingToken)
    {
        try
        {
            // Try to mark as processing (optimistic lock)
            var locked = await outboxRepository.TryMarkAsProcessing(entry.Id);
            if (!locked)
            {
                // Another worker grabbed it
                if (_settings.VerboseLogging)
                {
                    logger.LogDebug("Entry {Id} already being processed by another worker", entry.Id);
                }
                return;
            }

            if (_settings.VerboseLogging)
            {
                logger.LogDebug("Processing entry {Id}: {Operation} {Did}/{Collection}/{RecordKey}",
                    entry.Id, entry.Operation, entry.Did, entry.Collection, entry.RecordKey);
            }

            // Process the entry
            var result = await pdsService.ProcessOutboxEntryAsync(entry, stoppingToken);

            if (result.Success)
            {
                await outboxRepository.MarkAsCompleted(entry.Id, result.Uri, result.Cid);

                logger.LogInformation("Successfully synced entry {Id}: {Did}/{Collection}/{RecordKey}",
                    entry.Id, entry.Did, entry.Collection, entry.RecordKey);
            }
            else
            {
                var retryDelay = _settings.CalculateRetryDelay(entry.RetryCount);

                await outboxRepository.MarkAsFailed(
                    entry.Id,
                    result.Error ?? "Unknown error",
                    result.IsRetryable,
                    retryDelay,
                    _settings.MaxRetryCount);

                if (result.IsRetryable && entry.RetryCount < _settings.MaxRetryCount)
                {
                    logger.LogWarning("Entry {Id} failed (retry {Retry}/{Max}): {Error}. Next retry in {Delay}s",
                        entry.Id, entry.RetryCount + 1, _settings.MaxRetryCount, result.Error, retryDelay);
                }
                else
                {
                    logger.LogError("Entry {Id} permanently failed after {Retries} attempts: {Error}",
                        entry.Id, entry.RetryCount + 1, result.Error);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Exception processing entry {Id}", entry.Id);

            // Mark as failed with retryable error
            var retryDelay = _settings.CalculateRetryDelay(entry.RetryCount);
            await outboxRepository.MarkAsFailed(
                entry.Id,
                ex.Message,
                isRetryable: true,
                retryDelay,
                _settings.MaxRetryCount);
        }
    }
}
