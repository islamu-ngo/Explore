// ABOUTME: Route guard that restricts tenant admin routes to tenant-scoped administrators.
// ABOUTME: Uses BFF/API onboarding status endpoints for tenant authority and single-tenant fallback.

using Blazouter.Interfaces;
using Blazouter.Models;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

public sealed class TenantAdminRouteGuard(
    AuthenticationStateProvider authStateProvider,
    ITenantOnboardingService tenantOnboardingService,
    IInstanceOnboardingService instanceOnboardingService,
    IUserService userService) : IRouteGuard
{
    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = authState.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var instanceStatus = await instanceOnboardingService.GetStatusAsync().ConfigureAwait(false);
        var isSingleTenant = IsSingleTenant(instanceStatus?.SelectedDeploymentMode);
        if (isSingleTenant && instanceStatus?.IsAuthenticated == true && instanceStatus.IsCurrentUserInstanceAdmin)
        {
            return true;
        }

        var authority = await userService.GetAdminAuthorityAsync().ConfigureAwait(false);
        if (authority?.AdminTenantIds?.Any() == true)
        {
            return true;
        }

        if (isSingleTenant && authority?.IsInstanceAdmin == true)
        {
            return true;
        }

        if (isSingleTenant)
        {
            return false;
        }

        var tenantStatus = await tenantOnboardingService.GetStatusAsync().ConfigureAwait(false);
        return tenantStatus?.IsAuthenticated == true && tenantStatus.IsCurrentUserTenantAdministrator;
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

    private static bool IsSingleTenant(string? selectedDeploymentMode)
    {
        return string.IsNullOrWhiteSpace(selectedDeploymentMode)
            || string.Equals(selectedDeploymentMode, "SingleTenant", StringComparison.OrdinalIgnoreCase);
    }
}
