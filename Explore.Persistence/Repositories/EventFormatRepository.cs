using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class EventFormatRepository : GenericRepository<EventFormat, int>, IEventFormatRepository
{
    public EventFormatRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
