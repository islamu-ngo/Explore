// ABOUTME: Requests ticketing management data for one event.
// ABOUTME: Authorizes against the parent event ticket-management action.
using Explore.Application.Authorization;
using Explore.Application.DTOs.EventTicketing;
using MediatR;

namespace Explore.Application.Features.EventTicketing.Requests.Queries;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTickets)]
public sealed record GetEventTicketCatalogManagementQuery(Guid EventId) : IRequest<EventTicketCatalogManagementDto?>, ISecureRequest
{
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}
