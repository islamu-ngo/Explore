// ABOUTME: Command to replace all permissions for an existing custom role.
// ABOUTME: Enforces capability ceiling and triggers policy sync to Cerbos.

using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Roles.Requests.Commands;

public class UpdateRolePermissionsCommand : IRequest<BaseCommandResponse<int>>
{
    public int RoleId { get; set; }

    /// <summary>
    /// New complete set of permission IDs (replaces existing).
    /// </summary>
    public required List<int> PermissionIds { get; set; }

    /// <summary>
    /// The caller's role IDs (set by handler from context).
    /// </summary>
    public List<int> CallerRoleIds { get; set; } = [];

    /// <summary>
    /// The caller's highest scope (set by handler from context).
    /// </summary>
    public Domain.Enums.RoleScopeEnum CallerHighestScope { get; set; }
}
