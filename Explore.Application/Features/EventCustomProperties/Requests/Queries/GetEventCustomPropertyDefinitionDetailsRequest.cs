// ABOUTME: Query request for retrieving one event-local custom property definition with options.
// ABOUTME: Used by organizer detail views for event-specific property configuration.

using Explore.Application.DTOs.EventCustomProperty;
using MediatR;

namespace Explore.Application.Features.EventCustomProperties.Requests.Queries;

public class GetEventCustomPropertyDefinitionDetailsRequest : IRequest<EventCustomPropertyDefinitionDto>
{
    public Guid Id { get; set; }
}
