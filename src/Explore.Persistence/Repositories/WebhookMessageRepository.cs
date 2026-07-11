// ABOUTME: EF Core repository for canonical outgoing webhook messages and provider publish state.
// ABOUTME: Supports tenant status reads, provider queue transitions, and privacy retention cleanup.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class WebhookMessageRepository : IWebhookMessageRepository
{
    private const int MaxProviderMessageIdLength = 500;

    private readonly ExploreDbContext _dbContext;

    public WebhookMessageRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookMessage> CreateAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        if (message.Id == Guid.Empty)
        {
            message.Id = Guid.CreateVersion7();
        }

        await _dbContext.WebhookMessages.AddAsync(message, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<WebhookMessage?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == messageId, cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookMessage>> ListByTenantAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.PublishedAt ?? e.CreatedAt)
            .ThenByDescending(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task MarkProviderQueuedAsync(
        Guid tenantId,
        Guid messageId,
        string? providerMessageId,
        DateTime queuedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId && e.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookMessageStatus.Queued)
                .SetProperty(e => e.ProviderMessageId, Truncate(providerMessageId, MaxProviderMessageIdLength))
                .SetProperty(e => e.PublishedAt, queuedAt)
                .SetProperty(e => e.UpdatedAt, queuedAt), cancellationToken);
    }

    public async Task MarkProviderFailedAsync(
        Guid tenantId,
        Guid messageId,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .Where(e => e.TenantId == tenantId && e.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookMessageStatus.Failed)
                .SetProperty(e => e.UpdatedAt, failedAt), cancellationToken);
    }

    public async Task RefreshLocalDeliveryStatusAsync(
        Guid tenantId,
        Guid messageId,
        DateTime refreshedAt,
        CancellationToken cancellationToken)
    {
        var statuses = await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.MessageId == messageId)
            .Select(e => e.Status)
            .ToListAsync(cancellationToken);

        if (statuses.Count == 0)
        {
            return;
        }

        var hasActiveAttempt = statuses.Any(status => status is
            WebhookDeliveryAttemptStatus.Scheduled or
            WebhookDeliveryAttemptStatus.Sending);
        var hasSucceededAttempt = statuses.Any(status => status == WebhookDeliveryAttemptStatus.Succeeded);
        var messageStatus = hasActiveAttempt
            ? WebhookMessageStatus.Queued
            : statuses.All(status => status == WebhookDeliveryAttemptStatus.Succeeded)
                ? WebhookMessageStatus.Delivered
                : hasSucceededAttempt
                    ? WebhookMessageStatus.PartiallyFailed
                    : WebhookMessageStatus.Failed;

        await _dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => e.TenantId == tenantId && e.Id == messageId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, messageStatus)
                .SetProperty(e => e.UpdatedAt, refreshedAt), cancellationToken);
    }

    public async Task<int> ClearExpiredPayloadsAsync(
        DateTime now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var messageIds = await _dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.PayloadJson != null && e.PayloadRetentionUntil <= now)
            .OrderBy(e => e.PayloadRetentionUntil)
            .ThenBy(e => e.Id)
            .Take(batchSize)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (messageIds.Count == 0)
        {
            return 0;
        }

        return await _dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => messageIds.Contains(e.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.PayloadJson, (string?)null)
                .SetProperty(e => e.PayloadClearedAt, now)
                .SetProperty(e => e.UpdatedAt, now), cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
