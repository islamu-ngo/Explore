// ABOUTME: DelegatingHandler that forwards trusted setup secret headers to onboarding API endpoints.
// ABOUTME: Strips client-controlled setup headers and resolves secrets through the BFF resolver only.

namespace Explore.Blazor.Services;

/// <summary>
/// Forwards the X-Setup-Secret header to API endpoints that require it during initial onboarding.
/// In Blazor circuit context (HttpContext null), falls back to extracting the user ID from
/// the Authorization header set by AccessTokenForwardingHandler, then looks up the secret
/// from SetupSecretSessionService.
/// </summary>
public class SetupSecretForwardingHandler : DelegatingHandler
{
    private readonly ISetupSecretResolver _setupSecretResolver;

    public SetupSecretForwardingHandler(ISetupSecretResolver setupSecretResolver)
    {
        _setupSecretResolver = setupSecretResolver;
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

        _ = request.Headers.Remove("X-Setup-Secret");
        var setupSecret = _setupSecretResolver.Resolve(outboundRequest: request);
        if (setupSecret.Found && !string.IsNullOrWhiteSpace(setupSecret.Secret))
        {
            request.Headers.Add("X-Setup-Secret", setupSecret.Secret);
        }

        return base.SendAsync(request, cancellationToken);
    }

    private static bool RequiresSetupSecret(string pathAndQuery)
    {
        return pathAndQuery.Contains("/api/InstanceOnboarding/complete", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/validate-secret", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/auth-provider-configuration", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/authz-provider-configuration", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/instance/settings", StringComparison.OrdinalIgnoreCase);
    }
}
