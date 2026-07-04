// ABOUTME: EF Core repository for location detail, listing, and PII erasure operations.
// ABOUTME: Preserves entity-returning persistence boundaries and forwards cancellation into custom queries.

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

    public async Task<List<Location>> GetLocationsByTenant(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .Where(l => l.TenantId == tenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Location>> GetLocationsByCity(string city, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .Where(l => l.City == city)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Location>> GetLocationsByCountry(string country, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .Where(l => l.Country == country)
            .ToListAsync(cancellationToken);
    }

    public async Task<(List<Location> Items, int TotalCount)> GetLocationsWithDetailsPaged(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Locations
            .AsNoTracking()
            .Include(l => l.Pii)
            .OrderBy(l => l.FullName);

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<int> ForgetPiiAsync(Guid locationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.LocationPii
            .Where(p => p.LocationId == locationId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
