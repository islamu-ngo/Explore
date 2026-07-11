// ABOUTME: Application boundary for event-report synchronization providers.
// ABOUTME: Implementations live in Infrastructure and must keep local reporting usable on provider failure.

using Explore.Application.Features.EventReporting.Models;

namespace Explore.Application.Contracts.Infrastructure;

public interface IEventReportProvider
{
    Task<EventReportProviderSyncResult> SyncReportAsync(
        EventReportProviderEnvelope envelope,
        CancellationToken cancellationToken = default);
}
