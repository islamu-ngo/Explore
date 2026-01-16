using MediatR;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.SyncState;
using Explore.Application.Features.SyncStates.Requests.Queries;
using Explore.Domain;

namespace Explore.Application.Features.SyncStates.Handlers.Queries
{
    public class GetSyncStateDetailsRequestHandler : IRequestHandler<GetSyncStateDetailsRequest, SyncStateDto?>
    {
        private readonly ISyncStateRepository _syncStateRepository;
        private readonly IMapper _mapper;

        public GetSyncStateDetailsRequestHandler(ISyncStateRepository syncStateRepository, IMapper mapper)
        {
            _syncStateRepository = syncStateRepository;
            _mapper = mapper;
        }

        public async Task<SyncStateDto?> Handle(GetSyncStateDetailsRequest request, CancellationToken cancellationToken)
        {
            var syncState = await _syncStateRepository.GetById(request.Id);
            if (syncState == null)
            {
                return null;
            }

            return _mapper.Map<SyncStateDto>(syncState);
        }
    }
}
