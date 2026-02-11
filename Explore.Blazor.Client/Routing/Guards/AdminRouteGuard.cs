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

        return true;
    }

    public Task<string?> GetRedirectPathAsync(RouteMatch match)
    {
        var returnUrl = string.IsNullOrWhiteSpace(match.MatchedPath)
            ? "/"
            : match.MatchedPath;

        return Task.FromResult<string?>($"/login?returnUrl={Uri.EscapeDataString(returnUrl)}");
    }
}
