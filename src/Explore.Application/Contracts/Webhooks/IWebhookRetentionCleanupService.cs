// ABOUTME: Application boundary for scheduled webhook retention cleanup across bounded active tenants.
// ABOUTME: Exposes aggregate evidence without tenant identifiers or sensitive webhook content.

using Explore.Application.Contracts.Persistence;

namespace Explore.Application.Contracts.Webhooks;

public sealed record WebhookRetentionCleanupRunResult(
    int TenantCount,
    int SucceededTenantCount,
    int FailedTenantCount,
    WebhookRetentionCleanupResult Aggregate);

public interface IWebhookRetentionCleanupService
{
    Task<WebhookRetentionCleanupRunResult> CleanupAllTenantsAsync(
        DateTime utcNow,
        CancellationToken cancellationToken = default);
}
