// ABOUTME: Defines the closed lifecycle states of an instance bootstrap generation.
// ABOUTME: Stable numeric values are persisted across pending, superseded, and completed states.

namespace Explore.Domain.Enums;

public enum InstanceBootstrapStatus
{
    Pending = 1,
    Superseded = 2,
    Completed = 3
}
