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

    Task<IReadOnlyList<EmailDispatchOutbox>> GetStatusRows(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken);

    Task<bool> IsTenantPaused(Guid tenantId, CancellationToken cancellationToken);

    Task<bool> TryMarkAsProcessing(
        Guid id,
        Guid leaseToken,
        DateTime startedAt,
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

    Task RecordAttempt(EmailDispatchAttempt attempt, CancellationToken cancellationToken);

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
}
