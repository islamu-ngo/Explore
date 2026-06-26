// ABOUTME: Application service contract for event moderation attendee notification fanout.
// ABOUTME: Consumes durable outbox payloads and creates idempotent in-app notification rows.

using Explore.Application.Models.InternalEvents;

namespace Explore.Application.Contracts.Services;

public interface IEventModerationNotificationFanoutService
{
    Task FanoutLightModerationAsync(
        EventLightModeratedNotificationFanoutRequested request,
        CancellationToken cancellationToken = default);

    Task FanoutHeavyRedactionAsync(
        EventHeavyRedactedNotificationFanoutRequested request,
        CancellationToken cancellationToken = default);
}
