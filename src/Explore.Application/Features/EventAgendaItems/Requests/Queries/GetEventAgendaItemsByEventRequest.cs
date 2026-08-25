// ABOUTME: MediatR query for retrieving all event-level agenda items belonging to a specific event.
// ABOUTME: Returns a list since agenda items per event are typically small (< 50).

using Explore.Application.DTOs.EventAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Requests.Queries;

public sealed record GetEventAgendaItemsByEventRequest(Guid EventId) : IRequest<List<EventAgendaItemListDto>>;
