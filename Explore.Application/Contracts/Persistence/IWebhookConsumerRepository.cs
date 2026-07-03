// ABOUTME: Repository contract for tenant-scoped webhook consumers and provider app mappings.
// ABOUTME: Returns domain entities only so handlers own mapping, authorization, and HAL shaping.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IWebhookConsumerRepository
{
    Task<WebhookConsumer> CreateAsync(WebhookConsumer consumer, CancellationToken cancellationToken);

    Task<WebhookConsumer?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid consumerId,
        CancellationToken cancellationToken);

    Task<WebhookConsumer?> GetByTenantAndNameAsync(
        Guid tenantId,
        string name,
        CancellationToken cancellationToken);

    Task<WebhookConsumer?> GetByExternalProviderAppIdAsync(
        Guid tenantId,
        string externalProviderAppId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookConsumer>> ListByTenantAsync(
        Guid tenantId,
        int limit,
        CancellationToken cancellationToken);
}
