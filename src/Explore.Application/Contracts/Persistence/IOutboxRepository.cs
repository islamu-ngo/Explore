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
    /// Returns due Pending messages, expired Processing leases, and expired
    /// DeadLettered reconciliation leases, ordered by CreatedAt.
    /// </summary>
    Task<List<OutboxMessage>> GetPendingBatch(int batchSize, CancellationToken ct = default);

    /// <summary>
    /// Atomically claims a due message for processing and returns its exact persisted
    /// lease-expiry timestamp. Returns null if another processor owns the message.
    /// </summary>
    Task<DateTime?> TryClaimForProcessing(Guid id, DateTime claimedAt, CancellationToken ct = default);

    /// <summary>
    /// Marks a message as successfully dispatched only while the exact claim is current.
    /// </summary>
    Task<bool> MarkAsCompleted(Guid id, DateTime processingLeaseExpiresAt, CancellationToken ct = default);

    /// <summary>
    /// Records a dispatch failure. Increments RetryCount, sets LastError,
    /// and either schedules a retry or marks as Failed/DeadLettered using the message's
    /// persisted MaxRetries. A stale claim returns NotOwned without changing the row.
    /// </summary>
    Task<OutboxFailureTransition> MarkAsFailed(
        Guid id,
        DateTime processingLeaseExpiresAt,
        string error,
        bool isRetryable,
        int retryDelaySeconds,
        DateTime failedAt,
        CancellationToken ct = default);

    /// <summary>
    /// Claims an expired, unreconciled DeadLettered message without changing its terminal status.
    /// </summary>
    Task<DateTime?> TryClaimDeadLetterReconciliation(
        Guid id,
        DateTime claimedAt,
        CancellationToken ct = default);

    /// <summary>
    /// Clears the reconciliation lease only when the exact DeadLettered claim is current.
    /// </summary>
    Task<bool> MarkDeadLetterReconciled(
        Guid id,
        DateTime processingLeaseExpiresAt,
        CancellationToken ct = default);

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

public enum OutboxFailureTransition
{
    NotOwned,
    RetryScheduled,
    Failed,
    DeadLettered
}
