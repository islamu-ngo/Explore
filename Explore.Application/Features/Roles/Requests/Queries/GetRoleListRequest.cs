// ABOUTME: Query request to get roles with optional scope filter.
// ABOUTME: Replaces GetOrganizationRoleListRequest and GetUserRoleListRequest.

using Explore.Application.DTOs.Role;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.Roles.Requests.Queries;

public class GetRoleListRequest : IRequest<List<RoleListDto>>
{
    /// <summary>
    /// Optional scope filter. When null, returns all roles.
    /// </summary>
    public RoleScopeEnum? Scope { get; set; }
}
