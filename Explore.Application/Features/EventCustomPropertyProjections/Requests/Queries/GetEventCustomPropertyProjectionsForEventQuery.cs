// ABOUTME: Query to retrieve all projection rows for a specific event, optionally filtered by exposure ceiling.
// ABOUTME: Used for admin inspection and as a dependency for Milestone F aggregate view composition.

using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventCustomPropertyProjections.Requests.Queries;

public class GetEventCustomPropertyProjectionsForEventQuery : IRequest<BaseCommandResponse<IReadOnlyList<EventCustomPropertyProjectionDto>>>
{
    public Guid EventId { get; set; }
    public ExposureLevel? ExposureCeiling { get; set; }
}
