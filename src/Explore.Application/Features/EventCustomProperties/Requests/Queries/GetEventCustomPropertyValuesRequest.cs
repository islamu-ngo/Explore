// ABOUTME: Query request for retrieving all custom property values for a given event.
// ABOUTME: Returns a flat list of typed values keyed by definition, used for event detail views.

using Explore.Application.DTOs.EventCustomProperty;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Queries;

public sealed record GetEventCustomPropertyValuesRequest : IRequest<List<EventCustomPropertyValueDto>>
{
    public Guid EventId { get; init; }
}
