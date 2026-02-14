// ABOUTME: Centralized permission MasterCode constants following the {resource_kind}:{action} convention.
// ABOUTME: Used by authorization checks and matches Permission.MasterCode values in the database.

namespace Explore.Domain.Constants;

/// <summary>
/// Well-known permission codes following the {resource_kind}:{action} format.
/// These match the Permission.MasterCode values stored in the database.
/// </summary>
public static class PermissionCodes
{
    // ===== Event =====
    public const string EventCreate = "event:create";
    public const string EventUpdate = "event:update";
    public const string EventDelete = "event:delete";
    public const string EventPublish = "event:publish";

    // ===== Organization =====
    public const string OrganizationManage = "organization:manage";
    public const string OrganizationUpdate = "organization:update";
    public const string OrganizationDelete = "organization:delete";

    // ===== Organization Member =====
    public const string OrganizationMemberCreate = "organization_member:create";
    public const string OrganizationMemberUpdate = "organization_member:update";
    public const string OrganizationMemberDelete = "organization_member:delete";

    // ===== Event Session =====
    public const string EventSessionCreate = "event_session:create";
    public const string EventSessionUpdate = "event_session:update";
    public const string EventSessionDelete = "event_session:delete";
}
