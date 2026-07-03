// ABOUTME: Application boundary for converting product events into canonical webhook messages.
// ABOUTME: Allows outbox dispatchers to request webhook publication without knowing delivery providers.

namespace Explore.Application.Contracts.Webhooks;

public interface IWebhookEventPublisher
{
    Task<WebhookEventPublishResult> PublishAsync(
        WebhookEventBuildContext context,
        CancellationToken cancellationToken);
}
