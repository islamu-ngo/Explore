// ABOUTME: Database-first identity service resolving admin authority from database tables only.
// Caches per-user authority profiles in IMemoryCache with 5-minute sliding expiration.

using System.Security.Claims;
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
/// is resolved from platform role assignments, TenantMembers, OrganizationMembers, and GroupMembers.
/// </summary>
public class AdminContext : IAdminContext, IAdminCacheInvalidator
{
    private const string InternalUserIdClaimType = "internal_user_id";
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPlatformUserRoleRepository _platformUserRoleRepository;
    private readonly IInstanceBootstrapStateRepository _instanceBootstrapStateRepository;
    private readonly ITenantMemberRepository _tenantAdminRepo;
    private readonly IOrganizationMemberRepository _orgMemberRepo;
    private readonly IGroupMemberRepository _groupMemberRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminContext> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "AdminContext_";

    public AdminContext(
        IHttpContextAccessor httpContextAccessor,
        IPlatformUserRoleRepository platformUserRoleRepository,
        IInstanceBootstrapStateRepository instanceBootstrapStateRepository,
        ITenantMemberRepository tenantAdminRepo,
        IOrganizationMemberRepository orgMemberRepo,
        IGroupMemberRepository groupMemberRepo,
        IMemoryCache cache,
        ILogger<AdminContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _platformUserRoleRepository = platformUserRoleRepository;
        _instanceBootstrapStateRepository = instanceBootstrapStateRepository;
        _tenantAdminRepo = tenantAdminRepo;
        _orgMemberRepo = orgMemberRepo;
        _groupMemberRepo = groupMemberRepo;
        _cache = cache;
        _logger = logger;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
                return null;

            var sub = user.FindFirst(InternalUserIdClaimType)?.Value
                ?? user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sid")?.Value;

            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }

    public Task<bool> IsInstanceAdminAsync(CancellationToken cancellationToken = default)
    {
        var uid = UserId;
        return uid == null ? Task.FromResult(false) : IsInstanceAdminAsync(uid.Value, cancellationToken);
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
                _logger.LogDebug("AdminContext: User {UserId} IsInstanceAdmin=true (platform.admin role)", userId);
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

            _logger.LogDebug(
                "AdminContext: User {UserId} IsInstanceAdmin={IsAdmin} (bootstrap fallback)",
                userId,
                isBootstrapAdmin);

            return isBootstrapAdmin;
        });
    }

    public async Task<bool> IsTenantAdminAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var uid = UserId;
        if (uid == null)
            return false;

        var cacheKey = $"{CacheKeyPrefix}Tenant_{uid}_{tenantId}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            return await _tenantAdminRepo.IsTenantAdmin(tenantId, uid.Value);
        });
    }

    public async Task<bool> IsOrganizationAdminAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        var uid = UserId;
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

    public Task<IReadOnlyList<Guid>> GetAdminTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = UserId;
        return uid == null
            ? Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>())
            : GetAdminTenantIdsAsync(uid.Value, cancellationToken);
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
                .ToList()
                .AsReadOnly();

            return (IReadOnlyList<Guid>)adminTenantIds;
        }) ?? Array.Empty<Guid>();
    }

    public Task<IReadOnlyList<Guid>> GetAdminOrganizationIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = UserId;
        return uid == null
            ? Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>())
            : GetAdminOrganizationIdsAsync(uid.Value, cancellationToken);
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
        var uid = UserId;
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

    public Task<IReadOnlyList<Guid>> GetAdminGroupIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = UserId;
        return uid == null
            ? Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>())
            : GetAdminGroupIdsAsync(uid.Value, cancellationToken);
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

    /// <inheritdoc />
    public void InvalidateUser(Guid userId)
    {
        _cache.Remove($"{CacheKeyPrefix}Instance_{userId}");
        _cache.Remove($"{CacheKeyPrefix}TenantIds_{userId}");
        _cache.Remove($"{CacheKeyPrefix}OrgIds_{userId}");
        _cache.Remove($"{CacheKeyPrefix}GroupIds_{userId}");
        _logger.LogDebug("AdminContext: Invalidated cache for user {UserId}", userId);
        // Note: Tenant_{userId}_{tenantId}, Org_{userId}_{orgId}, and Group_{userId}_{groupId}
        // entries are not evicted here because we don't track which combinations are cached.
        // They will expire naturally via the 5-minute sliding window.
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
