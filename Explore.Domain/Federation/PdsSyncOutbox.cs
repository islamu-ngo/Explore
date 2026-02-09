// ABOUTME: PDS synchronization outbox entity for reliable AT Protocol record publishing.
// ABOUTME: Implements transactional outbox pattern for eventual consistency with remote PDS.

namespace Explore.Domain.Federation;

/// <summary>
/// Outbox entry for PDS synchronization. Created atomically with domain entity changes
/// and processed asynchronously by background worker for AT Protocol publishing.
/// </summary>
public class PdsSyncOutbox
{
    /// <summary>
    /// Unique identifier (UUID v7 for time-ordering).
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Actor DID that owns the record (e.g., "did:plc:xxx").
    /// </summary>
    public required string Did { get; set; }

    /// <summary>
    /// AT Protocol collection NSID (e.g., "app.islamu.event").
    /// </summary>
    public required string Collection { get; set; }

    /// <summary>
    /// Record key within the collection (TID format).
    /// </summary>
    public required string RecordKey { get; set; }

    /// <summary>
    /// Operation type: create, update, or delete.
    /// </summary>
    public PdsSyncOperation Operation { get; set; }

    /// <summary>
    /// JSON-serialized AT Protocol record payload. Null for delete operations.
    /// </summary>
    public string? Payload { get; set; }

    /// <summary>
    /// Target PDS host URL. Null means use Islamu-hosted PDS.
    /// </summary>
    public string? PdsHost { get; set; }

    /// <summary>
    /// Current processing status.
    /// </summary>
    public PdsSyncStatus Status { get; set; }

    /// <summary>
    /// When the outbox entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// When the entry was successfully processed. Null if pending or failed.
    /// </summary>
    public DateTime? ProcessedAt { get; set; }

    /// <summary>
    /// Number of failed sync attempts.
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// Error message from the last failed attempt.
    /// </summary>
    public string? LastError { get; set; }

    /// <summary>
    /// Next retry timestamp for exponential backoff. Null if not scheduled.
    /// </summary>
    public DateTime? NextRetryAt { get; set; }

    /// <summary>
    /// Source entity type (e.g., "Event", "EventSession") for debugging.
    /// </summary>
    public string? SourceEntityType { get; set; }

    /// <summary>
    /// Source entity ID for correlation and debugging.
    /// </summary>
    public Guid? SourceEntityId { get; set; }
}

/// <summary>
/// PDS synchronization operation types.
/// </summary>
public enum PdsSyncOperation
{
    /// <summary>Create a new record in PDS.</summary>
    Create = 1,

    /// <summary>Update an existing record in PDS.</summary>
    Update = 2,

    /// <summary>Delete a record from PDS.</summary>
    Delete = 3
}

/// <summary>
/// PDS synchronization processing status.
/// </summary>
public enum PdsSyncStatus
{
    /// <summary>Awaiting processing by background worker.</summary>
    Pending = 1,

    /// <summary>Currently being processed.</summary>
    Processing = 2,

    /// <summary>Successfully synchronized with PDS.</summary>
    Completed = 3,

    /// <summary>Failed after maximum retry attempts.</summary>
    Failed = 4
}
