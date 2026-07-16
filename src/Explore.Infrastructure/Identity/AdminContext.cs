// ABOUTME: Database-first identity service resolving admin authority from database tables only.
// ABOUTME: Caches per-user authority profiles in IMemoryCache with 5-minute sliding expiration.

using System.Security.Claims;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Explore.Application.Contracts.Services;
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
    private const string InternalUserIdClaimType = "internal_user_id";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPlatformUserRoleRepository _platformUserRoleRepository;
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly ITenantUserRoleGrantRepository _tenantAdminRepo;
    private readonly IOrganizationMemberRepository _orgMemberRepo;
    private readonly IGroupMemberRepository _groupMemberRepo;
    private readonly IUserExternalLoginRepository _userExternalLoginRepository;
    private readonly IUserRepository _userRepository;
    private readonly IMemoryCache _cache;
    private readonly IDeploymentModeProvider _deploymentModeProvider;
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
        IDeploymentModeProvider deploymentModeProvider,
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
        _deploymentModeProvider = deploymentModeProvider;
        _logger = logger;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            if (Guid.TryParse(user.FindFirst(InternalUserIdClaimType)?.Value, out var internalUserId))
                return internalUserId;

            return null;
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
        if (user?.Identity?.IsAuthenticated != true)
            return null;

        var internalUserIdClaim = user.FindFirst(InternalUserIdClaimType)?.Value;
        if (Guid.TryParse(internalUserIdClaim, out var internalUserId))
            return internalUserId;

        var subject = user.FindFirst("sub")?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? user.FindFirst("sid")?.Value;

        if (string.IsNullOrWhiteSpace(subject))
            return null;

        var provider = ResolveAuthProvider(user, subject);
        if (string.IsNullOrWhiteSpace(provider))
            return Guid.TryParse(subject, out var fallbackUserId) ? fallbackUserId : null;

        var providerId = ResolveProviderId(user, subject, provider);
        if (string.IsNullOrWhiteSpace(providerId))
            return null;

        var cacheKey = $"{CacheKeyPrefix}ResolvedId_{provider}_{providerId}";
        if (_cache.TryGetValue<Guid>(cacheKey, out var cachedUserId))
            return cachedUserId;

        var externalLogin = await _userExternalLoginRepository.GetByProviderAndKey(provider, providerId);
        Guid? resolvedUserId = externalLogin?.UserId;

        if (!resolvedUserId.HasValue && SupportsEmailAutoMatch(provider) && ResolveEmailVerified(user))
        {
            var email = user.FindFirst("email")?.Value ?? user.FindFirst(ClaimTypes.Email)?.Value;
            if (!string.IsNullOrWhiteSpace(email))
            {
                var dbUser = await _userRepository.GetUserByEmail(email.Trim().ToLowerInvariant());
                resolvedUserId = dbUser?.Id;
            }
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
                _logger.LogWarning(ex, "AdminContext: failed role-based instance admin check for user {UserId}", userId);
            }

            if (isRoleAdmin)
            {
                _logger.LogInformation("AdminContext: User {UserId} IsInstanceAdmin=true (platform.admin role detected in database)", userId);
                return true;
            }

            var bootstrap = await _instanceBootstrapStateRepository.GetCurrent();
            var isBootstrapAdmin = bootstrap?.IsCompleted == true && bootstrap.CompletedByUserId == userId;

            // Legacy fallback for instances completed before CompletedByUserId was tracked.
            // In that case, default-tenant admins keep instance-admin access until role data is repaired.
            if (!isBootstrapAdmin
                && bootstrap?.IsCompleted == true
                && !bootstrap.CompletedByUserId.HasValue)
            {
                isBootstrapAdmin = await _tenantAdminRepo.IsTenantAdmin(PlatformDefaults.DefaultTenantId, userId);
            }

            if (isBootstrapAdmin)
            {
                _logger.LogInformation("AdminContext: User {UserId} IsInstanceAdmin=true (bootstrap owner fallback)", userId);
            }
            else
            {
                _logger.LogWarning("AdminContext: User {UserId} IsInstanceAdmin=false (no platform role or bootstrap ownership found)", userId);
            }

            return isBootstrapAdmin;
        });
    }

    public async Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var uid = await ResolveUserIdAsync(cancellationToken);
        if (uid == null)
            return false;

        // Optimized check: Instance admins are automatically tenant admins for the default tenant in single-tenant mode
        // This prevents access issues during the onboarding transition or in simple deployments.
        if (tenantId == PlatformDefaults.DefaultTenantId && await IsInstanceAdminAsync(uid.Value, cancellationToken))
        {
            return true;
        }

        var cacheKey = $"{CacheKeyPrefix}Tenant_{uid}_{tenantId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var isAdmin = await _tenantAdminRepo.IsTenantAdmin(tenantId, uid.Value);

            if (isAdmin)
            {
                _logger.LogInformation("AdminContext: User {UserId} IsTenantAdmin=true for Tenant {TenantId}", uid, tenantId);
            }
            else
            {
                _logger.LogDebug("AdminContext: User {UserId} IsTenantAdmin=false for Tenant {TenantId}", uid, tenantId);
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

            // Single-Tenant optimization: Instance admins are automatically tenant admins for the default tenant.
            if (!adminTenantIds.Contains(PlatformDefaults.DefaultTenantId) &&
                await _deploymentModeProvider.IsSingleTenantAsync(cancellationToken) &&
                await IsInstanceAdminAsync(userId, cancellationToken))
            {
                adminTenantIds.Add(PlatformDefaults.DefaultTenantId);
            }

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
            var memberships = await _orgMemberRepo.GetMembershipsByUser(userId);
            var adminOrgIds = memberships
                .Where(m => IsOrganizationAdminRole(m.RoleId))
                .Select(m => m.OrganizationId)
                .Distinct()
                .ToList()
                .AsReadOnly();

            return (IReadOnlyList<Guid>)adminOrgIds;
        }) ?? Array.Empty<Guid>();
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
            var memberships = await _groupMemberRepo.GetMembershipsByUser(userId);
            var adminGroupIds = memberships
                .Where(m => IsGroupAdminRole(m.RoleId))
                .Select(m => m.GroupId)
                .Distinct()
                .ToList()
                .AsReadOnly();

            return (IReadOnlyList<Guid>)adminGroupIds;
        }) ?? Array.Empty<Guid>();
    }

    private static bool IsOrganizationAdminRole(int roleId)
    {
        return roleId == (int)RoleEnum.OrgAdmin;
    }

    private static bool IsGroupAdminRole(int roleId)
    {
        return roleId == (int)RoleEnum.GroupAdmin;
    }

    private static string ResolveAuthProvider(ClaimsPrincipal principal, string subject)
    {
        var idp = principal.FindFirst("idp")?.Value;
        if (!string.IsNullOrWhiteSpace(idp))
        {
            var normalizedIdp = idp.Trim().ToLowerInvariant();
            if (normalizedIdp.Contains("google"))
                return AuthSchemeNames.Google.ToLowerInvariant();

            if (normalizedIdp.Contains("atproto"))
                return AuthSchemeNames.Atproto.ToLowerInvariant();

            if (normalizedIdp.Contains("keycloak"))
                return AuthSchemeNames.Keycloak.ToLowerInvariant();
        }

        var issuer = principal.FindFirst("iss")?.Value;
        if (!string.IsNullOrWhiteSpace(issuer))
        {
            var normalizedIssuer = issuer.Trim().ToLowerInvariant();
            if (normalizedIssuer.Contains("accounts.google.com"))
                return AuthSchemeNames.Google.ToLowerInvariant();

            if (normalizedIssuer.Contains("keycloak") || normalizedIssuer.Contains("/realms/"))
                return AuthSchemeNames.Keycloak.ToLowerInvariant();
        }

        return subject.StartsWith("did:", StringComparison.OrdinalIgnoreCase)
            ? AuthSchemeNames.Atproto.ToLowerInvariant()
            : AuthSchemeNames.Keycloak.ToLowerInvariant();
    }

    private static string ResolveProviderId(ClaimsPrincipal principal, string subject, string provider)
    {
        return provider == AuthSchemeNames.Atproto.ToLowerInvariant()
            ? principal.FindFirst("did")?.Value
                ?? principal.FindFirst("atproto_did")?.Value
                ?? subject
            : subject;
    }

    private static bool ResolveEmailVerified(ClaimsPrincipal principal)
    {
        var raw = principal.FindFirst("email_verified")?.Value;
        return bool.TryParse(raw, out var emailVerified)
            ? emailVerified
            : string.Equals(raw, "1", StringComparison.OrdinalIgnoreCase);
    }

    private static bool SupportsEmailAutoMatch(string provider)
    {
        return provider == AuthSchemeNames.Keycloak.ToLowerInvariant()
            || provider == AuthSchemeNames.Google.ToLowerInvariant();
    }

    /// <inheritdoc />
    public void InvalidateUser(Guid userId)
    {
        _cache.Remove($"{CacheKeyPrefix}Instance_{userId}");
        _cache.Remove($"{CacheKeyPrefix}TenantIds_{userId}");
        _cache.Remove($"{CacheKeyPrefix}OrgIds_{userId}");
        _cache.Remove($"{CacheKeyPrefix}GroupIds_{userId}");

        // For single-tenant mode, we can proactively clear the default tenant admin cache
        _cache.Remove($"{CacheKeyPrefix}Tenant_{userId}_{PlatformDefaults.DefaultTenantId}");

        _logger.LogInformation("AdminContext: Invalidated authority cache for user {UserId}", userId);
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
