// ABOUTME: Defines the scope at which a role applies across platform, tenant, organization, group, and event resources.
// ABOUTME: Used by the unified Role entity to distinguish role templates from concrete membership or assignment rows.

namespace Explore.Domain.Enums;

public enum RoleScopeEnum
{
    Platform = 0,
    Tenant = 1,
    Organization = 2,
    Group = 3,
    Event = 4
}
