// ABOUTME: Repository contract for authoritative provider-publication aggregates and identity lookup.
// ABOUTME: Returns domain entities so transition rules remain owned by the aggregate and handlers.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IWebhookProviderPublicationRepository
{
    Task<WebhookProviderPublication> CreateAsync(
        WebhookProviderPublication publication,
        CancellationToken cancellationToken);

    Task<WebhookProviderPublication?> GetByIdentityAsync(
        Guid tenantId,
        Guid webhookMessageId,
        WebhookProviderKind providerKind,
        Guid providerBindingId,
        CancellationToken cancellationToken);

    Task<WebhookProviderPublication?> GetByTenantAndIdAsync(
        Guid tenantId,
        Guid publicationId,
        CancellationToken cancellationToken);

    Task<WebhookProviderPublication> UpdateAsync(
        WebhookProviderPublication publication,
        CancellationToken cancellationToken);
}
