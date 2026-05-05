// ABOUTME: Query to list permissions with scope, group, and filtered visibility options.
// ABOUTME: Used by admin UI for permission management and role assignment dropdowns.

using Explore.Application.DTOs.Permission;
using MediatR;

namespace Explore.Application.Features.Permissions.Requests.Queries;

public class GetPermissionListRequest : IRequest<List<PermissionListDto>>
{
    /// <summary>
    /// Optional normalized role scope lookup ID filter.
    /// </summary>
    public int? RoleScopeId { get; set; }

    /// <summary>
    /// Optional group filter (e.g., "Events", "Organizations").
    /// </summary>
    public string? GroupName { get; set; }

    /// <summary>
    /// When true, hides IsFiltered permissions (dangerous ones like tenant:delete).
    /// Default true for non-super-admins.
    /// </summary>
    public bool ExcludeFiltered { get; set; } = true;
}
