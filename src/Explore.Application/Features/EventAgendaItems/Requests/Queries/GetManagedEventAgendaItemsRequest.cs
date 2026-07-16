// ABOUTME: Event-scoped organizer queries for exact event agenda collection and detail reads.
// ABOUTME: Resource authorization protects physical location fields from public disclosure.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetManagedEventAgendaItemsByEventRequest
    : IRequest<List<EventAgendaItemListDto>>, ISecureRequest
{
    public Guid EventId { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ViewManagement)]
public sealed class GetManagedEventAgendaItemDetailRequest
    : IRequest<EventAgendaItemDto?>, ISecureRequest
{
    public Guid EventId { get; set; }
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => EventId.ToString();
}
