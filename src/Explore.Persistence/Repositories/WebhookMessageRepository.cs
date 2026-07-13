// ABOUTME: EF Core repository for canonical outgoing webhook messages and provider publish state.
// ABOUTME: Supports tenant status reads, provider queue transitions, and privacy retention cleanup.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class WebhookMessageRepository(ExploreDbContext dbContext) : IWebhookMessageRepository
{
    public async Task<WebhookMessage> CreateAsync(WebhookMessage message, CancellationToken cancellationToken)
    {
        if (message.Id == Guid.Empty)
        {
            throw new ArgumentException("Webhook messages must be created through the domain factory.", nameof(message));
        }

        await dbContext.WebhookMessages.AddAsync(message, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return message;
    }

    public async Task<WebhookMessage?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return await dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == messageId, cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookMessage>> ListByTenantAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId)
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.Id)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> ClearExpiredPayloadsAsync(
        DateTime now,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var messages = await dbContext.WebhookMessages
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => EF.Property<byte[]?>(e, "_payloadBytes") != null && e.PayloadRetentionUntil <= now)
            .OrderBy(e => e.PayloadRetentionUntil)
            .ThenBy(e => e.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in messages)
        {
            message.ClearPayload(now);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return messages.Count;
    }
}
