// ABOUTME: BFF-side admin claims enrichment service that resolves admin authority from the API.
// Bridges the gap between the API's DB-first admin context and the Blazor client's claim-based checks.
// This service enriches the cookie principal at session boundaries instead of using per-request claims transformation.

using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace Explore.Blazor.Services;

/// <summary>
/// Enriches the authenticated BFF cookie principal with admin authority claims by calling
/// the API's admin-authority endpoint at sign-in and refresh boundaries.
/// <para>
/// Positive results (user has admin authority) are cached for 5 minutes.
/// Negative results (user has no admin authority) are cached for 30 seconds to allow quick
/// recognition after role assignments (e.g., instance onboarding). Remote fetch failures are
/// cached briefly to avoid retry storms while downstream auth services are unhealthy.
/// </para>
/// </summary>
public sealed class BffAdminClaimsTransformation
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IMemoryCache _cache;
    private readonly IBffOnboardingStatusProvider _onboardingStatusProvider;
    private readonly ILogger<BffAdminClaimsTransformation> _logger;

    private static readonly TimeSpan PositiveCacheDuration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan NegativeCacheDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan FailureCacheDuration = TimeSpan.FromSeconds(10);

    internal const string CacheKeyPrefix = "BffAdminClaims_";
    internal const string HttpClientName = "AdminAuthority";

    // Claim types matching Explore.Application.Authorization.AdminClaimTypes constants.
    // Duplicated here to avoid adding a project reference from the BFF to Application layer.
    private const string InstanceAdminClaim = "explore:admin:instance";
    private const string TenantAdminClaim = "explore:admin:tenant";
    private const string OrganizationAdminClaim = "explore:admin:organization";

    public BffAdminClaimsTransformation(
        IHttpClientFactory httpClientFactory,
        IMemoryCache cache,
        IBffOnboardingStatusProvider onboardingStatusProvider,
        ILogger<BffAdminClaimsTransformation> logger)
    {
        _httpClientFactory = httpClientFactory;
        _cache = cache;
        _onboardingStatusProvider = onboardingStatusProvider;
        _logger = logger;
    }

    public async Task<bool> EnrichPrincipalAsync(
        ClaimsPrincipal principal,
        AuthenticationProperties? properties,
        bool forceRefresh = false,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        var sub = principal.FindFirst("sub")?.Value
                  ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
                  ?? principal.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(sub))
        {
            return false;
        }

        // Pre-onboarding skip: no admin records can exist in the DB yet, so calling
        // api/User/admin-authority would always yield empty and can hang when the API's
        // JWT signing keys are still warming up. Strip any stale admin claims and continue.
        var onboardingStatus = await _onboardingStatusProvider
            .GetStatusAsync(cancellationToken)
            .ConfigureAwait(false);
        if (onboardingStatus.Known && !onboardingStatus.IsCompleted)
        {
            RemoveAdminClaims(principal);
            return false;
        }

        var cacheKey = $"{CacheKeyPrefix}{sub}";

        if (forceRefresh)
        {
            _cache.Remove(cacheKey);
        }

        if (_cache.TryGetValue(cacheKey, out BffAdminAuthorityCacheEntry? cached) && cached is not null)
        {
            if (cached.Authority is not null)
            {
                ReplaceAdminClaims(principal, cached.Authority);
                return cached.Authority.HasAnyAuthority;
            }

            return HasAnyAdminClaims(principal);
        }

        var accessToken = properties?.GetTokenValue("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogDebug("BffAdminClaimsTransformation: No access token available for user {UserId}", sub);
            return HasAnyAdminClaims(principal);
        }

        var authority = await FetchAdminAuthorityAsync(sub, accessToken, cancellationToken);
        if (authority is not null)
        {
            var ttl = authority.HasAnyAuthority ? PositiveCacheDuration : NegativeCacheDuration;
            _cache.Set(cacheKey, BffAdminAuthorityCacheEntry.Success(authority), ttl);
            ReplaceAdminClaims(principal, authority);
            return authority.HasAnyAuthority;
        }

        _cache.Set(cacheKey, BffAdminAuthorityCacheEntry.Failure, FailureCacheDuration);
        return HasAnyAdminClaims(principal);
    }

    private async Task<BffAdminAuthorityResponse?> FetchAdminAuthorityAsync(
        string userId,
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, "api/User/admin-authority");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

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
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "BffAdminClaimsTransformation: Timed out while fetching admin authority for user {UserId}",
                userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "BffAdminClaimsTransformation: Failed to fetch admin authority for user {UserId}. " +
                "Keeping existing session claims. Server-side authorization remains enforced.", userId);
            return null;
        }
    }

    private static void ReplaceAdminClaims(ClaimsPrincipal principal, BffAdminAuthorityResponse authority)
    {
        RemoveAdminClaims(principal);

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

    private static bool HasAnyAdminClaims(ClaimsPrincipal principal)
    {
        return principal.HasClaim(c => c.Type is InstanceAdminClaim or TenantAdminClaim or OrganizationAdminClaim);
    }

    private static void RemoveAdminClaims(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            foreach (var claim in identity.Claims
                         .Where(c => c.Type is InstanceAdminClaim or TenantAdminClaim or OrganizationAdminClaim)
                         .ToList())
            {
                identity.RemoveClaim(claim);
            }
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

internal sealed record BffAdminAuthorityCacheEntry(BffAdminAuthorityResponse? Authority)
{
    public static readonly BffAdminAuthorityCacheEntry Failure = new((BffAdminAuthorityResponse?)null);

    public static BffAdminAuthorityCacheEntry Success(BffAdminAuthorityResponse authority) => new(authority);
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
