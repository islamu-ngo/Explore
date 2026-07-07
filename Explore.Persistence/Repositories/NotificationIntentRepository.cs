// ABOUTME: Repository for normalized notification intent, delivery, and external delegation rows.
// ABOUTME: Uses exact tenant predicates for worker-safe lookup without leaking IQueryable.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.Extensions;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class NotificationIntentRepository : GenericRepository<NotificationIntent, Guid>, INotificationIntentRepository
{
    private readonly ExploreDbContext _dbContext;

    public NotificationIntentRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<NotificationIntent> CreateIntentAsync(NotificationIntent intent, CancellationToken cancellationToken = default)
    {
        _dbContext.NotificationIntents.Add(intent);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return intent;
    }

    public async Task<NotificationIntent?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid intentId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationIntents
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .FirstOrDefaultAsync(intent => intent.TenantId == tenantId && intent.Id == intentId, cancellationToken);
    }

    public async Task<bool> ExistsByDeduplicationKeyAsync(
        Guid tenantId,
        string deduplicationKey,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.NotificationIntents
            .IgnoreTenantFilter(TenantFilterBypassReasons.TenantScopedRepositoryExactTenantPredicate)
            .AsNoTracking()
            .AnyAsync(intent => intent.TenantId == tenantId && intent.DeduplicationKey == deduplicationKey, cancellationToken);
    }

    public async Task<NotificationDelivery> AddDeliveryAsync(
        NotificationDelivery delivery,
        CancellationToken cancellationToken = default)
    {
        _dbContext.NotificationDeliveries.Add(delivery);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return delivery;
    }

    public async Task<NotificationExternalDelegation> AddExternalDelegationAsync(
        NotificationExternalDelegation delegation,
        CancellationToken cancellationToken = default)
    {
        _dbContext.NotificationExternalDelegations.Add(delegation);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return delegation;
    }
}
