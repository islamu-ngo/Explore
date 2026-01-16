using Explore.Domain;
using Explore.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class IndexedDidRepository : GenericRepository<IndexedDid, string>, IIndexedDidRepository
    {
        private readonly ExploreDbContext _dbContext;

        public IndexedDidRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<IndexedDid>> GetAllIndexedDids()
        {
            return await _dbContext.IndexedDids.ToListAsync();
        }

        public async Task<IndexedDid?> GetIndexedDidByDid(string did)
        {
            return await _dbContext.IndexedDids.FirstOrDefaultAsync(id => id.Did == did);
        }

        public async Task<bool> Exists(string did)
        {
            return await _dbContext.IndexedDids.AnyAsync(id => id.Did == did);
        }
    }
}
