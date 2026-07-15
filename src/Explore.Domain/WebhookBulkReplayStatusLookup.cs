// ABOUTME: Stable relational lookup rows for webhook bulk replay operation states.
// ABOUTME: Keeps queued, executing, completed, cancelled, and failed lifecycle values normalized.

namespace Explore.Domain;

public sealed class WebhookBulkReplayStatusLookup
{
    public int Id { get; set; }
    public required string MasterCode { get; set; }
    public required string FullName { get; set; }
    public string? Description { get; set; }
}

public enum WebhookBulkReplayStatus
{
    Queued = 1,
    Executing = 2,
    Completed = 3,
    Cancelled = 4,
    Failed = 5
}
