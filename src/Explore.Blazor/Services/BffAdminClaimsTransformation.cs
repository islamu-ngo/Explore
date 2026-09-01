// ABOUTME: Enriches the BFF cookie principal with persisted administrative authority from the API.
// ABOUTME: Projects instance, tenant, organization, and group scopes at trusted session boundaries.

using System.Net.Http.Headers;
using System.Security.Claims;
using Event.Web.BffHosting.Security;
using Explore.Blazor.Client.Clients;
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

    private const string InstanceAdminClaim = "explore:admin:instance";
    private const string TenantAdminClaim = "explore:admin:tenant";
    private const string OrganizationAdminClaim = "explore:admin:organization";
    private const string GroupAdminClaim = "explore:admin:group";
    private const string InternalUserIdClaim = "internal_user_id";

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
        bool synchronizeUser = false,
        CancellationToken cancellationToken = default)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (!principal.TryGetAdminSubject(out var sub))
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

        var accessToken = properties?.GetTokenValue("access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            _logger.LogDebug(
                "BFF admin enrichment skipped | Outcome={Outcome} Reason={Reason} Purpose={Purpose}",
                "skipped", "access_token_missing", "admin");
            return HasAnyAdminClaims(principal);
        }

        var cacheKey = $"{CacheKeyPrefix}{sub.PartitionKey}";

        if (synchronizeUser)
        {
            var internalUserId = await SynchronizeUserAsync(accessToken, cancellationToken);
            if (internalUserId is not null)
            {
                ReplaceInternalUserIdClaim(principal, internalUserId.Value);
            }

            _cache.Remove(cacheKey);
        }

        if (forceRefresh)
        {
            _cache.Remove(cacheKey);
        }

        if (_cache.TryGetValue(cacheKey, out BffAdminAuthorityCacheEntry? cached) && cached is not null)
        {
            if (cached.Authority is not null)
            {
                ReplaceAdminClaims(principal, cached.Authority);
                return cached.Authority.HasAnyAuthority == true;
            }

            return HasAnyAdminClaims(principal);
        }

        var authority = await FetchAdminAuthorityAsync(accessToken, cancellationToken);
        if (authority is not null)
        {
            var ttl = authority.HasAnyAuthority == true ? PositiveCacheDuration : NegativeCacheDuration;
            _cache.Set(cacheKey, BffAdminAuthorityCacheEntry.Success(authority), ttl);
            ReplaceAdminClaims(principal, authority);
            return authority.HasAnyAuthority == true;
        }

        _cache.Set(cacheKey, BffAdminAuthorityCacheEntry.Failure, FailureCacheDuration);
        return HasAnyAdminClaims(principal);
    }

    private async Task<Guid?> SynchronizeUserAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var apiClient = new EventApiClient(client);
            var response = await apiClient.SyncUserAsync(cancellationToken: cancellationToken);
            return response.Success == true && response.Id is { } internalUserId && internalUserId != Guid.Empty
                ? internalUserId
                : null;
        }
        catch (ApiException ex)
        {
            _logger.LogWarning(
                "BFF admin synchronization completed | Outcome={Outcome} Reason={Reason} Purpose={Purpose} StatusCode={StatusCode}",
                "rejected", "downstream_status", "admin", ex.StatusCode);
            return null;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "BFF admin synchronization completed | Outcome={Outcome} Reason={Reason} Purpose={Purpose}",
                "rejected", "timeout", "admin");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "BFF admin synchronization completed | Outcome={Outcome} Reason={Reason} Purpose={Purpose} FailureType={FailureType}",
                "rejected", "exception", "admin", ex.GetType().Name);
            return null;
        }
    }

    private async Task<AdminAuthorityDto?> FetchAdminAuthorityAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var apiClient = new EventApiClient(client);
            return await apiClient.GetCurrentUserAdminAuthorityAsync(cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "BFF admin authority fetch completed | Outcome={Outcome} Reason={Reason} Purpose={Purpose}",
                "rejected", "timeout", "admin");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "BFF admin authority fetch completed | Outcome={Outcome} Reason={Reason} Purpose={Purpose} FailureType={FailureType}",
                "rejected", "exception", "admin", ex.GetType().Name);
            return null;
        }
    }

    private static void ReplaceAdminClaims(ClaimsPrincipal principal, AdminAuthorityDto authority)
    {
        RemoveAdminClaims(principal);

        if (authority.HasAnyAuthority != true)
        {
            return;
        }

        var identity = new ClaimsIdentity();

        if (authority.IsInstanceAdmin == true)
        {
            identity.AddClaim(new Claim(InstanceAdminClaim, "true"));
        }

        foreach (var tenantId in authority.AdminTenantIds ?? [])
        {
            identity.AddClaim(new Claim(TenantAdminClaim, tenantId.ToString()));
        }

        foreach (var orgId in authority.AdminOrganizationIds ?? [])
        {
            identity.AddClaim(new Claim(OrganizationAdminClaim, orgId.ToString()));
        }

        foreach (var groupId in authority.AdminGroupIds ?? [])
        {
            identity.AddClaim(new Claim(GroupAdminClaim, groupId.ToString()));
        }

        if (identity.Claims.Any())
        {
            principal.AddIdentity(identity);
        }
    }

    private static void ReplaceInternalUserIdClaim(ClaimsPrincipal principal, Guid internalUserId)
    {
        foreach (var identity in principal.Identities)
        {
            foreach (var claim in identity.Claims.Where(claim => claim.Type == InternalUserIdClaim).ToList())
            {
                identity.RemoveClaim(claim);
            }
        }

        principal.AddIdentity(new ClaimsIdentity([new Claim(InternalUserIdClaim, internalUserId.ToString())]));
    }

    private static bool HasAnyAdminClaims(ClaimsPrincipal principal)
    {
        return principal.HasClaim(c => c.Type is InstanceAdminClaim
            or TenantAdminClaim
            or OrganizationAdminClaim
            or GroupAdminClaim);
    }

    private static void RemoveAdminClaims(ClaimsPrincipal principal)
    {
        foreach (var identity in principal.Identities)
        {
            foreach (var claim in identity.Claims
                         .Where(c => c.Type is InstanceAdminClaim
                             or TenantAdminClaim
                             or OrganizationAdminClaim
                             or GroupAdminClaim)
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

internal sealed record BffAdminAuthorityCacheEntry(AdminAuthorityDto? Authority)
{
    public static readonly BffAdminAuthorityCacheEntry Failure = new((AdminAuthorityDto?)null);

    public static BffAdminAuthorityCacheEntry Success(AdminAuthorityDto authority) => new(authority);
}
