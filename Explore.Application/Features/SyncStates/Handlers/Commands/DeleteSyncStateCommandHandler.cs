// ABOUTME: Handler for deleting an AT Protocol sync state record.
// ABOUTME: Fetches sync state by ID and delegates deletion.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.SyncStates.Requests.Commands;
using Explore.Application.Responses;
using Explore.Domain;
using MediatR;

namespace Explore.Application.Features.SyncStates.Handlers.Commands;

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
