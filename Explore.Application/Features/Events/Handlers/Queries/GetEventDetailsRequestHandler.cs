using System;
using System.Collections.Generic;
using System.Text;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.Events.Handlers.Queries
{
    public class GetEventDetailsRequestHandler : IRequestHandler<GetEventDetailsRequest, EventDto>
    {
        private readonly IEventRepository _eventRepository;
        private readonly IMapper _mapper;

        public GetEventDetailsRequestHandler(IEventRepository eventRepository, IMapper mapper)
        {
            _eventRepository = eventRepository;
            _mapper = mapper;
        }

        public async Task<EventDto> Handle(GetEventDetailsRequest request, CancellationToken cancellationToken)
        {
            var @event = await _eventRepository.GetEventWithDetails(request.Id);
            var eventDto = _mapper.Map<EventDto>(@event);
            return eventDto;
        }
    }
}
