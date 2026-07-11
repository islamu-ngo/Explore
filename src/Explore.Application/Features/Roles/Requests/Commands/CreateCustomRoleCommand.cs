// ABOUTME: Command to create a custom (non-system) role with initial permissions.
// ABOUTME: Enforces capability ceiling — caller can only grant permissions they have.

using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Roles.Requests.Commands;

public class CreateCustomRoleCommand : IRequest<BaseCommandResponse<int>>
{
    public required string FullName { get; set; }
    public string? Description { get; set; }
    public RoleScopeEnum Scope { get; set; }

    /// <summary>
    /// Permission IDs to assign to this role.
    /// </summary>
    public required List<int> PermissionIds { get; set; }

    /// <summary>
    /// The caller's role IDs (set by handler from context, not from client).
    /// </summary>
    public List<int> CallerRoleIds { get; set; } = [];

    /// <summary>
    /// The caller's highest scope (set by handler from context).
    /// </summary>
    public RoleScopeEnum CallerHighestScope { get; set; }
}
