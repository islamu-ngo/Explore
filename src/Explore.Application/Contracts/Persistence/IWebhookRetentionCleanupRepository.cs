// ABOUTME: Persistence boundary for bounded tenant-scoped webhook retention cleanup.
// ABOUTME: Returns aggregate counts while preserving terminal identities, hashes, outcomes, and held evidence.

namespace Explore.Application.Contracts.Persistence;

public sealed record WebhookRetentionCleanupResult(
    int OutboundPayloadsCleared,
    int InboundPayloadsCleared,
    int DeliveryAttemptsDeleted,
    int IncomingAttemptsDeleted,
    int IncomingRedriveRecordsDeleted,
    int ProviderAttemptsDeleted,
    int ProviderPublicationsDeleted,
    int AdministrativeAuditsDeleted,
    bool DryRun)
{
    public int TotalAffected =>
        OutboundPayloadsCleared +
        InboundPayloadsCleared +
        DeliveryAttemptsDeleted +
        IncomingAttemptsDeleted +
        IncomingRedriveRecordsDeleted +
        ProviderAttemptsDeleted +
        ProviderPublicationsDeleted +
        AdministrativeAuditsDeleted;
}

public interface IWebhookRetentionCleanupRepository
{
    Task<WebhookRetentionCleanupResult> CleanupTenantAsync(
        Guid tenantId,
        DateTime utcNow,
        int batchSize,
        bool dryRun,
        CancellationToken cancellationToken);
}
