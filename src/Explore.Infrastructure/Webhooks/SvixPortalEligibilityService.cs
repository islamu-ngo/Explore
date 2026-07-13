// ABOUTME: Batch eligibility service for Svix provider portal HAL affordances.
// ABOUTME: Fails closed on runtime, persistence, binding, version, or governance uncertainty.

using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Webhooks;
using Explore.Domain;
using Explore.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Explore.Infrastructure.Webhooks;

public sealed class SvixPortalEligibilityService(
    IWebhookConsumerProviderBindingRepository bindingRepository,
    IOptionsMonitor<WebhookOptions> options,
    ILogger<SvixPortalEligibilityService> logger) : IWebhookProviderPortalEligibilityService
{
    public async Task<IReadOnlySet<Guid>> GetEligibleConsumerIdsAsync(
        Guid tenantId,
        IReadOnlyCollection<Guid> consumerIds,
        CancellationToken cancellationToken)
    {
        if (tenantId == Guid.Empty || consumerIds.Count == 0)
        {
            return new HashSet<Guid>();
        }

        var currentOptions = options.CurrentValue;
        if (!SvixPortalAuthorityPolicy.IsRuntimeEnabled(currentOptions))
        {
            return new HashSet<Guid>();
        }

        try
        {
            var bindings = await bindingRepository.GetVerifiedByConsumersAsync(
                tenantId,
                consumerIds,
                WebhookProviderKind.Svix,
                currentOptions.Svix.Environment,
                cancellationToken);

            return bindings
                .Where(binding => SvixPortalAuthorityPolicy.AllowsBinding(
                    binding,
                    currentOptions,
                    tenantId,
                    binding.WebhookConsumerId))
                .Select(binding => binding.WebhookConsumerId)
                .ToHashSet();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Provider portal eligibility lookup failed for tenant {TenantId}; omitting portal affordances.",
                tenantId);
            return new HashSet<Guid>();
        }
    }
}
