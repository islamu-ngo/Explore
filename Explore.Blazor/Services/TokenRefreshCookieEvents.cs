// ABOUTME: Cookie authentication events that refresh expired access tokens using the IdP's token endpoint.
// ABOUTME: Reads the OIDC scheme from cookie properties, calls the refresh_token grant, and updates the cookie.

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace Explore.Blazor.Services;

public sealed class TokenRefreshCookieEvents : CookieAuthenticationEvents
{
    /// <summary>
    /// Key used to store the OIDC scheme name in the cookie's AuthenticationProperties.
    /// Written by <see cref="DynamicAuthSchemeManager"/> in OnTokenResponseReceived.
    /// </summary>
    internal const string OidcSchemePropertyKey = "oidc_scheme";

    private static readonly TimeSpan RefreshBuffer = TimeSpan.FromSeconds(60);

    private readonly IOptionsMonitor<OpenIdConnectOptions> _oidcOptionsMonitor;
    private readonly BffAdminClaimsTransformation _adminClaimsTransformation;
    private readonly IBffOnboardingStatusProvider _onboardingStatusProvider;
    private readonly ILogger<TokenRefreshCookieEvents> _logger;

    public TokenRefreshCookieEvents(
        IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
        BffAdminClaimsTransformation adminClaimsTransformation,
        IBffOnboardingStatusProvider onboardingStatusProvider,
        ILogger<TokenRefreshCookieEvents> logger)
    {
        _oidcOptionsMonitor = oidcOptionsMonitor;
        _adminClaimsTransformation = adminClaimsTransformation;
        _onboardingStatusProvider = onboardingStatusProvider;
        _logger = logger;
    }

    public override async Task SigningIn(CookieSigningInContext context)
    {
        if (context.Principal is not null)
        {
            await _adminClaimsTransformation.EnrichPrincipalAsync(
                context.Principal,
                context.Properties,
                cancellationToken: context.HttpContext.RequestAborted);
        }

        await base.SigningIn(context);
    }

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var accessToken = context.Properties.GetTokenValue("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            return;
        }

        if (!IsTokenExpiredOrNearExpiry(accessToken))
        {
            return;
        }

        var refreshToken = context.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            _logger.LogWarning("[TokenRefresh] Access token expired but no refresh_token available — signing out");
            await RejectAndSignOutAsync(context, reason: "no_refresh_token");
            return;
        }

        var schemeName = context.Properties.Items.TryGetValue(OidcSchemePropertyKey, out var scheme) ? scheme : null;

        try
        {
            var result = await RefreshAccessTokenAsync(schemeName, refreshToken, context);
            if (result.Tokens is null)
            {
                _logger.LogWarning(
                    "[TokenRefresh] Refresh failed for scheme {Scheme} (reason={Reason}) — signing out",
                    schemeName,
                    result.FailureReason);
                await RejectAndSignOutAsync(context, result.FailureReason ?? "refresh_failed");
                return;
            }

            context.Properties.StoreTokens(result.Tokens);

            if (context.Principal is not null)
            {
                await _adminClaimsTransformation.EnrichPrincipalAsync(
                    context.Principal,
                    context.Properties,
                    forceRefresh: true,
                    cancellationToken: context.HttpContext.RequestAborted);
                context.ReplacePrincipal(context.Principal);
            }

            context.ShouldRenew = true;

            // Propagate refreshed token into CircuitAccessTokenService so Blazor circuits use it
            var newAccessToken = result.Tokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
            if (!string.IsNullOrEmpty(newAccessToken))
            {
                var tokenService = context.HttpContext.RequestServices.GetService<ICircuitAccessTokenService>();
                tokenService?.SetToken(newAccessToken);
            }

            _logger.LogInformation("[TokenRefresh] Tokens refreshed successfully for scheme {Scheme}", schemeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TokenRefresh] Exception during refresh for scheme {Scheme} — signing out", schemeName);
            await RejectAndSignOutAsync(context, reason: "refresh_exception");
        }
    }

    private async Task RejectAndSignOutAsync(CookieValidatePrincipalContext context, string reason)
    {
        // Security: stale/revoked refresh tokens (Keycloak 'invalid_grant', 'Token is not active')
        // leave the user with a broken session that loops on every request. Clear the cookie
        // and redirect HTML navigations to /login; let XHR/API callers see 401.
        context.RejectPrincipal();

        try
        {
            await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TokenRefresh] SignOutAsync failed while rejecting principal");
        }

        if (context.HttpContext.Response.HasStarted)
        {
            return;
        }

        if (!IsHtmlNavigation(context.HttpContext.Request))
        {
            return;
        }

        var currentPath = context.HttpContext.Request.Path;

        // Pre-onboarding: the user cannot complete login until the instance is set up.
        // Sending them to /login would just loop them back to Keycloak. Send them to /setup
        // instead — unless they are already on /setup, in which case they stay anonymous.
        var onboardingStatus = await _onboardingStatusProvider
            .GetStatusAsync(context.HttpContext.RequestAborted)
            .ConfigureAwait(false);

        if (onboardingStatus.Known && !onboardingStatus.IsCompleted)
        {
            if (currentPath.StartsWithSegments("/setup", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            context.HttpContext.Response.Redirect($"/setup?session=expired&reason={reason}");
            return;
        }

        var returnUrl = currentPath + context.HttpContext.Request.QueryString;
        var encoded = Uri.EscapeDataString(returnUrl);
        context.HttpContext.Response.Redirect($"/login?returnUrl={encoded}&session=expired&reason={reason}");
    }

    private static bool IsHtmlNavigation(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var accept = request.Headers.Accept.ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsTokenExpiredOrNearExpiry(string accessToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(accessToken))
            {
                return true;
            }

            var jwt = handler.ReadJwtToken(accessToken);
            return jwt.ValidTo <= DateTime.UtcNow.Add(RefreshBuffer);
        }
        catch
        {
            return true;
        }
    }

    private async Task<RefreshResult> RefreshAccessTokenAsync(
        string? schemeName,
        string refreshToken,
        CookieValidatePrincipalContext context)
    {
        if (string.IsNullOrEmpty(schemeName))
        {
            _logger.LogWarning("[TokenRefresh] No oidc_scheme stored in cookie — cannot determine token endpoint");
            return RefreshResult.Failure("missing_scheme");
        }

        OpenIdConnectOptions oidcOptions;
        try
        {
            oidcOptions = _oidcOptionsMonitor.Get(schemeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TokenRefresh] Failed to resolve OIDC options for scheme {Scheme}", schemeName);
            return RefreshResult.Failure("oidc_options_error");
        }

        if (oidcOptions.ConfigurationManager is null)
        {
            _logger.LogWarning("[TokenRefresh] ConfigurationManager is null for scheme {Scheme}", schemeName);
            return RefreshResult.Failure("no_configuration_manager");
        }

        var oidcConfig = await oidcOptions.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
        var tokenEndpoint = oidcConfig.TokenEndpoint;

        if (string.IsNullOrEmpty(tokenEndpoint))
        {
            _logger.LogWarning("[TokenRefresh] Token endpoint not found in OIDC config for scheme {Scheme}", schemeName);
            return RefreshResult.Failure("no_token_endpoint");
        }

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["refresh_token"] = refreshToken,
            ["client_id"] = oidcOptions.ClientId,
        };

        if (!string.IsNullOrEmpty(oidcOptions.ClientSecret))
        {
            parameters["client_secret"] = oidcOptions.ClientSecret;
        }

        // Force IPv4 — self-hosted Keycloak may have unreachable AAAA records
        using var httpClient = CreateIpv4HttpClient();

        _logger.LogDebug("[TokenRefresh] Calling token endpoint {Endpoint} for scheme {Scheme}", tokenEndpoint, schemeName);

        using var content = new FormUrlEncodedContent(parameters);
        using var response = await httpClient.PostAsync(tokenEndpoint, content);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            var reason = ParseOidcErrorCode(body) ?? $"status_{(int)response.StatusCode}";
            _logger.LogWarning(
                "[TokenRefresh] Token endpoint returned {StatusCode} for scheme {Scheme} (error={Error}): {Body}",
                response.StatusCode, schemeName, reason, body);
            return RefreshResult.Failure(reason);
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("access_token", out var newAccessTokenElement))
        {
            _logger.LogWarning("[TokenRefresh] Response missing access_token for scheme {Scheme}", schemeName);
            return RefreshResult.Failure("missing_access_token");
        }

        var tokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token", Value = newAccessTokenElement.GetString()! }
        };

        // Preserve or update refresh_token
        if (root.TryGetProperty("refresh_token", out var newRefreshElement) &&
            !string.IsNullOrEmpty(newRefreshElement.GetString()))
        {
            tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = newRefreshElement.GetString()! });
        }
        else
        {
            tokens.Add(new AuthenticationToken { Name = "refresh_token", Value = refreshToken });
        }

        // Preserve or update id_token
        if (root.TryGetProperty("id_token", out var newIdTokenElement) &&
            !string.IsNullOrEmpty(newIdTokenElement.GetString()))
        {
            tokens.Add(new AuthenticationToken { Name = "id_token", Value = newIdTokenElement.GetString()! });
        }
        else
        {
            var existingIdToken = context.Properties.GetTokenValue("id_token");
            if (!string.IsNullOrEmpty(existingIdToken))
            {
                tokens.Add(new AuthenticationToken { Name = "id_token", Value = existingIdToken });
            }
        }

        // Store expires_at for diagnostics
        if (root.TryGetProperty("expires_in", out var expiresInElement) && expiresInElement.TryGetInt32(out var expiresIn))
        {
            var expiresAt = DateTimeOffset.UtcNow.AddSeconds(expiresIn);
            tokens.Add(new AuthenticationToken { Name = "expires_at", Value = expiresAt.ToString("o") });
        }

        return RefreshResult.Success(tokens);
    }

    private static string? ParseOidcErrorCode(string body)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var errorElement))
            {
                return errorElement.GetString();
            }
        }
        catch (JsonException)
        {
            // non-JSON body — fall through
        }

        return null;
    }

    private readonly record struct RefreshResult(List<AuthenticationToken>? Tokens, string? FailureReason)
    {
        public static RefreshResult Success(List<AuthenticationToken> tokens) => new(tokens, null);
        public static RefreshResult Failure(string reason) => new(null, reason);
    }

    private static HttpClient CreateIpv4HttpClient()
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(10),
            ConnectCallback = async (context, ct) =>
            {
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                try
                {
                    await socket.ConnectAsync(context.DnsEndPoint, ct);
                    return new NetworkStream(socket, ownsSocket: true);
                }
                catch
                {
                    socket.Dispose();
                    throw;
                }
            }
        };

        return new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(15) };
    }
}
