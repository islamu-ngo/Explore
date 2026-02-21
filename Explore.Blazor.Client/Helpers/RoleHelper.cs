// ABOUTME: Shared helper for organization role display (names, colors) using unified Role IDs.
// ABOUTME: Replaces 3 duplicate GetRoleName and 2 duplicate GetRoleColor methods across the codebase.

using MudBlazor;

namespace Explore.Blazor.Client.Helpers;

/// <summary>
/// Provides consistent role display logic using unified Role IDs.
/// IDs match the RoleEnum values from the Domain layer (Organization scope: 20-25).
/// </summary>
public static class RoleHelper
{
    // Organization-scope role IDs matching RoleEnum
    public const int OrgCreator = 20;
    public const int OrgCoOwner = 21;
    public const int OrgAdmin = 22;
    public const int OrgModerator = 23;
    public const int OrgMember = 24;
    public const int OrgViewer = 25;

    // Group-scope role IDs matching RoleEnum
    public const int GroupCreator = 30;
    public const int GroupAdmin = 31;
    public const int GroupModerator = 32;
    public const int GroupMember = 33;

    /// <summary>
    /// Checks if the given role ID has management permissions (Creator, CoOwner, or Admin).
    /// </summary>
    public static bool CanManage(int? roleId)
    {
        if (!roleId.HasValue) return false;
        return roleId.Value is OrgCreator or OrgCoOwner or OrgAdmin;
    }

    /// <summary>
    /// Checks if the given role ID has group management permissions (Creator or Admin).
    /// </summary>
    public static bool CanManageGroup(int? roleId)
    {
        if (!roleId.HasValue) return false;
        return roleId.Value is GroupCreator or GroupAdmin;
    }

    /// <summary>
    /// Returns a human-readable name for the organization role.
    /// </summary>
    public static string GetRoleName(int? roleId) => roleId switch
    {
        OrgCreator => "Creator",
        OrgCoOwner => "Co-Owner",
        OrgAdmin => "Admin",
        OrgModerator => "Moderator",
        OrgMember => "Member",
        OrgViewer => "Viewer",
        _ => "Unknown"
    };

    /// <summary>
    /// Returns a MudBlazor Color for the organization role badge.
    /// </summary>
    public static Color GetRoleColor(int? roleId) => roleId switch
    {
        OrgCreator => Color.Primary,
        OrgCoOwner => Color.Secondary,
        OrgAdmin => Color.Info,
        OrgModerator => Color.Warning,
        OrgMember => Color.Success,
        OrgViewer => Color.Default,
        _ => Color.Default
    };

    /// <summary>
    /// Returns all assignable organization roles (excludes Creator).
    /// </summary>
    public static IReadOnlyList<(int Id, string Name)> GetAssignableOrgRoles() =>
    [
        (OrgCoOwner, "Co-Owner"),
        (OrgAdmin, "Admin"),
        (OrgModerator, "Moderator"),
        (OrgMember, "Member"),
        (OrgViewer, "Viewer")
    ];

    /// <summary>
    /// Returns all organization roles (including Creator) for filter dropdowns.
    /// </summary>
    public static IReadOnlyList<(int Id, string Name)> GetAllOrgRoles() =>
    [
        (OrgCreator, "Creator"),
        (OrgCoOwner, "Co-Owner"),
        (OrgAdmin, "Admin"),
        (OrgModerator, "Moderator"),
        (OrgMember, "Member"),
        (OrgViewer, "Viewer")
    ];

    /// <summary>
    /// Returns a human-readable name for the group role.
    /// </summary>
    public static string GetGroupRoleName(int? roleId) => roleId switch
    {
        GroupCreator => "Creator",
        GroupAdmin => "Admin",
        GroupModerator => "Moderator",
        GroupMember => "Member",
        _ => "Unknown"
    };

    /// <summary>
    /// Returns a MudBlazor Color for the group role badge.
    /// </summary>
    public static Color GetGroupRoleColor(int? roleId) => roleId switch
    {
        GroupCreator => Color.Primary,
        GroupAdmin => Color.Info,
        GroupModerator => Color.Warning,
        GroupMember => Color.Success,
        _ => Color.Default
    };

    /// <summary>
    /// Returns assignable group roles for group membership administration.
    /// </summary>
    public static IReadOnlyList<(int Id, string Name)> GetAssignableGroupRoles() =>
    [
        (GroupAdmin, "Admin"),
        (GroupModerator, "Moderator"),
        (GroupMember, "Member")
    ];
}
