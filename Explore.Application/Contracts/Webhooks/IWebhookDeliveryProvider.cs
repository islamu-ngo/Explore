// ABOUTME: Delivery-provider abstraction for outgoing product webhooks.
// ABOUTME: Lets Local, Svix, DryRun, Disabled, and Composite providers share one application boundary.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookDeliveryProvider
{
    string ProviderName { get; }

    Task<WebhookProviderPublishResult> PublishAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken);
}

