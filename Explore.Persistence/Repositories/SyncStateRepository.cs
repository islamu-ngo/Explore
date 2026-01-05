using Explore.Application.Contracts.Persistence;
using Explore.Domain;
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

        public async Task<SyncState?> GetByService(string service)
        {
            return await _dbContext.SyncStates
                .FirstOrDefaultAsync(s => s.Service == service);
        }

        public async Task<long> GetCursor(string service)
        {
            var state = await GetByService(service);
            return state?.Cursor ?? 0;
        }

        public async Task UpdateCursor(string service, long cursor)
        {
            var state = await GetByService(service);
            if (state != null)
            {
                state.Cursor = cursor;
                state.UpdatedAt = DateTime.UtcNow;
                _dbContext.Entry(state).State = EntityState.Modified;
                await _dbContext.SaveChangesAsync();
            }
            else
            {
                var newState = new SyncState
                {
                    Service = service,
                    Cursor = cursor,
                    UpdatedAt = DateTime.UtcNow
                };
                await _dbContext.SyncStates.AddAsync(newState);
                await _dbContext.SaveChangesAsync();
            }
        }
    }
}
