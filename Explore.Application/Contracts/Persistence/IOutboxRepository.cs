// ABOUTME: Repository contract for generic outbox message persistence operations.
// ABOUTME: Standalone interface (not IGenericRepository) with methods optimized for background processor polling.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

/// <summary>
/// Persistence operations for <see cref="OutboxMessage"/> entities.
/// Designed for the outbox pattern: write inside transaction, poll and dispatch from background processor.
/// </summary>
public interface IOutboxRepository
{
    /// <summary>
    /// Persists a new outbox message. Call inside the UnitOfWork transaction lambda
    /// so the message is committed atomically with domain writes.
    /// </summary>
    Task<OutboxMessage> Create(OutboxMessage message);

    /// <summary>
    /// Returns the next batch of pending messages eligible for processing.
    /// Filters by Status == Pending and NextRetryAt &lt;= now, ordered by CreatedAt.
    /// </summary>
    Task<List<OutboxMessage>> GetPendingBatch(int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Atomically marks a message as Processing using optimistic concurrency.
    /// Returns false if another processor already claimed it.
    /// </summary>
    Task<bool> TryMarkAsProcessing(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Marks a message as successfully dispatched.
    /// </summary>
    Task MarkAsCompleted(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Records a dispatch failure. Increments RetryCount, sets LastError,
    /// and either schedules a retry or marks as Failed/DeadLettered based on retry policy.
    /// </summary>
    Task MarkAsFailed(Guid id, string error, bool isRetryable, int retryDelaySeconds, int maxRetries, CancellationToken ct = default);

    /// <summary>
    /// Returns failed/dead-lettered entries for monitoring and manual intervention.
    /// </summary>
    Task<List<OutboxMessage>> GetFailedEntries(int limit = 100, CancellationToken ct = default);

    /// <summary>
    /// Removes completed entries older than the cutoff date for table hygiene.
    /// Returns the number of rows deleted.
    /// </summary>
    Task<int> DeleteCompletedOlderThan(DateTime cutoff, CancellationToken ct = default);
}
