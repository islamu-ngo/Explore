// ABOUTME: Repository contract for durable native integration synchronization outbox rows.
// ABOUTME: Supports cancellation-aware worker polling, optimistic processing claims, completion, and retry/dead-letter transitions.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IIntegrationSyncOutboxRepository
{
    Task<IntegrationSyncOutbox> Create(IntegrationSyncOutbox outbox, CancellationToken cancellationToken);

    Task<IReadOnlyList<IntegrationSyncOutbox>> GetPendingBatch(
        int batchSize,
        DateTime now,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken);

    Task MarkAsCompleted(Guid id, DateTime completedAt, CancellationToken cancellationToken);

    Task MarkAsFailed(
        Guid id,
        string errorMessage,
        bool isRetryable,
        TimeSpan retryDelay,
        int maxAttempts,
        DateTime failedAt,
        CancellationToken cancellationToken);
}
