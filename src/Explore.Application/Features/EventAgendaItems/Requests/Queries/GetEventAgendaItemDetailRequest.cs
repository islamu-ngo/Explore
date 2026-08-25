// ABOUTME: MediatR query for retrieving a single event-level agenda item by Id.
// ABOUTME: Returns null when not found; caller translates to 404.

using Explore.Application.DTOs.EventAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Requests.Queries;

public sealed record GetEventAgendaItemDetailRequest(Guid Id) : IRequest<EventAgendaItemDto?>;
