// ABOUTME: EF Core repository for Basic Dispatch Mode email outbox state, attempts, and receipts.
// ABOUTME: Uses affected-row conditional updates for optimistic claims and durable retry/dead-letter transitions.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Persistence.QueryFilters;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EmailDispatchOutboxRepository : IEmailDispatchOutboxRepository
{
    private const int MaxErrorLength = 2000;
    private const int MaxReceiptFailureLength = 1000;

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
        return await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .AsNoTracking()
            .Where(e => (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
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
            .Where(e => (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now)
                && (e.RabbitMqLastPublishAttemptAt == null || e.RabbitMqLastPublishAttemptAt <= retryAttemptsBefore)
                && !pausedTenantIds.Contains(e.TenantId))
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync(cancellationToken);
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

    public async Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
            .Where(e => e.Id == id
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
            await _dbContext.EmailDispatchReceipts
                .IgnoreTenantFilter(TenantFilterBypassReasons.EmailDispatchWorkerCrossTenantQueue)
                .Where(receipt => outboxIds.Contains(receipt.EmailDispatchOutboxId)
                    && receipt.Status == EmailDispatchReceiptStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(receipt => receipt.Status, EmailDispatchReceiptStatus.Unknown)
                    .SetProperty(receipt => receipt.FailedAt, recoveredAt)
                    .SetProperty(receipt => receipt.FailureCode, Truncate(failureCategory, 100))
                    .SetProperty(receipt => receipt.FailureMessage, Truncate(errorMessage, MaxReceiptFailureLength))
                    .SetProperty(receipt => receipt.UpdatedAt, recoveredAt), cancellationToken);
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
        await _dbContext.EmailDispatchAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
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
}
