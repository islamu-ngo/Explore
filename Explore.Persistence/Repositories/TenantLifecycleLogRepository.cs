// ABOUTME: Repository implementation for TenantLifecycleLog audit entity.
// ABOUTME: Provides query methods for tenant lifecycle transition history, ordered by most recent first.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class TenantLifecycleLogRepository : GenericRepository<TenantLifecycleLog, Guid>, ITenantLifecycleLogRepository
{
    private readonly ExploreDbContext _dbContext;

    public TenantLifecycleLogRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<TenantLifecycleLog>> GetByTenantIdAsync(
        Guid tenantId,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.TenantLifecycleLogs
            .AsNoTracking()
            .Include(l => l.OldStatus)
            .Include(l => l.NewStatus)
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.TransitionedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
