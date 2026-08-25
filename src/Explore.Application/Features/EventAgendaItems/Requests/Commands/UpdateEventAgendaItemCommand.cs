// ABOUTME: MediatR command for updating an existing event-level agenda item.
// ABOUTME: Secured via AuthorizeResource for the event_agenda_item resource kind.

using Explore.Application.Authorization;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventAgendaItem, AuthorizationActions.Update)]
public sealed record UpdateEventAgendaItemCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid EventAgendaItemId { get; init; }
    public Guid ExpectedConcurrencyStamp { get; init; }
    public required UpdateEventAgendaItemDto EventAgendaItemDto { get; init; }

    string? ISecureRequest.ResourceId => EventAgendaItemId.ToString();
}
