using System.Security.Claims;
using Blazouter.Interfaces;
using Blazouter.Models;
using Microsoft.AspNetCore.Components.Authorization;

namespace Explore.Blazor.Client.Routing.Guards;

public sealed class AdminRouteGuard(AuthenticationStateProvider authStateProvider) : IRouteGuard
{
    public async Task<bool> CanActivateAsync(RouteMatch match)
    {
        var authState = await authStateProvider.GetAuthenticationStateAsync().ConfigureAwait(false);
        var user = authState.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        return user.IsInRole("Admin")
               || user.Claims.Any(c =>
                   c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase)
                   && c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));
    }

    public Task<string?> GetRedirectPathAsync(RouteMatch match)
    {
        var returnUrl = string.IsNullOrWhiteSpace(match.MatchedPath)
            ? "/"
            : match.MatchedPath;

        return Task.FromResult<string?>($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
