// ABOUTME: Event-scoped organizer query for exact session agenda items.
// ABOUTME: Authorizes against the parent event and carries the session ID only as a bounded child selector.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventSessionAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetManagedAgendaItemsBySessionRequest
    : IRequest<List<EventSessionAgendaItemListDto>?>, ISecureRequest
{
    public Guid EventId { get; set; }
    public Guid EventSessionId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
