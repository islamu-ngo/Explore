// ABOUTME: Manages access token storage for Blazor Server circuits and token forwarding to API requests.
// ABOUTME: Contains CircuitAccessTokenService (scoped token store) and AccessTokenForwardingHandler (HTTP message handler).

using Explore.Blazor.Client.Constants;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;

namespace Explore.Blazor.Services;

public interface ICircuitAccessTokenService
{
    string? AccessToken { get; }
    void SetToken(string? token);
}

/// <summary>
/// Stores the access token for the current user session.
/// Uses a static ConcurrentDictionary keyed by user identity for cross-scope access.
/// Also stores a "latest token" for fallback when user ID cannot be determined.
/// </summary>
public class CircuitAccessTokenService : ICircuitAccessTokenService
{
    // Static storage for tokens indexed by user identifier
    // This allows the HttpMessageHandler to access the token regardless of scope
    private static readonly ConcurrentDictionary<string, TokenEntry> _tokenStore = new();

    // Fallback: store the most recent valid token for cases where userId can't be determined
    private static TokenEntry? _latestToken;
    private static readonly object _latestTokenLock = new();

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

        // Always store as latest token (fallback)
        lock (_latestTokenLock)
        {
            _latestToken = new TokenEntry(token, DateTime.UtcNow);
        }
        _logger.LogDebug("[CircuitAccessTokenService] Stored as latest token (length: {TokenLen})", token.Length);

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
            _logger.LogWarning("[CircuitAccessTokenService] Could not extract userId from token - stored as latest only");
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
    /// Try to get any valid token from the store (for unauthenticated contexts).
    /// First checks the static store, then falls back to the latest stored token.
    /// </summary>
    public static string? GetAnyValidToken(ILogger? logger = null)
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var validEntries = _tokenStore.Values.Where(e => e.CreatedAt > cutoff).ToList();
        logger?.LogDebug("[CircuitAccessTokenService.GetAnyValidToken] Total store entries: {Total}, Valid entries: {Valid}",
            _tokenStore.Count, validEntries.Count);

        var entry = validEntries.FirstOrDefault();
        if (entry != null)
        {
            logger?.LogDebug("[CircuitAccessTokenService.GetAnyValidToken] Found valid token from store (length: {Len})", entry.Token.Length);
            return entry.Token;
        }

        // Fallback to latest token
        lock (_latestTokenLock)
        {
            if (_latestToken != null && _latestToken.CreatedAt > cutoff)
            {
                logger?.LogDebug("[CircuitAccessTokenService.GetAnyValidToken] Found valid latest token (length: {Len})", _latestToken.Token.Length);
                return _latestToken.Token;
            }
        }

        logger?.LogDebug("[CircuitAccessTokenService.GetAnyValidToken] No valid tokens found anywhere");
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
/// Uses multiple strategies to obtain the token.
/// </summary>
public class AccessTokenForwardingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AccessTokenForwardingHandler> _logger;

    public AccessTokenForwardingHandler(
        IHttpContextAccessor httpContextAccessor,
        ILogger<AccessTokenForwardingHandler> logger)
    {
        _httpContextAccessor = httpContextAccessor;
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

        // Strategy 2: Try to get token from static store by user ID
        if (string.IsNullOrEmpty(token))
        {
            var userId = httpContext?.User?.FindFirst("sub")?.Value
                ?? httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (!string.IsNullOrEmpty(userId))
            {
                token = CircuitAccessTokenService.GetTokenForUser(userId, _logger);
                if (!string.IsNullOrEmpty(token))
                {
                    source = "StaticStore(userId)";
                }
            }
        }

        // Strategy 3: Last resort - get any valid token from store
        if (string.IsNullOrEmpty(token))
        {
            token = CircuitAccessTokenService.GetAnyValidToken(_logger);
            if (!string.IsNullOrEmpty(token))
            {
                source = "StaticStore(any)";
            }
        }

        // Add Authorization header if we have a token
        if (!string.IsNullOrEmpty(token) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("[AccessTokenForwardingHandler] Added Bearer token from {Source} to {Path}", source, request.RequestUri?.PathAndQuery);
        }
        else if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[AccessTokenForwardingHandler] No token available for {Path} - request will likely fail with 401", request.RequestUri?.PathAndQuery);
        }

        // Always add X-Tenant-Id header for multi-tenant isolation
        // This ensures the API knows which tenant the request belongs to
        if (!request.Headers.Contains(TenantConstants.TenantIdHeaderName))
        {
            request.Headers.Add(TenantConstants.TenantIdHeaderName, TenantConstants.DefaultTenantId.ToString());
            _logger.LogDebug("[AccessTokenForwardingHandler] Added {Header}: {TenantId} to {Path}",
                TenantConstants.TenantIdHeaderName, TenantConstants.DefaultTenantId, request.RequestUri?.PathAndQuery);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
