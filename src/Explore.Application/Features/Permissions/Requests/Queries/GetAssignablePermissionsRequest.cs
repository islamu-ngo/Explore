// ABOUTME: Query to get permissions the current user can assign (capability ceiling).
// ABOUTME: Filters by caller's own permissions and target scope boundary.

using Explore.Application.DTOs.Permission;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Permissions.Requests.Queries;

public class GetAssignablePermissionsRequest : IRequest<List<PermissionListDto>>
{
    /// <summary>
    /// The caller's role IDs (from their memberships).
    /// </summary>
    public required List<int> CallerRoleIds { get; set; }

    /// <summary>
    /// The scope of the role being created/edited.
    /// </summary>
    public RoleScopeEnum TargetScope { get; set; }
}
