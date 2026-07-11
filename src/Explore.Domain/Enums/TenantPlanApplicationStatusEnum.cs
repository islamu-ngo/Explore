// ABOUTME: Stable integer identifiers for tenant plan application audit outcomes.
// ABOUTME: Mapped to TenantPlanApplicationStatus lookup rows for plan apply and rollback logs.

namespace Explore.Domain.Enums;

public enum TenantPlanApplicationStatusEnum
{
    Succeeded = 1,
    Failed = 2
}
