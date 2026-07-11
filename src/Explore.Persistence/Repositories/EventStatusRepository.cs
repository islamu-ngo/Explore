using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class EventStatusRepository : GenericRepository<EventStatus, int>, IEventStatusRepository
{
    public EventStatusRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
