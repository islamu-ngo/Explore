// ABOUTME: Lifecycle states for event-scoped operational role assignments.
// ABOUTME: Authorization treats only time-effective Active assignments as grants.

namespace Explore.Domain.Enums;

public enum EventRoleAssignmentStatus
{
    Pending = 1,
    Active = 2,
    Revoked = 3,
    Expired = 4
}
