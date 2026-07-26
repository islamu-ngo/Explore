// ABOUTME: Persists tenant-scoped groups and provides hierarchy validation queries.
// ABOUTME: Serializes hierarchy mutations with a retry-safe PostgreSQL advisory transaction lock.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class GroupRepository : GenericRepository<Group, Guid>, IGroupRepository
{
    private readonly ExploreDbContext _dbContext;

    public GroupRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public new async Task<Group?> GetById(Guid id)
    {
        return await _dbContext.Groups
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.ApprovalStatus)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<Group>> GetGroupsWithDetails()
    {
        return await _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.AtprotoIdentities)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.ApprovalStatus)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Tenant)
            .ToListAsync();
    }

    public async Task<Group?> GetGroupWithDetails(Guid id)
    {
        return await _dbContext.Groups
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.AtprotoIdentities)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.ApprovalStatus)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Tenant)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Members)
                .ThenInclude(m => m.User)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Members)
                .ThenInclude(m => m.User)
                .ThenInclude(u => u!.Pii)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Members)
                .ThenInclude(m => m.Role)
            .FirstOrDefaultAsync(g => g.Id == id);
    }

    public async Task<List<Group>> GetMyGroups(Guid userId)
    {
        return await _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.AtprotoIdentities)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.ApprovalStatus)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Members)
                    .ThenInclude(m => m.Role)
            .Where(g => g.TenantParticipations.Any(p => p.Members.Any(m => m.UserId == userId)))
            .ToListAsync();
    }

    public async Task<(List<Group> Items, int TotalCount)> GetGroupsWithDetailsPaged(int pageNumber, int pageSize)
    {
        var query = _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.AtprotoIdentities)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.ApprovalStatus)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Tenant)
            .OrderBy(g => g.FullName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<Group> Items, int TotalCount)> GetMyGroupsPaged(Guid userId, int pageNumber, int pageSize)
    {
        var query = _dbContext.Groups
            .AsNoTracking()
            .AsSplitQuery()
            .Include(g => g.Actor)
                .ThenInclude(a => a!.Pii)
            .Include(g => g.Actor)
                .ThenInclude(a => a!.AtprotoIdentities)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.ApprovalStatus)
            .Include(g => g.TenantParticipations)
                .ThenInclude(p => p.Members)
                    .ThenInclude(m => m.Role)
            .Where(g => g.TenantParticipations.Any(p => p.Members.Any(m => m.UserId == userId)))
            .OrderBy(g => g.FullName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> OrganizationExistsInTenant(Guid organizationId, Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.OrganizationTenants
            .AsNoTracking()
            .AnyAsync(p => p.OrganizationId == organizationId && p.TenantId == tenantId, cancellationToken);
    }

    public async Task<bool> GroupExistsInTenant(Guid groupId, Guid tenantId, CancellationToken cancellationToken)
    {
        return await _dbContext.GroupTenants
            .AsNoTracking()
            .AnyAsync(p => p.GroupId == groupId && p.TenantId == tenantId, cancellationToken);
    }

    public async Task<bool> WouldCreateHierarchyCycle(Guid groupId, Guid parentGroupId, Guid tenantId, CancellationToken cancellationToken)
    {
        var result = await _dbContext.Database
            .SqlQueryRaw<bool>(
                """
                WITH RECURSIVE ancestors AS (
                    SELECT id, parent_group_id, 1 AS depth
                    FROM groups
                    WHERE id = {0} AND tenant_id = {1}

                    UNION ALL

                    SELECT g.id, g.parent_group_id, a.depth + 1
                    FROM groups g
                    INNER JOIN ancestors a ON g.id = a.parent_group_id
                    WHERE g.tenant_id = {1} AND a.depth < {2}
                )
                SELECT EXISTS (
                    SELECT 1
                    FROM ancestors
                    WHERE id = {3}
                ) AS "Value"
                """,
                parentGroupId,
                tenantId,
                GroupHierarchyRules.MaxDepth + 1,
                groupId)
            .SingleAsync(cancellationToken);

        return result;
    }

    public async Task<bool> WouldExceedHierarchyDepth(Guid? parentGroupId, Guid tenantId, int maxDepth, CancellationToken cancellationToken)
    {
        if (!parentGroupId.HasValue)
        {
            return false;
        }

        var depth = await _dbContext.Database
            .SqlQueryRaw<int>(
                """
                WITH RECURSIVE ancestors AS (
                    SELECT id, parent_group_id, 1 AS depth
                    FROM groups
                    WHERE id = {0} AND tenant_id = {1}

                    UNION ALL

                    SELECT g.id, g.parent_group_id, a.depth + 1
                    FROM groups g
                    INNER JOIN ancestors a ON g.id = a.parent_group_id
                    WHERE g.tenant_id = {1} AND a.depth < {2}
                )
                SELECT COALESCE(MAX(depth), 0) AS "Value"
                FROM ancestors
                """,
                parentGroupId.Value,
                tenantId,
                maxDepth + 1)
            .SingleAsync(cancellationToken);

        return depth >= maxDepth;
    }

    public async Task<bool> WouldExceedHierarchyDepthForMove(Guid groupId, Guid? parentGroupId, Guid tenantId, int maxDepth, CancellationToken cancellationToken)
    {
        var wouldExceedDepth = await _dbContext.Database
            .SqlQueryRaw<bool>(
                """
                WITH RECURSIVE ancestors AS (
                    SELECT id, parent_group_id, 1 AS depth
                    FROM groups
                    WHERE id = {1} AND tenant_id = {2}

                    UNION ALL

                    SELECT g.id, g.parent_group_id, a.depth + 1
                    FROM groups g
                    INNER JOIN ancestors a ON g.id = a.parent_group_id
                    WHERE g.tenant_id = {2} AND a.depth < {4}
                ),
                descendants AS (
                    SELECT id, 1 AS depth
                    FROM groups
                    WHERE id = {0} AND tenant_id = {2}

                    UNION ALL

                    SELECT g.id, d.depth + 1
                    FROM groups g
                    INNER JOIN descendants d ON g.parent_group_id = d.id
                    WHERE g.tenant_id = {2} AND d.depth < {4}
                ),
                depth_summary AS (
                    SELECT
                        CASE WHEN {1} IS NULL THEN 0 ELSE COALESCE((SELECT MAX(depth) FROM ancestors), 0) END AS parent_depth,
                        COALESCE((SELECT MAX(depth) FROM descendants), 1) AS subtree_depth
                )
                SELECT (parent_depth + subtree_depth) > {3} AS "Value"
                FROM depth_summary
                """,
                groupId,
                parentGroupId,
                tenantId,
                maxDepth,
                maxDepth + 1)
            .SingleAsync(cancellationToken);

        return wouldExceedDepth;
    }

    public async Task<T> ExecuteWithHierarchyMutationLock<T>(Guid tenantId, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
    {
        var strategy = _dbContext.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            try
            {
                await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

                await _dbContext.Database.ExecuteSqlRawAsync(
                    "SELECT pg_advisory_xact_lock(hashtext({0}))",
                    [$"group-hierarchy:{tenantId}"],
                    cancellationToken);

                var result = await operation(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch
            {
                _dbContext.ChangeTracker.Clear();
                throw;
            }
        });
    }
}
