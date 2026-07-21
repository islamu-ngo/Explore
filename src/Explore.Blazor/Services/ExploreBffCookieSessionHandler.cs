// ABOUTME: Bridges shared BFF cookie-token refresh events into Explore-specific session state.
// ABOUTME: Handles admin-claim enrichment, setup-aware redirects, and circuit token cleanup outside Event.Web.BffHosting.

using System.Security.Claims;
using Event.Web.BffHosting.Authentication;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Explore.Blazor.Services;

public sealed class ExploreBffCookieSessionHandler(
    BffAdminClaimsTransformation adminClaimsTransformation,
    IBffOnboardingStatusProvider onboardingStatusProvider)
    : IEventBffCookieSessionHandler
{
    public async Task OnSigningInAsync(CookieSigningInContext context)
    {
        if (context.Principal is null)
        {
            return;
        }

        await adminClaimsTransformation.EnrichPrincipalAsync(
            context.Principal,
            context.Properties,
            synchronizeUser: true,
            cancellationToken: context.HttpContext.RequestAborted);
    }

    public async Task OnTokenRefreshSucceededAsync(
        CookieValidatePrincipalContext context,
        IReadOnlyList<AuthenticationToken> refreshedTokens)
    {
        if (context.Principal is not null)
        {
            await adminClaimsTransformation.EnrichPrincipalAsync(
                context.Principal,
                context.Properties,
                forceRefresh: true,
                cancellationToken: context.HttpContext.RequestAborted);
            context.ReplacePrincipal(context.Principal);
        }

        var newAccessToken = refreshedTokens.FirstOrDefault(token => token.Name == "access_token")?.Value;
        if (!string.IsNullOrEmpty(newAccessToken))
        {
            var tokenService = context.HttpContext.RequestServices.GetService<ICircuitAccessTokenService>();
            tokenService?.SetToken(newAccessToken);
        }
    }

    public Task OnTokenRefreshRejectedAsync(CookieValidatePrincipalContext context, string reason)
    {
        ClearCircuitTokenState(context);
        return Task.CompletedTask;
    }

    public async Task<bool> TryRedirectRejectedHtmlNavigationAsync(
        CookieValidatePrincipalContext context,
        string reason)
    {
        var currentPath = context.HttpContext.Request.Path;
        var onboardingStatus = await onboardingStatusProvider
            .GetStatusAsync(context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (!onboardingStatus.Known || onboardingStatus.IsCompleted)
        {
            return false;
        }

        if (currentPath.StartsWithSegments("/setup", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        context.HttpContext.Response.Redirect($"/setup?session=expired&reason={reason}");
        return true;
    }

    private static void ClearCircuitTokenState(CookieValidatePrincipalContext context)
    {
        var tokenService = context.HttpContext.RequestServices.GetService<ICircuitAccessTokenService>();
        tokenService?.ClearToken();

        context.HttpContext.RequestServices.GetService<ICircuitUserContext>()?.Clear();
        context.HttpContext.RequestServices.GetService<IBffAuthCookieStore>()?.Clear();

        var userId = ResolveUserId(context.Principal);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        var tokenStore = context.HttpContext.RequestServices.GetService<ICircuitTokenStore>();
        var sessionId = context.Principal?.FindFirst("sid")?.Value;
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            tokenStore?.ClearUser(userId);
        }
        else
        {
            tokenStore?.ClearSession(userId, sessionId);
        }
    }

    private static string? ResolveUserId(ClaimsPrincipal? principal)
    {
        return principal?.FindFirst("sub")?.Value
            ?? principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? principal?.FindFirst("sid")?.Value;
    }
}
