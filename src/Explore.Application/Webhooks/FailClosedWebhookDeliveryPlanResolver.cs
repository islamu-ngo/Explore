// ABOUTME: Default outgoing webhook plan resolver used until verified binding and capability resolution is registered.
// ABOUTME: Produces no runnable provider or Local work when authoritative routing facts are unavailable.

using Explore.Application.Contracts.Webhooks;

namespace Explore.Application.Webhooks;

public sealed class FailClosedWebhookDeliveryPlanResolver : IWebhookDeliveryPlanResolver
{
    public Task<WebhookDeliveryPlanResolution> ResolveAsync(
        WebhookEventBuildContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(WebhookDeliveryPlanResolution.Unavailable(
            "webhook_delivery_plan_unavailable",
            "Verified webhook delivery-plan resolution is not configured."));
    }
}
