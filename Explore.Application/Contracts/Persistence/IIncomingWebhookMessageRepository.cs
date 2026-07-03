// ABOUTME: Repository contract for verified incoming webhook callback idempotency rows.
// ABOUTME: Ensures provider callbacks are captured before outbox-backed side effects mutate aggregates.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IIncomingWebhookMessageRepository
{
    Task<bool> TryCreateAsync(IncomingWebhookMessage message, CancellationToken cancellationToken);

    Task<IncomingWebhookMessage?> GetByProviderMessageIdAsync(
        Guid tenantId,
        string provider,
        string providerMessageId,
        CancellationToken cancellationToken);

    Task MarkProcessedAsync(
        Guid tenantId,
        Guid messageId,
        DateTime processedAt,
        CancellationToken cancellationToken);

    Task MarkRejectedAsync(
        Guid tenantId,
        Guid messageId,
        string failureCategory,
        string? safeDetail,
        DateTime rejectedAt,
        CancellationToken cancellationToken);
}
