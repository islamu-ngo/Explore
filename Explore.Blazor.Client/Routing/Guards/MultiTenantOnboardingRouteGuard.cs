// ABOUTME: Route guard that keeps tenant onboarding reachable only for configured multi-tenant deployments.
// ABOUTME: Prevents single-tenant first-run flows from exposing tenant mechanics in the client router.

using Blazouter.Interfaces;
using Blazouter.Models;
using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Routing.Guards;

public sealed class MultiTenantOnboardingRouteGuard(IInstanceOnboardingService instanceOnboardingService) : IRouteGuard
{
    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        var status = await instanceOnboardingService.GetSystemOnboardingStatusAsync().ConfigureAwait(false);
        return string.Equals(status?.DeploymentMode, "MultiTenant", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string?> GetRedirectPathAsync(RouteMatch match) => Task.FromResult<string?>("/startup");
}
