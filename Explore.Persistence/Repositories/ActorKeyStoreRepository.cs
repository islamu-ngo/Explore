using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class ActorKeyStoreRepository : GenericRepository<ActorKeyStore, Guid>, IActorKeyStoreRepository
    {
        private readonly ExploreDbContext _dbContext;

        public ActorKeyStoreRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<ActorKeyStore?> GetActiveKeyByActorAndPurpose(Guid actorId, string keyPurpose)
        {
            return await _dbContext.ActorKeyStores
                .Where(k => k.ActorId == actorId && k.KeyPurpose == keyPurpose && k.IsActive == true)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ActorKeyStore>> GetKeysByActor(Guid actorId)
        {
            return await _dbContext.ActorKeyStores
                .Where(k => k.ActorId == actorId)
                .ToListAsync();
        }
    }
}
