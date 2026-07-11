// ABOUTME: Infrastructure-local contract for dispatching event-report provider sync outbox messages.
// ABOUTME: Lets the composite outbox dispatcher route report sync without depending on provider internals.

using Explore.Domain;

namespace Explore.Infrastructure.Services.Moderation;

public interface IReportProviderSyncDispatcher
{
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
