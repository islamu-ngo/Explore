// ABOUTME: MediatR query for fetching all agenda items in a session.
// ABOUTME: Returns IEnumerable<EventSessionAgendaItemDto>.
using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;

public sealed record GetAgendaItemsBySessionRequest : IRequest<List<EventSessionAgendaItemListDto>>
{
    public Guid EventSessionId { get; init; }
}
