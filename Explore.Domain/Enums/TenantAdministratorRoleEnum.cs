// ABOUTME: Canonical tenant administrator role identifiers for tenant-scoped admin assignments.
// ABOUTME: Mirrors lookup table IDs in TenantAdministratorRole for stable authorization semantics.

namespace Explore.Domain.Enums;

public enum TenantAdministratorRoleEnum
{
    TenantOwner = 1,
    TenantAdmin = 2,
    TenantModerator = 3
}
