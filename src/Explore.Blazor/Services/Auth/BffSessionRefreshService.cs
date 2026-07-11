// ABOUTME: Owns BFF session refresh orchestration while keeping bearer tokens server-side.
// ABOUTME: Updates cookie claims and circuit token state without exposing token material in responses.

using System.Security.Claims;
using Explore.Blazor.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Explore.Blazor.Services.Auth;

public interface IBffSessionRefreshService
{
    Task<IResult> RefreshSessionAsync(HttpContext context, CancellationToken cancellationToken);

    void ClearCircuitTokenState(HttpContext context, ClaimsPrincipal? principal, ILogger logger, string reason);
}

public sealed class BffSessionRefreshService(
    BffAdminClaimsTransformation adminClaimsTransformation,
    IBffAccessTokenAssessmentService tokenAssessmentService)
    : IBffSessionRefreshService
{
    public async Task<IResult> RefreshSessionAsync(HttpContext context, CancellationToken cancellationToken)
    {
        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("AuthEndpoints");

        var authResult = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!authResult.Succeeded || authResult.Principal is null || authResult.Properties is null)
        {
            logger.LogWarning("[AuthEndpoints] Refresh session failed because cookie authentication did not succeed");
            return Results.Unauthorized();
        }

        var accessToken = authResult.Properties.GetTokenValue("access_token");
        var tokenAssessment = tokenAssessmentService.Assess(accessToken);
        if (!tokenAssessment.IsUsable)
        {
            ClearCircuitTokenState(context, authResult.Principal, logger, tokenAssessment.Reason);
            logger.LogWarning(
                "[AuthEndpoints] Refresh session produced no API-usable bearer token | Reason={Reason} User={UserId}",
                tokenAssessment.Reason,
                tokenAssessmentService.ResolveUserId(authResult.Principal));
            return Results.Json(
                new { refreshed = false, reason = tokenAssessment.Reason },
                statusCode: StatusCodes.Status409Conflict);
        }

        // Invalidate onboarding status cache BEFORE enriching principal so that
        // EnrichPrincipalAsync fetches fresh status (e.g. "completed" after onboarding)
        // instead of serving a stale "not completed" entry from the cache.
        context.RequestServices.GetService<IBffOnboardingStatusProvider>()?.Invalidate();

        var adminClaimsUpdated = await adminClaimsTransformation.EnrichPrincipalAsync(
            authResult.Principal,
            authResult.Properties,
            forceRefresh: true,
            cancellationToken: cancellationToken);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            authResult.Principal,
            authResult.Properties);

        var tokenService = context.RequestServices.GetService<ICircuitAccessTokenService>();
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            tokenService?.SetToken(accessToken);
        }

        logger.LogInformation(
            "[AuthEndpoints] Refresh session confirmed usable bearer token | User={UserId} TokenSummary={TokenSummary} AdminClaimsUpdated={AdminClaimsUpdated}",
            tokenAssessmentService.ResolveUserId(authResult.Principal),
            tokenAssessmentService.Describe(accessToken),
            adminClaimsUpdated);

        return Results.Ok(new { refreshed = true, adminClaimsUpdated, tokenStatus = tokenAssessment.Reason });
    }

    public void ClearCircuitTokenState(HttpContext context, ClaimsPrincipal? principal, ILogger logger, string reason)
    {
        var tokenService = context.RequestServices.GetService<ICircuitAccessTokenService>();
        tokenService?.ClearToken();

        context.RequestServices.GetService<ICircuitUserContext>()?.Clear();
        context.RequestServices.GetService<IBffAuthCookieStore>()?.Clear();

        // ICircuitAccessTokenService.ClearToken() already delegates to ICircuitTokenStore
        // for the scoped user/session. For full user-wide clearing (e.g., signout with
        // unknown session), also clear via the store directly.
        var userId = tokenAssessmentService.ResolveUserId(principal);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var tokenStore = context.RequestServices.GetService<ICircuitTokenStore>();
            var sessionId = principal?.FindFirst("sid")?.Value;
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                tokenStore?.ClearUser(userId);
            }
            else
            {
                tokenStore?.ClearSession(userId, sessionId);
            }
        }

        logger.LogDebug(
            "[AuthEndpoints] Cleared circuit token state for user {UserId} session {SessionId} because {Reason}",
            tokenAssessmentService.ResolveUserId(principal) ?? "(unknown)",
            principal?.FindFirst("sid")?.Value ?? "(none)",
            reason);
    }
}
