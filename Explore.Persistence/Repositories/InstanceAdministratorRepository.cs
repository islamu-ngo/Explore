// ABOUTME: Repository implementation for instance administrator mappings.
// ABOUTME: Supports membership checks and assignment lookups by user identity.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class InstanceAdministratorRepository : GenericRepository<InstanceAdministrator, Guid>, IInstanceAdministratorRepository
{
    private readonly ExploreDbContext _dbContext;

    public InstanceAdministratorRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> IsUserInstanceAdmin(Guid userId)
    {
        return await _dbContext.InstanceAdministrators
            .AsNoTracking()
            .AnyAsync(x => x.UserId == userId);
    }

    public async Task<InstanceAdministrator?> GetByUserId(Guid userId)
    {
        return await _dbContext.InstanceAdministrators
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == userId);
    }

    public async Task<bool> HasAnyInstanceAdministrator()
    {
        return await _dbContext.InstanceAdministrators
            .AsNoTracking()
            .AnyAsync();
    }
}
