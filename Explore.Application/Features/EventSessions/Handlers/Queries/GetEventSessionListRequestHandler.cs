// ABOUTME: Query handler returning a paginated list of event sessions.
// ABOUTME: Maps entities to EventSessionListDto.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Features.EventSessions.Requests.Queries;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.EventSessions.Handlers.Queries;

public class GetEventSessionListRequestHandler : IRequestHandler<GetEventSessionListRequest, PaginatedResult<EventSessionListDto>>
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

    public async Task<PaginatedResult<EventSessionListDto>> Handle(GetEventSessionListRequest request, CancellationToken cancellationToken)
    {
        var (pageNumber, pageSize) = PaginatedResult<EventSessionListDto>.NormalizeParameters(request.PageNumber, request.PageSize);
        var (eventSessions, totalCount) = await _eventSessionRepository.GetSessionsWithDetailsPaged(pageNumber, pageSize);
        var dtos = _mapper.Map<List<EventSessionListDto>>(eventSessions);
        return PaginatedResult<EventSessionListDto>.Create(dtos, totalCount, pageNumber, pageSize);
    }
}
