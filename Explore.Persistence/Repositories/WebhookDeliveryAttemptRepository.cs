// ABOUTME: EF Core repository for LocalProvider webhook HTTP delivery attempts.
// ABOUTME: Exposes tenant-scoped audit reads while workers can append durable attempt rows.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class WebhookDeliveryAttemptRepository : IWebhookDeliveryAttemptRepository
{
    private const int MaxFailureCategoryLength = 100;
    private const int MaxResponseBodyPreviewLength = 4096;

    private readonly ExploreDbContext _dbContext;

    public WebhookDeliveryAttemptRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<WebhookDeliveryAttempt> CreateAsync(
        WebhookDeliveryAttempt attempt,
        CancellationToken cancellationToken)
    {
        if (attempt.Id == Guid.Empty)
        {
            attempt.Id = Guid.CreateVersion7();
        }

        await _dbContext.WebhookDeliveryAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return attempt;
    }

    public async Task<IReadOnlyList<WebhookDeliveryAttempt>> CreateManyAsync(
        IReadOnlyCollection<WebhookDeliveryAttempt> attempts,
        CancellationToken cancellationToken)
    {
        if (attempts.Count == 0)
        {
            return [];
        }

        foreach (var attempt in attempts)
        {
            if (attempt.Id == Guid.Empty)
            {
                attempt.Id = Guid.CreateVersion7();
            }
        }

        await _dbContext.WebhookDeliveryAttempts.AddRangeAsync(attempts, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return attempts.ToList();
    }

    public async Task<IReadOnlyList<WebhookDeliveryAttempt>> GetByMessageAsync(
        Guid tenantId,
        Guid messageId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.MessageId == messageId)
            .OrderBy(e => e.EndpointId)
            .ThenBy(e => e.AttemptNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookDeliveryAttempt>> ListByTenantAsync(
        Guid tenantId,
        Guid? messageId,
        Guid? endpointId,
        int limit,
        CancellationToken cancellationToken)
    {
        var query = _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Include(e => e.Endpoint)
            .Include(e => e.Message)
            .Where(e => e.TenantId == tenantId);

        if (messageId.HasValue)
        {
            query = query.Where(e => e.MessageId == messageId.Value);
        }

        if (endpointId.HasValue)
        {
            query = query.Where(e => e.EndpointId == endpointId.Value);
        }

        return await query
            .OrderByDescending(e => e.CreatedAt)
            .ThenByDescending(e => e.ScheduledAt)
            .ThenByDescending(e => e.AttemptNumber)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetNextAttemptNumberAsync(
        Guid tenantId,
        Guid messageId,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        var currentMaximum = await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId
                && e.MessageId == messageId
                && e.EndpointId == endpointId)
            .Select(e => (int?)e.AttemptNumber)
            .MaxAsync(cancellationToken);

        return (currentMaximum ?? 0) + 1;
    }

    public Task<bool> HasActiveAttemptForEndpointAsync(
        Guid tenantId,
        Guid messageId,
        Guid endpointId,
        CancellationToken cancellationToken)
    {
        return _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .AnyAsync(e => e.TenantId == tenantId
                && e.MessageId == messageId
                && e.EndpointId == endpointId
                && (e.Status == WebhookDeliveryAttemptStatus.Scheduled
                    || e.Status == WebhookDeliveryAttemptStatus.Sending), cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookDeliveryAttempt>> GetDueScheduledAsync(
        int batchSize,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .Include(e => e.Endpoint)
            .Include(e => e.Message)
            .Where(e => e.Status == WebhookDeliveryAttemptStatus.Scheduled && e.ScheduledAt <= now)
            .OrderBy(e => e.ScheduledAt)
            .ThenBy(e => e.CreatedAt)
            .ThenBy(e => e.Id)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDueScheduledAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        return _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.Status == WebhookDeliveryAttemptStatus.Scheduled
                && e.ScheduledAt <= now, cancellationToken);
    }

    public Task<int> CountStaleSendingAsync(
        DateTime processingStartedBefore,
        CancellationToken cancellationToken)
    {
        return _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.Status == WebhookDeliveryAttemptStatus.Sending
                && e.ProcessingStartedAt != null
                && e.ProcessingStartedAt < processingStartedBefore, cancellationToken);
    }

    public async Task<WebhookDeliveryAttempt?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid attemptId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookTenantOperation)
            .AsNoTracking()
            .Include(e => e.Endpoint)
            .Include(e => e.Message)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == attemptId, cancellationToken);
    }

    public async Task<bool> TryMarkAsSendingAsync(
        Guid tenantId,
        Guid attemptId,
        Guid processingLeaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => e.TenantId == tenantId
                && e.Id == attemptId
                && e.Status == WebhookDeliveryAttemptStatus.Scheduled)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookDeliveryAttemptStatus.Sending)
                .SetProperty(e => e.ProcessingLeaseToken, processingLeaseToken)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)startedAt)
                .SetProperty(e => e.SentAt, (DateTime?)startedAt)
                .SetProperty(e => e.UpdatedAt, startedAt), cancellationToken);

        return updated == 1;
    }

    public async Task MarkSucceededAsync(
        Guid tenantId,
        Guid attemptId,
        Guid processingLeaseToken,
        DateTime sentAt,
        DateTime completedAt,
        int httpStatusCode,
        int durationMs,
        string? responseBodyPreview,
        CancellationToken cancellationToken)
    {
        await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => e.TenantId == tenantId
                && e.Id == attemptId
                && e.Status == WebhookDeliveryAttemptStatus.Sending
                && e.ProcessingLeaseToken == processingLeaseToken)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookDeliveryAttemptStatus.Succeeded)
                .SetProperty(e => e.SentAt, (DateTime?)sentAt)
                .SetProperty(e => e.CompletedAt, (DateTime?)completedAt)
                .SetProperty(e => e.HttpStatusCode, (int?)httpStatusCode)
                .SetProperty(e => e.DurationMs, (int?)durationMs)
                .SetProperty(e => e.ResponseBodyPreview, Truncate(responseBodyPreview, MaxResponseBodyPreviewLength))
                .SetProperty(e => e.NextRetryAt, (DateTime?)null)
                .SetProperty(e => e.FailureCategory, (string?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.UpdatedAt, completedAt), cancellationToken);
    }

    public async Task MarkFailedAsync(
        Guid tenantId,
        Guid attemptId,
        Guid processingLeaseToken,
        WebhookDeliveryAttemptStatus status,
        DateTime completedAt,
        string failureCategory,
        int? httpStatusCode,
        int durationMs,
        string? responseBodyPreview,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => e.TenantId == tenantId
                && e.Id == attemptId
                && e.Status == WebhookDeliveryAttemptStatus.Sending
                && e.ProcessingLeaseToken == processingLeaseToken)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, status)
                .SetProperty(e => e.CompletedAt, (DateTime?)completedAt)
                .SetProperty(e => e.HttpStatusCode, httpStatusCode)
                .SetProperty(e => e.DurationMs, (int?)durationMs)
                .SetProperty(e => e.FailureCategory, Truncate(failureCategory, MaxFailureCategoryLength))
                .SetProperty(e => e.ResponseBodyPreview, Truncate(responseBodyPreview, MaxResponseBodyPreviewLength))
                .SetProperty(e => e.NextRetryAt, nextRetryAt)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.UpdatedAt, completedAt), cancellationToken);
    }

    public async Task<int> ResetStaleSendingAsync(
        DateTime processingStartedBefore,
        DateTime recoveredAt,
        string failureCategory,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var attemptIds = await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.Status == WebhookDeliveryAttemptStatus.Sending
                && e.ProcessingStartedAt != null
                && e.ProcessingStartedAt < processingStartedBefore)
            .OrderBy(e => e.ProcessingStartedAt)
            .ThenBy(e => e.Id)
            .Take(batchSize)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (attemptIds.Count == 0)
        {
            return 0;
        }

        return await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => attemptIds.Contains(e.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, WebhookDeliveryAttemptStatus.Scheduled)
                .SetProperty(e => e.ScheduledAt, recoveredAt)
                .SetProperty(e => e.SentAt, (DateTime?)null)
                .SetProperty(e => e.CompletedAt, (DateTime?)null)
                .SetProperty(e => e.HttpStatusCode, (int?)null)
                .SetProperty(e => e.DurationMs, (int?)null)
                .SetProperty(e => e.FailureCategory, Truncate(failureCategory, MaxFailureCategoryLength))
                .SetProperty(e => e.ResponseBodyPreview, (string?)null)
                .SetProperty(e => e.NextRetryAt, recoveredAt)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.UpdatedAt, recoveredAt), cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
