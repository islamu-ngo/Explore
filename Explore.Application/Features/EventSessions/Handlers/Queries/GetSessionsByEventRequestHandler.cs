using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Queries
{
    public class GetSessionsByEventRequestHandler : IRequestHandler<GetSessionsByEventRequest, List<EventSessionListDto>>
    {
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IMapper _mapper;

        public GetSessionsByEventRequestHandler(
            IEventSessionRepository eventSessionRepository,
            IMapper mapper)
        {
            _eventSessionRepository = eventSessionRepository;
            _mapper = mapper;
        }

        public async Task<List<EventSessionListDto>> Handle(GetSessionsByEventRequest request, CancellationToken cancellationToken)
        {
            var eventSessions = await _eventSessionRepository.GetSessionsByEvent(request.EventId);
            return _mapper.Map<List<EventSessionListDto>>(eventSessions);
        }
    }
}
