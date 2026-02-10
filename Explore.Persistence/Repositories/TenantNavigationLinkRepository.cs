// ABOUTME: Repository implementation for TenantNavigationLink entity providing data access
// for tenant-scoped custom navigation links.

namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

public class TenantNavigationLinkRepository : GenericRepository<TenantNavigationLink, Guid>, ITenantNavigationLinkRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantNavigationLinkRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TenantNavigationLink>> GetByTenantIdOrderedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantNavigationLinks
            .AsNoTracking()
            .Where(l => l.TenantId == tenantId && l.IsActive)
            .OrderBy(l => l.Order)
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantNavigationLink?> GetByIdAndTenantAsync(Guid id, Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantNavigationLinks
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, cancellationToken);
    }

    public async Task<int> GetMaxOrderByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var maxOrder = await _dbContext.TenantNavigationLinks
            .Where(l => l.TenantId == tenantId)
            .MaxAsync(l => (int?)l.Order, cancellationToken);

        return maxOrder ?? 0;
    }

    public async Task<int> DeleteByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantNavigationLinks
            .Where(l => l.TenantId == tenantId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
