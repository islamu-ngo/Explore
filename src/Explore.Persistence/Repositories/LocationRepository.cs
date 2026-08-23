// ABOUTME: EF Core repository for location detail, listing, and PII erasure operations.
// ABOUTME: Preserves entity-returning persistence boundaries and forwards cancellation into custom queries.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
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

    public async Task<IReadOnlyList<Guid>> GetExistingTenantLocationIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> candidateLocationIds,
        CancellationToken cancellationToken = default)
    {
        var boundedLocationIds = candidateLocationIds
            .Where(locationId => locationId != Guid.Empty)
            .Distinct()
            .ToArray();
        if (boundedLocationIds.Length == 0)
            return [];

        return await _dbContext.Locations
            .AsNoTracking()
            .IgnoreAutoIncludes()
            .Where(location => location.TenantId == tenantId && boundedLocationIds.Contains(location.Id))
            .Select(location => location.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Location>> GetOwnedPrivateHomesForGlobalErasureAsync(
        Guid ownerUserId,
        CancellationToken cancellationToken = default)
    {
        if (ownerUserId == Guid.Empty)
        {
            throw new ArgumentException("Owner user id is required.", nameof(ownerUserId));
        }

        return await _dbContext.Locations
            .IgnoreTenantFilter(TenantFilterBypassReasons.UserPrivacyErasure)
            .Where(location => location.OwnerUserId == ownerUserId
                && location.LocationKindId == (int)LocationKindEnum.PrivateHome)
            .OrderBy(location => location.TenantId)
            .ThenBy(location => location.Id)
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
