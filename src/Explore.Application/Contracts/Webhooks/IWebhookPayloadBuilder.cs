// ABOUTME: Builds stable, minimized webhook envelopes from application event data.
// ABOUTME: Centralizes payload versioning, retention, and hash calculation before provider delivery.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookPayloadBuilder
{
    Task<WebhookPayloadBuildResult> BuildAsync(
        WebhookEventBuildContext context,
        CancellationToken cancellationToken);
}

