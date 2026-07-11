// ABOUTME: EF Core repository for tenant reads, counts, and atomic lifecycle transitions.
// ABOUTME: Uses no-tracking reads and status-qualified ExecuteUpdate compare-and-swap writes.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantRepository : GenericRepository<Tenant, Guid>, ITenantRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Tenant?> GetTenantBySlug(string slug)
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Slug == slug);
    }

    public async Task<int> GetActiveTenantCountAsync()
    {
        return await _dbContext.Tenants
            .AsNoTracking()
            .CountAsync(t => t.TenantStatus.IsActiveState);
    }

    public Task<Tenant?> GetByIdAsNoTrackingAsync(Guid id, CancellationToken cancellationToken = default) =>
        _dbContext.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(tenant => tenant.Id == id, cancellationToken);

    public async Task<bool> TryTransitionStatusAsync(
        Guid id,
        int expectedStatusId,
        int newStatusId,
        DateTime updatedAt,
        Guid updatedBy,
        CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.Tenants
            .Where(tenant => tenant.Id == id && tenant.TenantStatusId == expectedStatusId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(tenant => tenant.TenantStatusId, newStatusId)
                .SetProperty(tenant => tenant.UpdatedAt, updatedAt)
                .SetProperty(tenant => tenant.UpdatedBy, updatedBy), cancellationToken);

        return affectedRows == 1;
    }
}
