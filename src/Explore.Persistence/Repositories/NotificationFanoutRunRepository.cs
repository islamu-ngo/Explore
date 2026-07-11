// ABOUTME: EF Core repository for durable notification fanout run state.
// ABOUTME: Supports idempotent source lookup and background worker polling for internal fanout.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class NotificationFanoutRunRepository : GenericRepository<NotificationFanoutRun, Guid>, INotificationFanoutRunRepository
{
    private readonly ExploreDbContext _dbContext;

    public NotificationFanoutRunRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationFanoutRun?> GetBySourceAsync(
        Guid tenantId,
        string fanoutKind,
        int notificationEntityTypeId,
        Guid entityId,
        Guid sourceActorId,
        bool trackChanges = false,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.NotificationFanoutRuns
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .Include(run => run.NotificationEntityType)
            .Include(run => run.SourceActor)
                .ThenInclude(actor => actor.Pii)
            .Where(run => run.TenantId == tenantId
                && run.FanoutKind == fanoutKind
                && run.NotificationEntityTypeId == notificationEntityTypeId
                && run.EntityId == entityId
                && run.SourceActorId == sourceActorId);

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<NotificationFanoutRun>> GetPendingBatchAsync(
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (pageSize <= 0)
        {
            return [];
        }

        return await _dbContext.NotificationFanoutRuns
            .IgnoreTenantFilter(TenantFilterBypassReasons.NotificationFanoutWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(run => run.Status == "pending")
            .OrderBy(run => run.CreatedAt)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }
}
