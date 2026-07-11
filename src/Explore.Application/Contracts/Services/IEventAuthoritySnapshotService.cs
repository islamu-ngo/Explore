// ABOUTME: Batch authority snapshot contract for event-scoped authorization and HAL evaluation.
// ABOUTME: Prevents N+1 authorization lookups while keeping consumers on Application-layer abstractions.

namespace Explore.Application.Contracts.Services;

public interface IEventAuthoritySnapshotService
{
    Task<EventAuthoritySnapshot> GetForUserAndEventsAsync(
        Guid tenantId,
        Guid userId,
        IReadOnlyCollection<Guid> eventIds,
        CancellationToken cancellationToken);
}

public sealed record EventAuthoritySnapshot(
    Guid TenantId,
    Guid UserId,
    IReadOnlyDictionary<Guid, EventAuthorityForUser> Events);

public sealed record EventAuthorityForUser(
    IReadOnlySet<string> RoleCodes,
    IReadOnlySet<string> PermissionCodes,
    bool IsOwner,
    bool IsManager);
