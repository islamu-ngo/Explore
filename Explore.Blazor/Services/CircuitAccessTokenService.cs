// ABOUTME: Manages access token storage for Blazor Server circuits and token forwarding to API requests.
// ABOUTME: Contains CircuitAccessTokenService (scoped token store) and AccessTokenForwardingHandler (HTTP message handler).

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Explore.Blazor.Services;

public interface ICircuitAccessTokenService
{
    string? AccessToken { get; }
    void SetToken(string? token);
    void ClearToken();
}

public interface ISetupSecretSessionService
{
    void SetForUser(string userId, string secret);
    string? GetForUser(string userId);
    void ClearForUser(string userId);
    string CreateAnonymousSession(string secret);
    string? GetForAnonymousSession(string sessionId);
    void ClearAnonymousSession(string sessionId);
}

public sealed class SetupSecretSessionService : ISetupSecretSessionService
{
    private readonly ConcurrentDictionary<string, SecretEntry> _userStore = new();
    private readonly ConcurrentDictionary<string, SecretEntry> _anonymousStore = new();

    public void SetForUser(string userId, string secret)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(secret))
        {
            return;
        }

        _userStore[userId] = new SecretEntry(secret.Trim(), DateTime.UtcNow);
        CleanupExpiredEntries();
    }

    public string? GetForUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (!_userStore.TryGetValue(userId, out var entry))
        {
            return null;
        }

        if (entry.StoredAtUtc < DateTime.UtcNow.AddHours(-2))
        {
            _userStore.TryRemove(userId, out _);
            return null;
        }

        return entry.Secret;
    }

    public void ClearForUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return;
        }

        _userStore.TryRemove(userId, out _);
    }

    public string CreateAnonymousSession(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            return string.Empty;
        }

        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _anonymousStore[sessionId] = new SecretEntry(secret.Trim(), DateTime.UtcNow);
        CleanupExpiredEntries();
        return sessionId;
    }

    public string? GetForAnonymousSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return null;
        }

        if (!_anonymousStore.TryGetValue(sessionId, out var entry))
        {
            return null;
        }

        if (entry.StoredAtUtc < DateTime.UtcNow.AddHours(-2))
        {
            _anonymousStore.TryRemove(sessionId, out _);
            return null;
        }

        return entry.Secret;
    }

    public void ClearAnonymousSession(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        _anonymousStore.TryRemove(sessionId, out _);
    }

    private void CleanupExpiredEntries()
    {
        var cutoff = DateTime.UtcNow.AddHours(-2);
        foreach (var key in _userStore.Where(kvp => kvp.Value.StoredAtUtc < cutoff).Select(kvp => kvp.Key).ToList())
        {
            _userStore.TryRemove(key, out _);
        }

        foreach (var key in _anonymousStore.Where(kvp => kvp.Value.StoredAtUtc < cutoff).Select(kvp => kvp.Key).ToList())
        {
            _anonymousStore.TryRemove(key, out _);
        }
    }

    private sealed record SecretEntry(string Secret, DateTime StoredAtUtc);
}

/// <summary>
/// Scoped circuit access token service that bridges the per-circuit token into the bounded
/// <see cref="ICircuitTokenStore"/>. Each Blazor circuit receives its own scoped instance,
/// but all instances share the singleton <see cref="ICircuitTokenStore"/> for cross-scope
/// token resolution (e.g., pooled HttpClient handlers).
/// </summary>
public class CircuitAccessTokenService : ICircuitAccessTokenService
{
    private readonly ICircuitTokenStore _tokenStore;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CircuitAccessTokenService> _logger;
    private string? _localToken;
    private string? _userId;
    private string? _sessionId;

    public CircuitAccessTokenService(
        ICircuitTokenStore tokenStore,
        IHttpContextAccessor httpContextAccessor,
        ILogger<CircuitAccessTokenService> logger)
    {
        _tokenStore = tokenStore;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? AccessToken
    {
        get
        {
            var userId = _userId ?? GetUserIdFromHttpContext();
            var sessionId = _sessionId ?? GetSessionIdFromHttpContext();

            if (!string.IsNullOrEmpty(userId))
            {
                var resolution = _tokenStore.Resolve(userId, sessionId);
                if (!resolution.Found && string.IsNullOrWhiteSpace(sessionId))
                {
                    resolution = _tokenStore.ResolveByUserId(userId);
                }

                if (resolution.Found)
                {
                    if (!string.IsNullOrEmpty(_localToken) && !string.Equals(_localToken, resolution.Token, StringComparison.Ordinal))
                    {
                        _logger.LogInformation(
                            "[CircuitAccessTokenService] Store token is overriding stale circuit-local token for {UserId}",
                            userId);
                    }

                    return resolution.Token;
                }
            }

            if (!CircuitTokenStore.IsTokenUsable(_localToken))
            {
                _localToken = null;
                return null;
            }

            return _localToken;
        }
    }

    public void SetToken(string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            ClearToken();
            return;
        }

        if (!CircuitTokenStore.IsTokenUsable(token))
        {
            _localToken = null;
            _logger.LogWarning("[CircuitAccessTokenService] SetToken ignored an expired or near-expiry access token");
            return;
        }

        _localToken = token;

        var userId = ExtractUserIdFromToken(token, _logger) ?? GetUserIdFromHttpContext();
        var sessionId = ExtractSessionIdFromToken(token, _logger) ?? GetSessionIdFromHttpContext();

        if (!string.IsNullOrEmpty(userId))
        {
            _userId = userId;
            _sessionId = sessionId;
            var result = _tokenStore.Store(userId, sessionId, token);
            if (!result.Accepted)
            {
                _logger.LogDebug(
                    "[CircuitAccessTokenService] Token store rejected token: {RejectionCode}",
                    result.RejectionCode);
            }
        }
        else
        {
            _logger.LogWarning("[CircuitAccessTokenService] Could not extract userId from token — not stored in shared cache");
        }
    }

    public void ClearToken()
    {
        var userId = _userId ?? GetUserIdFromHttpContext();
        var sessionId = _sessionId ?? GetSessionIdFromHttpContext();

        _localToken = null;
        _userId = null;
        _sessionId = null;

        if (!string.IsNullOrWhiteSpace(userId))
        {
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                _tokenStore.ClearUser(userId);
            }
            else
            {
                _tokenStore.ClearSession(userId, sessionId);
            }
        }
    }

    /// <summary>
    /// Extract the user ID (sub claim) directly from the JWT token.
    /// This works even without HttpContext.
    /// </summary>
    private static string? ExtractUserIdFromToken(string token, ILogger logger)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                var userId = TryResolveUserId(jwtToken.Claims);
                logger.LogDebug("[CircuitAccessTokenService] Extracted userId from JWT: {UserId}", userId ?? "(null)");
                return userId;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[CircuitAccessTokenService] Failed to parse JWT");
        }
        return null;
    }

    private static string? ExtractSessionIdFromToken(string token, ILogger logger)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                var sessionId = TryResolveSessionId(jwtToken.Claims);
                logger.LogDebug("[CircuitAccessTokenService] Extracted sessionId from JWT: {SessionId}", sessionId ?? "(none)");
                return sessionId;
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "[CircuitAccessTokenService] Failed to parse JWT session id");
        }
        return null;
    }

    private string? GetUserIdFromHttpContext()
    {
        return TryResolveUserId(_httpContextAccessor.HttpContext?.User);
    }

    private string? GetSessionIdFromHttpContext()
    {
        return TryResolveSessionId(_httpContextAccessor.HttpContext?.User?.Claims);
    }

    private static string? TryResolveUserId(ClaimsPrincipal? user)
    {
        return user is null ? null : TryResolveUserId(user.Claims);
    }

    private static string? TryResolveUserId(IEnumerable<Claim> claims)
    {
        return claims.FirstOrDefault(c => c.Type == "sub")?.Value
            ?? claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
            ?? claims.FirstOrDefault(c => c.Type == "sid")?.Value;
    }

    internal static string? TryResolveSessionId(IEnumerable<Claim>? claims)
    {
        return claims?.FirstOrDefault(c => c.Type == "sid")?.Value;
    }
}

/// <summary>
/// HTTP message handler that forwards the access token and trusted tenant slug to API requests.
/// Resolves token for the current authenticated user only via the bounded <see cref="ICircuitTokenStore"/>.
/// </summary>
public class AccessTokenForwardingHandler : DelegatingHandler
{
    private const string BffSelfClientName = "BffSelfClient";
    private const string InternalRefreshPath = "/bff/auth/refresh-session/internal";

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICircuitAccessTokenService _circuitAccessTokenService;
    private readonly ICircuitUserContext _circuitUserContext;
    private readonly ICircuitTokenStore _tokenStore;
    private readonly ILogger<AccessTokenForwardingHandler> _logger;
    private readonly IHttpClientFactory? _bffSelfClientFactory;

    public AccessTokenForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ICircuitAccessTokenService circuitAccessTokenService,
        ICircuitUserContext circuitUserContext,
        ICircuitTokenStore tokenStore,
        ILogger<AccessTokenForwardingHandler> logger,
        IHttpClientFactory? bffSelfClientFactory = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitAccessTokenService = circuitAccessTokenService;
        _circuitUserContext = circuitUserContext;
        _tokenStore = tokenStore;
        _logger = logger;
        _bffSelfClientFactory = bffSelfClientFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[AccessTokenForwardingHandler] Processing request to {Path}", request.RequestUri?.PathAndQuery);

        string? token = null;
        string? nearExpiryHttpContextToken = null;
        string source = "none";

        // Strategy 1: Try to get token from HttpContext (works during initial HTTP request)
        var httpContext = _httpContextAccessor.HttpContext;
        var isAuthenticated = httpContext?.User?.Identity?.IsAuthenticated == true;

        if (isAuthenticated)
        {
            try
            {
                token = await httpContext!.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    if (CircuitTokenStore.IsTokenUsable(token))
                    {
                        source = "HttpContext";
                        _logger.LogDebug("[AccessTokenForwardingHandler] Got token from HttpContext");
                    }
                    else if (CircuitTokenStore.IsTokenForwardable(token))
                    {
                        nearExpiryHttpContextToken = token;
                        _logger.LogInformation(
                            "[AccessTokenForwardingHandler] HttpContext token is near expiry at {Path}; will use it only if no fresher circuit token is available",
                            request.RequestUri?.PathAndQuery);
                        token = null;
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[AccessTokenForwardingHandler] Ignoring expired HttpContext token at {Path}",
                            request.RequestUri?.PathAndQuery);
                        token = null;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AccessTokenForwardingHandler] Could not get token from HttpContext");
            }
        }

        // Strategy 1b: If the HttpContext token was missing or not usable, try to refresh
        // the cookie session explicitly. This runs CookieAuthenticationEvents.ValidatePrincipal,
        // which can exchange the stored refresh_token for a new access_token and re-issue the
        // cookie. The refreshed token is also pushed into the circuit token store so later
        // SignalR/circuit-dispatched requests can reuse it.
        if (string.IsNullOrEmpty(token) && isAuthenticated && httpContext is not null)
        {
            try
            {
                var authResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (authResult.Succeeded && authResult.Properties is not null)
                {
                    var refreshedToken = authResult.Properties.GetTokenValue("access_token");
                    if (!string.IsNullOrEmpty(refreshedToken) && CircuitTokenStore.IsTokenUsable(refreshedToken))
                    {
                        _circuitAccessTokenService.SetToken(refreshedToken);
                        token = refreshedToken;
                        source = "HttpContextRefreshed";
                        _logger.LogInformation(
                            "[AccessTokenForwardingHandler] Refreshed access token from cookie authentication for {Path}",
                            request.RequestUri?.PathAndQuery);
                    }
                    else if (!string.IsNullOrEmpty(refreshedToken))
                    {
                        _logger.LogInformation(
                            "[AccessTokenForwardingHandler] Cookie authentication returned an unusable access token for {Path}; trying circuit and BFF refresh fallbacks",
                            request.RequestUri?.PathAndQuery);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AccessTokenForwardingHandler] Could not refresh access token via cookie authentication");
            }
        }

        // Strategy 2: Try to get token from bounded store by current user ID
        if (string.IsNullOrEmpty(token))
        {
            var userId = TryResolveUserId(httpContext?.User);

            if (!string.IsNullOrEmpty(userId))
            {
                var sessionId = CircuitAccessTokenService.TryResolveSessionId(httpContext?.User?.Claims);
                var resolution = _tokenStore.Resolve(userId, sessionId);
                if (resolution.Found)
                {
                    token = resolution.Token;
                    source = "TokenStore(userId)";
                }
                else if (string.IsNullOrWhiteSpace(sessionId))
                {
                    resolution = _tokenStore.ResolveByUserId(userId);
                    if (resolution.Found)
                    {
                        token = resolution.Token;
                        source = "TokenStore(userId-only)";
                        _logger.LogInformation(
                            "[AccessTokenForwardingHandler] Session-keyed lookup failed for {UserId}; resolved token via user-only fallback",
                            userId);
                    }
                }
            }
            else if (isAuthenticated)
            {
                _logger.LogWarning(
                    "[AccessTokenForwardingHandler] Authenticated user has no resolvable user identifier claims at {Path}",
                    request.RequestUri?.PathAndQuery);
            }
        }

        // Strategy 3: Fall back to the scoped ICircuitAccessTokenService which retains the circuit's user
        // identity even when HttpContext is unavailable (e.g., Blazor Server click events dispatched via
        // SignalR where IHttpContextAccessor.HttpContext is null).
        if (string.IsNullOrEmpty(token))
        {
            token = _circuitAccessTokenService.AccessToken;
            if (!string.IsNullOrEmpty(token))
            {
                source = "CircuitAccessTokenService";
                _logger.LogDebug(
                    "[AccessTokenForwardingHandler] Got token from CircuitAccessTokenService scoped instance (user: {UserId})",
                    _circuitUserContext.UserId ?? TryResolveUserId(httpContext?.User) ?? "(unknown)");
            }
        }

        // Strategy 4: Use AsyncLocal-backed ICircuitUserContext to get the userId and look up
        // the token in the bounded store. This works across DI scope boundaries where HttpContext
        // is null but the Blazor circuit async context still flows.
        if (string.IsNullOrEmpty(token))
        {
            var userId = _circuitUserContext.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                var resolution = _tokenStore.Resolve(userId, _circuitUserContext.SessionId);
                if (resolution.Found)
                {
                    token = resolution.Token;
                    source = "CircuitUserContext";
                    _logger.LogDebug("[AccessTokenForwardingHandler] Got token from store via CircuitUserContext");
                }
                else if (string.IsNullOrWhiteSpace(_circuitUserContext.SessionId))
                {
                    resolution = _tokenStore.ResolveByUserId(userId);
                    if (resolution.Found)
                    {
                        token = resolution.Token;
                        source = "CircuitUserContext(userId-only)";
                        _logger.LogInformation(
                            "[AccessTokenForwardingHandler] CircuitUserContext session-keyed lookup failed for {UserId}; resolved via user-only fallback",
                            userId);
                    }
                }
            }
        }

        if (string.IsNullOrEmpty(token) && isAuthenticated && httpContext is not null)
        {
            token = await TryRefreshViaBffSelfEndpointAsync(httpContext, request, cancellationToken);
            if (!string.IsNullOrEmpty(token))
            {
                source = "BffSelfRefresh";
            }
        }

        // Strategy 5: If the only token available is still valid but inside the persistence
        // safety buffer, forward it for this short request instead of dropping auth entirely.
        if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(nearExpiryHttpContextToken))
        {
            token = nearExpiryHttpContextToken;
            source = "HttpContextNearExpiry";
            _logger.LogInformation(
                "[AccessTokenForwardingHandler] Forwarding near-expiry HttpContext token to {Path}",
                request.RequestUri?.PathAndQuery);
        }

        // Add Authorization header if we have a token
        if (!string.IsNullOrEmpty(token) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation(
                "[AccessTokenForwardingHandler] Added Bearer token from {Source} to {Path}",
                source,
                request.RequestUri?.PathAndQuery);
        }
        else if (!string.IsNullOrEmpty(token))
        {
            _logger.LogDebug(
                "[AccessTokenForwardingHandler] Authorization header already present for {Path}; token source was {Source}",
                request.RequestUri?.PathAndQuery, source);
        }
        else if (string.IsNullOrEmpty(token))
        {
            var path = request.RequestUri?.PathAndQuery ?? string.Empty;
            if (IsAnonymousAllowedPath(path))
            {
                _logger.LogDebug("[AccessTokenForwardingHandler] No token needed for anonymous endpoint {Path}", path);
            }
            else
            {
                _logger.LogWarning(
                    "[AccessTokenForwardingHandler] No token available for current user at {Path} — request will likely fail with 401",
                    path);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task<string?> TryRefreshViaBffSelfEndpointAsync(
        HttpContext httpContext,
        HttpRequestMessage outboundRequest,
        CancellationToken cancellationToken)
    {
        if (_bffSelfClientFactory is null)
        {
            return null;
        }

        try
        {
            var selfClient = _bffSelfClientFactory.CreateClient(BffSelfClientName);
            using var refreshRequest = new HttpRequestMessage(HttpMethod.Post, BuildInternalRefreshUri(httpContext));
            using var response = await selfClient.SendAsync(refreshRequest, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "[AccessTokenForwardingHandler] BFF self refresh returned {StatusCode} before forwarding {Path}",
                    (int)response.StatusCode,
                    outboundRequest.RequestUri?.PathAndQuery);
                return null;
            }

            var userId = TryResolveUserId(httpContext.User) ?? _circuitUserContext.UserId;
            var sessionId = CircuitAccessTokenService.TryResolveSessionId(httpContext.User?.Claims)
                ?? _circuitUserContext.SessionId;
            var refreshedToken = ResolveTokenFromStore(userId, sessionId);
            if (!string.IsNullOrEmpty(refreshedToken))
            {
                _circuitAccessTokenService.SetToken(refreshedToken);
                _logger.LogInformation(
                    "[AccessTokenForwardingHandler] Refreshed access token through BFF self endpoint for {Path}",
                    outboundRequest.RequestUri?.PathAndQuery);
                return refreshedToken;
            }

            _logger.LogWarning(
                "[AccessTokenForwardingHandler] BFF self refresh succeeded before forwarding {Path}, but no usable token was available in the circuit token store",
                outboundRequest.RequestUri?.PathAndQuery);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[AccessTokenForwardingHandler] Could not refresh access token through BFF self endpoint before forwarding {Path}",
                outboundRequest.RequestUri?.PathAndQuery);
            return null;
        }
    }

    private string? ResolveTokenFromStore(string? userId, string? sessionId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var resolution = _tokenStore.Resolve(userId, sessionId);
        if (!resolution.Found && string.IsNullOrWhiteSpace(sessionId))
        {
            resolution = _tokenStore.ResolveByUserId(userId);
        }

        return resolution.Found ? resolution.Token : null;
    }

    private static Uri BuildInternalRefreshUri(HttpContext httpContext)
    {
        var request = httpContext.Request;
        var pathBase = request.PathBase.HasValue ? request.PathBase.Value : string.Empty;
        var refreshPath = string.Concat(pathBase, InternalRefreshPath);

        return new UriBuilder(request.Scheme, request.Host.Host, request.Host.Port ?? -1, refreshPath).Uri;
    }

    private static bool IsAnonymousAllowedPath(string pathAndQuery)
    {
        return pathAndQuery.Contains("/api/PublicExperience/settings", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/status", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/validate-secret", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/auth-provider-configuration/internal", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/auth-provider-configuration", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/authz-provider-configuration/internal", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/authz-provider-configuration", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/InstanceOnboarding/auth-provider-configured", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/translation", StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryResolveUserId(ClaimsPrincipal? user)
    {
        return user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user?.FindFirst("sid")?.Value;
    }
}
