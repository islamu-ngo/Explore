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
            if (dispatch.TenantId != tenantId || dispatch.Id != outboxId)
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
        throw new NotSupportedException();
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
            attempt.Id = attempt.Id == Guid.Empty ? Guid.CreateVersion7() : attempt.Id;
            Attempts.Add(attempt);
        }

        return Task.CompletedTask;
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
}
