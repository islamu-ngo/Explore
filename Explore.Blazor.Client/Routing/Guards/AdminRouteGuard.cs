// ABOUTME: Route guard that restricts instance admin routes to platform-scoped instance administrators.
// Uses DB-backed admin claims first, then instance onboarding status as a fallback source of truth.

using Blazouter.Interfaces;
using Blazouter.Models;
using Explore.Blazor.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

/// <summary>
/// Guards instance-admin routes by verifying the user has platform-scoped instance admin authority.
/// Admin claims are resolved from the database by <c>AdminClaimsTransformation</c>
/// and serialized to WASM via <c>AddAuthenticationStateSerialization</c>.
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

        // DB-first authority: admin claims are added by AdminClaimsTransformation.
        // Claim types match Explore.Application.Authorization.AdminClaimTypes constants.
        if (user.HasClaim(c => c.Type == "explore:admin:instance"))
        {
            return true;
        }

        // Fallback for deployments where admin claims are not serialized to WASM.
        // Use instance onboarding status as the source of truth.
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
