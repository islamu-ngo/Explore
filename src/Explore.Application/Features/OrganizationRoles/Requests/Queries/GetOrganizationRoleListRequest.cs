using Explore.Application.DTOs.OrganizationRole;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.OrganizationRoles.Requests.Queries
{
    public class GetOrganizationRoleListRequest : IRequest<List<OrganizationRoleListDto>>
    {
    }
}
