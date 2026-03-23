// ABOUTME: MediatR query request for fetching a single visibility type by ID.
// ABOUTME: Returns VisibilityTypeDto.
using Explore.Application.DTOs.VisibilityType;
using MediatR;

namespace Explore.Application.Features.VisibilityTypes.Requests.Queries;

public class GetVisibilityTypeDetailsRequest : IRequest<VisibilityTypeDto>
{
    public int Id { get; set; }
}
