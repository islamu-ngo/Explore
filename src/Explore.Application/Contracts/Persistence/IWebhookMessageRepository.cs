// ABOUTME: Repository contract for immutable canonical outgoing webhook messages.
// ABOUTME: Enables idempotent creation, tenant-scoped reads, and payload-retention cleanup.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IWebhookMessageRepository
{
    Task<WebhookMessage> CreateAsync(WebhookMessage message, CancellationToken cancellationToken);

    Task<WebhookMessage?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid messageId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookMessage>> ListByTenantAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken);

    Task<int> ClearExpiredPayloadsAsync(
        DateTime now,
        int batchSize,
        CancellationToken cancellationToken);
}
