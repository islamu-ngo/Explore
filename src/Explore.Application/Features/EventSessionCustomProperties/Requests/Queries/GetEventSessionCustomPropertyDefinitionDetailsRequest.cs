// ABOUTME: Query request for retrieving one session-local custom property definition with options.
// ABOUTME: Used by organizer detail views for session-specific property configuration.

using Explore.Application.DTOs.EventSessionCustomProperty;
using MediatR;

namespace Explore.Application.Features.EventSessionCustomProperties.Requests.Queries;

public sealed record GetEventSessionCustomPropertyDefinitionDetailsRequest : IRequest<EventSessionCustomPropertyDefinitionDto>
{
    public Guid Id { get; init; }
}
