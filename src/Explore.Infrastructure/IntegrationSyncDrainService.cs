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
    private const string PrivacyErasureFencedMessage = "Integration sync was not sent because the subscriber is subject to privacy erasure.";

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
        var startedAt = DateTime.UtcNow;
        var leaseToken = Guid.CreateVersion7();

        {
            await using var claimScope = scopeFactory.CreateAsyncScope();
            var claimRepository = claimScope.ServiceProvider.GetRequiredService<IIntegrationSyncOutboxRepository>();
            var claimed = await claimRepository.TryMarkAsProcessing(outbox.Id, leaseToken, startedAt, cancellationToken);
            if (!claimed)
            {
                return new IntegrationSyncSingleDrainResult(IntegrationSyncDrainOutcome.AlreadyClaimed, outbox.Id);
            }
        }

        await using var executionScope = scopeFactory.CreateAsyncScope();
        var repository = executionScope.ServiceProvider.GetRequiredService<IIntegrationSyncOutboxRepository>();
        var activeClaim = await repository.GetActiveClaimAsync(
            outbox.TenantId,
            outbox.Id,
            leaseToken,
            cancellationToken);
        if (activeClaim is null)
        {
            return new IntegrationSyncSingleDrainResult(IntegrationSyncDrainOutcome.AlreadyClaimed, outbox.Id);
        }

        if (activeClaim.UserId is not Guid userId)
        {
            return new IntegrationSyncSingleDrainResult(IntegrationSyncDrainOutcome.AlreadyClaimed, activeClaim.Id);
        }

        var privacyErasureStateRepository = executionScope.ServiceProvider.GetRequiredService<IPrivacyErasureStateRepository>();
        if (await privacyErasureStateRepository.GetBySubjectAsync(userId, cancellationToken) is not null)
        {
            await repository.MarkAsFailed(
                activeClaim.Id,
                PrivacyErasureFencedMessage,
                false,
                TimeSpan.Zero,
                _settings.MaxAttemptCount,
                DateTime.UtcNow,
                cancellationToken);
            return new IntegrationSyncSingleDrainResult(IntegrationSyncDrainOutcome.DeadLettered, activeClaim.Id);
        }

        var listmonkSyncService = executionScope.ServiceProvider.GetRequiredService<ListmonkSyncService>();
        var syncResult = await listmonkSyncService.SyncSubscriberAsync(activeClaim, cancellationToken);
        if (syncResult.Succeeded)
        {
            await repository.MarkAsCompleted(activeClaim.Id, DateTime.UtcNow, cancellationToken);
            return new IntegrationSyncSingleDrainResult(IntegrationSyncDrainOutcome.Completed, activeClaim.Id);
        }

        var failedAttemptCount = activeClaim.AttemptCount;
        var maxAttempts = Math.Min(activeClaim.MaxAttempts, _settings.MaxAttemptCount);
        var willDeadLetter = !syncResult.IsRetryable || failedAttemptCount >= maxAttempts;
        var retryDelay = TimeSpan.FromSeconds(_settings.CalculateRetryDelay(failedAttemptCount));

        await repository.MarkAsFailed(
            activeClaim.Id,
            syncResult.ErrorMessage ?? "Listmonk sync failed.",
            syncResult.IsRetryable,
            retryDelay,
            _settings.MaxAttemptCount,
            DateTime.UtcNow,
            cancellationToken);

        return new IntegrationSyncSingleDrainResult(
            willDeadLetter ? IntegrationSyncDrainOutcome.DeadLettered : IntegrationSyncDrainOutcome.RetryScheduled,
            activeClaim.Id);
    }
}
