// ABOUTME: Defines the scope at which a role applies: Platform-wide, Tenant-specific, or Organization-specific.
// ABOUTME: Used by the unified Role entity to distinguish between platform admins, tenant members, and org members.

namespace Explore.Domain.Enums;

public enum RoleScopeEnum
{
    Platform = 0,
    Tenant = 1,
    Organization = 2,
    Group = 3
}
