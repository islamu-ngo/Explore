// ABOUTME: EF Core repository for tenant-local user participation records.
// ABOUTME: Provides active-state checks used by tenant membership and admin authority paths.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantUserRepository : GenericRepository<TenantUser, Guid>, ITenantUserRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantUserRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<TenantUser?> GetByTenantAndUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantUsers
            .IgnoreTenantFilter()
            .Include(x => x.Profile)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.UserId == userId, cancellationToken);
    }

    public async Task<TenantUser?> GetByTenantAndActorAsync(Guid tenantId, Guid actorId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantUsers
            .IgnoreTenantFilter()
            .AsNoTracking()
            .Include(x => x.Profile)
            .FirstOrDefaultAsync(x => x.TenantId == tenantId && x.ActorId == actorId, cancellationToken);
    }

    public async Task<bool> IsActiveTenantUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantUsers
            .IgnoreTenantFilter()
            .AsNoTracking()
            .AnyAsync(x => x.TenantId == tenantId
                && x.UserId == userId
                && x.StatusId == (int)TenantUserStatusEnum.Active
                && !x.IsDeleted, cancellationToken);
    }
}
