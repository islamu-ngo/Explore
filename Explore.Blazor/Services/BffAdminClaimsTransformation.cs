// ABOUTME: BFF-side IClaimsTransformation that resolves admin authority from the API.
// Bridges the gap between the API's DB-first admin context and the Blazor WASM client's claim-based checks.
// This transformation runs on the Blazor BFF server during authentication, calling the API to
// get admin authority and adding the corresponding claims to the ClaimsPrincipal.
// These claims are then serialized to WASM via AddAuthenticationStateSerialization.

using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;

namespace Explore.Blazor.Services;

/// <summary>
/// Claims transformation for the BFF server that enriches the authenticated ClaimsPrincipal
/// with admin authority claims by calling the API's admin-authority endpoint.
/// <para>
/// Positive results (user has admin authority) are cached for 5 minutes.
/// Negative results (user has no admin authority) are cached for 30 seconds to allow quick
/// recognition after role assignments (e.g., instance onboarding).
/// </para>
/// </summary>
public sealed class BffAdminClaimsTransformation : IClaimsTransformation
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAuthenticationHandlerProvider _handlerProvider;
    private readonly IMemoryCache _cache;
    private readonly ILogger<BffAdminClaimsTransformation> _logger;

    private static readonly TimeSpan PositiveCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromSeconds(30);

    internal const string CacheKeyPrefix = "BffAdminClaims_";
    internal const string HttpClientName = "AdminAuthority";

    // Claim types matching Explore.Application.Authorization.AdminClaimTypes constants.
    // Duplicated here to avoid adding a project reference from the BFF to Application layer.
    private const string InstanceAdminClaim = "explore:admin:instance";
    private const string TenantAdminClaim = "explore:admin:tenant";
    private const string OrganizationAdminClaim = "explore:admin:organization";

    public BffAdminClaimsTransformation(
        IHttpClientFactory httpClientFactory,
        IHttpContextAccessor httpContextAccessor,
        IAuthenticationHandlerProvider handlerProvider,
        IMemoryCache cache,
        ILogger<BffAdminClaimsTransformation> logger)
    {
        _httpClientFactory = httpClientFactory;
        _httpContextAccessor = httpContextAccessor;
        _handlerProvider = handlerProvider;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return principal;
        }

        // Skip if admin claims are already present (avoid duplicate transformation)
        if (principal.HasClaim(c => c.Type is InstanceAdminClaim
                                        or TenantAdminClaim
                                        or OrganizationAdminClaim))
        {
            return principal;
        }

        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            return principal;
        }

        var cacheKey = $"{CacheKeyPrefix}{sub}";

        if (_cache.TryGetValue(cacheKey, out BffAdminAuthorityResponse? cached) && cached is not null)
        {
            AddAdminClaims(principal, cached);
            return principal;
        }

        var authority = await FetchAdminAuthorityAsync(sub);
        if (authority is not null)
        {
            var ttl = authority.HasAnyAuthority ? PositiveCacheDuration : NegativeCacheDuration;
            _cache.Set(cacheKey, authority, ttl);
            AddAdminClaims(principal, authority);
        }

        return principal;
    }

    private async Task<BffAdminAuthorityResponse?> FetchAdminAuthorityAsync(string userId)
    {
        try
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext is null)
            {
                _logger.LogDebug("BffAdminClaimsTransformation: No HttpContext available");
                return null;
            }

            // Call the underlying cookie handler directly to get the access token.
            // Do NOT use httpContext.GetTokenAsync() — it triggers AuthenticateAsync
            // which re-invokes this IClaimsTransformation, causing infinite recursion
            // and a stack overflow.
            string? token = null;
            var handler = await _handlerProvider.GetHandlerAsync(
                httpContext, CookieAuthenticationDefaults.AuthenticationScheme);

            if (handler is IAuthenticationHandler authHandler)
            {
                var authResult = await authHandler.AuthenticateAsync();
                token = authResult?.Properties?.GetTokenValue("access_token");
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                _logger.LogDebug("BffAdminClaimsTransformation: No access token available for user {UserId}", userId);
                return null;
            }

            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/User/admin-authority");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "BffAdminClaimsTransformation: API returned {StatusCode} for user {UserId}",
                    response.StatusCode, userId);
                return null;
            }

            var authority = await response.Content.ReadFromJsonAsync<BffAdminAuthorityResponse>();
            return authority;
        }
        catch (Exception ex)
        {
            // Fail open — log warning but don't block authentication.
            // The API's authorization layer (Cerbos/MediatR behavior) provides the hard security boundary.
            _logger.LogWarning(ex,
                "BffAdminClaimsTransformation: Failed to fetch admin authority for user {UserId}. " +
                "Admin UI will be hidden but server-side authorization remains enforced.", userId);
            return null;
        }
    }

    private static void AddAdminClaims(ClaimsPrincipal principal, BffAdminAuthorityResponse authority)
    {
        if (!authority.HasAnyAuthority)
        {
            return;
        }

        var identity = new ClaimsIdentity();

        if (authority.IsInstanceAdmin)
        {
            identity.AddClaim(new Claim(InstanceAdminClaim, "true"));
        }

        foreach (var tenantId in authority.AdminTenantIds)
        {
            identity.AddClaim(new Claim(TenantAdminClaim, tenantId.ToString()));
        }

        foreach (var orgId in authority.AdminOrganizationIds)
        {
            identity.AddClaim(new Claim(OrganizationAdminClaim, orgId.ToString()));
        }

        if (identity.Claims.Any())
        {
            principal.AddIdentity(identity);
        }
    }

    /// <summary>
    /// Invalidates the cached admin authority for the specified user.
    /// Call this after role changes (e.g., onboarding completion) to force a fresh API lookup.
    /// </summary>
    public void InvalidateUser(string userId)
    {
        _cache.Remove($"{CacheKeyPrefix}{userId}");
    }
}

/// <summary>
/// BFF-local deserialization model for the API's admin-authority response.
/// Mirrors Explore.Application.DTOs.User.AdminAuthorityDto without requiring a project reference.
/// </summary>
internal sealed class BffAdminAuthorityResponse
{
    public bool IsInstanceAdmin { get; set; }
    public List<Guid> AdminTenantIds { get; set; } = [];
    public List<Guid> AdminOrganizationIds { get; set; } = [];
    public bool HasAnyAuthority => IsInstanceAdmin || AdminTenantIds.Count > 0 || AdminOrganizationIds.Count > 0;
}
