// ABOUTME: Application boundary for enqueueing durable notification intents after ownership resolution.
// ABOUTME: Coordinates resolver and repository only; delivery providers remain outside this layer.

using Explore.Application.Notifications;

namespace Explore.Application.Contracts.Notifications;

public interface INotificationOrchestrator
{
    Task<NotificationOrchestrationResult> EnqueueAsync(
        NotificationIntentDraft draft,
        CancellationToken cancellationToken = default);
}
