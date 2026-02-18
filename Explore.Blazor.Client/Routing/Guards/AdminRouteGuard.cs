// ABOUTME: Route guard that restricts /admin/* routes to users with DB-backed admin authority.
// Checks for admin claims added by AdminClaimsTransformation (instance or tenant admin).

using Blazouter.Interfaces;
using Blazouter.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

/// <summary>
/// Guards admin routes by verifying the user has instance or tenant admin authority.
/// Admin claims are resolved from the database by <c>AdminClaimsTransformation</c>
/// and serialized to WASM via <c>AddAuthenticationStateSerialization</c>.
/// </summary>
public sealed class AdminRouteGuard(AuthenticationStateProvider authStateProvider) : IRouteGuard
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
        return user.HasClaim(c => c.Type == "explore:admin:instance")
               || user.HasClaim(c => c.Type == "explore:admin:tenant");
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
