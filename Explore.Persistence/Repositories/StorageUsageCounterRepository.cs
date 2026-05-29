// ABOUTME: EF Core repository for tenant/provider storage usage counters.
// ABOUTME: Provides quota-counter lookup and creation while preserving tenant-scoped persistence rules.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class StorageUsageCounterRepository : GenericRepository<StorageUsageCounter, Guid>, IStorageUsageCounterRepository
{
    private readonly ExploreDbContext _dbContext;

    public StorageUsageCounterRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<StorageUsageCounter?> GetByTenantAndProviderAsync(
        Guid tenantId,
        string provider,
        CancellationToken cancellationToken)
    {
        return await _dbContext.StorageUsageCounters
            .AsNoTracking()
            .SingleOrDefaultAsync(counter =>
                    counter.TenantId == tenantId &&
                    counter.Provider == provider,
                cancellationToken);
    }

    public async Task<StorageUsageCounter> GetOrCreateAsync(
        Guid tenantId,
        string provider,
        CancellationToken cancellationToken)
    {
        var counter = await _dbContext.StorageUsageCounters
            .SingleOrDefaultAsync(existing =>
                    existing.TenantId == tenantId &&
                    existing.Provider == provider,
                cancellationToken);

        if (counter is not null)
        {
            return counter;
        }

        counter = new StorageUsageCounter
        {
            TenantId = tenantId,
            Provider = provider
        };

        await _dbContext.StorageUsageCounters.AddAsync(counter, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return counter;
    }
}
