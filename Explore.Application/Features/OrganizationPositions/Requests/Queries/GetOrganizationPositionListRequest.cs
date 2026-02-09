using System.Collections.Generic;
using Explore.Application.DTOs.OrganizationPosition;
using MediatR;

namespace Explore.Application.Features.OrganizationPositions.Requests.Queries;

public class GetOrganizationPositionListRequest : IRequest<List<OrganizationPositionListDto>>
{
}
