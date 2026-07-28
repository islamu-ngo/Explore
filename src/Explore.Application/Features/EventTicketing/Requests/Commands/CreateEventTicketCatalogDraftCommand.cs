// ABOUTME: Creates an event ticket catalog draft with the supplied currency.
// ABOUTME: Authorizes against the parent event ticket-management action.
using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventTicketing.Requests.Commands;

[AuthorizeResource(ResourceKinds.Event, AuthorizationActions.Events.ManageTickets)]
public sealed class CreateEventTicketCatalogDraftCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventId { get; init; }
    public required string CurrencyCode { get; init; }
    string? ISecureRequest.ResourceId => EventId == Guid.Empty ? null : EventId.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => new Dictionary<string, object> { ["eventId"] = EventId.ToString() };
}
