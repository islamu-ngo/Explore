// ABOUTME: Repository contract for event registration aggregate reads and cancellation writes.
// ABOUTME: Returns EventRegistration entities so Application handlers own authorization and DTO mapping.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRegistrationRepository : IGenericRepository<EventRegistration, Guid>
{
    Task<EventRegistration?> GetByIdWithDetails(Guid id, CancellationToken cancellationToken = default);
    Task<EventRegistration?> GetRegistrationByUserAndSession(Guid userId, Guid eventSessionId, CancellationToken cancellationToken = default);
    Task<List<EventRegistration>> GetRegistrationsBySession(Guid eventSessionId, CancellationToken cancellationToken = default);
    Task<List<EventRegistration>> GetRegistrationsByUser(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<EventRegistration>> GetLocationAccessCoverageAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken);
    Task<bool> IsUserRegisteredForSession(Guid userId, Guid eventSessionId);
    Task<(List<EventRegistration> Items, int TotalCount)> GetRegistrationsByUserWithDetailsPaged(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<(List<EventRegistration> Items, int TotalCount)> GetRegistrationsByEventWithDetailsPaged(
        Guid eventId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
    Task<EventRegistrationTransitionResult> UpdateAndAdjustCapacityAsync(
        EventRegistration registration,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        EventRegistrationActorProvenance actorProvenance,
        Guid? actorUserId,
        CancellationToken cancellationToken);
    Task<EventRegistrationTransitionResult> CancelAndReleaseCapacityAsync(
        Guid registrationId,
        Guid expectedOwnerUserId,
        Guid occurrenceId,
        DateTimeOffset occurredAt,
        EventRegistrationActorProvenance actorProvenance,
        Guid? actorUserId,
        CancellationToken cancellationToken);
}
