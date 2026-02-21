// ABOUTME: Route guard that restricts tenant admin routes to tenant-scoped administrators only.
// ABOUTME: Uses tenant admin claims first, then tenant onboarding status as a fallback source of truth.

using Blazouter.Interfaces;
using Blazouter.Models;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

public sealed class TenantAdminRouteGuard(
    AuthenticationStateProvider authStateProvider,
    ITenantOnboardingService tenantOnboardingService) : IRouteGuard
{
    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = authState.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.HasClaim(c => c.Type == "explore:admin:tenant"))
        {
            return true;
        }

        var tenantStatus = await tenantOnboardingService.GetStatusAsync().ConfigureAwait(false);
        return tenantStatus?.IsAuthenticated == true
            && tenantStatus.IsCurrentUserTenantAdministrator;
    }

    public async Task<string?> GetRedirectPathAsync(RouteMatch match)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        if (authState.User.Identity?.IsAuthenticated == true)
        {
            return "/";
        }

        var returnUrl = string.IsNullOrWhiteSpace(match.MatchedPath)
            ? "/"
            : match.MatchedPath;

        return $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
