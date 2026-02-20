// ABOUTME: Unified role identifiers covering Platform, Tenant, and Organization scopes.
// ABOUTME: Stable integer IDs matching the Role seed data for compile-time safety in authorization checks.

using System;
using System.Collections.Generic;
using System.Text;

namespace Explore.Domain.Enums;

public enum RoleEnum
{
    // Platform scope (1-9)
    Admin = 1,
    Moderator = 2,
    Editor = 3,
    Member = 4,

    // Tenant scope (10-19)
    TenantOwner = 10,
    TenantAdmin = 11,
    TenantModerator = 12,
    TenantMember = 13,

    // Organization scope (20-29)
    OrgCreator = 20,
    OrgCoOwner = 21,
    OrgAdmin = 22,
    OrgModerator = 23,
    OrgMember = 24,
    OrgViewer = 25,

    // Group scope (30-39)
    GroupCreator = 30,
    GroupAdmin = 31,
    GroupModerator = 32,
    GroupMember = 33
}
