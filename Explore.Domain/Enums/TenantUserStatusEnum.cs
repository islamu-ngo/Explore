// ABOUTME: Tenant-local user participation lifecycle states.
// ABOUTME: Keeps tenant bans, suspensions, and removals scoped away from the global User account.

namespace Explore.Domain.Enums;

public enum TenantUserStatusEnum
{
    Active = 1,
    Suspended = 2,
    Banned = 3,
    Removed = 4
}
