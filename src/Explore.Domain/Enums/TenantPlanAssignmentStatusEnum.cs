// ABOUTME: Stable integer identifiers for tenant plan assignment lifecycle states.
// ABOUTME: Mapped to TenantPlanAssignmentStatus lookup rows to constrain active tenant subscriptions.

namespace Explore.Domain.Enums;

public enum TenantPlanAssignmentStatusEnum
{
    Active = 1,
    Superseded = 2,
    RolledBack = 3
}
