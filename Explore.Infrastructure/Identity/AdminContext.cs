// ABOUTME: Hybrid identity service resolving admin authority from JWT claims + database tables.
// Caches per-user authority profiles in IMemoryCache with 5-minute sliding expiration.

using System.Security.Claims;
using Explore.Application.Contracts.Identity;
using Explore.Application.Contracts.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Explore.Infrastructure.Identity;

/// <summary>
/// Resolves the current user's administrative authority using a hybrid model:
/// 1. JWT claims (Keycloak "Admin" role) for InstanceAdmin
/// 2. Database tables (InstanceAdministrators, TenantAdministrators, OrganizationMembers) as fallback/override
/// This enables ATProto/PDS-only deployments where Keycloak is absent.
/// </summary>
public class AdminContext : IAdminContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IInstanceAdministratorRepository _instanceAdminRepo;
    private readonly ITenantAdministratorRepository _tenantAdminRepo;
    private readonly IOrganizationMemberRepository _orgMemberRepo;
    private readonly IMemoryCache _cache;
    private readonly ILogger<AdminContext> _logger;

    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);
    private const string CacheKeyPrefix = "AdminContext_";

    public AdminContext(
        IHttpContextAccessor httpContextAccessor,
        IInstanceAdministratorRepository instanceAdminRepo,
        ITenantAdministratorRepository tenantAdminRepo,
        IOrganizationMemberRepository orgMemberRepo,
        IMemoryCache cache,
        ILogger<AdminContext> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _instanceAdminRepo = instanceAdminRepo;
        _tenantAdminRepo = tenantAdminRepo;
        _orgMemberRepo = orgMemberRepo;
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

            var sub = user.FindFirst("sub")?.Value
                ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sid")?.Value;

            return Guid.TryParse(sub, out var userId) ? userId : null;
        }
    }

    public async Task<bool> IsInstanceAdminAsync(CancellationToken cancellationToken = default)
    {
        // Path 1: Check JWT "Admin" role claim (Keycloak)
        var user = _httpContextAccessor.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            var hasAdminRole = user.IsInRole("Admin")
                || user.Claims.Any(c =>
                    c.Type.Equals("roles", StringComparison.OrdinalIgnoreCase)
                    && c.Value.Equals("Admin", StringComparison.OrdinalIgnoreCase));

            if (hasAdminRole)
                return true;
        }

        // Path 2: Fallback to database (ATProto/PDS-only deployments)
        var uid = UserId;
        if (uid == null)
            return false;

        var cacheKey = $"{CacheKeyPrefix}Instance_{uid}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var isAdmin = await _instanceAdminRepo.IsUserInstanceAdmin(uid.Value);
            _logger.LogDebug("AdminContext: User {UserId} IsInstanceAdmin={IsAdmin} (from DB)", uid, isAdmin);
            return isAdmin;
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
            return await _tenantAdminRepo.IsTenantAdministrator(tenantId, uid.Value);
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
            return await _orgMemberRepo.IsUserAdminOfOrganization(organizationId, uid.Value);
        });
    }

    public async Task<IReadOnlyList<Guid>> GetAdminTenantIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = UserId;
        if (uid == null)
            return Array.Empty<Guid>();

        var cacheKey = $"{CacheKeyPrefix}TenantIds_{uid}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var admins = await _tenantAdminRepo.GetByUserId(uid.Value);
            return (IReadOnlyList<Guid>)admins.Select(a => a.TenantId).ToList().AsReadOnly();
        }) ?? Array.Empty<Guid>();
    }

    public async Task<IReadOnlyList<Guid>> GetAdminOrganizationIdsAsync(CancellationToken cancellationToken = default)
    {
        var uid = UserId;
        if (uid == null)
            return Array.Empty<Guid>();

        var cacheKey = $"{CacheKeyPrefix}OrgIds_{uid}";
        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.SlidingExpiration = CacheExpiration;
            var memberships = await _orgMemberRepo.GetMembershipsByUser(uid.Value);
            return (IReadOnlyList<Guid>)memberships
                .Where(m => m.OrganizationRoleId <= 3) // Creator, CoOwner, Admin
                .Select(m => m.OrganizationId)
                .ToList()
                .AsReadOnly();
        }) ?? Array.Empty<Guid>();
    }
}
