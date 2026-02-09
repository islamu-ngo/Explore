using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class RegistrationModeRepository : GenericRepository<RegistrationMode, int>, IRegistrationModeRepository
{
    public RegistrationModeRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
