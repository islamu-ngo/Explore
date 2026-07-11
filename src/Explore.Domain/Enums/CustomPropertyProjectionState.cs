// ABOUTME: Lifecycle state of a custom-property projection rebuild tracked per (projection, version, tenant).
// ABOUTME: Drives operator visibility, advisory-lock coordination, and rebuild resume decisions.

namespace Explore.Domain.Enums;

public enum CustomPropertyProjectionState
{
    Idle = 0,
    Rebuilding = 1,
    Failed = 2,
}
