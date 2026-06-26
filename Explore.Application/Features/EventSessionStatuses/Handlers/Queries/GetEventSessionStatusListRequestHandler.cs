// ABOUTME: Query handler returning all EventSessionStatus lookup rows.
// ABOUTME: Maps EventSessionStatus entities to EventSessionStatusListDto via AutoMapper.
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.EventSessionStatus;
using Explore.Application.Features.EventSessionStatuses.Requests.Queries;
using MediatR;

namespace Explore.Application.Features.EventSessionStatuses.Handlers.Queries;

public class GetEventSessionStatusListRequestHandler
    : IRequestHandler<GetEventSessionStatusListRequest, List<EventSessionStatusListDto>>
{
    private readonly IEventSessionStatusRepository _eventSessionStatusRepository;
    private readonly IMapper _mapper;

    public GetEventSessionStatusListRequestHandler(
        IEventSessionStatusRepository eventSessionStatusRepository,
        IMapper mapper)
    {
        _eventSessionStatusRepository = eventSessionStatusRepository;
        _mapper = mapper;
    }

    public async Task<List<EventSessionStatusListDto>> Handle(
        GetEventSessionStatusListRequest request,
        CancellationToken cancellationToken)
    {
        var statuses = await _eventSessionStatusRepository.GetAll();
        return _mapper.Map<List<EventSessionStatusListDto>>(statuses);
    }
}
