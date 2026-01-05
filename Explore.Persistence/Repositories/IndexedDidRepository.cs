using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class IndexedDidRepository : IIndexedDidRepository
    {
        private readonly ExploreDbContext _dbContext;

        public IndexedDidRepository(ExploreDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IndexedDid?> GetByDid(string did)
        {
            return await _dbContext.IndexedDids
                .FirstOrDefaultAsync(d => d.Did == did);
        }

        public async Task<IndexedDid?> GetByHandle(string handle)
        {
            return await _dbContext.IndexedDids
                .FirstOrDefaultAsync(d => d.Handle == handle);
        }

        public async Task<List<IndexedDid>> GetActiveDids()
        {
            return await _dbContext.IndexedDids
                .Where(d => d.IsActive)
                .ToListAsync();
        }

        public async Task<IndexedDid> Upsert(IndexedDid indexedDid)
        {
            var existing = await GetByDid(indexedDid.Did);
            if (existing != null)
            {
                existing.Handle = indexedDid.Handle;
                existing.PdsHost = indexedDid.PdsHost;
                existing.SigningKey = indexedDid.SigningKey;
                existing.IsActive = indexedDid.IsActive;
                existing.LastIndexedAt = indexedDid.LastIndexedAt;
                existing.LastSeenAt = indexedDid.LastSeenAt;
                _dbContext.Entry(existing).State = EntityState.Modified;
            }
            else
            {
                await _dbContext.IndexedDids.AddAsync(indexedDid);
            }
            await _dbContext.SaveChangesAsync();
            return existing ?? indexedDid;
        }

        public async Task Delete(string did)
        {
            var entity = await GetByDid(did);
            if (entity != null)
            {
                _dbContext.IndexedDids.Remove(entity);
                await _dbContext.SaveChangesAsync();
            }
        }

        public async Task<bool> Exists(string did)
        {
            return await _dbContext.IndexedDids.AnyAsync(d => d.Did == did);
        }
    }
}
