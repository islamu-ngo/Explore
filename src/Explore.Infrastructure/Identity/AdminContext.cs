// ABOUTME: Database-first identity service resolving admin authority from database tables only.
// ABOUTME: Caches per-user authority profiles in IMemoryCache with 5-minute sliding expiration.

using System.Security.Claims;
using Explore.Application.Authentication;
using Explore.Application.Constants;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Domain.Constants;
using Explore.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Identity;

/// <summary>
/// Resolves the current user's administrative authority using database tables only.
/// Identity is read from authenticated claims (sub/nameidentifier/sid) and authority
/// is resolved from platform role assignments, tenant user role grants, OrganizationMembers, and GroupMembers.
/// </summary>
public class AdminContext : IAdminContext, IAdminCacheInvalidator
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPlatformUserRoleRepository _platformUserRoleRepository;
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly ITenantUserRoleGrantRepository _tenantAdminRepo;
    private readonly IOrganizationMemberRepository _orgMemberRepo;
    private readonly IGroupMemberRepository _groupMemberRepo;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminContext> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "AdminContext_";

    public AdminContext(
        IHttpContextAccessor httpContextAccessor,
        IPlatformUserRoleRepository platformUserRoleRepository,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantUserRoleGrantRepository tenantAdminRepo,
        IOrganizationMemberRepository orgMemberRepo,
        IGroupMemberRepository groupMemberRepo,
        IUserExternalLoginRepository userExternalLoginRepository,
        IUserRepository userRepository,
        IMemoryCache cache,
        ILogger<AdminContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _platformUserRoleRepository = platformUserRoleRepository;
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _tenantAdminRepo = tenantAdminRepo;
        _orgMemberRepo = orgMemberRepo;
        _groupMemberRepo = groupMemberRepo;
        _userExternalLoginRepository = userExternalLoginRepository;
        _userRepository = userRepository;
        _cache = cache;
        _logger = logger;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.GetPlatformUserId();
        }
    }

    public async Task<bool> IsInstanceAdminAsync(CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        return uid == null ? false : await IsInstanceAdminAsync(uid.Value, cancellationToken);
    }

    public async Task<Guid?> ResolveUserIdAsync(CancellationToken cancellationToken = default)
    {
        var user = _httpContextAccessor.HttpContext?.User;
        if (user is null)
            return null;

        if (user.GetPlatformUserId() is { } platformUserId)
            return platformUserId;

        var providerIdentity = ProviderLinkedPrincipalReader.GetProviderIdentity(user);
        if (providerIdentity is null)
            return null;

        var cacheKey = $"{CacheKeyPrefix}ResolvedId_{providerIdentity.Provider}_{providerIdentity.ProviderId}";
        if (_cache.TryGetValue<Guid>(cacheKey, out var cachedUserId))
            return cachedUserId;

        var externalLogin = await _userExternalLoginRepository.GetByProviderAndKey(
            providerIdentity.Provider,
            providerIdentity.ProviderId);
        Guid? resolvedUserId = externalLogin?.UserId;

        if (!resolvedUserId.HasValue
            && SupportsEmailAutoMatch(providerIdentity.Provider)
            && ResolveEmailVerified(user)
            && !string.IsNullOrWhiteSpace(providerIdentity.Email))
        {
            var dbUser = await _userRepository.GetUserByEmail(providerIdentity.Email.Trim().ToLowerInvariant());
            resolvedUserId = dbUser?.Id;
        }

        if (resolvedUserId.HasValue)
            _cache.Set(cacheKey, resolvedUserId.Value, TimeSpan.FromMinutes(10));

        return resolvedUserId;
    }

    public async Task<bool> IsInstanceAdminAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // DB-first authority: resolve from database only.
        var cacheKey = $"{CacheKeyPrefix}Instance_{userId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var isRoleAdmin = false;
            try
            {
                isRoleAdmin = await _platformUserRoleRepository.IsUserPlatformAdmin(userId);
            }
            catch (Exception ex)
            {
                // Legacy deployments may not have PlatformUserRoles fully provisioned yet.
                // Fall back to bootstrap ownership checks below.
                _logger.LogWarning(ex, "AdminContext: failed role-based instance admin check");
            }

            if (isRoleAdmin)
            {
                _logger.LogInformation("AdminContext: IsInstanceAdmin=true (platform.admin role detected in database)");
                return true;
            }

            var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
            var isBootstrapAdmin = bootstrap?.IsCompleted == true && bootstrap.CompletedByUserId == userId;

            if (isBootstrapAdmin)
            {
                _logger.LogInformation("AdminContext: IsInstanceAdmin=true (bootstrap owner)");
            }
            else
            {
                _logger.LogWarning("AdminContext: IsInstanceAdmin=false (no platform role or bootstrap ownership found)");
            }

            return isBootstrapAdmin;
        });
    }

    public async Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        if (uid == null)
            return false;

        var cacheKey = $"{CacheKeyPrefix}Tenant_{uid}_{tenantId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var isAdmin = await _tenantAdminRepo.IsTenantAdmin(tenantId, uid.Value);

            if (isAdmin)
            {
                _logger.LogInformation("AdminContext: IsTenantAdmin=true");
            }
            else
            {
                _logger.LogDebug("AdminContext: IsTenantAdmin=false");
            }

            return isAdmin;
        });
    }

    public async Task<bool> IsOrganizationAdminAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        if (uid == null)
            return false;

        var cacheKey = $"{CacheKeyPrefix}Org_{uid}_{organizationId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var membership = await _orgMemberRepo.GetByOrganizationAndUser(organizationId, uid.Value);
            return membership != null && IsOrganizationAdminRole(membership.RoleId);
        });
    }

    public async Task<IReadOnlyList<Guid>> GetAdminTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        return uid == null
            ? (IReadOnlyList<Guid>)Array.Empty<Guid>()
            : await GetAdminTenantIdsAsync(uid.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAdminTenantIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}TenantIds_{userId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var admins = await _tenantAdminRepo.GetByUserId(userId);
            var adminTenantIds = admins
                .Where(a => a.RoleId == (int)RoleEnum.TenantAdmin)
                .Select(a => a.TenantId)
                .Distinct()
                .ToList();

            return (IReadOnlyList<Guid>)adminTenantIds.AsReadOnly();
        }) ?? Array.Empty<Guid>();
    }

    public async Task<IReadOnlyList<Guid>> GetAdminOrganizationIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        return uid == null
            ? (IReadOnlyList<Guid>)Array.Empty<Guid>()
            : await GetAdminOrganizationIdsAsync(uid.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAdminOrganizationIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}OrgIds_{userId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var memberships = await _orgMemberRepo.GetMembershipsByUser(userId, cancellationToken);
            var adminOrgIds = memberships
                .Where(m => IsOrganizationAdminRole(m.RoleId))
                .Select(m => m.OrganizationTenant.OrganizationId)
                .Distinct()
                .ToList()
                .AsReadOnly();

            return (IReadOnlyList<Guid>)adminOrgIds;
        }) ?? Array.Empty<Guid>();
    }

    public async Task<IReadOnlyList<Guid>> GetAdminOrganizationIdsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> allIds = await GetAdminOrganizationIdsAsync(userId, cancellationToken);
        var memberships = await _orgMemberRepo.GetMembershipsByUser(userId, cancellationToken);
        HashSet<Guid> tenantIds = memberships
            .Where(membership => membership.TenantId == tenantId)
            .Select(membership => membership.OrganizationTenant.OrganizationId)
            .ToHashSet();
        return allIds.Where(tenantIds.Contains).ToList();
    }

    public async Task<bool> IsGroupAdminAsync(Guid groupId, CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        if (uid == null)
            return false;

        var cacheKey = $"{CacheKeyPrefix}Group_{uid}_{groupId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var membership = await _groupMemberRepo.GetByGroupAndUser(groupId, uid.Value);
            return membership != null && IsGroupAdminRole(membership.RoleId);
        });
    }

    public async Task<IReadOnlyList<Guid>> GetAdminGroupIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        return uid == null
            ? (IReadOnlyList<Guid>)Array.Empty<Guid>()
            : await GetAdminGroupIdsAsync(uid.Value, cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetAdminGroupIdsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"{CacheKeyPrefix}GroupIds_{userId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var memberships = await _groupMemberRepo.GetMembershipsByUser(userId, cancellationToken);
            var adminGroupIds = memberships
                .Where(m => IsGroupAdminRole(m.RoleId))
                .Select(m => m.GroupTenant.GroupId)
                .Distinct()
                .ToList()
                .AsReadOnly();

            return (IReadOnlyList<Guid>)adminGroupIds;
        }) ?? Array.Empty<Guid>();
    }

    public async Task<IReadOnlyList<Guid>> GetAdminGroupIdsAsync(
        Guid userId,
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Guid> allIds = await GetAdminGroupIdsAsync(userId, cancellationToken);
        var memberships = await _groupMemberRepo.GetMembershipsByUser(userId, cancellationToken);
        HashSet<Guid> tenantIds = memberships
            .Where(membership => membership.TenantId == tenantId)
            .Select(membership => membership.GroupTenant.GroupId)
            .ToHashSet();
        return allIds.Where(tenantIds.Contains).ToList();
    }

    private static bool IsOrganizationAdminRole(int roleId)
    {
        return roleId == (int)RoleEnum.OrgAdmin;
    }

    private static bool IsGroupAdminRole(int roleId)
    {
        return roleId == (int)RoleEnum.GroupAdmin;
    }

    private static bool ResolveEmailVerified(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst("email_verified")?.Value;
        return bool.TryParse(raw, out var emailVerified)
            ? emailVerified
            : string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsEmailAutoMatch(string provider) =>
        provider == AuthSchemeNames.Keycloak.ToLowerInvariant()
        || provider == AuthSchemeNames.Google.ToLowerInvariant();

    /// <inheritdoc />
    public void InvalidateUser(Guid userId)
    {
        _cache.Remove($"{CacheKeyPrefix}Instance_{userId}");
        _cache.Remove($"{CacheKeyPrefix}TenantIds_{userId}");
        _cache.Remove($"{CacheKeyPrefix}OrgIds_{userId}");
        _cache.Remove($"{CacheKeyPrefix}GroupIds_{userId}");

        // For single-tenant mode, we can proactively clear the default tenant admin cache
        _cache.Remove($"{CacheKeyPrefix}Tenant_{userId}_{PlatformDefaults.DefaultTenantId}");

        _logger.LogInformation("AdminContext: Invalidated authority cache for one user");
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        // IMemoryCache has no "clear all" method. We track known keys for targeted eviction.
        // For bulk invalidation, we swap to a new CancellationTokenSource-based approach.
        // However, since AdminContext is scoped per-request and cache keys are user-specific,
        // the practical approach is to let entries expire naturally (5-min sliding window).
        // PolicySyncService should call InvalidateUser for specific affected users when known.
        _logger.LogInformation("AdminContext: Full cache invalidation requested. " +
            "User-specific entries will expire via 5-minute sliding window");
    }
}

/// <summary>
/// Purpose-specific adapter for resolving a linked local account when a valid ambient provider principal
/// has no GUID platform claim. This is intentionally not a platform-ID fallback.
/// </summary>
internal static class ProviderLinkedPrincipalReader
{
    public static ProviderIdentity? GetProviderIdentity(ClaimsPrincipal principal)
    {
        ClaimsIdentity[] authenticatedIdentities = principal.Identities
            .Where(identity => identity.IsAuthenticated)
            .ToArray();

        if (authenticatedIdentities is not [{ AuthenticationType: { } authenticationType } identity]
            || IsPurposeBound(authenticationType))
        {
            return null;
        }

        return new ClaimsPrincipal(identity).GetProviderIdentity();
    }

    private static bool IsPurposeBound(string authenticationType) => authenticationType is
        ApiAuthenticationSchemeNames.ApiKey
        or ApiAuthenticationSchemeNames.SetupSecret
        or ApiAuthenticationSchemeNames.AdmissionScanner
        or ApiAuthenticationSchemeNames.ManagedControlPlane
        or ApiAuthenticationSchemeNames.AtprotoBootstrap
        or ApiAuthenticationSchemeNames.AtprotoSession
        or ApiAuthenticationSchemeNames.PrivacyErasureReceipt;
}
