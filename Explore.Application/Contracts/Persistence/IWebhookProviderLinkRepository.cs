// ABOUTME: Repository contract for external webhook provider object links.
// ABOUTME: Stores Svix app, endpoint, and message ids without making provider ids primary state.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IWebhookProviderLinkRepository
{
    Task<WebhookProviderLink> CreateAsync(WebhookProviderLink link, CancellationToken cancellationToken);

    Task<WebhookProviderLink?> GetByExternalMessageIdAsync(
        Guid tenantId,
        WebhookExternalProvider provider,
        string externalMessageId,
        CancellationToken cancellationToken);

    Task<WebhookProviderLink?> GetByTenantMessageAndProviderAsync(
        Guid tenantId,
        WebhookExternalProvider provider,
        Guid messageId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<WebhookProviderLink>> GetPendingByProviderAsync(
        WebhookExternalProvider provider,
        int limit,
        CancellationToken cancellationToken);
}
