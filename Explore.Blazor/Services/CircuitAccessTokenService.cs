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
/// Stores access tokens keyed by user identity.
/// Tokens are only resolved for the current authenticated user.
/// </summary>
public class CircuitAccessTokenService : ICircuitAccessTokenService
{
    // User-scoped token cache (keyed by user id).
    private static readonly ConcurrentDictionary<string, TokenEntry> _tokenStore = new();

    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<CircuitAccessTokenService> _logger;
    private string? _localToken;
    private string? _userId;

    public CircuitAccessTokenService(IHttpContextAccessor httpContextAccessor, ILogger<CircuitAccessTokenService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string? AccessToken
    {
        get
        {
            var storedToken = GetStoredToken();

            if (!string.IsNullOrEmpty(storedToken))
            {
                if (!string.IsNullOrEmpty(_localToken) && !string.Equals(_localToken, storedToken, StringComparison.Ordinal))
                {
                    _logger.LogInformation(
                        "[CircuitAccessTokenService] Shared token store is overriding a stale circuit-local token for user {UserId}",
                        _userId ?? GetUserIdFromHttpContext() ?? "(unknown)");
                }

                return storedToken;
            }

            if (!IsUsableAccessToken(_localToken))
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
            _localToken = null;
            _logger.LogWarning("[CircuitAccessTokenService] SetToken called with null/empty token");
            return;
        }

        if (!IsUsableAccessToken(token))
        {
            _localToken = null;
            _logger.LogWarning("[CircuitAccessTokenService] SetToken ignored an expired or near-expiry access token");
            return;
        }

        _localToken = token;

        // Extract userId from the JWT token itself (not from HttpContext)
        var userId = ExtractUserIdFromToken(token, _logger) ?? GetUserIdFromHttpContext();

        _logger.LogDebug("[CircuitAccessTokenService] SetToken called. Token length: {TokenLen}, UserId: {UserId}",
            token.Length, userId ?? "(null)");

        if (!string.IsNullOrEmpty(userId))
        {
            _userId = userId;
            _tokenStore[userId] = new TokenEntry(token, DateTime.UtcNow);
            _logger.LogDebug("[CircuitAccessTokenService] Token stored for userId: {UserId}. Store has {Count} entries",
                userId, _tokenStore.Count);
            CleanupOldTokens();
        }
        else
        {
            _logger.LogWarning("[CircuitAccessTokenService] Could not extract userId from token - token not persisted to shared cache");
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

    private string? GetUserIdFromHttpContext()
    {
        var userId = TryResolveUserId(_httpContextAccessor.HttpContext?.User);
        _logger.LogDebug("[CircuitAccessTokenService] GetUserIdFromHttpContext: {UserId}", userId ?? "(null)");
        return userId;
    }

    private string? GetStoredToken()
    {
        var userId = _userId ?? GetUserIdFromHttpContext();
        if (!string.IsNullOrEmpty(userId) && _tokenStore.TryGetValue(userId, out var entry))
        {
            if (IsUsableAccessToken(entry.Token))
            {
                return entry.Token;
            }

            _tokenStore.TryRemove(userId, out _);
        }
        return null;
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

    /// <summary>
    /// Get token for a specific user ID.
    /// </summary>
    public static string? GetTokenForUser(string userId, ILogger? logger = null)
    {
        logger?.LogDebug("[CircuitAccessTokenService.GetTokenForUser] Looking for userId: {UserId}, Store has {Count} entries",
            userId, _tokenStore.Count);

        if (_tokenStore.TryGetValue(userId, out var entry))
        {
            if (IsUsableAccessToken(entry.Token))
            {
                logger?.LogDebug("[CircuitAccessTokenService.GetTokenForUser] Found valid token for {UserId} (length: {Len})",
                    userId, entry.Token.Length);
                return entry.Token;
            }
            _tokenStore.TryRemove(userId, out _);
            logger?.LogDebug("[CircuitAccessTokenService.GetTokenForUser] Token for {UserId} is expired", userId);
        }
        else
        {
            logger?.LogDebug("[CircuitAccessTokenService.GetTokenForUser] No entry found for userId: {UserId}", userId);
        }
        return null;
    }

    private static void CleanupOldTokens()
    {
        var cutoff = DateTime.UtcNow.AddHours(-2);
        var oldKeys = _tokenStore
            .Where(kvp => kvp.Value.CreatedAt < cutoff || !IsUsableAccessToken(kvp.Value.Token))
            .Select(kvp => kvp.Key)
            .ToList();
        foreach (var key in oldKeys)
        {
            _tokenStore.TryRemove(key, out _);
        }
    }

    internal static bool IsUsableAccessToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return true;
            }

            var jwt = handler.ReadJwtToken(token);
            if (jwt.ValidTo == DateTime.MinValue)
            {
                return true;
            }

            return jwt.ValidTo > DateTime.UtcNow.AddSeconds(30);
        }
        catch
        {
            return false;
        }
    }

    private record TokenEntry(string Token, DateTime CreatedAt);
}

/// <summary>
/// HTTP message handler that forwards the access token and trusted tenant slug to API requests.
/// Resolves token for the current authenticated user only.
/// </summary>
public class AccessTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICircuitAccessTokenService _circuitAccessTokenService;
    private readonly ICircuitUserContext _circuitUserContext;
    private readonly ILogger<AccessTokenForwardingHandler> _logger;

    public AccessTokenForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ICircuitAccessTokenService circuitAccessTokenService,
        ICircuitUserContext circuitUserContext,
        ILogger<AccessTokenForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _circuitAccessTokenService = circuitAccessTokenService;
        _circuitUserContext = circuitUserContext;
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
                    if (CircuitAccessTokenService.IsUsableAccessToken(token))
                    {
                        source = "HttpContext";
                        _logger.LogDebug("[AccessTokenForwardingHandler] Got token from HttpContext (length: {Len})", token.Length);
                    }
                    else
                    {
                        _logger.LogWarning(
                            "[AccessTokenForwardingHandler] Ignoring expired or near-expiry HttpContext token at {Path} | TokenSummary={TokenSummary}",
                            request.RequestUri?.PathAndQuery,
                            DescribeToken(token));
                        token = null;
                    }
                }
                else
                {
                    _logger.LogDebug("[AccessTokenForwardingHandler] HttpContext.GetTokenAsync returned null/empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AccessTokenForwardingHandler] Could not get token from HttpContext");
            }
        }

        // Strategy 2: Try to get token from shared store by current user ID
        if (string.IsNullOrEmpty(token))
        {
            var userId = TryResolveUserId(httpContext?.User);

            if (!string.IsNullOrEmpty(userId))
            {
                token = CircuitAccessTokenService.GetTokenForUser(userId, _logger);
                if (!string.IsNullOrEmpty(token))
                {
                    source = "TokenStore(userId)";
                }
            }
            else if (isAuthenticated)
            {
                _logger.LogWarning("[AccessTokenForwardingHandler] Authenticated user has no resolvable user identifier claims at {Path}", request.RequestUri?.PathAndQuery);
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
                    "[AccessTokenForwardingHandler] Got token from CircuitAccessTokenService scoped instance (length: {Len}, user: {UserId})",
                    token.Length,
                    _circuitUserContext.UserId ?? TryResolveUserId(httpContext?.User) ?? "(unknown)");
            }
        }

        // Strategy 4: Use AsyncLocal-backed ICircuitUserContext to get the userId and look up
        // the token in the static store. This works across DI scope boundaries where HttpContext
        // is null but the Blazor circuit async context still flows.
        if (string.IsNullOrEmpty(token))
        {
            var userId = _circuitUserContext.UserId;
            if (!string.IsNullOrEmpty(userId))
            {
                token = CircuitAccessTokenService.GetTokenForUser(userId, _logger);
                if (!string.IsNullOrEmpty(token))
                {
                    source = "CircuitUserContext";
                    _logger.LogDebug("[AccessTokenForwardingHandler] Got token from static store via CircuitUserContext (length: {Len})", token.Length);
                }
            }
        }

        // Add Authorization header if we have a token
        if (!string.IsNullOrEmpty(token) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation(
                "[AccessTokenForwardingHandler] Added Bearer token from {Source} to {Path} | TokenSummary={TokenSummary}",
                source,
                request.RequestUri?.PathAndQuery,
                DescribeToken(token));
        }
        else if (!string.IsNullOrEmpty(token))
        {
            _logger.LogDebug("[AccessTokenForwardingHandler] Authorization header already present for {Path}; token source was {Source}", request.RequestUri?.PathAndQuery, source);
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
                _logger.LogWarning("[AccessTokenForwardingHandler] No token available for current user at {Path} - request will likely fail with 401", path);
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

    private static string DescribeToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (!handler.CanReadToken(token))
            {
                return "unreadable_jwt";
            }

            var jwt = handler.ReadJwtToken(token);
            var userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? jwt.Claims.FirstOrDefault(c => c.Type == "sid")?.Value
                ?? "(none)";

            return $"sub={userId};validTo={jwt.ValidTo:O}";
        }
        catch
        {
            return "jwt_parse_failed";
        }
    }
}
