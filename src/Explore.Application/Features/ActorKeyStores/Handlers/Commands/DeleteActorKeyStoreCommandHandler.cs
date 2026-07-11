// ABOUTME: Handler for deleting an actor key store entry.
// ABOUTME: Fetches entity and delegates deletion to the repository.
using Explore.Application.Contracts.Persistence;
using Explore.Application.Features.ActorKeyStores.Requests.Commands;
using MediatR;

namespace Explore.Application.Features.ActorKeyStores.Handlers.Commands;

public class DeleteActorKeyStoreCommandHandler : IRequestHandler<DeleteActorKeyStoreCommand, bool>
{
    private readonly IActorKeyStoreRepository _actorKeyStoreRepository;

    public DeleteActorKeyStoreCommandHandler(IActorKeyStoreRepository actorKeyStoreRepository)
    {
        _actorKeyStoreRepository = actorKeyStoreRepository;
    }

    public async Task<bool> Handle(DeleteActorKeyStoreCommand request, CancellationToken cancellationToken)
    {
        var keyStore = await _actorKeyStoreRepository.GetById(request.Id);
        if (keyStore == null)
        {
            return false;
        }

        await _actorKeyStoreRepository.Delete(keyStore);
        return true;
    }
}
