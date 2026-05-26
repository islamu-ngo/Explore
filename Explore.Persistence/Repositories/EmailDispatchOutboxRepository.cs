// ABOUTME: EF Core repository for Basic Dispatch Mode email outbox state, attempts, and receipts.
// ABOUTME: Uses affected-row conditional updates for optimistic claims and durable retry/dead-letter transitions.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
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
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(e => (e.Status == EmailDispatchStatus.Pending || e.Status == EmailDispatchStatus.RetryScheduled)
                && (e.NextAttemptAt == null || e.NextAttemptAt <= now))
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
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Where(dispatch => dispatch.TenantId == tenantId)
            .OrderByDescending(dispatch => dispatch.LastFailureAt ?? dispatch.SentAt ?? dispatch.UnknownAt ?? dispatch.ParkedAt ?? dispatch.CreatedAt)
            .ThenByDescending(dispatch => dispatch.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsTenantPaused(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.EmailDispatchTenantControls
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
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

    public async Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        var updated = await _dbContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
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

    public async Task MarkAsSent(
        Guid id,
        DateTime sentAt,
        string? providerMessageId,
        CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchOutbox
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
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

    public async Task RecordAttempt(EmailDispatchAttempt attempt, CancellationToken cancellationToken)
    {
        await _dbContext.EmailDispatchAttempts.AddAsync(attempt, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryClaimReceipt(EmailDispatchReceipt receipt, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.EmailDispatchReceipts
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
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
            .IgnoreQueryFilters()
            .Where(e => e.Id == receiptId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(e => e.Status, EmailDispatchReceiptStatus.Failed)
                .SetProperty(e => e.FailedAt, failedAt)
                .SetProperty(e => e.FailureCode, Truncate(failureCode, 100))
                .SetProperty(e => e.FailureMessage, Truncate(failureMessage, MaxReceiptFailureLength))
                .SetProperty(e => e.UpdatedAt, failedAt), cancellationToken);
    }

    private static string? Truncate(string? value, int maxLength)
    {
        return value is null || value.Length <= maxLength ? value : value[..maxLength];
    }
}
