// ABOUTME: Result contract for canonical webhook event type catalog synchronization.
// ABOUTME: Reports created, updated, and unchanged rows for startup diagnostics and operations logging.

namespace Explore.Application.Contracts.Webhooks;

public sealed record WebhookEventTypeCatalogSyncResult(
    int CreatedCount,
    int UpdatedCount,
    int UnchangedCount);
