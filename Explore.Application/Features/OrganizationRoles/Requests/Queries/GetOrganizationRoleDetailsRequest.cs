using Explore.Application.DTOs.OrganizationRole;
using MediatR;

namespace Explore.Application.Features.OrganizationRoles.Requests.Queries;

public class GetOrganizationRoleDetailsRequest : IRequest<OrganizationRoleDto>
{
    public int Id { get; set; }
}
