// ABOUTME: Query to get all permissions assigned to a specific role.
// ABOUTME: Returns RolePermissionDto list showing the role's granted capabilities.

using Explore.Application.DTOs.Permission;
using MediatR;

namespace Explore.Application.Features.Permissions.Requests.Queries;

public sealed record GetRolePermissionsRequest(int RoleId = default) : IRequest<List<RolePermissionDto>>;
