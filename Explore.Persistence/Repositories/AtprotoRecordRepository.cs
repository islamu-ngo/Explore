using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories
{
    public class AtprotoRecordRepository : GenericRepository<AtprotoRecord, Guid>, IAtprotoRecordRepository
    {
        private readonly ExploreDbContext _dbContext;

        public AtprotoRecordRepository(ExploreDbContext dbContext) : base(dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<AtprotoRecord?> GetByUri(string uri)
        {
            return await _dbContext.AtprotoRecords
                .FirstOrDefaultAsync(r => r.Uri == uri);
        }

        public async Task<AtprotoRecord?> GetByDidAndCollection(string did, string collection, string recordKey)
        {
            return await _dbContext.AtprotoRecords
                .FirstOrDefaultAsync(r => r.Did == did && r.Collection == collection && r.RecordKey == recordKey);
        }

        public async Task<List<AtprotoRecord>> GetByDid(string did)
        {
            return await _dbContext.AtprotoRecords
                .Where(r => r.Did == did)
                .ToListAsync();
        }
    }
}
