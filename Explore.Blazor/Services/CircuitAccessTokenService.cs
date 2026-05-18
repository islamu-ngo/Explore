// ABOUTME: Manages access token storage for Blazor Server circuits and token forwarding to API requests.
// ABOUTME: Contains CircuitAccessTokenService (scoped token store) and AccessTokenForwardingHandler (HTTP message handler).

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Explore.Application.Contracts.Services;
using Microsoft.AspNetCore.Authentication;
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
}

public sealed class SetupSecretSessionService : ISetupSecretSessionService
{
    private readonly ConcurrentDictionary<string, SecretEntry> _store = new();

    public void SetForUser(string userId, string secret)
    {
        if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(secret))
        {
            return;
        }

        _store[userId] = new SecretEntry(secret.Trim(), DateTime.UtcNow);
        CleanupExpiredEntries();
    }

    public string? GetForUser(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        if (!_store.TryGetValue(userId, out var entry))
        {
            return null;
        }

        if (entry.StoredAtUtc < DateTime.UtcNow.AddHours(-2))
        {
            _store.TryRemove(userId, out _);
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

        _store.TryRemove(userId, out _);
    }

    private void CleanupExpiredEntries()
    {
        var cutoff = DateTime.UtcNow.AddHours(-2);
        foreach (var key in _store.Where(kvp => kvp.Value.StoredAtUtc < cutoff).Select(kvp => kvp.Key).ToList())
        {
            _store.TryRemove(key, out _);
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
                if (resolution.Found)
                {
                    if (!string.IsNullOrEmpty(_localToken) && !string.Equals(_localToken, resolution.Token, StringComparison.Ordinal))
                    {
                        _logger.LogInformation(
                            "[CircuitAccessTokenService] Store token is overriding stale circuit-local token for {UserId}/{SessionId}",
                            userId, sessionId ?? "(none)");
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
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICircuitAccessTokenService _circuitAccessTokenService;
    private readonly ICircuitUserContext _circuitUserContext;
    private readonly ICircuitTokenStore _tokenStore;
    private readonly ILogger<AccessTokenForwardingHandler> _logger;

    public AccessTokenForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ICircuitAccessTokenService circuitAccessTokenService,
        ICircuitUserContext circuitUserContext,
        ICircuitTokenStore tokenStore,
        ILogger<AccessTokenForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitAccessTokenService = circuitAccessTokenService;
        _circuitUserContext = circuitUserContext;
        _tokenStore = tokenStore;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("[AccessTokenForwardingHandler] Processing request to {Path}", request.RequestUri?.PathAndQuery);

        string? token = null;
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
                    else
                    {
                        _logger.LogWarning(
                            "[AccessTokenForwardingHandler] Ignoring expired or near-expiry HttpContext token at {Path}",
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
            }
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
