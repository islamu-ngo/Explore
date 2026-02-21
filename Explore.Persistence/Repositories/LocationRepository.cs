using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class LocationRepository : GenericRepository<Location, Guid>, ILocationRepository
{
    private readonly ExploreDbContext _dbContext;

    public LocationRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<Location?> GetById(Guid id)
    {
        return await _dbContext.Locations
            .Include(l => l.Pii)
            .FirstOrDefaultAsync(l => l.Id == id);
    }

    public async Task<List<Location>> GetLocationsByTenant(Guid tenantId)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .Where(l => l.TenantId == tenantId)
            .ToListAsync();
    }

    public async Task<List<Location>> GetLocationsByCity(string city)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .Where(l => l.City == city)
            .ToListAsync();
    }

    public async Task<List<Location>> GetLocationsByCountry(string country)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .Where(l => l.Country == country)
            .ToListAsync();
    }

    public async Task<(List<Location> Items, int TotalCount)> GetLocationsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .OrderBy(l => l.FullName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }
}
