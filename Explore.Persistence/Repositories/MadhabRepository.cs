using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class MadhabRepository : GenericRepository<Madhab, int>, IMadhabRepository
{
    public MadhabRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
