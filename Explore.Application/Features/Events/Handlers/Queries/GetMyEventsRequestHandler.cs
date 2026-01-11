using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries
{
    public class GetMyEventsRequestHandler : IRequestHandler<GetMyEventsRequest, List<EventListDto>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public GetMyEventsRequestHandler(IEventRepository eventRepository, IMapper mapper)
        {
            _eventRepository = eventRepository;
            _mapper = mapper;
        }

        public async Task<List<EventListDto>> Handle(GetMyEventsRequest request, CancellationToken cancellationToken)
        {
            var events = await _eventRepository.GetMyEventsWithDetails(request.UserId);
            return _mapper.Map<List<EventListDto>>(events);
        }
    }
}
