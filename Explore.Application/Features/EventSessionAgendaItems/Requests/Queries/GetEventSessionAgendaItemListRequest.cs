using System.Collections.Generic;
using Explore.Application.DTOs.EventSessionAgendaItem;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Requests.Queries
{
    public class GetEventSessionAgendaItemListRequest : IRequest<List<EventSessionAgendaItemListDto>>
    {
    }
}
