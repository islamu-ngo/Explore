// ABOUTME: Repository for persisted event-role assignment grants and effective authority lookups.
// ABOUTME: Applies the canonical lifecycle/time predicate so fallback authorization is not weaker than Cerbos.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventRoleAssignmentRepository : GenericRepository<EventRoleAssignment, Guid>, IEventRoleAssignmentRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRoleAssignmentRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventRoleAssignment?> GetByEventUserRoleAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        int roleId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EventRoleAssignments
            .AsNoTracking()
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a =>
                a.TenantId == tenantId &&
                a.EventId == eventId &&
                a.UserId == userId &&
                a.RoleId == roleId,
                cancellationToken);
    }

    public async Task<EventRoleAssignment?> GetOpenByEventUserRoleAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        int roleId,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EventRoleAssignments
            .Include(a => a.Role)
            .FirstOrDefaultAsync(a =>
                a.TenantId == tenantId &&
                a.EventId == eventId &&
                a.UserId == userId &&
                a.RoleId == roleId &&
                (a.Status == EventRoleAssignmentStatus.Pending || a.Status == EventRoleAssignmentStatus.Active),
                cancellationToken);
    }

    public async Task<IReadOnlyList<EventRoleAssignment>> GetEffectiveForUserAndEventsAsync(
        Guid tenantId,
        Guid userId,
        IReadOnlyCollection<Guid> eventIds,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0)
        {
            return [];
        }

        return await _dbContext.EventRoleAssignments
            .AsNoTracking()
            .Include(a => a.Role)
            .Where(a =>
                a.TenantId == tenantId &&
                a.UserId == userId &&
                eventIds.Contains(a.EventId) &&
                a.Status == EventRoleAssignmentStatus.Active &&
                a.StartsAtUtc <= utcNow &&
                (a.ExpiresAtUtc == null || a.ExpiresAtUtc > utcNow))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasAnotherEffectiveOwnerAsync(
        Guid tenantId,
        Guid eventId,
        Guid excludedAssignmentId,
        DateTime utcNow,
        CancellationToken cancellationToken)
    {
        return await _dbContext.EventRoleAssignments
            .AsNoTracking()
            .AnyAsync(a =>
                a.Id != excludedAssignmentId &&
                a.TenantId == tenantId &&
                a.EventId == eventId &&
                a.RoleId == (int)RoleEnum.EventOwner &&
                a.Status == EventRoleAssignmentStatus.Active &&
                a.StartsAtUtc <= utcNow &&
                (a.ExpiresAtUtc == null || a.ExpiresAtUtc > utcNow),
                cancellationToken);
    }
}
