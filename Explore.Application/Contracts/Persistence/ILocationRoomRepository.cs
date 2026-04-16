// ABOUTME: Repository contract for LocationRoom - conference-style rooms under a Location.
// ABOUTME: Provides location-scoped queries for room management and session scheduling.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface ILocationRoomRepository : IGenericRepository<LocationRoom, Guid>
{
    Task<List<LocationRoom>> GetByLocationAsync(Guid locationId, CancellationToken cancellationToken);
}
