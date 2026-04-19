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
    private readonly ILogger<TokenRefreshCookieEvents> _logger;

    public TokenRefreshCookieEvents(
        IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
        ILogger<TokenRefreshCookieEvents> logger)
    {
        _oidcOptionsMonitor = oidcOptionsMonitor;
        _logger = logger;
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
            _logger.LogWarning("[TokenRefresh] Access token expired but no refresh_token available — rejecting principal");
            context.RejectPrincipal();
            return;
        }

        var schemeName = context.Properties.Items.TryGetValue(OidcSchemePropertyKey, out var scheme) ? scheme : null;

        try
        {
            var newTokens = await RefreshAccessTokenAsync(schemeName, refreshToken, context);
            if (newTokens is null)
            {
                _logger.LogWarning("[TokenRefresh] Refresh failed for scheme {Scheme} — rejecting principal", schemeName);
                context.RejectPrincipal();
                return;
            }

            context.Properties.StoreTokens(newTokens);
            context.ShouldRenew = true;

            // Propagate refreshed token into CircuitAccessTokenService so Blazor circuits use it
            var newAccessToken = newTokens.FirstOrDefault(t => t.Name == "access_token")?.Value;
            if (!string.IsNullOrEmpty(newAccessToken))
            {
                var tokenService = context.HttpContext.RequestServices.GetService<ICircuitAccessTokenService>();
                tokenService?.SetToken(newAccessToken);
            }

            _logger.LogInformation("[TokenRefresh] Tokens refreshed successfully for scheme {Scheme}", schemeName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[TokenRefresh] Exception during refresh for scheme {Scheme} — rejecting principal", schemeName);
            context.RejectPrincipal();
        }
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

    private async Task<List<AuthenticationToken>?> RefreshAccessTokenAsync(
        string? schemeName,
        string refreshToken,
        CookieValidatePrincipalContext context)
    {
        if (string.IsNullOrEmpty(schemeName))
        {
            _logger.LogWarning("[TokenRefresh] No oidc_scheme stored in cookie — cannot determine token endpoint");
            return null;
        }

        OpenIdConnectOptions oidcOptions;
        try
        {
            oidcOptions = _oidcOptionsMonitor.Get(schemeName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TokenRefresh] Failed to resolve OIDC options for scheme {Scheme}", schemeName);
            return null;
        }

        if (oidcOptions.ConfigurationManager is null)
        {
            _logger.LogWarning("[TokenRefresh] ConfigurationManager is null for scheme {Scheme}", schemeName);
            return null;
        }

        var oidcConfig = await oidcOptions.ConfigurationManager.GetConfigurationAsync(CancellationToken.None);
        var tokenEndpoint = oidcConfig.TokenEndpoint;

        if (string.IsNullOrEmpty(tokenEndpoint))
        {
            _logger.LogWarning("[TokenRefresh] Token endpoint not found in OIDC config for scheme {Scheme}", schemeName);
            return null;
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
            _logger.LogWarning(
                "[TokenRefresh] Token endpoint returned {StatusCode} for scheme {Scheme}: {Body}",
                response.StatusCode, schemeName, body);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        if (!root.TryGetProperty("access_token", out var newAccessTokenElement))
        {
            _logger.LogWarning("[TokenRefresh] Response missing access_token for scheme {Scheme}", schemeName);
            return null;
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

        return tokens;
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
