using System.Collections.Generic;
using Explore.Application.DTOs.OrganizationRole;
using MediatR;

namespace Explore.Application.Features.OrganizationRoles.Requests.Queries;

public class GetOrganizationRoleListRequest : IRequest<List<OrganizationRoleListDto>>
{
}
