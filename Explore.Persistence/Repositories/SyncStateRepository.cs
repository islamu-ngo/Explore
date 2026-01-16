using Explore.Domain;
using Explore.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class SyncStateRepository : GenericRepository<SyncState, int>, ISyncStateRepository
    {
        private readonly ExploreDbContext _dbContext;

        public SyncStateRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<SyncState>> GetAllSyncStates()
        {
            return await _dbContext.SyncStates.ToListAsync();
        }

        public async Task<SyncState?> GetSyncStateByService(string service)
        {
            return await _dbContext.SyncStates.FirstOrDefaultAsync(s => s.Service == service);
        }

        public async Task<bool> Exists(int id)
        {
            return await _dbContext.SyncStates.AnyAsync(s => s.Id == id);
        }

        public async Task<bool> ExistsByService(string service)
        {
            return await _dbContext.SyncStates.AnyAsync(s => s.Service == service);
        }
    }
}
