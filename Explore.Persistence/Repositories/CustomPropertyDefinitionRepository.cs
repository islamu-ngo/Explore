// ABOUTME: Repository implementation for shared Layer 3 custom-property definitions used by organizations and groups.
// ABOUTME: Supports namespaced machine-key lookups plus transactional option persistence for the first CQRS slice.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Explore.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class CustomPropertyDefinitionRepository : GenericRepository<CustomPropertyDefinition, Guid>, ICustomPropertyDefinitionRepository
{
    private readonly ExploreDbContext _dbContext;

    public CustomPropertyDefinitionRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CustomPropertyDefinition?> GetDefinitionWithDetails(Guid id)
    {
        return await _dbContext.CustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(x => x.DefaultOption)
            .Include(x => x.Options.OrderBy(option => option.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<CustomPropertyDefinition?> GetTrackedDefinitionWithOptions(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.CustomPropertyDefinitions
            .Include(x => x.Options.OrderBy(option => option.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(List<CustomPropertyDefinition> Items, int TotalCount)> GetDefinitionsWithDetailsPaged(
        EntityTypeName entityTypeName,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.CustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Where(x => x.EntityTypeName == entityTypeName)
            .Include(x => x.Options)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName);

        var totalCount = await query.CountAsync();
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<bool> ExistsScopedMachineKey(Guid tenantId, EntityTypeName entityTypeName, string namespaceValue, string key, Guid? excludeDefinitionId = null)
    {
        return await _dbContext.CustomPropertyDefinitions
            .AnyAsync(x => x.TenantId == tenantId
                && x.EntityTypeName == entityTypeName
                && x.Namespace == namespaceValue
                && x.Key == key
                && (!excludeDefinitionId.HasValue || x.Id != excludeDefinitionId.Value));
    }

    public async Task<CustomPropertyDefinition> CreateWithOptions(
        CustomPropertyDefinition definition,
        IReadOnlyCollection<CustomPropertyOption> options,
        Guid? defaultOptionId,
        CancellationToken cancellationToken)
    {
        await _dbContext.CustomPropertyDefinitions.AddAsync(definition, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (options.Count > 0)
        {
            await _dbContext.CustomPropertyOptions.AddRangeAsync(options, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (defaultOptionId.HasValue)
        {
            definition.DefaultOptionId = defaultOptionId;
            _dbContext.Entry(definition).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetDefinitionWithDetails(definition.Id) ?? definition;
    }

    public async Task<CustomPropertyDefinition> UpdateWithOptions(
        CustomPropertyDefinition definition,
        IReadOnlyCollection<CustomPropertyOption> options,
        Guid? defaultOptionId,
        CancellationToken cancellationToken)
    {
        definition.DefaultOptionId = null;
        _dbContext.Entry(definition).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.CustomPropertyOptions
            .IgnoreQueryFilters()
            .Where(x => x.CustomPropertyDefinitionId == definition.Id)
            .ExecuteDeleteAsync(cancellationToken);

        if (options.Count > 0)
        {
            await _dbContext.CustomPropertyOptions.AddRangeAsync(options, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (defaultOptionId.HasValue)
        {
            definition.DefaultOptionId = defaultOptionId;
            _dbContext.Entry(definition).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return await GetDefinitionWithDetails(definition.Id) ?? definition;
    }

    public async Task<bool> DeleteDefinition(Guid id, CancellationToken cancellationToken)
    {
        var affectedRows = await _dbContext.CustomPropertyDefinitions
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }
}
