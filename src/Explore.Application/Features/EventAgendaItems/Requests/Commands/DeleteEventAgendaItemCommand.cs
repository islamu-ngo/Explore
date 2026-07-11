// ABOUTME: MediatR command for soft-deleting an event-level agenda item.
// ABOUTME: Secured via AuthorizeResource for the event_agenda_item resource kind.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Requests.Commands;

[AuthorizeResource(ResourceKinds.EventAgendaItem, AuthorizationActions.Delete)]
public class DeleteEventAgendaItemCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid Id { get; set; }

    string? ISecureRequest.ResourceId => Id.ToString();
    IDictionary<string, object>? ISecureRequest.ResourceAttributes => null;
}
