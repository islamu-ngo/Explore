// ABOUTME: Repository implementation for event-session-scoped runtime custom-property definitions, options, and values.
// ABOUTME: Supports CQRS read/write flows, single/multi-value persistence, and provenance-aware queries.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventSessionCustomPropertyRepository : GenericRepository<EventSessionCustomPropertyDefinition, Guid>, IEventSessionCustomPropertyRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventSessionCustomPropertyRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventSessionCustomPropertyDefinition?> GetDefinitionWithDetails(Guid id)
    {
        return await _dbContext.EventSessionCustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(x => x.DefaultOption)
            .Include(x => x.Options.OrderBy(o => o.SortOrder))
            .Include(x => x.Values.OrderBy(v => v.Ordinal))
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<EventSessionCustomPropertyDefinition?> GetTrackedDefinitionWithOptions(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.EventSessionCustomPropertyDefinitions
            .Include(x => x.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(List<EventSessionCustomPropertyDefinition> Items, int TotalCount)> GetDefinitionsForSessionPaged(
        Guid eventSessionId,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.EventSessionCustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Where(x => x.EventSessionId == eventSessionId)
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

    public async Task<List<EventSessionCustomPropertyDefinition>> GetAllDefinitionsForSession(Guid eventSessionId)
    {
        return await _dbContext.EventSessionCustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Where(x => x.EventSessionId == eventSessionId)
            .Include(x => x.DefaultOption)
            .Include(x => x.Options.OrderBy(o => o.SortOrder))
            .Include(x => x.Values.OrderBy(v => v.Ordinal))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync();
    }

    public async Task<List<EventSessionCustomPropertyDefinition>> GetTrackedDefinitionsForSession(Guid eventSessionId, CancellationToken cancellationToken)
    {
        return await _dbContext.EventSessionCustomPropertyDefinitions
            .Where(x => x.EventSessionId == eventSessionId)
            .Include(x => x.Options.OrderBy(o => o.SortOrder))
            .Include(x => x.Values.OrderBy(v => v.Ordinal))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsDefinitionKey(Guid eventSessionId, string namespaceValue, string key, Guid? excludeDefinitionId = null)
    {
        return await _dbContext.EventSessionCustomPropertyDefinitions
            .AnyAsync(x => x.EventSessionId == eventSessionId
                && x.Namespace == namespaceValue
                && x.Key == key
                && (!excludeDefinitionId.HasValue || x.Id != excludeDefinitionId.Value));
    }

    public async Task<EventSessionCustomPropertyDefinition> CreateWithOptions(
        EventSessionCustomPropertyDefinition definition,
        IReadOnlyCollection<EventSessionCustomPropertyOption> options,
        Guid? defaultOptionId,
        CancellationToken cancellationToken)
    {
        await _dbContext.EventSessionCustomPropertyDefinitions.AddAsync(definition, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (options.Count > 0)
        {
            await _dbContext.EventSessionCustomPropertyOptions.AddRangeAsync(options, cancellationToken);
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

    public async Task<EventSessionCustomPropertyDefinition> UpdateWithOptions(
        EventSessionCustomPropertyDefinition definition,
        IReadOnlyCollection<EventSessionCustomPropertyOption> options,
        Guid? defaultOptionId,
        CancellationToken cancellationToken)
    {
        definition.DefaultOptionId = null;
        _dbContext.Entry(definition).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.EventSessionCustomPropertyOptions
            .IgnoreQueryFilters()
            .Where(x => x.EventSessionCustomPropertyDefinitionId == definition.Id)
            .ExecuteDeleteAsync(cancellationToken);

        if (options.Count > 0)
        {
            await _dbContext.EventSessionCustomPropertyOptions.AddRangeAsync(options, cancellationToken);
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
        await _dbContext.EventSessionCustomPropertyValues
            .IgnoreQueryFilters()
            .Where(x => x.EventSessionCustomPropertyDefinitionId == id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.EventSessionCustomPropertyOptions
            .IgnoreQueryFilters()
            .Where(x => x.EventSessionCustomPropertyDefinitionId == id)
            .ExecuteDeleteAsync(cancellationToken);

        var affectedRows = await _dbContext.EventSessionCustomPropertyDefinitions
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }

    public async Task<List<EventSessionCustomPropertyValue>> GetValuesForSession(Guid eventSessionId)
    {
        return await _dbContext.EventSessionCustomPropertyValues
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.EventSessionId == eventSessionId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync();
    }

    public async Task<List<EventSessionCustomPropertyValue>> GetValuesForDefinition(Guid definitionId)
    {
        return await _dbContext.EventSessionCustomPropertyValues
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.EventSessionCustomPropertyDefinitionId == definitionId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync();
    }

    public async Task<EventSessionCustomPropertyValue> SetValue(EventSessionCustomPropertyValue value, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EventSessionCustomPropertyValues
            .FirstOrDefaultAsync(x =>
                x.EventSessionCustomPropertyDefinitionId == value.EventSessionCustomPropertyDefinitionId
                && x.EventSessionId == value.EventSessionId
                && x.Ordinal == value.Ordinal,
                cancellationToken);

        if (existing != null)
        {
            existing.TextValue = value.TextValue;
            existing.NumberValue = value.NumberValue;
            existing.BooleanValue = value.BooleanValue;
            existing.DateTimeValue = value.DateTimeValue;
            existing.OptionId = value.OptionId;
            existing.UpdatedBy = value.UpdatedBy;
            existing.UpdatedAt = value.UpdatedAt;
            _dbContext.Entry(existing).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return existing;
        }

        await _dbContext.EventSessionCustomPropertyValues.AddAsync(value, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return value;
    }

    public async Task<EventSessionCustomPropertyOption> CreateOption(EventSessionCustomPropertyOption option, CancellationToken cancellationToken)
    {
        await _dbContext.EventSessionCustomPropertyOptions.AddAsync(option, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return option;
    }

    public async Task UpdateOption(EventSessionCustomPropertyOption option, CancellationToken cancellationToken)
    {
        _dbContext.Entry(option).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SetMultiValues(
        Guid definitionId,
        Guid eventSessionId,
        IReadOnlyCollection<EventSessionCustomPropertyValue> values,
        CancellationToken cancellationToken)
    {
        await _dbContext.EventSessionCustomPropertyValues
            .IgnoreQueryFilters()
            .Where(x => x.EventSessionCustomPropertyDefinitionId == definitionId && x.EventSessionId == eventSessionId)
            .ExecuteDeleteAsync(cancellationToken);

        if (values.Count > 0)
        {
            await _dbContext.EventSessionCustomPropertyValues.AddRangeAsync(values, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
