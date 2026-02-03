// ABOUTME: Repository contract for PDS sync outbox operations.
// ABOUTME: Provides methods for managing outbox entries used in transactional outbox pattern.

using Explore.Domain.Federation;

namespace Explore.Application.Contracts.Persistence;

/// <summary>
/// Repository for PDS synchronization outbox operations.
/// Used by background worker and command handlers for atomic outbox management.
/// </summary>
public interface IPdsSyncOutboxRepository
{
    /// <summary>
    /// Creates a new outbox entry.
    /// </summary>
    /// <param name="outbox">The outbox entry to create.</param>
    /// <returns>The created entry with generated ID.</returns>
    Task<PdsSyncOutbox> Create(PdsSyncOutbox outbox);

    /// <summary>
    /// Gets pending outbox entries ready for processing.
    /// Returns entries where Status is Pending and NextRetryAt is null or in the past.
    /// </summary>
    /// <param name="batchSize">Maximum number of entries to return.</param>
    /// <returns>List of pending entries ordered by creation time.</returns>
    Task<List<PdsSyncOutbox>> GetPendingBatch(int batchSize);

    /// <summary>
    /// Gets an outbox entry by ID.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <returns>The entry, or null if not found.</returns>
    Task<PdsSyncOutbox?> GetById(Guid id);

    /// <summary>
    /// Gets outbox entries for a specific source entity.
    /// </summary>
    /// <param name="sourceEntityType">The source entity type (e.g., "Event").</param>
    /// <param name="sourceEntityId">The source entity ID.</param>
    /// <returns>List of outbox entries for the entity.</returns>
    Task<List<PdsSyncOutbox>> GetBySourceEntity(string sourceEntityType, Guid sourceEntityId);

    /// <summary>
    /// Updates an existing outbox entry.
    /// </summary>
    /// <param name="outbox">The entry to update.</param>
    /// <returns>The updated entry.</returns>
    Task<PdsSyncOutbox> Update(PdsSyncOutbox outbox);

    /// <summary>
    /// Marks an entry as processing (locks it for the current worker).
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <returns>True if successfully marked, false if already processing.</returns>
    Task<bool> TryMarkAsProcessing(Guid id);

    /// <summary>
    /// Marks an entry as completed successfully.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="uri">The AT URI returned from PDS (optional).</param>
    /// <param name="cid">The CID returned from PDS (optional).</param>
    Task MarkAsCompleted(Guid id, string? uri = null, string? cid = null);

    /// <summary>
    /// Marks an entry as failed after a retry attempt.
    /// Updates RetryCount, LastError, and NextRetryAt based on backoff settings.
    /// </summary>
    /// <param name="id">The entry ID.</param>
    /// <param name="error">The error message.</param>
    /// <param name="isRetryable">Whether the error allows retrying.</param>
    /// <param name="retryDelaySeconds">Delay before next retry (for exponential backoff).</param>
    /// <param name="maxRetries">Maximum retry attempts before marking as permanently failed.</param>
    Task MarkAsFailed(Guid id, string error, bool isRetryable, int retryDelaySeconds, int maxRetries);

    /// <summary>
    /// Gets failed entries for manual review or retry.
    /// </summary>
    /// <param name="limit">Maximum entries to return.</param>
    /// <returns>List of failed entries.</returns>
    Task<List<PdsSyncOutbox>> GetFailedEntries(int limit = 100);

    /// <summary>
    /// Deletes completed entries older than the specified age.
    /// Used for cleanup of processed entries.
    /// </summary>
    /// <param name="olderThan">Delete entries processed before this time.</param>
    /// <returns>Number of deleted entries.</returns>
    Task<int> DeleteCompletedOlderThan(DateTime olderThan);
}
