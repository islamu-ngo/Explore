// ABOUTME: Client read model for projection rebuild outcomes (rows processed / drained).
// ABOUTME: Mirrors the server RebuildProjectionResponseDto for snackbar / audit log display.

namespace Explore.Blazor.Client.Models.CustomProperties;

public sealed class RebuildProjectionResult
{
    public bool LockAcquired { get; set; }
    public long RowsProcessed { get; set; }
    public long RowsFailed { get; set; }
    public int DrainedDirtyScopes { get; set; }
    public DateTimeOffset? StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
}
