// ABOUTME: Manages access token storage for Blazor Server circuits and token forwarding to API requests.
// ABOUTME: Contains CircuitAccessTokenService (scoped token store) and AccessTokenForwardingHandler (HTTP message handler).

using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using Explore.Blazor.Client.Constants;
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
    private static readonly ConcurrentDictionary<string, SecretEntry> _store = new();

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

    private static void CleanupExpiredEntries()
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

    public string? AccessToken => _localToken ?? GetStoredToken();

    public void SetToken(string? token)
    {
        _localToken = token;

        if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[CircuitAccessTokenService] SetToken called with null/empty token");
            return;
        }

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
                var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                logger.LogDebug("[CircuitAccessTokenService] Extracted userId from JWT: {UserId}", sub ?? "(null)");
                return sub;
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
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        _logger.LogDebug("[CircuitAccessTokenService] GetUserIdFromHttpContext: {UserId}", userId ?? "(null)");
        return userId;
    }

    private string? GetStoredToken()
    {
        var userId = _userId ?? GetUserIdFromHttpContext();
        if (!string.IsNullOrEmpty(userId) && _tokenStore.TryGetValue(userId, out var entry))
        {
            // Token expires after 1 hour (should match JWT expiry)
            if (entry.CreatedAt > DateTime.UtcNow.AddHours(-1))
            {
                return entry.Token;
            }
        }
        return null;
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
            if (entry.CreatedAt > DateTime.UtcNow.AddHours(-1))
            {
                logger?.LogDebug("[CircuitAccessTokenService.GetTokenForUser] Found valid token for {UserId} (length: {Len})",
                    userId, entry.Token.Length);
                return entry.Token;
            }
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
        var oldKeys = _tokenStore.Where(kvp => kvp.Value.CreatedAt < cutoff).Select(kvp => kvp.Key).ToList();
        foreach (var key in oldKeys)
        {
            _tokenStore.TryRemove(key, out _);
        }
    }

    private record TokenEntry(string Token, DateTime CreatedAt);
}

/// <summary>
/// HTTP message handler that forwards the access token and tenant ID to API requests.
/// Resolves token for the current authenticated user only.
/// </summary>
public class AccessTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ISetupSecretSessionService _setupSecretSessionService;
    private readonly ILogger<AccessTokenForwardingHandler> _logger;

    public AccessTokenForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ISetupSecretSessionService setupSecretSessionService,
        ILogger<AccessTokenForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _setupSecretSessionService = setupSecretSessionService;
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
                    source = "HttpContext";
                    _logger.LogDebug("[AccessTokenForwardingHandler] Got token from HttpContext (length: {Len})", token.Length);
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
            var userId = httpContext?.User?.FindFirst("sub")?.Value
                ?? httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

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

        // Add Authorization header if we have a token
        if (!string.IsNullOrEmpty(token) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("[AccessTokenForwardingHandler] Added Bearer token from {Source} to {Path}", source, request.RequestUri?.PathAndQuery);
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

        var incomingTenantHeader = httpContext?.Request.Headers[TenantConstants.TenantIdHeaderName].FirstOrDefault();
        if (!request.Headers.Contains(TenantConstants.TenantIdHeaderName) &&
            !string.IsNullOrWhiteSpace(incomingTenantHeader))
        {
            request.Headers.Add(TenantConstants.TenantIdHeaderName, incomingTenantHeader);
            _logger.LogDebug("[AccessTokenForwardingHandler] Forwarded tenant header {Header}: {TenantId} to {Path}",
                TenantConstants.TenantIdHeaderName, incomingTenantHeader, request.RequestUri?.PathAndQuery);
        }

        var forwardedHost = httpContext?.Request.Headers["X-Forwarded-Host"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(forwardedHost))
        {
            forwardedHost = httpContext?.Request.Host.Value;
        }

        if (!request.Headers.Contains("X-Forwarded-Host") && !string.IsNullOrWhiteSpace(forwardedHost))
        {
            request.Headers.Add("X-Forwarded-Host", forwardedHost);
            _logger.LogDebug("[AccessTokenForwardingHandler] Forwarded host header X-Forwarded-Host: {Host} to {Path}",
                forwardedHost, request.RequestUri?.PathAndQuery);
        }

        var pathAndQuery = request.RequestUri?.PathAndQuery ?? string.Empty;
        var setupSecret = httpContext?.Request.Cookies["setup-secret"];
        if (string.IsNullOrWhiteSpace(setupSecret))
        {
            var userId = httpContext?.User?.FindFirst("sub")?.Value
                ?? httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrWhiteSpace(userId))
            {
                setupSecret = _setupSecretSessionService.GetForUser(userId);
            }
        }

        if (RequiresSetupSecret(pathAndQuery) &&
            !request.Headers.Contains("X-Setup-Secret") &&
            !string.IsNullOrWhiteSpace(setupSecret))
        {
            request.Headers.Add("X-Setup-Secret", setupSecret);
            _logger.LogDebug("[AccessTokenForwardingHandler] Forwarded setup secret header for {Path}", pathAndQuery);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static bool IsAnonymousAllowedPath(string pathAndQuery)
    {
        return pathAndQuery.Contains("/api/v1/PublicExperience/settings", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/v1/InstanceOnboarding/status", StringComparison.OrdinalIgnoreCase);
    }

    private static bool RequiresSetupSecret(string pathAndQuery)
    {
        return pathAndQuery.Contains("/api/v1/InstanceOnboarding/complete", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/v1/InstanceOnboarding/settings", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/v1/InstanceOnboarding/storage-settings", StringComparison.OrdinalIgnoreCase)
            || pathAndQuery.Contains("/api/v1/InstanceOnboarding/test-storage", StringComparison.OrdinalIgnoreCase);
    }
}
