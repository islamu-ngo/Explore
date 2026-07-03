// ABOUTME: EF Core repository for tenant-scoped webhook consumers and external provider app ids.
// ABOUTME: Uses explicit tenant predicates for provider-neutral webhook management queries.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class WebhookConsumerRepository : IWebhookConsumerRepository
{
    private readonly ExploreDbContext _dbContext;

    public WebhookConsumerRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookConsumer> CreateAsync(WebhookConsumer consumer, CancellationToken cancellationToken)
    {
        if (consumer.Id == Guid.Empty)
        {
            consumer.Id = Guid.CreateVersion7();
        }

        await _dbContext.WebhookConsumers.AddAsync(consumer, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return consumer;
    }

    public async Task<WebhookConsumer?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid consumerId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookConsumers
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == consumerId, cancellationToken);
    }

    public async Task<WebhookConsumer?> GetByTenantAndNameAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookConsumers
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId && e.Name == name,
                cancellationToken);
    }

    public async Task<WebhookConsumer?> GetByExternalProviderAppIdAsync(
        Guid tenantId,
        string externalProviderAppId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookConsumers
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId && e.ExternalProviderAppId == externalProviderAppId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookConsumer>> ListByTenantAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookConsumers
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .OrderBy(e => e.Name)
            .ThenBy(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
