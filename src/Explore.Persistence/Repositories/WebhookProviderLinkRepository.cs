// ABOUTME: EF Core repository for Svix and future external webhook provider object links.
// ABOUTME: Keeps provider ids queryable for sync/idempotency while ISLAMU retains canonical state.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class WebhookProviderLinkRepository : IWebhookProviderLinkRepository
{
    private readonly ExploreDbContext _dbContext;

    public WebhookProviderLinkRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookProviderLink> CreateAsync(WebhookProviderLink link, CancellationToken cancellationToken)
    {
        if (link.Id == Guid.Empty)
        {
            link.Id = Guid.CreateVersion7();
        }

        await _dbContext.WebhookProviderLinks.AddAsync(link, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return link;
    }

    public async Task<WebhookProviderLink?> GetByExternalMessageIdAsync(
        Guid tenantId,
        WebhookExternalProvider provider,
        string externalMessageId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookProviderLinks
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId
                    && e.Provider == provider
                    && e.ExternalMessageId == externalMessageId,
                cancellationToken);
    }

    public async Task<WebhookProviderLink?> GetByTenantMessageAndProviderAsync(
        Guid tenantId,
        WebhookExternalProvider provider,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookProviderLinks
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(
                e => e.TenantId == tenantId
                    && e.Provider == provider
                    && e.MessageId == messageId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookProviderLink>> GetPendingByProviderAsync(
        WebhookExternalProvider provider,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookProviderLinks
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.Provider == provider && e.SyncState == WebhookProviderLinkSyncState.Pending)
            .OrderBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }
}
