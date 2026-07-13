// ABOUTME: Application contract for batch-evaluating server-owned provider portal eligibility.
// ABOUTME: Returns only eligible consumer identifiers so HAL remains the sole client affordance authority.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookProviderPortalEligibilityService
{
    Task<IReadOnlySet<Guid>> GetEligibleConsumerIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> consumerIds,
        CancellationToken cancellationToken);
}
