// ABOUTME: Query request for retrieving all custom property values for a given event session.
// ABOUTME: Returns a flat list of typed values keyed by definition, used for session detail views.

using Explore.Application.DTOs.EventSessionCustomProperty;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Queries;

public sealed record GetEventSessionCustomPropertyValuesRequest : IRequest<List<EventSessionCustomPropertyValueDto>>
{
    public Guid EventSessionId { get; init; }
}
