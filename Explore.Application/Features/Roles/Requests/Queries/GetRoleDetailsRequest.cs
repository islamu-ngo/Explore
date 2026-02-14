// ABOUTME: Query request to get a single role by ID.
// ABOUTME: Replaces GetOrganizationRoleDetailsRequest and GetUserRoleDetailsRequest.

using Explore.Application.DTOs.Role;
using MediatR;

namespace Explore.Application.Features.Roles.Requests.Queries;

public class GetRoleDetailsRequest : IRequest<RoleDto?>
{
    public int Id { get; set; }
}
