// ABOUTME: Shared helper for organization role display (names, colors) using the OrganizationRole enum.
// ABOUTME: Replaces 3 duplicate GetRoleName and 2 duplicate GetRoleColor methods across the codebase.

using Explore.Blazor.Client.Models.Enums;
using MudBlazor;

namespace Explore.Blazor.Client.Helpers;

/// <summary>
/// Provides consistent role display logic using the <see cref="OrganizationRole"/> enum.
/// </summary>
public static class RoleHelper
{
    /// <summary>
    /// Checks if the given role ID has management permissions (Creator, CoOwner, or Admin).
    /// </summary>
    public static bool CanManage(int? roleId)
    {
        if (!roleId.HasValue) return false;
        return roleId.Value is (int)OrganizationRole.Creator
            or (int)OrganizationRole.CoOwner
            or (int)OrganizationRole.Admin;
    }

    /// <summary>
    /// Returns a human-readable name for the organization role.
    /// </summary>
    public static string GetRoleName(int? roleId) => roleId switch
    {
        (int)OrganizationRole.Creator => "Creator",
        (int)OrganizationRole.CoOwner => "Co-Owner",
        (int)OrganizationRole.Admin => "Admin",
        (int)OrganizationRole.Moderator => "Moderator",
        (int)OrganizationRole.Member => "Member",
        (int)OrganizationRole.Viewer => "Viewer",
        _ => "Unknown"
    };

    /// <summary>
    /// Returns a MudBlazor Color for the organization role badge.
    /// </summary>
    public static Color GetRoleColor(int? roleId) => roleId switch
    {
        (int)OrganizationRole.Creator => Color.Primary,
        (int)OrganizationRole.CoOwner => Color.Secondary,
        (int)OrganizationRole.Admin => Color.Info,
        (int)OrganizationRole.Moderator => Color.Warning,
        (int)OrganizationRole.Member => Color.Success,
        (int)OrganizationRole.Viewer => Color.Default,
        _ => Color.Default
    };
}
