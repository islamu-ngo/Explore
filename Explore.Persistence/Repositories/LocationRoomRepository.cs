// ABOUTME: EF implementation of ILocationRoomRepository - delegates CRUD to GenericRepository and adds location-scoped queries.
// ABOUTME: Reads are AsNoTracking for query handler use.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class LocationRoomRepository : GenericRepository<LocationRoom, Guid>, ILocationRoomRepository
{
    private readonly ExploreDbContext _dbContext;

    public LocationRoomRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<LocationRoom>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken)
    {
        return await _dbContext.LocationRooms
            .AsNoTracking()
            .Where(r => r.LocationId == locationId)
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }
}
