// ABOUTME: Application service contract for synchronizing canonical webhook event types into persistence.
// ABOUTME: Keeps Local and Svix provider catalogs aligned without making catalog reads mutate state.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookEventTypeCatalogSyncService
{
    Task<WebhookEventTypeCatalogSyncResult> SyncAsync(CancellationToken cancellationToken);
}
