// ABOUTME: Query handler returning a paginated list of agenda items.
// ABOUTME: Maps entities to EventSessionAgendaItemListDto.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionAgendaItem;
using Explore.Application.Features.EventSessionAgendaItems.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessionAgendaItems.Handlers.Queries;

public class GetEventSessionAgendaItemListRequestHandler : IRequestHandler<GetEventSessionAgendaItemListRequest, PaginatedResult<EventSessionAgendaItemListDto>>
{
    private readonly IEventSessionAgendaItemRepository _agendaItemRepository;
    private readonly IMapper _mapper;

    public GetEventSessionAgendaItemListRequestHandler(
        IEventSessionAgendaItemRepository agendaItemRepository,
        IMapper mapper)
    {
        _agendaItemRepository = agendaItemRepository;
        _mapper = mapper;
    }

    public async Task<PaginatedResult<EventSessionAgendaItemListDto>> Handle(GetEventSessionAgendaItemListRequest request, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = PaginatedResult<EventSessionAgendaItemListDto>.NormalizeParameters(request.PageNumber, request.PageSize);
        var (agendaItems, totalCount) = await _agendaItemRepository.GetAgendaItemsWithDetailsPaged(pageNumber, pageSize, cancellationToken);
        var dtos = _mapper.Map<List<EventSessionAgendaItemListDto>>(agendaItems);
        return PaginatedResult<EventSessionAgendaItemListDto>.Create(dtos, totalCount, pageNumber, pageSize);
    }
}
