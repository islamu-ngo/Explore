// ABOUTME: Response DTO for a projection rebuild operation with outcome statistics.
// ABOUTME: Includes lock-acquisition status and dirty-scope drain count.

namespace Explore.Application.DTOs.CustomPropertyProjection;

public class RebuildProjectionResponseDto
{
    public bool LockAcquired { get; set; }
    public long RowsProcessed { get; set; }
    public long RowsFailed { get; set; }
    public int DrainedDirtyScopes { get; set; }
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset CompletedAt { get; set; }
}
