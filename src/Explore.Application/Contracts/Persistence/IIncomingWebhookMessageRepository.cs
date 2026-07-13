// ABOUTME: Repository contract for verified incoming webhook callback idempotency rows.
// ABOUTME: Ensures provider callbacks are captured before outbox-backed side effects mutate aggregates.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IIncomingWebhookMessageRepository
{
    Task<bool> TryCreateAsync(IncomingWebhookMessage message, CancellationToken cancellationToken);

    Task<IncomingWebhookMessage?> GetByProviderMessageIdForUpdateAsync(
        Guid tenantId,
        string provider,
        string providerMessageId,
        CancellationToken cancellationToken);

    Task SaveChangesAsync(CancellationToken cancellationToken);
}
