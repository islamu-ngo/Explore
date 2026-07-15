// ABOUTME: Application contract for resolving bounded Local webhook delivery policy.
// ABOUTME: Exposes lock-aware tenant governance without leaking settings infrastructure into workers.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookDeliveryGovernanceResolver
{
    Task<WebhookDeliveryGovernancePolicy> ResolveAsync(
        Guid? tenantId,
        CancellationToken cancellationToken = default);
}

public sealed record WebhookDeliveryGovernancePolicy(
    int GlobalInFlightLimit,
    int MaxInFlightPerTenant,
    int MaxInFlightPerEndpoint,
    int MaxItemsPerTenantPerClaimCycle,
    int MaxAttempts,
    int EndpointTimeoutSeconds,
    int AutoPauseThreshold,
    string ResolutionVersion);
