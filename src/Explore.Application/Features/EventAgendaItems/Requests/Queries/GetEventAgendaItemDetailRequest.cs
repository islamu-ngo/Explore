// ABOUTME: MediatR query for retrieving a single event-level agenda item by Id.
// ABOUTME: Returns null when not found; caller translates to 404.

using Explore.Application.DTOs.EventAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Requests.Queries;

public class GetEventAgendaItemDetailRequest : IRequest<EventAgendaItemDto?>
{
    public Guid Id { get; set; }
}
