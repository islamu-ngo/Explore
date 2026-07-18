// ABOUTME: EF Core repository for Basic Dispatch Mode email outbox state, attempts, and receipts.
// ABOUTME: Uses affected-row conditional updates for optimistic claims and durable retry/dead-letter transitions.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EmailDispatchOutboxRepository : IEmailDispatchOutboxRepository
{
    private const int MaxErrorLength = 2000;
    private const int MaxReceiptFailureLength = 1000;
    private const string AcceptedSettlementUnknownCategory = "accepted_settlement_unknown";
    private const string AcceptedSettlementUnknownMessage = "SMTP accepted the message, but local settlement is uncertain. Automatic resend is disabled pending reconciliation.";

    private readonly ExploreDbContext _dbContext;

    public EmailDispatchOutboxRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EmailDispatchOutbox> Create(EmailDispatchOutbox entity, CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox.AddAsync(entity, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return entity;
    }

    public async Task<IReadOnlyList<EmailDispatchOutbox>> GetPendingBatch(
        int batchSize,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await GetPendingBatch(batchSize, batchSize, true, now, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchOutbox>> GetPendingBatch(
        int batchSize,
        int maxRowsPerTenant,
        bool includeOptionalReminders,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchOutbox
            .FromSqlInterpolated($"""
                WITH ranked AS (
                    SELECT outbox.id,
                           ROW_NUMBER() OVER (
                               PARTITION BY outbox.tenant_id
                               ORDER BY
                                   CASE
                                       WHEN delivery.is_required = TRUE THEN 0
                                       WHEN outbox.kind = {(int)EmailDispatchKind.EventReminder} THEN 2
                                       ELSE 1
                                   END,
                                   outbox.created_at,
                                   outbox.id) AS tenant_rank,
                           CASE
                               WHEN delivery.is_required = TRUE THEN 0
                               WHEN outbox.kind = {(int)EmailDispatchKind.EventReminder} THEN 2
                               ELSE 1
                           END AS priority
                    FROM email_dispatch_outbox AS outbox
                    LEFT JOIN notification_deliveries AS delivery
                      ON delivery.tenant_id = outbox.tenant_id
                     AND delivery.email_dispatch_outbox_id = outbox.id
                    WHERE outbox.content_redacted_at IS NULL
                      AND outbox.is_deleted = FALSE
                      AND outbox.status IN ({(int)EmailDispatchStatus.Pending}, {(int)EmailDispatchStatus.RetryScheduled})
                      AND (outbox.next_attempt_at IS NULL OR outbox.next_attempt_at <= {now})
                      AND ({includeOptionalReminders} OR outbox.kind <> {(int)EmailDispatchKind.EventReminder})
                      AND NOT EXISTS (
                          SELECT 1
                          FROM email_dispatch_tenant_controls AS control
                          WHERE control.tenant_id = outbox.tenant_id
                            AND control.is_paused = TRUE)
                )
                SELECT outbox.*
                FROM email_dispatch_outbox AS outbox
                INNER JOIN ranked ON ranked.id = outbox.id
                WHERE ranked.tenant_rank <= {maxRowsPerTenant}
                ORDER BY ranked.priority, ranked.tenant_rank, outbox.created_at, outbox.id
                LIMIT {batchSize}
                """)
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchOutbox>> GetRabbitMqPublishBatch(
        int batchSize,
        DateTime now,
        DateTime retryAttemptsBefore,
        CancellationToken cancellationToken)
    {
        var pausedTenantIds = _dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(control => control.IsPaused)
            .Select(control => control.TenantId);

        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now)
                && (e.RabbitMqLastPublishAttemptAt == null || e.RabbitMqLastPublishAttemptAt <= retryAttemptsBefore)
                && !pausedTenantIds.Contains(e.TenantId))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountDueDispatchAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now), cancellationToken);
    }

    public Task<DateTime?> GetOldestDueCreatedAtAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .MinAsync(e => (DateTime?)e.CreatedAt, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, int>> CountDueDispatchByTenantAsync(
        DateTime now,
        int tenantLimit,
        CancellationToken cancellationToken)
    {
        var rows = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .GroupBy(e => e.TenantId)
            .Select(group => new { TenantId = group.Key, Count = group.Count() })
            .OrderByDescending(row => row.Count)
            .ThenBy(row => row.TenantId)
            .Take(tenantLimit)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(row => row.TenantId, row => row.Count);
    }

    public Task<int> CountRetryScheduledAsync(CancellationToken cancellationToken)
    {
        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.Status == EmailDispatchStatus.RetryScheduled, cancellationToken);
    }

    public Task<int> CountStaleProcessingAsync(
        DateTime processingStartedBefore,
        CancellationToken cancellationToken)
    {
        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.Status == EmailDispatchStatus.Processing
                && e.ProcessingStartedAt != null
                && e.ProcessingStartedAt <= processingStartedBefore, cancellationToken);
    }

    public Task<int> CountDeadLetteredAsync(CancellationToken cancellationToken)
    {
        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .CountAsync(e => e.Status == EmailDispatchStatus.DeadLettered, cancellationToken);
    }

    public async Task<IReadOnlyList<EmailDispatchOutbox>> GetStatusRows(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .Where(dispatch => dispatch.TenantId == tenantId)
            .OrderByDescending(dispatch => dispatch.LastFailureAt ?? dispatch.SentAt ?? dispatch.UnknownAt ?? dispatch.ParkedAt ?? dispatch.CreatedAt)
            .ThenByDescending(dispatch => dispatch.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<EmailDispatchOutbox?> GetByTenantAndId(
        Guid tenantId,
        Guid outboxId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.Id == outboxId, cancellationToken);
    }

    public async Task<EmailDispatchOutbox?> GetByTenantAndPublishEventId(
        Guid tenantId,
        Guid publishEventId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.TenantId == tenantId && e.PublishEventId == publishEventId, cancellationToken);
    }

    public async Task<bool> IsTenantPaused(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .AnyAsync(e => e.TenantId == tenantId && e.IsPaused, cancellationToken);
    }

    public async Task<EmailDispatchTenantControl> SetTenantPauseState(
        Guid tenantId,
        bool isPaused,
        string? pauseReason,
        Guid? changedBy,
        DateTime changedAt,
        CancellationToken cancellationToken)
    {
        var control = await _dbContext.EmailDispatchTenantControls
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .FirstOrDefaultAsync(e => e.TenantId == tenantId, cancellationToken);

        if (control is null)
        {
            control = new EmailDispatchTenantControl
            {
                Id = Guid.CreateVersion7(),
                TenantId = tenantId,
                CreatedAt = changedAt,
                CreatedBy = changedBy
            };
            await _dbContext.EmailDispatchTenantControls.AddAsync(control, cancellationToken);
        }

        control.IsPaused = isPaused;
        control.PauseReason = isPaused ? Truncate(pauseReason, 500) : null;
        control.PausedAt = isPaused ? changedAt : null;
        control.PausedBy = isPaused ? changedBy : null;
        control.UpdatedAt = changedAt;
        control.UpdatedBy = changedBy;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return control;
    }

    public async Task<bool> TryParkForOperator(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime parkedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == outboxId
                && e.ContentRedactedAt == null
                && e.Status != EmailDispatchStatus.Sent
                && e.Status != EmailDispatchStatus.Skipped
                && e.Status != EmailDispatchStatus.Parked)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Parked)
                .SetProperty(e => e.ParkedAt, parkedAt)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.LastFailureCategory, "operator_parked")
                .SetProperty(e => e.LastError, Truncate(reason, MaxErrorLength))
                .SetProperty(e => e.LastFailureAt, parkedAt)
                .SetProperty(e => e.UpdatedAt, parkedAt)
                .SetProperty(e => e.UpdatedBy, changedBy), cancellationToken);

        return updated > 0;
    }

    public async Task<bool> TryReplayForOperator(
        Guid tenantId,
        Guid outboxId,
        Guid? changedBy,
        DateTime replayAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == outboxId
                && e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.DeadLettered
                    || e.Status == EmailDispatchStatus.Parked
                    || e.Status == EmailDispatchStatus.Unknown
                    || e.Status == EmailDispatchStatus.RetryScheduled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Pending)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.DeadLetteredAt, (DateTime?)null)
                .SetProperty(e => e.ParkedAt, (DateTime?)null)
                .SetProperty(e => e.UnknownAt, (DateTime?)null)
                .SetProperty(e => e.LastFailureCategory, (string?)null)
                .SetProperty(e => e.LastError, (string?)null)
                .SetProperty(e => e.LastFailureAt, (DateTime?)null)
                .SetProperty(e => e.RabbitMqLastPublishedAt, (DateTime?)null)
                .SetProperty(e => e.RabbitMqLastPublishAttemptAt, (DateTime?)null)
                .SetProperty(e => e.RabbitMqPublishAttemptCount, 0)
                .SetProperty(e => e.RabbitMqLastPublishFailureCategory, (string?)null)
                .SetProperty(e => e.UpdatedAt, replayAt)
                .SetProperty(e => e.UpdatedBy, changedBy), cancellationToken);

        return updated > 0;
    }

    public async Task<bool> TryResolveWithoutReplay(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime resolvedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(e => e.TenantId == tenantId
                && e.Id == outboxId
                && e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.DeadLettered
                    || e.Status == EmailDispatchStatus.Parked
                    || e.Status == EmailDispatchStatus.Unknown))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Skipped)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.LastFailureCategory, "operator_resolved_without_replay")
                .SetProperty(e => e.LastError, Truncate(reason, MaxErrorLength))
                .SetProperty(e => e.LastFailureAt, resolvedAt)
                .SetProperty(e => e.UpdatedAt, resolvedAt)
                .SetProperty(e => e.UpdatedBy, changedBy), cancellationToken);

        if (updated == 0)
        {
            return false;
        }

        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(receipt => receipt.TenantId == tenantId
                && receipt.EmailDispatchOutboxId == outboxId
                && receipt.Status != EmailDispatchReceiptStatus.Completed)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Skipped)
                .SetProperty(receipt => receipt.CompletedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailedAt, resolvedAt)
                .SetProperty(receipt => receipt.FailureCode, "operator_resolved_without_replay")
                .SetProperty(receipt => receipt.FailureMessage, Truncate(reason, MaxReceiptFailureLength))
                .SetProperty(receipt => receipt.UpdatedAt, resolvedAt)
                .SetProperty(receipt => receipt.UpdatedBy, changedBy), cancellationToken);

        await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId == outboxId
                && delivery.StatusId != (int)NotificationDeliveryStatusEnum.Delivered)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Skipped)
                .SetProperty(delivery => delivery.ProviderStatus, "skipped")
                .SetProperty(delivery => delivery.FailureCategory, "operator_resolved_without_replay")
                .SetProperty(delivery => delivery.CompletedAt, resolvedAt)
                .SetProperty(delivery => delivery.UpdatedAt, resolvedAt)
                .SetProperty(delivery => delivery.UpdatedBy, changedBy), cancellationToken);

        return true;
    }

    public async Task<IReadOnlyList<Guid>> GetRetentionTenantIds(
        DateTime cutoffUtc,
        int maxTenants,
        CancellationToken cancellationToken)
    {
        return await RetentionRedactionEligible(cutoffUtc)
            .GroupBy(outbox => outbox.TenantId)
            .Select(group => new
            {
                TenantId = group.Key,
                OldestCreatedAt = group.Min(outbox => outbox.CreatedAt)
            })
            .OrderBy(row => row.OldestCreatedAt)
            .ThenBy(row => row.TenantId)
            .Take(maxTenants)
            .Select(row => row.TenantId)
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountRetentionRedactionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        return RetentionRedactionEligible(cutoffUtc)
            .Where(outbox => outbox.TenantId == tenantId)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .CountAsync(cancellationToken);
    }

    public async Task<int> RedactRetentionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        DateTime redactedAt,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var rows = await RetentionRedactionEligible(cutoffUtc)
            .Where(outbox => outbox.TenantId == tenantId)
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (rows.Count == 0)
        {
            return 0;
        }

        var ids = rows.ToArray();
        await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(attempt => ids.Contains(attempt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Provider, (string?)null)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, (string?)null)
                .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                .SetProperty(attempt => attempt.CorrelationId, (string?)null)
                .SetProperty(attempt => attempt.UpdatedAt, redactedAt), cancellationToken);

        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(receipt => ids.Contains(receipt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.ConsumerId, (string?)null)
                .SetProperty(receipt => receipt.FailureMessage, (string?)null)
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, redactedAt), cancellationToken);

        await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId != null
                && ids.Contains(delivery.EmailDispatchOutboxId.Value))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, (string?)null)
                .SetProperty(delivery => delivery.UpdatedAt, redactedAt), cancellationToken);

        var redactedCount = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => ids.Contains(outbox.Id) && outbox.ContentRedactedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.RecipientEmail, string.Empty)
                .SetProperty(outbox => outbox.Subject, string.Empty)
                .SetProperty(outbox => outbox.PlainTextBody, (string?)null)
                .SetProperty(outbox => outbox.HtmlBody, (string?)null)
                .SetProperty(outbox => outbox.ReplyTo, (string?)null)
                .SetProperty(outbox => outbox.LastError, (string?)null)
                .SetProperty(outbox => outbox.ProviderMessageId, (string?)null)
                .SetProperty(outbox => outbox.CorrelationId, (string?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.ContentRedactedAt, redactedAt)
                .SetProperty(outbox => outbox.UpdatedAt, redactedAt), cancellationToken);
        return redactedCount;
    }

    public async Task<int> SuppressAndRedactTenant(
        Guid tenantId,
        Guid? changedBy,
        DateTime redactedAt,
        CancellationToken cancellationToken)
    {
        var ids = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .Where(outbox => outbox.TenantId == tenantId && outbox.ContentRedactedAt == null)
            .Select(outbox => outbox.Id)
            .ToListAsync(cancellationToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(attempt => attempt.TenantId == tenantId && ids.Contains(attempt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Provider, (string?)null)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, (string?)null)
                .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                .SetProperty(attempt => attempt.CorrelationId, (string?)null)
                .SetProperty(attempt => attempt.UpdatedAt, redactedAt)
                .SetProperty(attempt => attempt.UpdatedBy, changedBy), cancellationToken);

        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(receipt => receipt.TenantId == tenantId && ids.Contains(receipt.EmailDispatchOutboxId))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, receipt => receipt.Status == EmailDispatchReceiptStatus.Completed
                    ? EmailDispatchReceiptStatus.Completed
                    : EmailDispatchReceiptStatus.Skipped)
                .SetProperty(receipt => receipt.ConsumerId, (string?)null)
                .SetProperty(receipt => receipt.FailedAt, receipt => receipt.Status == EmailDispatchReceiptStatus.Completed
                    ? receipt.FailedAt
                    : redactedAt)
                .SetProperty(receipt => receipt.FailureCode, receipt => receipt.Status == EmailDispatchReceiptStatus.Completed
                    ? receipt.FailureCode
                    : "tenant_deleted")
                .SetProperty(receipt => receipt.FailureMessage, (string?)null)
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, redactedAt)
                .SetProperty(receipt => receipt.UpdatedBy, changedBy), cancellationToken);

        await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(delivery => delivery.TenantId == tenantId
                && delivery.EmailDispatchOutboxId != null
                && ids.Contains(delivery.EmailDispatchOutboxId.Value))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, delivery => delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                    ? (int)NotificationDeliveryStatusEnum.Delivered
                    : (int)NotificationDeliveryStatusEnum.Skipped)
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, (string?)null)
                .SetProperty(delivery => delivery.FailureCategory, delivery => delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                    ? delivery.FailureCategory
                    : "tenant_deleted")
                .SetProperty(delivery => delivery.CompletedAt, delivery => delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                    ? delivery.CompletedAt
                    : redactedAt)
                .SetProperty(delivery => delivery.UpdatedAt, redactedAt)
                .SetProperty(delivery => delivery.UpdatedBy, changedBy), cancellationToken);

        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .Where(outbox => outbox.TenantId == tenantId
                && ids.Contains(outbox.Id)
                && outbox.ContentRedactedAt == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, outbox => outbox.Status == EmailDispatchStatus.Pending
                    || outbox.Status == EmailDispatchStatus.RetryScheduled
                    || outbox.Status == EmailDispatchStatus.Processing
                        ? EmailDispatchStatus.Skipped
                        : outbox.Status)
                .SetProperty(outbox => outbox.RecipientEmail, string.Empty)
                .SetProperty(outbox => outbox.Subject, string.Empty)
                .SetProperty(outbox => outbox.PlainTextBody, (string?)null)
                .SetProperty(outbox => outbox.HtmlBody, (string?)null)
                .SetProperty(outbox => outbox.ReplyTo, (string?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, outbox => outbox.Status == EmailDispatchStatus.Pending
                    || outbox.Status == EmailDispatchStatus.RetryScheduled
                    || outbox.Status == EmailDispatchStatus.Processing
                        ? "tenant_deleted"
                        : outbox.LastFailureCategory)
                .SetProperty(outbox => outbox.LastError, (string?)null)
                .SetProperty(outbox => outbox.LastFailureAt, outbox => outbox.Status == EmailDispatchStatus.Pending
                    || outbox.Status == EmailDispatchStatus.RetryScheduled
                    || outbox.Status == EmailDispatchStatus.Processing
                        ? redactedAt
                        : outbox.LastFailureAt)
                .SetProperty(outbox => outbox.ProviderMessageId, (string?)null)
                .SetProperty(outbox => outbox.CorrelationId, (string?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.ContentRedactedAt, redactedAt)
                .SetProperty(outbox => outbox.UpdatedAt, redactedAt)
                .SetProperty(outbox => outbox.UpdatedBy, changedBy), cancellationToken);
    }

    public async Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id
                && e.ContentRedactedAt == null
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= startedAt))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Processing)
                .SetProperty(e => e.ProcessingStartedAt, startedAt)
                .SetProperty(e => e.ProcessingLeaseToken, leaseToken)
                .SetProperty(e => e.AttemptCount, e => e.AttemptCount + 1)
                .SetProperty(e => e.UpdatedAt, startedAt), cancellationToken);

        return updated > 0;
    }

    public async Task<int> MarkStaleProcessingAsUnknown(
        DateTime processingStartedBefore,
        DateTime recoveredAt,
        string failureCategory,
        string errorMessage,
        int batchSize,
        CancellationToken cancellationToken)
    {
        var outboxIds = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => e.Status == EmailDispatchStatus.Processing
                && e.ProcessingStartedAt != null
                && e.ProcessingStartedAt <= processingStartedBefore)
            .OrderBy(e => e.ProcessingStartedAt)
            .Take(batchSize)
            .Select(e => e.Id)
            .ToListAsync(cancellationToken);

        if (outboxIds.Count == 0)
        {
            return 0;
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => outboxIds.Contains(e.Id)
                && e.Status == EmailDispatchStatus.Processing
                && e.ProcessingStartedAt != null
                && e.ProcessingStartedAt <= processingStartedBefore)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Unknown)
                .SetProperty(e => e.UnknownAt, recoveredAt)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.LastFailureCategory, Truncate(failureCategory, 100))
                .SetProperty(e => e.LastError, Truncate(errorMessage, MaxErrorLength))
                .SetProperty(e => e.LastFailureAt, recoveredAt)
                .SetProperty(e => e.UpdatedAt, recoveredAt), cancellationToken);

        if (updated > 0)
        {
            await _dbContext.EmailDispatchAttempts
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(attempt => _dbContext.EmailDispatchOutbox
                    .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                    .Any(outbox => outboxIds.Contains(outbox.Id)
                        && outbox.TenantId == attempt.TenantId
                        && outbox.Id == attempt.EmailDispatchOutboxId
                        && outbox.Status == EmailDispatchStatus.Unknown
                        && outbox.UnknownAt == recoveredAt
                        && outbox.AttemptCount == attempt.AttemptNumber))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(attempt => attempt.Outcome, EmailDispatchAttemptOutcome.Unknown)
                    .SetProperty(attempt => attempt.CompletedAt, recoveredAt)
                    .SetProperty(attempt => attempt.FailureCategory, Truncate(failureCategory, 100))
                    .SetProperty(attempt => attempt.SanitizedErrorMessage, Truncate(errorMessage, MaxErrorLength))
                    .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                    .SetProperty(attempt => attempt.UpdatedAt, recoveredAt), cancellationToken);

            await _dbContext.EmailDispatchReceipts
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(receipt => _dbContext.EmailDispatchOutbox
                    .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                    .Any(outbox => outboxIds.Contains(outbox.Id)
                        && outbox.TenantId == receipt.TenantId
                        && outbox.Id == receipt.EmailDispatchOutboxId
                        && outbox.Status == EmailDispatchStatus.Unknown
                        && outbox.UnknownAt == recoveredAt)
                    && receipt.Status == EmailDispatchReceiptStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Unknown)
                    .SetProperty(receipt => receipt.FailedAt, recoveredAt)
                    .SetProperty(receipt => receipt.FailureCode, Truncate(failureCategory, 100))
                    .SetProperty(receipt => receipt.FailureMessage, Truncate(errorMessage, MaxReceiptFailureLength))
                    .SetProperty(receipt => receipt.UpdatedAt, recoveredAt), cancellationToken);

            await _dbContext.NotificationDeliveries
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(delivery => delivery.EmailDispatchOutboxId != null
                    && _dbContext.EmailDispatchOutbox
                        .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                        .Any(outbox => outboxIds.Contains(outbox.Id)
                            && outbox.TenantId == delivery.TenantId
                            && outbox.Id == delivery.EmailDispatchOutboxId.Value
                            && outbox.Status == EmailDispatchStatus.Unknown
                            && outbox.UnknownAt == recoveredAt)
                    && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Unknown)
                    .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                    .SetProperty(delivery => delivery.ProviderStatus, "unknown")
                    .SetProperty(delivery => delivery.FailureCategory, Truncate(failureCategory, 100))
                    .SetProperty(delivery => delivery.CompletedAt, recoveredAt)
                    .SetProperty(delivery => delivery.UpdatedAt, recoveredAt), cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return updated;
    }

    public async Task MarkAsSent(
        Guid id,
        DateTime sentAt,
        string? providerMessageId,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Sent)
                .SetProperty(e => e.SentAt, sentAt)
                .SetProperty(e => e.ProviderMessageId, Truncate(providerMessageId, 500))
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.LastFailureCategory, (string?)null)
                .SetProperty(e => e.LastError, (string?)null)
                .SetProperty(e => e.RabbitMqLastPublishFailureCategory, (string?)null)
                .SetProperty(e => e.UpdatedAt, sentAt), cancellationToken);
    }

    public async Task MarkAsFailed(
        Guid id,
        string failureCategory,
        string errorMessage,
        bool isRetryable,
        TimeSpan retryDelay,
        int maxAttempts,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        var entry = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .FirstAsync(e => e.Id == id, cancellationToken);
        var exhausted = !isRetryable || entry.AttemptCount >= Math.Min(entry.MaxAttempts, maxAttempts);

        entry.Status = exhausted ? EmailDispatchStatus.DeadLettered : EmailDispatchStatus.RetryScheduled;
        entry.DeadLetteredAt = exhausted ? failedAt : null;
        entry.NextAttemptAt = exhausted ? null : failedAt.Add(retryDelay);
        entry.ProcessingStartedAt = null;
        entry.ProcessingLeaseToken = null;
        entry.LastFailureCategory = Truncate(failureCategory, 100);
        entry.LastError = Truncate(errorMessage, MaxErrorLength);
        entry.LastFailureAt = failedAt;
        entry.UpdatedAt = failedAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkAsUnknown(
        Guid id,
        string failureCategory,
        string errorMessage,
        DateTime unknownAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Unknown)
                .SetProperty(e => e.UnknownAt, unknownAt)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.LastFailureCategory, Truncate(failureCategory, 100))
                .SetProperty(e => e.LastError, Truncate(errorMessage, MaxErrorLength))
                .SetProperty(e => e.LastFailureAt, unknownAt)
                .SetProperty(e => e.UpdatedAt, unknownAt), cancellationToken);
    }

    public async Task MarkAsSkipped(
        Guid id,
        string reasonCategory,
        string reasonMessage,
        DateTime skippedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchStatus.Skipped)
                .SetProperty(e => e.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(e => e.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(e => e.NextAttemptAt, (DateTime?)null)
                .SetProperty(e => e.LastFailureCategory, Truncate(reasonCategory, 100))
                .SetProperty(e => e.LastError, Truncate(reasonMessage, MaxErrorLength))
                .SetProperty(e => e.LastFailureAt, skippedAt)
                .SetProperty(e => e.UpdatedAt, skippedAt), cancellationToken);
    }

    public async Task MarkRabbitMqPublishSucceeded(
        Guid id,
        DateTime publishedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.RabbitMqLastPublishedAt, publishedAt)
                .SetProperty(e => e.RabbitMqLastPublishAttemptAt, publishedAt)
                .SetProperty(e => e.RabbitMqPublishAttemptCount, e => e.RabbitMqPublishAttemptCount + 1)
                .SetProperty(e => e.RabbitMqLastPublishFailureCategory, (string?)null)
                .SetProperty(e => e.UpdatedAt, publishedAt), cancellationToken);
    }

    public async Task MarkRabbitMqPublishFailed(
        Guid id,
        string failureCategory,
        DateTime attemptedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id
                && (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.RabbitMqLastPublishAttemptAt, attemptedAt)
                .SetProperty(e => e.RabbitMqPublishAttemptCount, e => e.RabbitMqPublishAttemptCount + 1)
                .SetProperty(e => e.RabbitMqLastPublishFailureCategory, Truncate(failureCategory, 100))
                .SetProperty(e => e.UpdatedAt, attemptedAt), cancellationToken);
    }

    public async Task RecordAttempt(EmailDispatchAttempt attempt, CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(existing => existing.TenantId == attempt.TenantId
                && existing.EmailDispatchOutboxId == attempt.EmailDispatchOutboxId
                && existing.AttemptNumber == attempt.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(existing => existing.Transport, attempt.Transport)
                .SetProperty(existing => existing.Provider, attempt.Provider)
                .SetProperty(existing => existing.Outcome, attempt.Outcome)
                .SetProperty(existing => existing.StartedAt, attempt.StartedAt)
                .SetProperty(existing => existing.CompletedAt, attempt.CompletedAt)
                .SetProperty(existing => existing.FailureCategory, Truncate(attempt.FailureCategory, 100))
                .SetProperty(existing => existing.SanitizedErrorMessage, Truncate(attempt.SanitizedErrorMessage, MaxErrorLength))
                .SetProperty(existing => existing.ProviderMessageId, Truncate(attempt.ProviderMessageId, 500))
                .SetProperty(existing => existing.CorrelationId, Truncate(attempt.CorrelationId, 200))
                .SetProperty(existing => existing.UpdatedAt, DateTime.UtcNow), cancellationToken);
        if (updated > 0)
        {
            return;
        }

        await _dbContext.EmailDispatchAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SettleProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var attemptUpdated = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(attempt => attempt.TenantId == settlement.TenantId
                && attempt.EmailDispatchOutboxId == settlement.OutboxId
                && attempt.AttemptNumber == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Outcome, EmailDispatchAttemptOutcome.Succeeded)
                .SetProperty(attempt => attempt.CompletedAt, settlement.SettledAt)
                .SetProperty(attempt => attempt.FailureCategory, (string?)null)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, (string?)null)
                .SetProperty(attempt => attempt.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(attempt => attempt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(attemptUpdated, "email dispatch attempt");

        var receiptUpdated = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(receipt => receipt.TenantId == settlement.TenantId
                && receipt.EmailDispatchOutboxId == settlement.OutboxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Completed)
                .SetProperty(receipt => receipt.CompletedAt, settlement.SettledAt)
                .SetProperty(receipt => receipt.FailedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailureCode, (string?)null)
                .SetProperty(receipt => receipt.FailureMessage, (string?)null)
                .SetProperty(receipt => receipt.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(receipt => receipt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(receiptUpdated, "email dispatch receipt");

        var outboxUpdated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId
                && outbox.Status == EmailDispatchStatus.Processing)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, EmailDispatchStatus.Sent)
                .SetProperty(outbox => outbox.SentAt, settlement.SettledAt)
                .SetProperty(outbox => outbox.UnknownAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, (string?)null)
                .SetProperty(outbox => outbox.LastError, (string?)null)
                .SetProperty(outbox => outbox.LastFailureAt, (DateTime?)null)
                .SetProperty(outbox => outbox.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(outboxUpdated, "email dispatch outbox");

        var deliveryUpdated = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(delivery => delivery.TenantId == settlement.TenantId
                && delivery.EmailDispatchOutboxId == settlement.OutboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Delivered)
                .SetProperty(delivery => delivery.ProviderMessageId, Truncate(settlement.ProviderMessageId, 500))
                .SetProperty(delivery => delivery.ProviderStatus, "accepted")
                .SetProperty(delivery => delivery.FailureCategory, (string?)null)
                .SetProperty(delivery => delivery.CompletedAt, settlement.SettledAt)
                .SetProperty(delivery => delivery.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(deliveryUpdated, "email notification delivery");

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<EmailDispatchAcceptedReconciliationOutcome> ReconcileProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        var providerMessageId = Truncate(settlement.ProviderMessageId, 500);

        var outboxSent = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId
                && outbox.Status == EmailDispatchStatus.Sent
                && (providerMessageId == null || outbox.ProviderMessageId == providerMessageId), cancellationToken);
        var attemptSent = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(attempt => attempt.TenantId == settlement.TenantId
                && attempt.EmailDispatchOutboxId == settlement.OutboxId
                && attempt.AttemptNumber == settlement.AttemptNumber
                && attempt.Outcome == EmailDispatchAttemptOutcome.Succeeded
                && (providerMessageId == null || attempt.ProviderMessageId == providerMessageId), cancellationToken);
        var receiptSent = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(receipt => receipt.TenantId == settlement.TenantId
                && receipt.EmailDispatchOutboxId == settlement.OutboxId
                && receipt.Status == EmailDispatchReceiptStatus.Completed
                && (providerMessageId == null || receipt.ProviderMessageId == providerMessageId), cancellationToken);
        var deliverySent = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .AnyAsync(delivery => delivery.TenantId == settlement.TenantId
                && delivery.EmailDispatchOutboxId == settlement.OutboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email
                && delivery.StatusId == (int)NotificationDeliveryStatusEnum.Delivered
                && (providerMessageId == null || delivery.ProviderMessageId == providerMessageId), cancellationToken);

        if (outboxSent && attemptSent && receiptSent && deliverySent)
        {
            await transaction.CommitAsync(cancellationToken);
            return EmailDispatchAcceptedReconciliationOutcome.Sent;
        }

        var attemptUpdated = await _dbContext.EmailDispatchAttempts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(attempt => attempt.TenantId == settlement.TenantId
                && attempt.EmailDispatchOutboxId == settlement.OutboxId
                && attempt.AttemptNumber == settlement.AttemptNumber)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(attempt => attempt.Outcome, EmailDispatchAttemptOutcome.Unknown)
                .SetProperty(attempt => attempt.CompletedAt, settlement.SettledAt)
                .SetProperty(attempt => attempt.FailureCategory, AcceptedSettlementUnknownCategory)
                .SetProperty(attempt => attempt.SanitizedErrorMessage, AcceptedSettlementUnknownMessage)
                .SetProperty(attempt => attempt.ProviderMessageId, (string?)null)
                .SetProperty(attempt => attempt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(attemptUpdated, "email dispatch attempt");

        var receiptUpdated = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(receipt => receipt.TenantId == settlement.TenantId
                && receipt.EmailDispatchOutboxId == settlement.OutboxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Unknown)
                .SetProperty(receipt => receipt.CompletedAt, (DateTime?)null)
                .SetProperty(receipt => receipt.FailedAt, settlement.SettledAt)
                .SetProperty(receipt => receipt.FailureCode, AcceptedSettlementUnknownCategory)
                .SetProperty(receipt => receipt.FailureMessage, AcceptedSettlementUnknownMessage)
                .SetProperty(receipt => receipt.ProviderMessageId, (string?)null)
                .SetProperty(receipt => receipt.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(receiptUpdated, "email dispatch receipt");

        var outboxUpdated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(outbox => outbox.TenantId == settlement.TenantId
                && outbox.Id == settlement.OutboxId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(outbox => outbox.Status, EmailDispatchStatus.Unknown)
                .SetProperty(outbox => outbox.SentAt, (DateTime?)null)
                .SetProperty(outbox => outbox.UnknownAt, settlement.SettledAt)
                .SetProperty(outbox => outbox.ProviderMessageId, (string?)null)
                .SetProperty(outbox => outbox.ProcessingStartedAt, (DateTime?)null)
                .SetProperty(outbox => outbox.ProcessingLeaseToken, (Guid?)null)
                .SetProperty(outbox => outbox.NextAttemptAt, (DateTime?)null)
                .SetProperty(outbox => outbox.LastFailureCategory, AcceptedSettlementUnknownCategory)
                .SetProperty(outbox => outbox.LastError, AcceptedSettlementUnknownMessage)
                .SetProperty(outbox => outbox.LastFailureAt, settlement.SettledAt)
                .SetProperty(outbox => outbox.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(outboxUpdated, "email dispatch outbox");

        var deliveryUpdated = await _dbContext.NotificationDeliveries
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(delivery => delivery.TenantId == settlement.TenantId
                && delivery.EmailDispatchOutboxId == settlement.OutboxId
                && delivery.ChannelId == (int)NotificationPreferenceChannelEnum.Email)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(delivery => delivery.StatusId, (int)NotificationDeliveryStatusEnum.Unknown)
                .SetProperty(delivery => delivery.ProviderMessageId, (string?)null)
                .SetProperty(delivery => delivery.ProviderStatus, "unknown")
                .SetProperty(delivery => delivery.FailureCategory, AcceptedSettlementUnknownCategory)
                .SetProperty(delivery => delivery.CompletedAt, settlement.SettledAt)
                .SetProperty(delivery => delivery.UpdatedAt, settlement.SettledAt), cancellationToken);
        EnsureExactlyOne(deliveryUpdated, "email notification delivery");

        await transaction.CommitAsync(cancellationToken);
        return EmailDispatchAcceptedReconciliationOutcome.Unknown;
    }

    public async Task<bool> TryClaimReceipt(EmailDispatchReceipt receipt, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchTenantOperation)
            .AsNoTracking()
            .AnyAsync(e => e.TenantId == receipt.TenantId && e.PublishEventId == receipt.PublishEventId, cancellationToken);
        if (exists)
        {
            return false;
        }

        await _dbContext.EmailDispatchReceipts.AddAsync(receipt, cancellationToken);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            _dbContext.ChangeTracker.Clear();
            return false;
        }
    }

    public async Task MarkReceiptCompleted(
        Guid receiptId,
        DateTime completedAt,
        string? providerMessageId,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == receiptId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchReceiptStatus.Completed)
                .SetProperty(e => e.CompletedAt, completedAt)
                .SetProperty(e => e.ProviderMessageId, Truncate(providerMessageId, 500))
                .SetProperty(e => e.UpdatedAt, completedAt), cancellationToken);
    }

    public async Task MarkReceiptFailed(
        Guid receiptId,
        string failureCode,
        string failureMessage,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == receiptId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchReceiptStatus.Failed)
                .SetProperty(e => e.FailedAt, failedAt)
                .SetProperty(e => e.FailureCode, Truncate(failureCode, 100))
                .SetProperty(e => e.FailureMessage, Truncate(failureMessage, MaxReceiptFailureLength))
                .SetProperty(e => e.UpdatedAt, failedAt), cancellationToken);
    }

    public async Task MarkReceiptSkipped(
        Guid receiptId,
        string reasonCode,
        string reasonMessage,
        DateTime skippedAt,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchReceipts
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == receiptId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchReceiptStatus.Skipped)
                .SetProperty(e => e.FailedAt, skippedAt)
                .SetProperty(e => e.FailureCode, Truncate(reasonCode, 100))
                .SetProperty(e => e.FailureMessage, Truncate(reasonMessage, MaxReceiptFailureLength))
                .SetProperty(e => e.UpdatedAt, skippedAt), cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }

    private IQueryable<EmailDispatchOutbox> RetentionRedactionEligible(DateTime cutoffUtc)
    {
        return _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(outbox => outbox.ContentRedactedAt == null
                && ((outbox.Status == EmailDispatchStatus.Sent && outbox.SentAt <= cutoffUtc)
                    || (outbox.Status == EmailDispatchStatus.Skipped && outbox.LastFailureAt <= cutoffUtc)));
    }

    private static void EnsureExactlyOne(int affectedRows, string ledger)
    {
        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Accepted SMTP settlement expected one {ledger} row.");
        }
    }
}
