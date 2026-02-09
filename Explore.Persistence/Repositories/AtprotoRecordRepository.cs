using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class AtprotoRecordRepository : GenericRepository<AtprotoRecord, Guid>, IAtprotoRecordRepository
{
    private readonly ExploreDbContext _dbContext;

    public AtprotoRecordRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<AtprotoRecord>> GetAllAtprotoRecords()
    {
        return await _dbContext.AtprotoRecords.AsNoTracking().ToListAsync();
    }

    public async Task<AtprotoRecord?> GetAtprotoRecordByUri(string uri)
    {
        return await _dbContext.AtprotoRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Uri == uri);
    }

    public async Task<List<AtprotoRecord>> GetAtprotoRecordsByDid(string did)
    {
        return await _dbContext.AtprotoRecords.AsNoTracking().Where(r => r.Did == did).ToListAsync();
    }

    public async Task<List<AtprotoRecord>> GetAtprotoRecordsByCollection(string collection)
    {
        return await _dbContext.AtprotoRecords.AsNoTracking().Where(r => r.Collection == collection).ToListAsync();
    }

    public async Task<bool> Exists(Guid id)
    {
        return await _dbContext.AtprotoRecords.AsNoTracking().AnyAsync(r => r.Id == id);
    }
}
