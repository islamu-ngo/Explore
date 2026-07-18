// ABOUTME: Persistence contract for specialized email dispatch outbox state, attempts, and receipts.
// ABOUTME: Keeps SMTP dispatch state machine in Application while EF Core implementation stays in Persistence.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEmailDispatchOutboxRepository
{
    Task<EmailDispatchOutbox> Create(EmailDispatchOutbox entity, CancellationToken cancellationToken);

    Task<IReadOnlyList<EmailDispatchOutbox>> GetPendingBatch(
        int batchSize,
        DateTime now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmailDispatchOutbox>> GetPendingBatch(
        int batchSize,
        int maxRowsPerTenant,
        bool includeOptionalReminders,
        DateTime now,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EmailDispatchOutbox>> GetRabbitMqPublishBatch(
        int batchSize,
        DateTime now,
        DateTime retryAttemptsBefore,
        CancellationToken cancellationToken);

    Task<int> CountDueDispatchAsync(
        DateTime now,
        CancellationToken cancellationToken);

    Task<DateTime?> GetOldestDueCreatedAtAsync(
        DateTime now,
        CancellationToken cancellationToken);

    Task<IReadOnlyDictionary<Guid, int>> CountDueDispatchByTenantAsync(
        DateTime now,
        int tenantLimit,
        CancellationToken cancellationToken);

    Task<int> CountRetryScheduledAsync(CancellationToken cancellationToken);

    Task<int> CountStaleProcessingAsync(
        DateTime processingStartedBefore,
        CancellationToken cancellationToken);

    Task<int> CountDeadLetteredAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<EmailDispatchOutbox>> GetStatusRows(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken);

    Task<EmailDispatchOutbox?> GetByTenantAndId(
        Guid tenantId,
        Guid outboxId,
        CancellationToken cancellationToken);

    Task<EmailDispatchOutbox?> GetByTenantAndPublishEventId(
        Guid tenantId,
        Guid publishEventId,
        CancellationToken cancellationToken);

    Task<bool> IsTenantPaused(Guid tenantId, CancellationToken cancellationToken);

    Task<EmailDispatchTenantControl> SetTenantPauseState(
        Guid tenantId,
        bool isPaused,
        string? pauseReason,
        Guid? changedBy,
        DateTime changedAt,
        CancellationToken cancellationToken);

    Task<bool> TryParkForOperator(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime parkedAt,
        CancellationToken cancellationToken);

    Task<bool> TryReplayForOperator(
        Guid tenantId,
        Guid outboxId,
        Guid? changedBy,
        DateTime replayAt,
        CancellationToken cancellationToken);

    Task<bool> TryResolveWithoutReplay(
        Guid tenantId,
        Guid outboxId,
        string reason,
        Guid? changedBy,
        DateTime resolvedAt,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> GetRetentionTenantIds(
        DateTime cutoffUtc,
        int maxTenants,
        CancellationToken cancellationToken);

    Task<int> CountRetentionRedactionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        int batchSize,
        CancellationToken cancellationToken);

    Task<int> RedactRetentionEligible(
        Guid tenantId,
        DateTime cutoffUtc,
        DateTime redactedAt,
        int batchSize,
        CancellationToken cancellationToken);

    Task<int> SuppressAndRedactTenant(
        Guid tenantId,
        Guid? changedBy,
        DateTime redactedAt,
        CancellationToken cancellationToken);

    Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
        CancellationToken cancellationToken);

    Task<int> MarkStaleProcessingAsUnknown(
        DateTime processingStartedBefore,
        DateTime recoveredAt,
        string failureCategory,
        string errorMessage,
        int batchSize,
        CancellationToken cancellationToken);

    Task MarkAsSent(
        Guid id,
        DateTime sentAt,
        string? providerMessageId,
        CancellationToken cancellationToken);

    Task MarkAsFailed(
        Guid id,
        string failureCategory,
        string errorMessage,
        bool isRetryable,
        TimeSpan retryDelay,
        int maxAttempts,
        DateTime failedAt,
        CancellationToken cancellationToken);

    Task MarkAsUnknown(
        Guid id,
        string failureCategory,
        string errorMessage,
        DateTime unknownAt,
        CancellationToken cancellationToken);

    Task MarkAsSkipped(
        Guid id,
        string reasonCategory,
        string reasonMessage,
        DateTime skippedAt,
        CancellationToken cancellationToken);

    Task MarkRabbitMqPublishSucceeded(
        Guid id,
        DateTime publishedAt,
        CancellationToken cancellationToken);

    Task MarkRabbitMqPublishFailed(
        Guid id,
        string failureCategory,
        DateTime attemptedAt,
        CancellationToken cancellationToken);

    Task RecordAttempt(EmailDispatchAttempt attempt, CancellationToken cancellationToken);

    Task SettleProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken);

    Task<EmailDispatchAcceptedReconciliationOutcome> ReconcileProviderAccepted(
        EmailDispatchAcceptedSettlement settlement,
        CancellationToken cancellationToken);

    Task<bool> TryClaimReceipt(EmailDispatchReceipt receipt, CancellationToken cancellationToken);

    Task MarkReceiptCompleted(
        Guid receiptId,
        DateTime completedAt,
        string? providerMessageId,
        CancellationToken cancellationToken);

    Task MarkReceiptFailed(
        Guid receiptId,
        string failureCode,
        string failureMessage,
        DateTime failedAt,
        CancellationToken cancellationToken);

    Task MarkReceiptSkipped(
        Guid receiptId,
        string reasonCode,
        string reasonMessage,
        DateTime skippedAt,
        CancellationToken cancellationToken);
}

public sealed record EmailDispatchAcceptedSettlement(
    Guid TenantId,
    Guid OutboxId,
    int AttemptNumber,
    DateTime SettledAt,
    string? ProviderMessageId);

public enum EmailDispatchAcceptedReconciliationOutcome
{
    Sent = 1,
    Unknown = 2
}
