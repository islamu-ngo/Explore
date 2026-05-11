// ABOUTME: Route guard that restricts instance admin routes to platform-scoped instance administrators.
// Uses the BFF onboarding status endpoint as the source of truth for instance-admin authority.

using Blazouter.Interfaces;
using Blazouter.Models;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

/// <summary>
/// Guards instance-admin routes by verifying the BFF-reported platform-scoped instance admin authority.
/// </summary>
public sealed class AdminRouteGuard(
    AuthenticationStateProvider authStateProvider,
    IInstanceOnboardingService instanceOnboardingService) : IRouteGuard
{
    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        if (authStateProvider is null)
        {
            return false;
        }

        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = authState.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var instanceStatus = await instanceOnboardingService.GetStatusAsync().ConfigureAwait(false);
        return instanceStatus?.IsAuthenticated == true && instanceStatus.IsCurrentUserInstanceAdmin;
    }

    public async Task<string?> GetRedirectPathAsync(RouteMatch match)
    {
        if (authStateProvider is not null)
        {
            var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
            if (authState?.User?.Identity?.IsAuthenticated == true)
            {
                return "/";
            }
        }

        var returnUrl = string.IsNullOrWhiteSpace(match.MatchedPath)
            ? "/"
            : match.MatchedPath;

        return $"/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
    }
}
