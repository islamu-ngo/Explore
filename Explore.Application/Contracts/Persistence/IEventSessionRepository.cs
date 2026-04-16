using Explore.Application.Specifications.EventSessions;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionRepository : IGenericRepository<EventSession, Guid>
{
    Task<EventSession?> GetSessionWithDetails(Guid id);
    Task<List<EventSession>> GetSessionsByEvent(Guid eventId);
    Task<List<EventSession>> GetSessionsByLocation(Guid locationId);
    Task<(List<EventSession> Items, int TotalCount)> GetSessionsWithDetailsPaged(int pageNumber, int pageSize);
    Task<(List<EventSession> Items, int TotalCount)> GetSessionsWithDetailsPagedFiltered(int pageNumber, int pageSize, EventSessionQuerySpecification specification);

    /// <summary>
    /// Layer A read used by CreateEventSessionDtoValidator/UpdateEventSessionDtoValidator.
    /// Returns persisted (non-deleted) sessions in the given room whose UTC time range overlaps
    /// [startUtc, endUtc), optionally excluding a session being updated.
    /// </summary>
    Task<IReadOnlyList<EventSession>> GetOverlappingSessionsInRoomAsync(
        Guid roomId,
        DateTimeOffset startUtc,
        DateTimeOffset endUtc,
        Guid? excludeSessionId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Layer B create path: opens a Serializable transaction, re-checks same-room overlap inside the
    /// transaction, and commits only if no overlap exists. Throws
    /// <see cref="Explore.Application.Exceptions.RoomScheduleConflictException"/> on conflict.
    /// If <see cref="EventSession.RoomId"/> is null, falls back to the base Create path.
    /// </summary>
    Task<EventSession> CreateWithRoomOverlapGuardAsync(EventSession session, CancellationToken cancellationToken);

    /// <summary>
    /// Layer B update path: opens a Serializable transaction, re-checks same-room overlap (excluding
    /// the session being updated) inside the transaction, and commits only if no overlap exists.
    /// Throws <see cref="Explore.Application.Exceptions.RoomScheduleConflictException"/> on conflict.
    /// </summary>
    Task UpdateWithRoomOverlapGuardAsync(EventSession session, CancellationToken cancellationToken);
}
