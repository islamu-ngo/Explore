// ABOUTME: Query to list permissions with scope, group, and filtered visibility options.
// ABOUTME: Used by admin UI for permission management and role assignment dropdowns.

using Explore.Application.DTOs.Permission;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Permissions.Requests.Queries;

public class GetPermissionListRequest : IRequest<List<PermissionListDto>>
{
    /// <summary>
    /// Optional scope filter (Platform, Tenant, Organization).
    /// </summary>
    public RoleScopeEnum? Scope { get; set; }

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
