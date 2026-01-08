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
    public class GetEventSessionListRequestHandler : IRequestHandler<GetEventSessionListRequest, List<EventSessionListDto>>
    {
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IMapper _mapper;

        public GetEventSessionListRequestHandler(
            IEventSessionRepository eventSessionRepository,
            IMapper mapper)
        {
            _eventSessionRepository = eventSessionRepository;
            _mapper = mapper;
        }

        public async Task<List<EventSessionListDto>> Handle(GetEventSessionListRequest request, CancellationToken cancellationToken)
        {
            var eventSessions = await _eventSessionRepository.GetAll();
            return _mapper.Map<List<EventSessionListDto>>(eventSessions);
        }
    }
}
