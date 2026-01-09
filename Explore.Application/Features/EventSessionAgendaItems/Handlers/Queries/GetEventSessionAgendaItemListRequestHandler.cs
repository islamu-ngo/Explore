using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Queries
{
    public class GetEventSessionAgendaItemListRequestHandler : IRequestHandler<GetEventSessionAgendaItemListRequest, List<EventSessionAgendaItemListDto>>
    {
        private readonly IEventSessionAgendaItemRepository _agendaItemRepository;
        private readonly IMapper _mapper;

        public GetEventSessionAgendaItemListRequestHandler(
            IEventSessionAgendaItemRepository agendaItemRepository,
            IMapper mapper)
        {
            _agendaItemRepository = agendaItemRepository;
            _mapper = mapper;
        }

        public async Task<List<EventSessionAgendaItemListDto>> Handle(GetEventSessionAgendaItemListRequest request, CancellationToken cancellationToken)
        {
            var agendaItems = await _agendaItemRepository.GetAll();
            return _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems);
        }
    }
}
