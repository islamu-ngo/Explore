// ABOUTME: Persistence contract for event-scoped operational role assignment lookups.
// ABOUTME: Keeps authorization handlers and services dependent on Application abstractions, not EF details.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRoleAssignmentRepository : IGenericRepository<EventRoleAssignment, Guid>
{
    Task<EventRoleAssignment?> GetByEventUserRoleAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        int roleId,
        CancellationToken cancellationToken);

    Task<EventRoleAssignment?> GetOpenByEventUserRoleAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        int roleId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<EventRoleAssignment>> GetEffectiveForUserAndEventsAsync(
        Guid tenantId,
        Guid userId,
        IReadOnlyCollection<Guid> eventIds,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task<bool> HasAnotherEffectiveOwnerAsync(
        Guid tenantId,
        Guid eventId,
        Guid excludedAssignmentId,
        DateTime utcNow,
        CancellationToken cancellationToken);
}
