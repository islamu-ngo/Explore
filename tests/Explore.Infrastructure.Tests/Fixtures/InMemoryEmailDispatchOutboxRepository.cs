// ABOUTME: In-memory EmailDispatch outbox repository for infrastructure drain and consumer tests.
// ABOUTME: Models the success-path state transitions needed by Mailpit and RabbitMQ runtime tests.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Infrastructure.Tests.Fixtures;

public sealed class InMemoryEmailDispatchOutboxRepository(EmailDispatchOutbox dispatch)
    : IEmailDispatchOutboxRepository
{
    private readonly object _gate = new();

    public EmailDispatchOutbox Dispatch => dispatch;

    public List<EmailDispatchAttempt> Attempts { get; } = [];

    public List<EmailDispatchReceipt> Receipts { get; } = [];

    public int ReplayCount { get; private set; }

    public Task<EmailDispatchOutbox> Create(EmailDispatchOutbox entity, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<EmailDispatchOutbox>> GetPendingBatch(
        int batchSize,
        DateTime now,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<EmailDispatchOutbox> rows = dispatch.Status == EmailDispatchStatus.Pending
                ? [dispatch]
                : [];
            return Task.FromResult(rows);
        }
    }

    public Task<IReadOnlyList<EmailDispatchOutbox>> GetPendingBatch(
        int batchSize,
        int maxRowsPerTenant,
        bool includeOptionalReminders,
        DateTime now,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<EmailDispatchOutbox> rows = dispatch.Status == EmailDispatchStatus.Pending &&
                (includeOptionalReminders || dispatch.Kind != EmailDispatchKind.EventReminder)
                    ? [dispatch]
                    : [];
            return Task.FromResult(rows);
        }
    }

    public Task<IReadOnlyList<EmailDispatchOutbox>> GetRabbitMqPublishBatch(
        int batchSize,
        DateTime now,
        DateTime retryAttemptsBefore,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<IReadOnlyList<EmailDispatchOutbox>> GetStatusRows(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<EmailDispatchOutbox?> GetByTenantAndId(
        Guid tenantId,
        Guid outboxId,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<EmailDispatchOutbox?> GetByTenantAndPublishEventId(
        Guid tenantId,
        Guid publishEventId,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(dispatch.TenantId == tenantId && dispatch.PublishEventId == publishEventId
                ? dispatch
                : null);
        }
    }

    public Task<int> CountDueDispatchAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var isDue = dispatch.Status is EmailDispatchStatus.Pending or EmailDispatchStatus.RetryScheduled
                && (dispatch.NextAttemptAt is null || dispatch.NextAttemptAt <= now);

            return Task.FromResult(isDue ? 1 : 0);
        }
    }

    public Task<DateTime?> GetOldestDueCreatedAtAsync(DateTime now, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            DateTime? createdAt = dispatch.Status is EmailDispatchStatus.Pending or EmailDispatchStatus.RetryScheduled
                && (dispatch.NextAttemptAt is null || dispatch.NextAttemptAt <= now)
                    ? dispatch.CreatedAt
                    : null;
            return Task.FromResult(createdAt);
        }
    }

    public Task<IReadOnlyDictionary<Guid, int>> CountDueDispatchByTenantAsync(
        DateTime now,
        int tenantLimit,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var isDue = dispatch.Status is EmailDispatchStatus.Pending or EmailDispatchStatus.RetryScheduled
                && (dispatch.NextAttemptAt is null || dispatch.NextAttemptAt <= now);
            IReadOnlyDictionary<Guid, int> result = isDue
                ? new Dictionary<Guid, int> { [dispatch.TenantId] = 1 }
                : new Dictionary<Guid, int>();
            return Task.FromResult(result);
        }
    }

    public Task<int> CountRetryScheduledAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(dispatch.Status == EmailDispatchStatus.RetryScheduled ? 1 : 0);
        }
    }

    public Task<int> CountStaleProcessingAsync(
        DateTime processingStartedBefore,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var isStaleProcessing = dispatch.Status == EmailDispatchStatus.Processing
                && dispatch.ProcessingStartedAt is not null
                && dispatch.ProcessingStartedAt <= processingStartedBefore;

            return Task.FromResult(isStaleProcessing ? 1 : 0);
        }
    }

    public Task<int> CountDeadLetteredAsync(CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            return Task.FromResult(dispatch.Status == EmailDispatchStatus.DeadLettered ? 1 : 0);
        }
    }

    public Task<bool> IsTenantPaused(Guid tenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult(false);
    }

    public Task<EmailDispatchTenantControl> SetTenantPauseState(
        Guid tenantId,
        bool isPaused,
        string? pauseReason,
        Guid? changedBy,
        DateTime changedAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<bool> TryParkForOperator(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime parkedAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task<bool> TryReplayForOperator(
        Guid tenantId,
        Guid outboxId,
        Guid? changedBy,
        DateTime replayAt,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (dispatch.TenantId != tenantId ||
                dispatch.Id != outboxId ||
                dispatch.ContentRedactedAt is not null)
            {
                return Task.FromResult(false);
            }

            if (dispatch.Status is not (EmailDispatchStatus.DeadLettered
                or EmailDispatchStatus.Parked
                or EmailDispatchStatus.Unknown
                or EmailDispatchStatus.RetryScheduled))
            {
                return Task.FromResult(false);
            }

            dispatch.Status = EmailDispatchStatus.Pending;
            dispatch.NextAttemptAt = null;
            dispatch.ProcessingStartedAt = null;
            dispatch.ProcessingLeaseToken = null;
            dispatch.DeadLetteredAt = null;
            dispatch.ParkedAt = null;
            dispatch.UnknownAt = null;
            dispatch.LastFailureCategory = null;
            dispatch.LastError = null;
            dispatch.UpdatedAt = replayAt;
            dispatch.UpdatedBy = changedBy;
            ReplayCount++;
            return Task.FromResult(true);
        }
    }

    public Task<bool> TryResolveWithoutReplay(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime resolvedAt,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (dispatch.TenantId != tenantId ||
                dispatch.Id != outboxId ||
                dispatch.ContentRedactedAt is not null ||
                dispatch.Status is not (EmailDispatchStatus.DeadLettered
                    or EmailDispatchStatus.Parked
                    or EmailDispatchStatus.Unknown))
            {
                return Task.FromResult(false);
            }

            dispatch.Status = EmailDispatchStatus.Skipped;
            dispatch.LastFailureCategory = "operator_resolved_without_replay";
            dispatch.LastError = reason;
            dispatch.LastFailureAt = resolvedAt;
            dispatch.UpdatedAt = resolvedAt;
            dispatch.UpdatedBy = changedBy;
            return Task.FromResult(true);
        }
    }

    public Task<IReadOnlyList<Guid>> GetRetentionTenantIds(
        DateTime cutoffUtc,
        int maxTenants,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            IReadOnlyList<Guid> tenantIds = maxTenants > 0 && IsRetentionEligible(cutoffUtc)
                ? [dispatch.TenantId]
                : [];
            return Task.FromResult(tenantIds);
        }
    }

    public Task<int> CountRetentionRedactionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var isEligible = dispatch.TenantId == tenantId && batchSize > 0 && IsRetentionEligible(cutoffUtc);
            return Task.FromResult(isEligible ? 1 : 0);
        }
    }

    public Task<int> RedactRetentionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        DateTime redactedAt,
        int batchSize,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var isEligible = dispatch.TenantId == tenantId && batchSize > 0 && IsRetentionEligible(cutoffUtc);
            if (!isEligible)
            {
                return Task.FromResult(0);
            }

            dispatch.RecipientEmail = string.Empty;
            dispatch.Subject = string.Empty;
            dispatch.PlainTextBody = null;
            dispatch.HtmlBody = null;
            dispatch.ReplyTo = null;
            dispatch.LastError = null;
            dispatch.ProviderMessageId = null;
            dispatch.CorrelationId = null;
            dispatch.ContentRedactedAt = redactedAt;
            dispatch.UpdatedAt = redactedAt;

            foreach (var attempt in Attempts)
            {
                attempt.Provider = null;
                attempt.SanitizedErrorMessage = null;
                attempt.ProviderMessageId = null;
                attempt.CorrelationId = null;
            }

            foreach (var receipt in Receipts)
            {
                receipt.ConsumerId = null;
                receipt.FailureMessage = null;
                receipt.ProviderMessageId = null;
            }

            return Task.FromResult(1);
        }
    }

    public Task<int> SuppressAndRedactTenant(
        Guid tenantId,
        Guid? changedBy,
        DateTime redactedAt,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (dispatch.TenantId != tenantId || dispatch.ContentRedactedAt is not null)
            {
                return Task.FromResult(0);
            }

            if (dispatch.Status is EmailDispatchStatus.Pending or EmailDispatchStatus.RetryScheduled or EmailDispatchStatus.Processing)
            {
                dispatch.Status = EmailDispatchStatus.Skipped;
                dispatch.LastFailureCategory = "tenant_deleted";
                dispatch.LastFailureAt = redactedAt;
            }

            dispatch.RecipientEmail = string.Empty;
            dispatch.Subject = string.Empty;
            dispatch.PlainTextBody = null;
            dispatch.HtmlBody = null;
            dispatch.ReplyTo = null;
            dispatch.LastError = null;
            dispatch.ProviderMessageId = null;
            dispatch.CorrelationId = null;
            dispatch.NextAttemptAt = null;
            dispatch.ProcessingStartedAt = null;
            dispatch.ProcessingLeaseToken = null;
            dispatch.ContentRedactedAt = redactedAt;
            dispatch.UpdatedAt = redactedAt;
            dispatch.UpdatedBy = changedBy;

            foreach (var attempt in Attempts)
            {
                attempt.Provider = null;
                attempt.SanitizedErrorMessage = null;
                attempt.ProviderMessageId = null;
                attempt.CorrelationId = null;
            }

            foreach (var receipt in Receipts)
            {
                receipt.Status = receipt.Status == EmailDispatchReceiptStatus.Completed
                    ? EmailDispatchReceiptStatus.Completed
                    : EmailDispatchReceiptStatus.Skipped;
                receipt.ConsumerId = null;
                receipt.FailureMessage = null;
                receipt.ProviderMessageId = null;
            }

            return Task.FromResult(1);
        }
    }

    public Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (id != dispatch.Id || dispatch.Status != EmailDispatchStatus.Pending)
            {
                return Task.FromResult(false);
            }

            dispatch.Status = EmailDispatchStatus.Processing;
            dispatch.ProcessingLeaseToken = leaseToken;
            dispatch.ProcessingStartedAt = startedAt;
            return Task.FromResult(true);
        }
    }

    public Task<int> MarkStaleProcessingAsUnknown(
        DateTime processingStartedBefore,
        DateTime recoveredAt,
        string failureCategory,
        string errorMessage,
        int batchSize,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (batchSize <= 0 ||
                dispatch.Status != EmailDispatchStatus.Processing ||
                dispatch.ProcessingStartedAt is null ||
                dispatch.ProcessingStartedAt > processingStartedBefore)
            {
                return Task.FromResult(0);
            }

            dispatch.Status = EmailDispatchStatus.Unknown;
            dispatch.UnknownAt = recoveredAt;
            dispatch.NextAttemptAt = null;
            dispatch.ProcessingStartedAt = null;
            dispatch.ProcessingLeaseToken = null;
            dispatch.LastFailureCategory = failureCategory;
            dispatch.LastError = errorMessage;

            foreach (var attempt in Attempts.Where(value =>
                         value.EmailDispatchOutboxId == dispatch.Id &&
                         value.AttemptNumber == dispatch.AttemptCount))
            {
                attempt.Outcome = EmailDispatchAttemptOutcome.Unknown;
                attempt.CompletedAt = recoveredAt;
                attempt.FailureCategory = failureCategory;
                attempt.SanitizedErrorMessage = errorMessage;
                attempt.ProviderMessageId = null;
            }

            foreach (var receipt in Receipts.Where(value => value.EmailDispatchOutboxId == dispatch.Id))
            {
                receipt.Status = EmailDispatchReceiptStatus.Unknown;
                receipt.CompletedAt = null;
                receipt.FailedAt = recoveredAt;
                receipt.FailureCode = failureCategory;
                receipt.FailureMessage = errorMessage;
                receipt.ProviderMessageId = null;
            }

            return Task.FromResult(1);
        }
    }

    public Task MarkAsSent(
        Guid id,
        DateTime sentAt,
        string? providerMessageId,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            dispatch.Status = EmailDispatchStatus.Sent;
            dispatch.SentAt = sentAt;
            dispatch.ProviderMessageId = providerMessageId;
            dispatch.AttemptCount++;
            dispatch.ProcessingLeaseToken = null;
            dispatch.ProcessingStartedAt = null;
        }

        return Task.CompletedTask;
    }

    public Task MarkAsFailed(
        Guid id,
        string failureCategory,
        string errorMessage,
        bool isRetryable,
        TimeSpan retryDelay,
        int maxAttempts,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkAsUnknown(
        Guid id,
        string failureCategory,
        string errorMessage,
        DateTime unknownAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkAsSkipped(
        Guid id,
        string reasonCategory,
        string reasonMessage,
        DateTime skippedAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkRabbitMqPublishSucceeded(Guid id, DateTime publishedAt, CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkRabbitMqPublishFailed(
        Guid id,
        string failureCategory,
        DateTime attemptedAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task RecordAttempt(EmailDispatchAttempt attempt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var existing = Attempts.SingleOrDefault(value =>
                value.EmailDispatchOutboxId == attempt.EmailDispatchOutboxId &&
                value.AttemptNumber == attempt.AttemptNumber);
            if (existing is null)
            {
                attempt.Id = attempt.Id == Guid.Empty ? Guid.CreateVersion7() : attempt.Id;
                Attempts.Add(attempt);
            }
            else
            {
                existing.Outcome = attempt.Outcome;
                existing.CompletedAt = attempt.CompletedAt;
                existing.FailureCategory = attempt.FailureCategory;
                existing.SanitizedErrorMessage = attempt.SanitizedErrorMessage;
                existing.ProviderMessageId = attempt.ProviderMessageId;
            }
        }

        return Task.CompletedTask;
    }

    public Task SettleProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var attempt = Attempts.Single(value =>
                value.EmailDispatchOutboxId == settlement.OutboxId &&
                value.AttemptNumber == settlement.AttemptNumber);
            attempt.Outcome = EmailDispatchAttemptOutcome.Succeeded;
            attempt.CompletedAt = settlement.SettledAt;
            attempt.FailureCategory = null;
            attempt.SanitizedErrorMessage = null;
            attempt.ProviderMessageId = settlement.ProviderMessageId;

            dispatch.Status = EmailDispatchStatus.Sent;
            dispatch.SentAt = settlement.SettledAt;
            dispatch.ProviderMessageId = settlement.ProviderMessageId;
            dispatch.ProcessingLeaseToken = null;
            dispatch.ProcessingStartedAt = null;

            var receipt = Receipts.Single(value => value.EmailDispatchOutboxId == settlement.OutboxId);
            receipt.Status = EmailDispatchReceiptStatus.Completed;
            receipt.CompletedAt = settlement.SettledAt;
            receipt.ProviderMessageId = settlement.ProviderMessageId;
        }

        return Task.CompletedTask;
    }

    public Task<EmailDispatchAcceptedReconciliationOutcome> ReconcileProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (dispatch.Status == EmailDispatchStatus.Sent &&
                Attempts.Any(value => value.EmailDispatchOutboxId == settlement.OutboxId &&
                    value.AttemptNumber == settlement.AttemptNumber &&
                    value.Outcome == EmailDispatchAttemptOutcome.Succeeded) &&
                Receipts.Any(value => value.EmailDispatchOutboxId == settlement.OutboxId &&
                    value.Status == EmailDispatchReceiptStatus.Completed))
            {
                return Task.FromResult(EmailDispatchAcceptedReconciliationOutcome.Sent);
            }

            var attempt = Attempts.Single(value =>
                value.EmailDispatchOutboxId == settlement.OutboxId &&
                value.AttemptNumber == settlement.AttemptNumber);
            attempt.Outcome = EmailDispatchAttemptOutcome.Unknown;
            attempt.CompletedAt = settlement.SettledAt;
            attempt.FailureCategory = "accepted_settlement_unknown";
            attempt.SanitizedErrorMessage = "SMTP accepted the message, but local settlement is uncertain. Automatic resend is disabled pending reconciliation.";
            attempt.ProviderMessageId = null;

            dispatch.Status = EmailDispatchStatus.Unknown;
            dispatch.SentAt = null;
            dispatch.UnknownAt = settlement.SettledAt;
            dispatch.ProviderMessageId = null;
            dispatch.NextAttemptAt = null;
            dispatch.ProcessingLeaseToken = null;
            dispatch.ProcessingStartedAt = null;

            var receipt = Receipts.Single(value => value.EmailDispatchOutboxId == settlement.OutboxId);
            receipt.Status = EmailDispatchReceiptStatus.Unknown;
            receipt.CompletedAt = null;
            receipt.FailedAt = settlement.SettledAt;
            receipt.ProviderMessageId = null;
            receipt.FailureCode = "accepted_settlement_unknown";
        }

        return Task.FromResult(EmailDispatchAcceptedReconciliationOutcome.Unknown);
    }

    public Task<bool> TryClaimReceipt(EmailDispatchReceipt receipt, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            if (Receipts.Any(value => value.PublishEventId == receipt.PublishEventId))
            {
                return Task.FromResult(false);
            }

            receipt.Id = receipt.Id == Guid.Empty ? Guid.CreateVersion7() : receipt.Id;
            Receipts.Add(receipt);
            return Task.FromResult(true);
        }
    }

    public Task MarkReceiptCompleted(
        Guid receiptId,
        DateTime completedAt,
        string? providerMessageId,
        CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            var receipt = Receipts.Single(value => value.Id == receiptId);
            receipt.Status = EmailDispatchReceiptStatus.Completed;
            receipt.CompletedAt = completedAt;
            receipt.ProviderMessageId = providerMessageId;
        }

        return Task.CompletedTask;
    }

    public Task MarkReceiptFailed(
        Guid receiptId,
        string failureCode,
        string failureMessage,
        DateTime failedAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    public Task MarkReceiptSkipped(
        Guid receiptId,
        string reasonCode,
        string reasonMessage,
        DateTime skippedAt,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException();
    }

    private bool IsRetentionEligible(DateTime cutoffUtc) =>
        dispatch.ContentRedactedAt is null &&
        (dispatch.Status == EmailDispatchStatus.Sent && dispatch.SentAt <= cutoffUtc ||
         dispatch.Status == EmailDispatchStatus.Skipped && dispatch.LastFailureAt <= cutoffUtc);
}
