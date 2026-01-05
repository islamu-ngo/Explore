using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class ActorRepository : GenericRepository<Actor, Guid>, IActorRepository
    {
        private readonly ExploreDbContext _dbContext;

        public ActorRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Actor?> GetActorWithDetails(Guid id)
        {
            return await _dbContext.Actors
                .Include(a => a.ActorType)
                .Include(a => a.DidCustodyType)
                .Include(a => a.ProfilePicture)
                .Include(a => a.Tenant)
                .FirstOrDefaultAsync(a => a.Id == id);
        }

        public async Task<Actor?> GetActorByDid(string did)
        {
            return await _dbContext.Actors
                .Include(a => a.ActorType)
                .FirstOrDefaultAsync(a => a.Did == did);
        }

        public async Task<Actor?> GetActorByHandle(string handle)
        {
            return await _dbContext.Actors
                .Include(a => a.ActorType)
                .FirstOrDefaultAsync(a => a.Handle == handle);
        }

        public async Task<List<Actor>> GetActorsByTenant(Guid tenantId)
        {
            return await _dbContext.Actors
                .Include(a => a.ActorType)
                .Where(a => a.TenantId == tenantId)
                .ToListAsync();
        }
    }
}
