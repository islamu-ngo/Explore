using System;
using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Queries
{
    public class GetAgendaItemsBySessionRequest : IRequest<List<EventSessionAgendaItemListDto>>
    {
        public Guid EventSessionId { get; set; }
    }
}
