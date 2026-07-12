// ABOUTME: Defines the aggregate readiness observation required by managed upgrade safety checks.
// ABOUTME: Keeps ASP.NET Core health-check implementation details outside the Application layer.

namespace Explore.Application.Contracts.Infrastructure;

public sealed record ManagedEventHealthObservation(string Status, DateTime ObservedAt);

public interface IManagedEventHealthProbe
{
    Task<ManagedEventHealthObservation> CheckAsync(CancellationToken cancellationToken = default);
}
