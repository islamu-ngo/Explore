using System;
using Explore.Application.DTOs.EventSessionAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;

public class GetEventSessionAgendaItemDetailsRequest : IRequest<EventSessionAgendaItemDto>
{
    public Guid Id { get; set; }
}
