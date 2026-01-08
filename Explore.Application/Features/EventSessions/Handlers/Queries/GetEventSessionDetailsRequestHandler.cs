using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Queries
{
    public class GetEventSessionDetailsRequestHandler : IRequestHandler<GetEventSessionDetailsRequest, EventSessionDto>
    {
        private readonly IEventSessionRepository _eventSessionRepository;
        private readonly IMapper _mapper;

        public GetEventSessionDetailsRequestHandler(
            IEventSessionRepository eventSessionRepository,
            IMapper mapper)
        {
            _eventSessionRepository = eventSessionRepository;
            _mapper = mapper;
        }

        public async Task<EventSessionDto> Handle(GetEventSessionDetailsRequest request, CancellationToken cancellationToken)
        {
            var eventSession = await _eventSessionRepository.GetSessionWithDetails(request.Id);
            return _mapper.Map<EventSessionDto>(eventSession);
        }
    }
}
