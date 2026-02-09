// ABOUTME: Repository implementation for instance bootstrap state persistence.
// ABOUTME: Provides current bootstrap marker access for first-run onboarding logic.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class InstanceBootstrapStateRepository : GenericRepository<InstanceBootstrapState, Guid>, IInstanceBootstrapStateRepository
{
    private readonly ExploreDbContext _dbContext;

    public InstanceBootstrapStateRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<InstanceBootstrapState?> GetCurrent()
    {
        return await _dbContext.InstanceBootstrapStates
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }
}
