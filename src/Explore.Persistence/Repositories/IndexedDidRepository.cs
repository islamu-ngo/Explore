using Explore.Application.Contracts.Persistence;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class IndexedDidRepository(ExploreDbContext dbContext)
    : GenericRepository<IndexedDid, string>(dbContext), IIndexedDidRepository;
