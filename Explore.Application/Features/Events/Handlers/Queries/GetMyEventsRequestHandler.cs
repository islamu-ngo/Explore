using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries
{
    public class GetMyEventsRequestHandler : IRequestHandler<GetMyEventsRequest, PaginatedResult<EventListDto>>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public GetMyEventsRequestHandler(IEventRepository eventRepository, IMapper mapper)
        {
            _eventRepository = eventRepository;
            _mapper = mapper;
        }

        public async Task<PaginatedResult<EventListDto>> Handle(GetMyEventsRequest request, CancellationToken cancellationToken)
        {
            var (events, totalCount) = await _eventRepository.GetMyEventsWithDetailsPaged(request.UserId, request.PageNumber, request.PageSize);
            var eventDtos = _mapper.Map<List<EventListDto>>(events);

            return PaginatedResult<EventListDto>.Create(eventDtos, totalCount, request.PageNumber, request.PageSize);
        }
    }
}
