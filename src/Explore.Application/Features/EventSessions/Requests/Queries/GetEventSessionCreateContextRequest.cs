// ABOUTME: MediatR query request for dedicated event session/program item creation context.
// ABOUTME: Returns server-owned defaults and selector options for an event-scoped composer.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed record GetEventSessionCreateContextRequest : IRequest<EventSessionCreateContextDto?>, ISecureRequest
{
    public Guid EventId { get; init; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
