// ABOUTME: Repository contract for assigning event sessions to one or more event session groups.
// ABOUTME: Supports ordered group membership reads without leaking EF concerns upward.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionGroupSessionRepository : IGenericRepository<EventSessionGroupSession, Guid>
{
    Task<List<EventSessionGroupSession>> GetByGroupAsync(Guid eventSessionGroupId, CancellationToken cancellationToken);

    Task<List<EventSessionGroupSession>> GetPublicByGroupAsync(Guid eventSessionGroupId, CancellationToken cancellationToken);

    Task<List<EventSessionGroupSession>> GetBySessionAsync(Guid eventSessionId, CancellationToken cancellationToken);

    Task<EventSessionGroupSession?> GetExistingAssignmentAsync(
        Guid eventSessionGroupId,
        Guid eventSessionId,
        CancellationToken cancellationToken);

    Task<List<EventSessionGroupSession>> GetPrimaryAssignmentsForSessionAsync(
        Guid eventSessionId,
        Guid? excludeAssignmentId,
        CancellationToken cancellationToken);

    Task<List<EventSessionGroupSession>> GetAssignmentsForGroupUpdateAsync(
        Guid eventSessionGroupId,
        CancellationToken cancellationToken);
}
