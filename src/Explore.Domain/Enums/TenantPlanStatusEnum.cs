// ABOUTME: Stable integer identifiers for tenant plan version lifecycle states.
// ABOUTME: Mapped to TenantPlanStatus lookup rows for normalized SaaS tier persistence.

namespace Explore.Domain.Enums;

public enum TenantPlanStatusEnum
{
    Draft = 1,
    Published = 2,
    Archived = 3
}
