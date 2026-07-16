// ABOUTME: Event-scoped organizer query for an exact event session management read.
// ABOUTME: Resource authorization prevents public redaction bypass and cross-event identifier probing.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSession;
using MediatR;

namespace Explore.Application.Features.EventSessions.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetManagedEventSessionDetailsRequest : IRequest<EventSessionDto?>, ISecureRequest
{
    public Guid EventId { get; set; }
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
