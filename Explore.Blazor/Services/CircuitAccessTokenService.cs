using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Server.Circuits;
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
    private readonly ILogger<CircuitAccessTokenService>? _logger;
    private string? _localToken;
    private string? _userId;

    public CircuitAccessTokenService(IHttpContextAccessor httpContextAccessor, ILogger<CircuitAccessTokenService>? logger = null)
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
            _logger?.LogWarning("[CircuitAccessTokenService] SetToken called with null/empty token");
            return;
        }

        // Extract userId from the JWT token itself (not from HttpContext)
        var userId = ExtractUserIdFromToken(token) ?? GetUserIdFromHttpContext();

        _logger?.LogInformation("[CircuitAccessTokenService] SetToken called. Token length: {TokenLen}, UserId: {UserId}",
            token.Length, userId ?? "(null)");

        // Always store as latest token (fallback)
        lock (_latestTokenLock)
        {
            _latestToken = new TokenEntry(token, DateTime.UtcNow);
        }
        Console.WriteLine($"[CircuitAccessTokenService] Stored as latest token (length: {token.Length})");

        if (!string.IsNullOrEmpty(userId))
        {
            _userId = userId;
            _tokenStore[userId] = new TokenEntry(token, DateTime.UtcNow);
            _logger?.LogInformation("[CircuitAccessTokenService] ✓ Token stored in static store for userId: {UserId}. Total tokens in store: {Count}",
                userId, _tokenStore.Count);
            Console.WriteLine($"[CircuitAccessTokenService] ✓ Token stored for userId: {userId}. Store has {_tokenStore.Count} entries");
            CleanupOldTokens();
        }
        else
        {
            _logger?.LogWarning("[CircuitAccessTokenService] Could not extract userId from token - stored as latest only");
            Console.WriteLine($"[CircuitAccessTokenService] ✗ Could not extract userId - stored as latest only");
        }
    }

    /// <summary>
    /// Extract the user ID (sub claim) directly from the JWT token.
    /// This works even without HttpContext.
    /// </summary>
    private static string? ExtractUserIdFromToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            if (handler.CanReadToken(token))
            {
                var jwtToken = handler.ReadJwtToken(token);
                var sub = jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
                Console.WriteLine($"[CircuitAccessTokenService] Extracted userId from JWT: {sub ?? "(null)"}");
                return sub;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CircuitAccessTokenService] Failed to parse JWT: {ex.Message}");
        }
        return null;
    }

    private string? GetUserIdFromHttpContext()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        var userId = user?.FindFirst("sub")?.Value
            ?? user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        Console.WriteLine($"[CircuitAccessTokenService] GetUserIdFromHttpContext: {userId ?? "(null)"}");
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
    public static string? GetAnyValidToken()
    {
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var validEntries = _tokenStore.Values.Where(e => e.CreatedAt > cutoff).ToList();
        Console.WriteLine($"[CircuitAccessTokenService.GetAnyValidToken] Total store entries: {_tokenStore.Count}, Valid entries: {validEntries.Count}");

        var entry = validEntries.FirstOrDefault();
        if (entry != null)
        {
            Console.WriteLine($"[CircuitAccessTokenService.GetAnyValidToken] Found valid token from store (length: {entry.Token.Length})");
            return entry.Token;
        }

        // Fallback to latest token
        lock (_latestTokenLock)
        {
            if (_latestToken != null && _latestToken.CreatedAt > cutoff)
            {
                Console.WriteLine($"[CircuitAccessTokenService.GetAnyValidToken] Found valid latest token (length: {_latestToken.Token.Length})");
                return _latestToken.Token;
            }
        }

        Console.WriteLine("[CircuitAccessTokenService.GetAnyValidToken] No valid tokens found anywhere");
        return null;
    }

    /// <summary>
    /// Get token for a specific user ID.
    /// </summary>
    public static string? GetTokenForUser(string userId)
    {
        Console.WriteLine($"[CircuitAccessTokenService.GetTokenForUser] Looking for userId: {userId}, Store has {_tokenStore.Count} entries, Keys: [{string.Join(", ", _tokenStore.Keys)}]");
        if (_tokenStore.TryGetValue(userId, out var entry))
        {
            if (entry.CreatedAt > DateTime.UtcNow.AddHours(-1))
            {
                Console.WriteLine($"[CircuitAccessTokenService.GetTokenForUser] Found valid token for {userId} (length: {entry.Token.Length})");
                return entry.Token;
            }
            Console.WriteLine($"[CircuitAccessTokenService.GetTokenForUser] Token for {userId} is expired");
        }
        else
        {
            Console.WriteLine($"[CircuitAccessTokenService.GetTokenForUser] No entry found for userId: {userId}");
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
    private const string TenantIdHeaderName = "X-Tenant-Id";

    /// <summary>
    /// Default tenant ID for single-instance deployments.
    /// MUST match Explore.API.Services.TenantContext.DefaultTenantId
    /// and Explore.Persistence.SeedIds.DefaultTenantId.
    /// </summary>
    private static readonly Guid DefaultTenantId = Guid.Parse("018e4e5c-7f00-7000-8000-000000000001");

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
        _logger.LogInformation("[AccessTokenForwardingHandler] Processing request to {Path}", request.RequestUri?.PathAndQuery);

        string? token = null;
        string source = "none";

        // Strategy 1: Try to get token from HttpContext (works during initial HTTP request)
        var httpContext = _httpContextAccessor.HttpContext;
        var isAuthenticated = httpContext?.User?.Identity?.IsAuthenticated == true;
        _logger.LogInformation("[AccessTokenForwardingHandler] HttpContext available: {HasContext}, User authenticated: {IsAuth}",
            httpContext != null, isAuthenticated);

        if (isAuthenticated)
        {
            try
            {
                token = await httpContext!.GetTokenAsync("access_token");
                if (!string.IsNullOrEmpty(token))
                {
                    source = "HttpContext";
                    _logger.LogInformation("[AccessTokenForwardingHandler] Got token from HttpContext (length: {Len})", token.Length);

                    // Debug: Parse and log token details
                    try
                    {
                        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                        if (handler.CanReadToken(token))
                        {
                            var jwt = handler.ReadJwtToken(token);
                            var aud = jwt.Audiences?.ToList() ?? new List<string>();
                            var azp = jwt.Claims.FirstOrDefault(c => c.Type == "azp")?.Value;
                            var iss = jwt.Issuer;
                            var exp = jwt.ValidTo;
                            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                            _logger.LogInformation("[AccessTokenForwardingHandler] Token details - Issuer: {Issuer}, Audiences: [{Audiences}], Azp: {Azp}, Sub: {Sub}, Expires: {Exp}",
                                iss, string.Join(", ", aud), azp ?? "(null)", sub ?? "(null)", exp);
                        }
                        else
                        {
                            _logger.LogWarning("[AccessTokenForwardingHandler] Token is NOT a valid JWT! First 50 chars: {Preview}",
                                token.Length > 50 ? token.Substring(0, 50) + "..." : token);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning("[AccessTokenForwardingHandler] Could not parse token: {Error}", ex.Message);
                    }
                }
                else
                {
                    _logger.LogInformation("[AccessTokenForwardingHandler] HttpContext.GetTokenAsync returned null/empty");
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AccessTokenForwardingHandler] Could not get token from HttpContext: {Message}", ex.Message);
            }
        }

        // Strategy 2: Try to get token from static store by user ID
        if (string.IsNullOrEmpty(token))
        {
            var userId = httpContext?.User?.FindFirst("sub")?.Value
                ?? httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            _logger.LogInformation("[AccessTokenForwardingHandler] Strategy 2: UserId from claims = {UserId}", userId ?? "(null)");

            if (!string.IsNullOrEmpty(userId))
            {
                token = CircuitAccessTokenService.GetTokenForUser(userId);
                if (!string.IsNullOrEmpty(token))
                {
                    source = "StaticStore(userId)";
                    _logger.LogInformation("[AccessTokenForwardingHandler] Got token from static store by userId (length: {Len})", token.Length);
                }
                else
                {
                    _logger.LogInformation("[AccessTokenForwardingHandler] No token in static store for userId: {UserId}", userId);
                }
            }
        }

        // Strategy 3: Last resort - get any valid token from store
        if (string.IsNullOrEmpty(token))
        {
            token = CircuitAccessTokenService.GetAnyValidToken();
            if (!string.IsNullOrEmpty(token))
            {
                source = "StaticStore(any)";
                _logger.LogInformation("[AccessTokenForwardingHandler] Got token from static store (any valid token, length: {Len})", token.Length);
            }
            else
            {
                _logger.LogInformation("[AccessTokenForwardingHandler] No valid tokens in static store at all");
            }
        }

        // Add Authorization header if we have a token
        if (!string.IsNullOrEmpty(token) && !request.Headers.Contains("Authorization"))
        {
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            _logger.LogInformation("[AccessTokenForwardingHandler] ✓ Added Bearer token from {Source} to {Path}", source, request.RequestUri?.PathAndQuery);
        }
        else if (string.IsNullOrEmpty(token))
        {
            _logger.LogWarning("[AccessTokenForwardingHandler] ✗ NO TOKEN AVAILABLE for {Path} - request will likely fail with 401", request.RequestUri?.PathAndQuery);
        }

        // Always add X-Tenant-Id header for multi-tenant isolation
        // This ensures the API knows which tenant the request belongs to
        if (!request.Headers.Contains(TenantIdHeaderName))
        {
            request.Headers.Add(TenantIdHeaderName, DefaultTenantId.ToString());
            _logger.LogDebug("[AccessTokenForwardingHandler] Added {Header}: {TenantId} to {Path}",
                TenantIdHeaderName, DefaultTenantId, request.RequestUri?.PathAndQuery);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
