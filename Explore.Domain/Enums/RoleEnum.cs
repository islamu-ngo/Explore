// ABOUTME: Unified role identifiers covering Platform, Tenant, Organization, and Group scopes.
// ABOUTME: Three roles per scope (Admin, Moderator, Member). IDs match the Role seed data.

namespace Explore.Domain.Enums;

public enum RoleEnum
{
    // Platform scope (1-9)
    Admin = 1,
    Moderator = 2,
    Member = 4,

    // Tenant scope (10-19)
    TenantAdmin = 11,
    TenantModerator = 12,
    TenantMember = 13,

    // Organization scope (20-29)
    OrgAdmin = 22,
    OrgModerator = 23,
    OrgMember = 24,

    // Group scope (30-39)
    GroupAdmin = 31,
    GroupModerator = 32,
    GroupMember = 33
}
