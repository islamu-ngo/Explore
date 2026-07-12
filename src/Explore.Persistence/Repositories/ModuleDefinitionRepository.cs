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

    public async Task<ModuleDefinition?> GetByKey(string moduleKey)
    {
        return await _dbContext.ModuleDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModuleKey == moduleKey);
    }

    public async Task<List<ModuleDefinition>> GetAllActive()
    {
        return await _dbContext.ModuleDefinitions
            .AsNoTracking()
            .Where(m => m.IsActive)
            .OrderBy(m => m.DisplayOrder)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<ModuleDefinition>> GetActiveByKeysAsync(
        IReadOnlyCollection<string> keys,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.ModuleDefinitions
            .AsNoTracking()
            .Where(module => module.IsActive && keys.Contains(module.ModuleKey))
            .OrderBy(module => module.ModuleKey)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsActive(string moduleKey)
    {
        var module = await _dbContext.ModuleDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.ModuleKey == moduleKey);

        return module?.IsActive ?? false;
    }
}
