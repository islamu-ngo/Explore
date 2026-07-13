// ABOUTME: EF Core repository for webhook endpoints and event type subscription resolution.
// ABOUTME: Powers LocalProvider fanout queries with tenant predicates and provider-mode filtering.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class WebhookEndpointRepository : IWebhookEndpointRepository
{
    private readonly ExploreDbContext _dbContext;

    public WebhookEndpointRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookEndpoint> CreateWithSubscriptionsAsync(
        WebhookEndpoint endpoint,
        IReadOnlyCollection<WebhookEndpointSubscription> subscriptions,
        CancellationToken cancellationToken)
    {
        if (endpoint.Id == Guid.Empty)
        {
            endpoint.Id = Guid.CreateVersion7();
        }

        foreach (var subscription in subscriptions)
        {
            if (subscription.Id == Guid.Empty)
            {
                subscription.Id = Guid.CreateVersion7();
            }

            subscription.TenantId = endpoint.TenantId;
            subscription.EndpointId = endpoint.Id;
        }

        await _dbContext.WebhookEndpoints.AddAsync(endpoint, cancellationToken);
        if (subscriptions.Count > 0)
        {
            await _dbContext.WebhookEndpointSubscriptions.AddRangeAsync(subscriptions, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return endpoint;
    }

    public async Task<IReadOnlyList<WebhookEndpoint>> ListByTenantAsync(
        Guid tenantId,
        Guid? consumerId,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Include(e => e.Consumer)
            .Include(e => e.Subscriptions)
            .ThenInclude(e => e.EventType)
            .Where(e => e.TenantId == tenantId && e.Status != WebhookEndpointStatus.Archived);

        if (consumerId is not null)
        {
            query = query.Where(e => e.ConsumerId == consumerId.Value);
        }

        return await query
            .OrderBy(e => e.Consumer != null ? e.Consumer.Name : string.Empty)
            .ThenBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<WebhookEndpoint?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Include(e => e.Consumer)
            .Include(e => e.Subscriptions)
            .ThenInclude(e => e.EventType)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == endpointId, cancellationToken);
    }

    public async Task<WebhookEndpoint?> GetByTenantAndIdForUpdateAsync(
        Guid tenantId,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == endpointId, cancellationToken);
    }

    public async Task<WebhookEndpoint?> GetByTenantConsumerAndUrlAsync(
        Guid tenantId,
        Guid consumerId,
        string url,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId
                && e.ConsumerId == consumerId
                && e.Url == url
                && e.Status != WebhookEndpointStatus.Archived, cancellationToken);
    }

    public async Task<WebhookEndpoint> UpdateWithSubscriptionsAsync(
        WebhookEndpoint endpoint,
        IReadOnlyCollection<WebhookEndpointSubscription> subscriptions,
        CancellationToken cancellationToken)
    {
        var existingSubscriptions = await _dbContext.WebhookEndpointSubscriptions
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(subscription => subscription.TenantId == endpoint.TenantId && subscription.EndpointId == endpoint.Id)
            .ToListAsync(cancellationToken);

        _dbContext.WebhookEndpointSubscriptions.RemoveRange(existingSubscriptions);

        foreach (var subscription in subscriptions)
        {
            if (subscription.Id == Guid.Empty)
            {
                subscription.Id = Guid.CreateVersion7();
            }

            subscription.TenantId = endpoint.TenantId;
            subscription.EndpointId = endpoint.Id;
        }

        if (subscriptions.Count > 0)
        {
            await _dbContext.WebhookEndpointSubscriptions.AddRangeAsync(subscriptions, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return endpoint;
    }

    public async Task<WebhookEndpoint> UpdateAsync(WebhookEndpoint endpoint, CancellationToken cancellationToken)
    {
        if (_dbContext.Entry(endpoint).State == EntityState.Detached)
        {
            _dbContext.WebhookEndpoints.Update(endpoint);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return endpoint;
    }

    public async Task ArchiveAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime archivedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId && e.Id == endpointId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookEndpointStatus.Archived)
                .SetProperty(e => e.DeliveryStateVersion, e => e.DeliveryStateVersion + 1)
                .SetProperty(e => e.UpdatedAt, archivedAt), cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookEndpoint>> GetActiveSubscribedEndpointsAsync(
        Guid tenantId,
        string eventTypeName,
        WebhookProviderMode providerMode,
        CancellationToken cancellationToken)
    {
        var includeComposite = providerMode is WebhookProviderMode.Local or WebhookProviderMode.Svix;

        return await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Include(e => e.Consumer)
            .Include(e => e.Subscriptions)
            .ThenInclude(e => e.EventType)
            .Where(e => e.TenantId == tenantId
                && e.Status == WebhookEndpointStatus.Active
                && e.Consumer != null
                && (e.Consumer.ProviderMode == providerMode
                    || (includeComposite && e.Consumer.ProviderMode == WebhookProviderMode.Composite))
                && e.Consumer.Status == WebhookConsumerStatus.Active
                && e.Subscriptions.Any(subscription => subscription.TenantId == tenantId
                    && subscription.IsEnabled
                    && subscription.EventType != null
                    && subscription.EventType.Name == eventTypeName
                    && subscription.EventType.IsEnabled))
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task DisableAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime disabledAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId && e.Id == endpointId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookEndpointStatus.Disabled)
                .SetProperty(e => e.LastFailureAt, disabledAt)
                .SetProperty(e => e.DeliveryStateVersion, e => e.DeliveryStateVersion + 1)
                .SetProperty(e => e.UpdatedAt, disabledAt), cancellationToken);
    }

    public async Task MarkSuccessAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime succeededAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId && e.Id == endpointId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.LastSuccessAt, succeededAt)
                .SetProperty(
                    e => e.ConsecutiveFailureCount,
                    e => e.Status == WebhookEndpointStatus.Active ? 0 : e.ConsecutiveFailureCount)
                .SetProperty(
                    e => e.DeliveryStateVersion,
                    e => e.Status == WebhookEndpointStatus.Active
                        ? e.DeliveryStateVersion + 1
                        : e.DeliveryStateVersion)
                .SetProperty(e => e.UpdatedAt, succeededAt), cancellationToken);
    }

    public async Task<WebhookEndpointFailureState> RecordFailureAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime failedAt,
        string failureCategory,
        int autoPauseThreshold,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(autoPauseThreshold, 1);
        var normalizedFailureCategory = Truncate(failureCategory, 100);

        await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == endpointId
                && e.Status == WebhookEndpointStatus.Active)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.LastFailureAt, failedAt)
                .SetProperty(e => e.ConsecutiveFailureCount, e => e.ConsecutiveFailureCount + 1)
                .SetProperty(
                    e => e.Status,
                    e => e.ConsecutiveFailureCount + 1 >= autoPauseThreshold
                        ? WebhookEndpointStatus.AutoPaused
                        : e.Status)
                .SetProperty(
                    e => e.CircuitOpenedAt,
                    e => e.ConsecutiveFailureCount + 1 >= autoPauseThreshold
                        ? failedAt
                        : e.CircuitOpenedAt)
                .SetProperty(
                    e => e.AutoPausedAt,
                    e => e.ConsecutiveFailureCount + 1 >= autoPauseThreshold
                        ? failedAt
                        : e.AutoPausedAt)
                .SetProperty(
                    e => e.AutoPauseReason,
                    e => e.ConsecutiveFailureCount + 1 >= autoPauseThreshold
                        ? normalizedFailureCategory
                        : e.AutoPauseReason)
                .SetProperty(e => e.DeliveryStateVersion, e => e.DeliveryStateVersion + 1)
                .SetProperty(e => e.UpdatedAt, failedAt), cancellationToken);

        var state = await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.Id == endpointId)
            .Select(e => new WebhookEndpointFailureState(
                e.ConsecutiveFailureCount,
                e.Status == WebhookEndpointStatus.AutoPaused))
            .SingleOrDefaultAsync(cancellationToken);

        return state ?? new WebhookEndpointFailureState(0, false);
    }

    public async Task<bool> TryResumeAsync(
        Guid tenantId,
        Guid endpointId,
        DateTime resumedAt,
        Guid actorUserId,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.WebhookEndpoints
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == endpointId
                && e.Status == WebhookEndpointStatus.AutoPaused)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookEndpointStatus.Active)
                .SetProperty(e => e.ConsecutiveFailureCount, 0)
                .SetProperty(e => e.CircuitOpenedAt, (DateTime?)null)
                .SetProperty(e => e.AutoPausedAt, (DateTime?)null)
                .SetProperty(e => e.AutoPauseReason, (string?)null)
                .SetProperty(e => e.LastResumedAt, resumedAt)
                .SetProperty(e => e.LastResumedBy, actorUserId)
                .SetProperty(e => e.UpdatedAt, resumedAt)
                .SetProperty(e => e.UpdatedBy, actorUserId)
                .SetProperty(e => e.DeliveryStateVersion, e => e.DeliveryStateVersion + 1),
                cancellationToken);

        return updated == 1;
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
