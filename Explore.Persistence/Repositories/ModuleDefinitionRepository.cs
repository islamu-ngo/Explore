// ABOUTME: Repository implementation for ModuleDefinition entity providing
// data access for module governance and discovery.

using Explore.Application.Contracts.Persistence;
using Explore.Domain.Modules;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class ModuleDefinitionRepository : GenericRepository<ModuleDefinition, Guid>, IModuleDefinitionRepository
{
    private readonly ExploreDbContext _dbContext;

    public ModuleDefinitionRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<ModuleDefinition?> GetByKey(string key)
    {
        return await _dbContext.ModuleDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Key == key);
    }

    public async Task<List<ModuleDefinition>> GetAllActive()
    {
        return await _dbContext.ModuleDefinitions
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<bool> IsActive(string key)
    {
        var module = await _dbContext.ModuleDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Key == key);

        return module?.IsActive ?? false;
    }
}
