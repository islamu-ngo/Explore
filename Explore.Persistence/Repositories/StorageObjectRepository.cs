// ABOUTME: EF Core repository for storage objects with detail projections for legacy admin surfaces.
// ABOUTME: Returns domain entities only; DTO mapping remains in application handlers.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class StorageObjectRepository : GenericRepository<StorageObject, Guid>, IStorageObjectRepository
{
    private readonly ExploreDbContext _dbContext;

    public StorageObjectRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<StorageObject>> GetFilesWithDetails()
    {
        return await _dbContext.StorageObjects
            .AsNoTracking()
            .Include(f => f.FileType)
            .Include(f => f.Tenant)
            .Include(f => f.Actor)
                .ThenInclude(a => a!.Pii)
            .ToListAsync();
    }

    public async Task<StorageObject?> GetFileWithDetails(Guid id)
    {
        return await _dbContext.StorageObjects
            .AsNoTracking()
            .Include(f => f.FileType)
            .Include(f => f.Tenant)
            .Include(f => f.Actor)
                .ThenInclude(a => a!.Pii)
            .FirstOrDefaultAsync(f => f.Id == id);
    }

    public async Task<(List<StorageObject> Items, int TotalCount)> GetFilesWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.StorageObjects
            .AsNoTracking()
            .Include(f => f.FileType)
            .Include(f => f.Actor)
                .ThenInclude(a => a!.Pii)
            .OrderByDescending(f => f.Id);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<IReadOnlyList<StorageObject>> GetAllForInstanceStorageReportAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.StorageObjects
            .AsNoTracking()
            .IgnoreTenantFilter(TenantFilterBypassReasons.InstanceStorageAdministration)
            .ToListAsync(cancellationToken);
    }
}
