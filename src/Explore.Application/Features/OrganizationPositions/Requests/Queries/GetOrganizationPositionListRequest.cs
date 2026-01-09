using Explore.Application.DTOs.OrganizationPosition;
using MediatR;
using System.Collections.Generic;

namespace Explore.Application.Features.OrganizationPositions.Requests.Queries
{
    public class GetOrganizationPositionListRequest : IRequest<List<OrganizationPositionListDto>>
    {
    }
}
