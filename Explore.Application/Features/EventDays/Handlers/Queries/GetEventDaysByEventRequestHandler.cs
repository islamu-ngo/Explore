// ABOUTME: Handler for retrieving all EventDays belonging to a specific event.
// ABOUTME: Returns a sorted list via the repository; mapping is handled by AutoMapper.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Features.EventDays.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventDays.Handlers.Queries;

public class GetEventDaysByEventRequestHandler : IRequestHandler<GetEventDaysByEventRequest, List<EventDayListDto>>
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IMapper _mapper;

    public GetEventDaysByEventRequestHandler(
        IEventDayRepository eventDayRepository,
        IMapper mapper)
    {
        _eventDayRepository = eventDayRepository;
        _mapper = mapper;
    }

    public async Task<List<EventDayListDto>> Handle(GetEventDaysByEventRequest request, CancellationToken cancellationToken)
    {
        var eventDays = await _eventDayRepository.GetByEventAsync(request.EventId, cancellationToken);
        return _mapper.Map<List<EventDayListDto>>(eventDays);
    }
}
