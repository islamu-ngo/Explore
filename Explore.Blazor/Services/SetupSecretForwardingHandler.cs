// ABOUTME: DelegatingHandler that forwards the setup secret header to onboarding API endpoints.
// ABOUTME: Resolves the secret from cookies or the session service for the current user.

using System.Security.Claims;

namespace Explore.Blazor.Services;

/// <summary>
/// Forwards the X-Setup-Secret header to API endpoints that require it during initial onboarding.
/// </summary>
public class SetupSecretForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SetupSecretSessionService _setupSecretSessionService;

    public SetupSecretForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        SetupSecretSessionService setupSecretSessionService)
    {
        _httpContextAccessor = httpContextAccessor;
        _setupSecretSessionService = setupSecretSessionService;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;

        if (!RequiresSetupSecret(pathAndQuery))
        {
            return base.SendAsync(request, cancellationToken);
        }

        if (request.Headers.Contains("X-Setup-Secret"))
        {
            return base.SendAsync(request, cancellationToken);
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var setupSecret = httpContext?.Request.Cookies["setup-secret"];

        if (string.IsNullOrWhiteSpace(setupSecret))
        {
            var userId = httpContext?.User?.FindFirst("sub")?.Value
                ?? httpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext?.User?.FindFirst("sid")?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                setupSecret = _setupSecretSessionService.GetForUser(userId);
            }
        }

        if (!string.IsNullOrWhiteSpace(setupSecret))
        {
            request.Headers.Add("X-Setup-Secret", setupSecret);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool RequiresSetupSecret(string pathAndQuery)
    {
        return pathAndQuery.Contains("/api/InstanceOnboarding/complete", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/validate-secret", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/auth-provider-configuration", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/authz-provider-configuration", StringComparison.OrdinalIgnoreCase);
    }
}
