// ABOUTME: Query to retrieve all projection rows for a specific event session with optional exposure ceiling.
// ABOUTME: Used for admin inspection and future aggregate view composition.

using Explore.Application.DTOs.CustomPropertyProjection;
using Explore.Application.Responses;
using Explore.Domain.Enums;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomPropertyProjections.Requests.Queries;

public class GetEventSessionCustomPropertyProjectionsForSessionQuery : IRequest<BaseCommandResponse<IReadOnlyList<EventSessionCustomPropertyProjectionDto>>>
{
    public Guid EventSessionId { get; set; }
    public ExposureLevel? ExposureCeiling { get; set; }
}
