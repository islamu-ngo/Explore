// ABOUTME: Repository contract for order-linked event admission coverage reads.
// ABOUTME: Returns EventRegistration entities only for location-access evaluation.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRegistrationRepository : IGenericRepository<EventRegistration, Guid>
{
    Task<IReadOnlyList<EventRegistration>> GetLocationAccessCoverageAsync(
        Guid tenantId,
        Guid eventId,
        Guid userId,
        CancellationToken cancellationToken);
}
