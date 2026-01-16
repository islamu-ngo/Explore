using MediatR;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Responses;
using Explore.Domain;
using Explore.Application.Features.SyncStates.Requests.Commands;

namespace Explore.Application.Features.SyncStates.Handlers.Commands
{
    public class DeleteSyncStateCommandHandler : IRequestHandler<DeleteSyncStateCommand, bool>
    {
        private readonly ISyncStateRepository _syncStateRepository;

        public DeleteSyncStateCommandHandler(ISyncStateRepository syncStateRepository)
        {
            _syncStateRepository = syncStateRepository;
        }

        public async Task<bool> Handle(DeleteSyncStateCommand request, CancellationToken cancellationToken)
        {
            var syncState = await _syncStateRepository.GetById(request.Id);
            if (syncState == null)
            {
                return false;
            }

            await _syncStateRepository.Delete(syncState);
            return true;
        }
    }
}
