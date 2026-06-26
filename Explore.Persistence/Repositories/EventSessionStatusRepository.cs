// ABOUTME: EF Core backed implementation of IEventSessionStatusRepository lookup access.
// ABOUTME: Inherits GenericRepository behavior so it stays a thin, queryable lookup repository.
using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class EventSessionStatusRepository : GenericRepository<EventSessionStatus, int>, IEventSessionStatusRepository
{
    public EventSessionStatusRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
