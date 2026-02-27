// ABOUTME: Contract for root/startup route decision logic.
// ABOUTME: Allows route bootstrap behavior to be mocked in tests and swapped by host.

using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Contracts.Providers;

public interface IStartupRoutingService
{
    Task<StartupRouteDecision> GetRootDecisionAsync();
}
