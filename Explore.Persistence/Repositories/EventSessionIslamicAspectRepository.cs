namespace Explore.Persistence.Repositories;

using Explore.Application.Contracts.Persistence;
using Explore.Domain;

public class EventSessionIslamicAspectRepository : GenericRepository<EventSessionIslamicAspect, Guid>, IEventSessionIslamicAspectRepository
{
    public EventSessionIslamicAspectRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
