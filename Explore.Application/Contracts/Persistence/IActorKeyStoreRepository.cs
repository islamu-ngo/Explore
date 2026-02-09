using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IActorKeyStoreRepository : IGenericRepository<ActorKeyStore, Guid>
{
    Task<ActorKeyStore?> GetActiveKeyByActorAndPurpose(Guid actorId, string keyPurpose);
    Task<List<ActorKeyStore>> GetKeysByActor(Guid actorId);
    Task<ActorKeyStore?> GetActorKeyStoreWithDetails(Guid id);
    Task<List<ActorKeyStore>> GetActorKeyStoresWithDetails();
}
