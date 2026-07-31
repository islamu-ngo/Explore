// ABOUTME: Enum mirror for stable ticket-assignment status lookup identities.
// ABOUTME: Separates absent, completed, and explicitly deferred participant assignment.

namespace Explore.Domain.Enums;

public enum AssignmentStatusEnum
{
    Unassigned = 1,
    Assigned = 2,
    Deferred = 3
}
