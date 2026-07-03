// ABOUTME: Provider synchronization states for external report links.
// ABOUTME: Tracks pending, synced, failed, disabled, and ignored provider mirror state.

namespace Explore.Domain.Enums;

public enum EventReportSyncState
{
    Pending = 1,
    Synced = 2,
    Failed = 3,
    Disabled = 4,
    Ignored = 5
}
