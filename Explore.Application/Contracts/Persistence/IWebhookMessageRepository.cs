// ABOUTME: Repository contract for canonical outgoing webhook messages and provider publish state.
// ABOUTME: Enables outbox-backed creation, provider switching, retention cleanup, and safe tenant status queries.

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

    Task MarkProviderQueuedAsync(
        Guid tenantId,
        Guid messageId,
        string? providerMessageId,
        DateTime queuedAt,
        CancellationToken cancellationToken);

    Task MarkProviderFailedAsync(
        Guid tenantId,
        Guid messageId,
        DateTime failedAt,
        CancellationToken cancellationToken);

    Task RefreshLocalDeliveryStatusAsync(
        Guid tenantId,
        Guid messageId,
        DateTime refreshedAt,
        CancellationToken cancellationToken);

    Task<int> ClearExpiredPayloadsAsync(
        DateTime now,
        int batchSize,
        CancellationToken cancellationToken);
}
