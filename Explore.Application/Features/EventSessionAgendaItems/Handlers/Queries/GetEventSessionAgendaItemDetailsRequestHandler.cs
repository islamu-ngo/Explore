// ABOUTME: Query handler returning a single agenda item by ID.
// ABOUTME: Maps entity to EventSessionAgendaItemDto.
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Queries;

public class GetEventSessionAgendaItemDetailsRequestHandler : IRequestHandler<GetEventSessionAgendaItemDetailsRequest, EventSessionAgendaItemDto>
{
    private readonly IEventSessionAgendaItemRepository _agendaItemRepository;
    private readonly IMapper _mapper;

    public GetEventSessionAgendaItemDetailsRequestHandler(
        IEventSessionAgendaItemRepository agendaItemRepository,
        IMapper mapper)
    {
        _agendaItemRepository = agendaItemRepository;
        _mapper = mapper;
    }

    public async Task<EventSessionAgendaItemDto> Handle(GetEventSessionAgendaItemDetailsRequest request, CancellationToken cancellationToken)
    {
        var agendaItem = await _agendaItemRepository.GetByIdWithDetails(request.Id, cancellationToken);
        return _mapper.Map<EventSessionAgendaItemDto>(agendaItem);
    }
}
