using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.SyncState;
using Explore.Application.Features.SyncStates.Requests.Queries;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.SyncStates.Handlers.Queries;

public class GetSyncStateListRequestHandler : IRequestHandler<GetSyncStateListRequest, List<SyncStateListDto>>
{
    private readonly ISyncStateRepository _syncStateRepository;
    private readonly IMapper _mapper;

    public GetSyncStateListRequestHandler(ISyncStateRepository syncStateRepository, IMapper mapper)
    {
        _syncStateRepository = syncStateRepository;
        _mapper = mapper;
    }

    public async Task<List<SyncStateListDto>> Handle(GetSyncStateListRequest request, CancellationToken cancellationToken)
    {
        var syncStates = await _syncStateRepository.GetAllSyncStates();
        return _mapper.Map<List<SyncStateListDto>>(syncStates);
    }
}
