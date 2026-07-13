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
                && (e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Scheduled
                    || e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Sending), cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetDueTenantIdsAsync(
        int tenantLimit,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Scheduled
                && e.ScheduledAt <= now
                && e.Endpoint != null
                && e.Endpoint.StatusId == (int)WebhookEndpointStatus.Active)
            .GroupBy(e => e.TenantId)
            .Select(group => new
            {
                TenantId = group.Key,
                OldestScheduledAt = group.Min(e => e.ScheduledAt)
            })
            .OrderBy(candidate => candidate.OldestScheduledAt)
            .ThenBy(candidate => candidate.TenantId)
            .Take(tenantLimit)
            .Select(candidate => candidate.TenantId)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WebhookDeliveryClaim>> ClaimDueAsync(
        WebhookDeliveryClaimRequest request,
        IReadOnlyDictionary<Guid, WebhookDeliveryClaimLimits> tenantLimits,
        CancellationToken cancellationToken)
    {
        if (request.BatchSize < 1
            || request.CandidateBatchSize < request.BatchSize
            || request.GlobalInFlightLimit < 1
            || request.LeaseDuration <= TimeSpan.Zero
            || request.TenantOrder.Count == 0
            || tenantLimits.Count == 0)
        {
            return [];
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        if (_dbContext.Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                "SELECT pg_advisory_xact_lock(hashtext({0}))",
                ["webhook-delivery-claim"],
                cancellationToken);
        }

        var activeLeaseQuery = _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Sending
                && (e.ProcessingLeaseExpiresAt == null || e.ProcessingLeaseExpiresAt > request.ClaimedAt));
        var currentGlobalInFlight = await activeLeaseQuery.CountAsync(cancellationToken);
        var remainingGlobalCapacity = Math.Min(
            request.BatchSize,
            request.GlobalInFlightLimit - currentGlobalInFlight);
        if (remainingGlobalCapacity <= 0)
        {
            return [];
        }

        var tenantOrder = request.TenantOrder
            .Where(tenantLimits.ContainsKey)
            .Distinct()
            .ToArray();
        if (tenantOrder.Length == 0)
        {
            return [];
        }

        var tenantInFlight = await activeLeaseQuery
            .Where(e => tenantOrder.Contains(e.TenantId))
            .GroupBy(e => e.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(row => row.TenantId, row => row.Count, cancellationToken);
        var endpointInFlight = (await activeLeaseQuery
                .Where(e => tenantOrder.Contains(e.TenantId))
                .GroupBy(e => new { e.TenantId, e.EndpointId })
                .Select(group => new { group.Key.TenantId, group.Key.EndpointId, Count = group.Count() })
                .ToListAsync(cancellationToken))
            .ToDictionary(row => (row.TenantId, row.EndpointId), row => row.Count);

        var perTenantCandidateLimit = Math.Max(1, request.CandidateBatchSize / tenantOrder.Length);
        var queues = new Dictionary<Guid, Queue<WebhookDeliveryAttempt>>(tenantOrder.Length);
        foreach (var tenantId in tenantOrder)
        {
            var query = _dbContext.WebhookDeliveryAttempts
                .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
                .Include(e => e.Endpoint)
                .Include(e => e.Message)
                .Where(e => e.TenantId == tenantId
                    && e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Scheduled
                    && e.ScheduledAt <= request.ClaimedAt
                    && e.Endpoint != null
                    && e.Endpoint.StatusId == (int)WebhookEndpointStatus.Active);

            if (request.AttemptId is { } attemptId)
            {
                query = query.Where(e => e.Id == attemptId);
            }

            var candidates = await query
                .OrderBy(e => e.ScheduledAt)
                .ThenBy(e => e.CreatedAt)
                .ThenBy(e => e.Id)
                .Take(perTenantCandidateLimit)
                .ToListAsync(cancellationToken);
            queues[tenantId] = new Queue<WebhookDeliveryAttempt>(candidates);
        }

        var selected = new List<WebhookDeliveryAttempt>(remainingGlobalCapacity);
        var selectedByTenant = new Dictionary<Guid, int>();
        while (selected.Count < remainingGlobalCapacity)
        {
            var added = false;
            foreach (var tenantId in tenantOrder)
            {
                var limits = tenantLimits[tenantId];
                var currentTenantInFlight = tenantInFlight.GetValueOrDefault(tenantId);
                var selectedTenantCount = selectedByTenant.GetValueOrDefault(tenantId);
                if (selectedTenantCount >= limits.MaxItemsPerClaimCycle
                    || currentTenantInFlight + selectedTenantCount >= limits.MaxInFlightPerTenant)
                {
                    continue;
                }

                var queue = queues[tenantId];
                while (queue.TryDequeue(out var candidate))
                {
                    var endpointKey = (candidate.TenantId, candidate.EndpointId);
                    var currentEndpointInFlight = endpointInFlight.GetValueOrDefault(endpointKey);
                    if (currentEndpointInFlight >= limits.MaxInFlightPerEndpoint)
                    {
                        continue;
                    }

                    selected.Add(candidate);
                    selectedByTenant[tenantId] = selectedTenantCount + 1;
                    endpointInFlight[endpointKey] = currentEndpointInFlight + 1;
                    added = true;
                    break;
                }

                if (selected.Count == remainingGlobalCapacity)
                {
                    break;
                }
            }

            if (!added)
            {
                break;
            }
        }

        var leaseExpiresAt = request.ClaimedAt.Add(request.LeaseDuration);
        var claims = new List<WebhookDeliveryClaim>(selected.Count);
        foreach (var attempt in selected)
        {
            var leaseToken = Guid.CreateVersion7();
            attempt.Outcome = WebhookDeliveryAttemptOutcome.Sending;
            attempt.ProcessingLeaseToken = leaseToken;
            attempt.ProcessingFence = checked(attempt.ProcessingFence + 1);
            attempt.ProcessingStartedAt = request.ClaimedAt;
            attempt.ProcessingLeaseExpiresAt = leaseExpiresAt;
            attempt.SentAt = request.ClaimedAt;
            attempt.UpdatedAt = request.ClaimedAt;
            claims.Add(new WebhookDeliveryClaim(
                attempt,
                leaseToken,
                attempt.ProcessingFence,
                request.ClaimedAt,
                leaseExpiresAt));
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return claims;
    }

    public Task<int> CountDueScheduledAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        return _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Scheduled
                && e.ScheduledAt <= now, cancellationToken);
    }

    public Task<int> CountStaleSendingAsync(
        DateTime processingStartedBefore,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        return _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Sending
                && ((e.ProcessingLeaseExpiresAt != null && e.ProcessingLeaseExpiresAt < now)
                    || (e.ProcessingLeaseExpiresAt == null
                        && e.ProcessingStartedAt != null
                        && e.ProcessingStartedAt < processingStartedBefore)), cancellationToken);
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

    public async Task<bool> MarkSucceededAsync(
        Guid tenantId,
        Guid attemptId,
        Guid processingLeaseToken,
        long processingFence,
        DateTime sentAt,
        DateTime completedAt,
        int httpStatusCode,
        int durationMs,
        CancellationToken cancellationToken)
    {
        var affected = await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => e.TenantId == tenantId
                && e.Id == attemptId
                && e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Sending
                && e.ProcessingLeaseToken == processingLeaseToken
                && e.ProcessingFence == processingFence)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.OutcomeId, (int)WebhookDeliveryAttemptOutcome.Succeeded)
                .SetProperty(e => e.SentAt, (DateTime?)sentAt)
                .SetProperty(e => e.CompletedAt, (DateTime?)completedAt)
                .SetProperty(e => e.HttpStatusCode, (int?)httpStatusCode)
                .SetProperty(e => e.DurationMs, (int?)durationMs)
                .SetProperty(e => e.NextRetryAt, (DateTime?)null)
                .SetProperty(e => e.FailureCategory, (string?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseExpiresAt, (DateTime?)null)
                .SetProperty(e => e.UpdatedAt, completedAt), cancellationToken);

        return affected == 1;
    }

    public async Task<bool> MarkFailedAsync(
        Guid tenantId,
        Guid attemptId,
        Guid processingLeaseToken,
        long processingFence,
        WebhookDeliveryAttemptOutcome outcome,
        DateTime completedAt,
        string failureCategory,
        int? httpStatusCode,
        int durationMs,
        DateTime? nextRetryAt,
        CancellationToken cancellationToken)
    {
        var affected = await _dbContext.WebhookDeliveryAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.WebhookWorkerCrossTenantQueue)
            .Where(e => e.TenantId == tenantId
                && e.Id == attemptId
                && e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Sending
                && e.ProcessingLeaseToken == processingLeaseToken
                && e.ProcessingFence == processingFence)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.OutcomeId, (int)outcome)
                .SetProperty(e => e.CompletedAt, (DateTime?)completedAt)
                .SetProperty(e => e.HttpStatusCode, httpStatusCode)
                .SetProperty(e => e.DurationMs, (int?)durationMs)
                .SetProperty(e => e.FailureCategory, Truncate(failureCategory, MaxFailureCategoryLength))
                .SetProperty(e => e.NextRetryAt, nextRetryAt)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseExpiresAt, (DateTime?)null)
                .SetProperty(e => e.UpdatedAt, completedAt), cancellationToken);

        return affected == 1;
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
            .Where(e => e.OutcomeId == (int)WebhookDeliveryAttemptOutcome.Sending
                && ((e.ProcessingLeaseExpiresAt != null && e.ProcessingLeaseExpiresAt < recoveredAt)
                    || (e.ProcessingLeaseExpiresAt == null
                        && e.ProcessingStartedAt != null
                        && e.ProcessingStartedAt < processingStartedBefore)))
            .OrderBy(e => e.ProcessingLeaseExpiresAt ?? e.ProcessingStartedAt)
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
                .SetProperty(e => e.OutcomeId, (int)WebhookDeliveryAttemptOutcome.Scheduled)
                .SetProperty(e => e.ScheduledAt, recoveredAt)
                .SetProperty(e => e.SentAt, (DateTime?)null)
                .SetProperty(e => e.CompletedAt, (DateTime?)null)
                .SetProperty(e => e.HttpStatusCode, (int?)null)
                .SetProperty(e => e.DurationMs, (int?)null)
                .SetProperty(e => e.FailureCategory, Truncate(failureCategory, MaxFailureCategoryLength))
                .SetProperty(e => e.NextRetryAt, recoveredAt)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.ProcessingFence, e => e.ProcessingFence + 1)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseExpiresAt, (DateTime?)null)
                .SetProperty(e => e.UpdatedAt, recoveredAt), cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
