// ABOUTME: Defines the host boundary for invalidating event-reporting-dependent HTTP output caches.
// ABOUTME: Lets Application setting notifications evict API responses without depending on ASP.NET Core.

namespace Explore.Application.Contracts.Infrastructure;

public interface IEventReportingOutputCacheInvalidator
{
    Task InvalidateAsync(CancellationToken cancellationToken);
}
