// ABOUTME: Handler for retrieving a single public event-level agenda item by Id.
// ABOUTME: Returns published data while redacting exact physical location and room IDs.

using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventAgendaItem;
using Explore.Application.Features.EventAgendaItems.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventAgendaItems.Handlers.Queries;

public class GetEventAgendaItemDetailRequestHandler : IRequestHandler<GetEventAgendaItemDetailRequest, EventAgendaItemDto?>
{
    private readonly IEventAgendaItemRepository _eventAgendaItemRepository;
    private readonly IMapper _mapper;

    public GetEventAgendaItemDetailRequestHandler(
        IEventAgendaItemRepository eventAgendaItemRepository,
        IMapper mapper)
    {
        _eventAgendaItemRepository = eventAgendaItemRepository;
        _mapper = mapper;
    }

    public async Task<EventAgendaItemDto?> Handle(GetEventAgendaItemDetailRequest request, CancellationToken cancellationToken)
    {
        var agendaItem = await _eventAgendaItemRepository.GetPublicByIdAsync(request.Id, cancellationToken);
        if (agendaItem == null)
            return null;

        var dto = _mapper.Map<EventAgendaItemDto>(agendaItem);
        dto.LocationId = null;
        dto.RoomId = null;
        return dto;
    }
}
