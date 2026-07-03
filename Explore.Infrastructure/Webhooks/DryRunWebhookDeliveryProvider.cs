// ABOUTME: Dry-run webhook provider that accepts messages without network delivery.
// ABOUTME: Supports development and tests while preserving provider publish semantics.

using Explore.Application.Contracts.Webhooks;

namespace Explore.Infrastructure.Webhooks;

public sealed class DryRunWebhookDeliveryProvider : IWebhookDeliveryProvider
{
    public string ProviderName => "DryRun";

    public Task<WebhookProviderPublishResult> PublishAsync(
        WebhookProviderMessage message,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Task.FromResult(WebhookProviderPublishResult.Success($"dryrun:{message.MessageId:N}"));
    }
}
