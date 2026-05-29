// ABOUTME: Application service contract for internal event-published notification fanout.
// ABOUTME: Consumes durable outbox payloads and creates idempotent notification inbox rows.

using Explore.Application.Models.InternalEvents;

namespace Explore.Application.Contracts.Services;

public interface IEventPublishedNotificationFanoutService
{
    Task FanoutAsync(EventPublishedNotificationFanoutRequested request, CancellationToken cancellationToken = default);
}
