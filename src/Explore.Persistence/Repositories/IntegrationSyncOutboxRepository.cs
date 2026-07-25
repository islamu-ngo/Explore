// ABOUTME: EF Core repository for durable native integration sync outbox rows.
// ABOUTME: Uses bounded tenant-filter bypass queries and affected-row claims for worker-safe at-least-once processing.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public sealed class IntegrationSyncOutboxRepository(ExploreDbContext dbContext) : IIntegrationSyncOutboxRepository
{
    public async Task<IntegrationSyncOutbox> Create(IntegrationSyncOutbox outbox, CancellationToken cancellationToken)
    {
        await dbContext.IntegrationSyncOutbox.AddAsync(outbox, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return outbox;
    }

    public async Task<IReadOnlyList<IntegrationSyncOutbox>> GetPendingBatch(
        int batchSize,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await dbContext.IntegrationSyncOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.IntegrationSyncWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(o => (o.Status == IntegrationSyncStatus.Pending || o.Status == IntegrationSyncStatus.RetryScheduled)
                && (o.NextAttemptAt == null || o.NextAttemptAt <= now))
            .OrderBy(o => o.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        var updated = await dbContext.IntegrationSyncOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.IntegrationSyncWorkerCrossTenantQueue)
            .Where(o => o.Id == id
                && (o.Status == IntegrationSyncStatus.Pending || o.Status == IntegrationSyncStatus.RetryScheduled)
                && (o.NextAttemptAt == null || o.NextAttemptAt <= startedAt))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, IntegrationSyncStatus.Processing)
                .SetProperty(o => o.ProcessingStartedAt, startedAt)
                .SetProperty(o => o.ProcessingLeaseToken, leaseToken)
                .SetProperty(o => o.AttemptCount, o => o.AttemptCount + 1)
                .SetProperty(o => o.UpdatedAt, startedAt),
                cancellationToken);

        return updated > 0;
    }

    public Task<IntegrationSyncOutbox?> GetActiveClaimAsync(
        Guid tenantId,
        Guid id,
        Guid leaseToken,
        CancellationToken cancellationToken)
    {
        return dbContext.IntegrationSyncOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.IntegrationSyncWorkerCrossTenantQueue)
            .AsNoTracking()
            .FirstOrDefaultAsync(outbox =>
                outbox.TenantId == tenantId &&
                outbox.Id == id &&
                outbox.Status == IntegrationSyncStatus.Processing &&
                outbox.ProcessingLeaseToken == leaseToken &&
                outbox.UserId != null,
                cancellationToken);
    }

    public async Task MarkAsCompleted(Guid id, DateTime completedAt, CancellationToken cancellationToken)
    {
        await dbContext.IntegrationSyncOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.IntegrationSyncWorkerCrossTenantQueue)
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.Status, IntegrationSyncStatus.Completed)
                .SetProperty(o => o.CompletedAt, completedAt)
                .SetProperty(o => o.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(o => o.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(o => o.LastError, (string?)null)
                .SetProperty(o => o.UpdatedAt, completedAt),
                cancellationToken);
    }

    public async Task MarkAsFailed(
        Guid id,
        string errorMessage,
        bool isRetryable,
        TimeSpan retryDelay,
        int maxAttempts,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.IntegrationSyncOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.IntegrationSyncWorkerCrossTenantQueue)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);
        if (entry is null)
        {
            return;
        }

        var exhausted = !isRetryable || entry.AttemptCount >= Math.Min(entry.MaxAttempts, maxAttempts);
        entry.Status = exhausted ? IntegrationSyncStatus.DeadLettered : IntegrationSyncStatus.RetryScheduled;
        entry.LastFailureAt = failedAt;
        entry.LastError = errorMessage.Length > 2000 ? errorMessage[..2000] : errorMessage;
        entry.NextAttemptAt = exhausted ? null : failedAt.Add(retryDelay);
        entry.DeadLetteredAt = exhausted ? failedAt : null;
        entry.ProcessingStartedAt = null;
        entry.ProcessingLeaseToken = null;
        entry.UpdatedAt = failedAt;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
