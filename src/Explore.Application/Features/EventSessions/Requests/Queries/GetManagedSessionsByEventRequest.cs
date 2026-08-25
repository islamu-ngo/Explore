// ABOUTME: Organizer-facing query request for all sessions attached to an event.
// ABOUTME: Used after API/HAL management gating so draft sessions are visible to management surfaces.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed record GetManagedSessionsByEventRequest : IRequest<List<EventSessionListDto>>, ISecureRequest
{
    public Guid EventId { get; init; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
