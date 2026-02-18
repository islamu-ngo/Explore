// ABOUTME: Stable integer identifiers for tenant lifecycle statuses.
// ABOUTME: Mapped to TenantStatus lookup-table rows for domain and application flow checks.

namespace Explore.Domain.Enums;

public enum TenantStatusEnum
{
    Provisioning = 1,
    Active = 2,
    Suspended = 3,
    Archived = 4,
    Purged = 5
}
