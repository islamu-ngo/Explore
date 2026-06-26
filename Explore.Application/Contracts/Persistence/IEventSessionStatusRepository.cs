// ABOUTME: Application-layer contract for accessing EventSessionStatus lookup rows.
// ABOUTME: Mirrors IEventStatusRepository so lookup repositories stay thin and generic.
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventSessionStatusRepository : IGenericRepository<EventSessionStatus, int>
{
}
