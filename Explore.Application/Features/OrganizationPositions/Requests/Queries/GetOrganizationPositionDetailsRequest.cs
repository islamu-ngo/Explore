// ABOUTME: MediatR query request for fetching a single organization position by ID.
// ABOUTME: Returns OrganizationPositionDto.
using Explore.Application.DTOs.OrganizationPosition;
using MediatR;

namespace Explore.Application.Features.OrganizationPositions.Requests.Queries;

public class GetOrganizationPositionDetailsRequest : IRequest<OrganizationPositionDto>
{
    public int Id { get; set; }
}
