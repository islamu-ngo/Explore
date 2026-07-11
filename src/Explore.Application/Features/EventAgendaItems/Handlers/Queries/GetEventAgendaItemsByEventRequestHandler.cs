// ABOUTME: Handler for retrieving all event-level agenda items belonging to a specific event.
// ABOUTME: Returns a sorted list via the repository; mapping is handled by AutoMapper.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Features.EventAgendaItems.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Handlers.Queries;

public class GetEventAgendaItemsByEventRequestHandler : IRequestHandler<GetEventAgendaItemsByEventRequest, List<EventAgendaItemListDto>>
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IMapper _mapper;

    public GetEventAgendaItemsByEventRequestHandler(
        IEventAgendaItemRepository eventAgendaItemRepository,
        IMapper mapper)
    {
        _eventAgendaItemRepository = eventAgendaItemRepository;
        _mapper = mapper;
    }

    public async Task<List<EventAgendaItemListDto>> Handle(GetEventAgendaItemsByEventRequest request, CancellationToken cancellationToken)
    {
        var items = await _eventAgendaItemRepository.GetByEventAsync(request.EventId, cancellationToken);
        return _mapper.Map<List<EventAgendaItemListDto>>(items);
    }
}
