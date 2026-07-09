// ABOUTME: Drains native integration sync outbox rows and dispatches them to provider services.
// ABOUTME: Applies at-least-once retry/dead-letter transitions around generated Listmonk client calls.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
using Explore.Domain;
using Explore.Infrastructure.Integrations.Listmonk;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure;

public sealed class IntegrationSyncDrainService(
    IServiceScopeFactory scopeFactory,
    IOptions<IntegrationSyncProcessorSettings> settings,
    ILogger<IntegrationSyncDrainService> logger) : IIntegrationSyncDrainService
{
    private readonly IntegrationSyncProcessorSettings _settings = settings.Value;

    public async Task<IntegrationSyncDrainResult> ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationSyncOutboxRepository>();
        var pending = await repository.GetPendingBatch(_settings.BatchSize, DateTime.UtcNow, cancellationToken);

        var completed = 0;
        var retryScheduled = 0;
        var deadLettered = 0;
        var alreadyClaimed = 0;

        foreach (var outbox in pending)
        {
            var result = await ProcessOutboxAsync(outbox, cancellationToken);
            switch (result.Outcome)
            {
                case IntegrationSyncDrainOutcome.Completed:
                    completed++;
                    break;
                case IntegrationSyncDrainOutcome.RetryScheduled:
                    retryScheduled++;
                    break;
                case IntegrationSyncDrainOutcome.DeadLettered:
                    deadLettered++;
                    break;
                case IntegrationSyncDrainOutcome.AlreadyClaimed:
                    alreadyClaimed++;
                    break;
            }
        }

        if (_settings.VerboseLogging && pending.Count > 0)
        {
            logger.LogInformation(
                "Integration sync processed {Processed}/{Pending} rows: {Completed} completed, {RetryScheduled} retry, {DeadLettered} dead-lettered, {AlreadyClaimed} already claimed",
                pending.Count - alreadyClaimed,
                pending.Count,
                completed,
                retryScheduled,
                deadLettered,
                alreadyClaimed);
        }

        return new IntegrationSyncDrainResult(
            pending.Count,
            pending.Count - alreadyClaimed,
            completed,
            retryScheduled,
            deadLettered,
            alreadyClaimed);
    }

    private async Task<IntegrationSyncSingleDrainResult> ProcessOutboxAsync(
        IntegrationSyncOutbox outbox,
        CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IIntegrationSyncOutboxRepository>();
        var listmonkSyncService = scope.ServiceProvider.GetRequiredService<ListmonkSyncService>();
        var startedAt = DateTime.UtcNow;
        var leaseToken = Guid.CreateVersion7();

        var claimed = await repository.TryMarkAsProcessing(outbox.Id, leaseToken, startedAt, cancellationToken);
        if (!claimed)
        {
            return new IntegrationSyncSingleDrainResult(IntegrationSyncDrainOutcome.AlreadyClaimed, outbox.Id);
        }

        var syncResult = await listmonkSyncService.SyncSubscriberAsync(outbox, cancellationToken);
        if (syncResult.Succeeded)
        {
            await repository.MarkAsCompleted(outbox.Id, DateTime.UtcNow, cancellationToken);
            return new IntegrationSyncSingleDrainResult(IntegrationSyncDrainOutcome.Completed, outbox.Id);
        }

        var failedAttemptCount = outbox.AttemptCount + 1;
        var maxAttempts = Math.Min(outbox.MaxAttempts, _settings.MaxAttemptCount);
        var willDeadLetter = !syncResult.IsRetryable || failedAttemptCount >= maxAttempts;
        var retryDelay = TimeSpan.FromSeconds(_settings.CalculateRetryDelay(failedAttemptCount));

        await repository.MarkAsFailed(
            outbox.Id,
            syncResult.ErrorMessage ?? "Listmonk sync failed.",
            syncResult.IsRetryable,
            retryDelay,
            _settings.MaxAttemptCount,
            DateTime.UtcNow,
            cancellationToken);

        return new IntegrationSyncSingleDrainResult(
            willDeadLetter ? IntegrationSyncDrainOutcome.DeadLettered : IntegrationSyncDrainOutcome.RetryScheduled,
            outbox.Id);
    }
}
