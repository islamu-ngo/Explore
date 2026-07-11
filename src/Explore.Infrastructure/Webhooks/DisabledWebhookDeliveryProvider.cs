// ABOUTME: Disabled outgoing webhook provider used when product webhook delivery is turned off.
// ABOUTME: Returns a non-retryable provider result so outbox dispatchers do not keep retrying disabled work.

using Explore.Application.Contracts.Webhooks;

namespace Explore.Infrastructure.Webhooks;

public sealed class DisabledWebhookDeliveryProvider : IWebhookDeliveryProvider
{
    public string ProviderName => "Disabled";

    public Task<WebhookProviderPublishResult> PublishAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(WebhookProviderPublishResult.Failure(
            "webhooks_disabled",
            isRetryable: false,
            "Outgoing product webhooks are disabled."));
    }
}
