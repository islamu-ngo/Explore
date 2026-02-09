using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class LanguageRepository : GenericRepository<Language, int>, ILanguageRepository
{
    public LanguageRepository(ExploreDbContext dbContext) : base(dbContext)
    {
    }
}
