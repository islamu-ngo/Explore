// ABOUTME: Handler for retrieving a single EventDay by Id.
// ABOUTME: Returns null when not found; the controller translates to 404.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventDay;
using Explore.Application.Features.EventDays.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventDays.Handlers.Queries;

public class GetEventDayDetailRequestHandler : IRequestHandler<GetEventDayDetailRequest, EventDayDto?>
{
    private readonly IEventDayRepository _eventDayRepository;
    private readonly IMapper _mapper;

    public GetEventDayDetailRequestHandler(
        IEventDayRepository eventDayRepository,
        IMapper mapper)
    {
        _eventDayRepository = eventDayRepository;
        _mapper = mapper;
    }

    public async Task<EventDayDto?> Handle(GetEventDayDetailRequest request, CancellationToken cancellationToken)
    {
        var eventDay = await _eventDayRepository.GetById(request.Id);
        if (eventDay == null)
            return null;

        return _mapper.Map<EventDayDto>(eventDay);
    }
}
