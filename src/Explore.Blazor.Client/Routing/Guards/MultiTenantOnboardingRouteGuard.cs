// ABOUTME: Route guard for first-run instance onboarding and multi-tenant-only onboarding routes.
// ABOUTME: Keeps completed single-tenant deployments out while allowing launch onboarding to finish.

using Blazouter.Interfaces;
using Blazouter.Models;
using Explore.Blazor.Client.Services;

namespace Explore.Blazor.Client.Routing.Guards;

public sealed class MultiTenantOnboardingRouteGuard(IInstanceOnboardingService instanceOnboardingService) : IRouteGuard
{
    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        var status = await instanceOnboardingService.GetSystemOnboardingStatusAsync().ConfigureAwait(false);
        return status?.RequiresOnboarding == true
            || string.Equals(status?.DeploymentMode, "MultiTenant", StringComparison.OrdinalIgnoreCase);
    }

    public Task<string?> GetRedirectPathAsync(RouteMatch match) => Task.FromResult<string?>("/startup");
}
