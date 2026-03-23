// ABOUTME: MediatR query request for fetching all organization positions.
// ABOUTME: Returns IEnumerable<OrganizationPositionDto>.
using System.Collections.Generic;
using Explore.Application.DTOs.OrganizationPosition;
using MediatR;

namespace Explore.Application.Features.OrganizationPositions.Requests.Queries;

public class GetOrganizationPositionListRequest : IRequest<List<OrganizationPositionListDto>>
{
}
