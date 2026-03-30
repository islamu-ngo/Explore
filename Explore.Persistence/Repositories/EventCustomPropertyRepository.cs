// ABOUTME: Repository implementation for event-scoped runtime custom-property definitions, options, and values.
// ABOUTME: Supports CQRS read/write flows, single/multi-value persistence, and provenance-aware queries.

using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

namespace Explore.Persistence.Repositories;

public class EventCustomPropertyRepository : GenericRepository<EventCustomPropertyDefinition, Guid>, IEventCustomPropertyRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventCustomPropertyRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<EventCustomPropertyDefinition?> GetDefinitionWithDetails(Guid id)
    {
        return await _dbContext.EventCustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Include(x => x.DefaultOption)
            .Include(x => x.Options.OrderBy(o => o.SortOrder))
            .Include(x => x.Values.OrderBy(v => v.Ordinal))
            .FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task<EventCustomPropertyDefinition?> GetTrackedDefinitionWithOptions(Guid id, CancellationToken cancellationToken)
    {
        return await _dbContext.EventCustomPropertyDefinitions
            .Include(x => x.Options.OrderBy(o => o.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<(List<EventCustomPropertyDefinition> Items, int TotalCount)> GetDefinitionsForEventPaged(
        Guid eventId,
        int pageNumber,
        int pageSize)
    {
        var query = _dbContext.EventCustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Where(x => x.EventId == eventId)
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

    public async Task<List<EventCustomPropertyDefinition>> GetAllDefinitionsForEvent(Guid eventId)
    {
        return await _dbContext.EventCustomPropertyDefinitions
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .Where(x => x.EventId == eventId)
            .Include(x => x.DefaultOption)
            .Include(x => x.Options.OrderBy(o => o.SortOrder))
            .Include(x => x.Values.OrderBy(v => v.Ordinal))
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.DisplayName)
            .ToListAsync();
    }

    public async Task<bool> ExistsDefinitionKey(Guid eventId, string namespaceValue, string key, Guid? excludeDefinitionId = null)
    {
        return await _dbContext.EventCustomPropertyDefinitions
            .AnyAsync(x => x.EventId == eventId
                && x.Namespace == namespaceValue
                && x.Key == key
                && (!excludeDefinitionId.HasValue || x.Id != excludeDefinitionId.Value));
    }

    public async Task<EventCustomPropertyDefinition> CreateWithOptions(
        EventCustomPropertyDefinition definition,
        IReadOnlyCollection<EventCustomPropertyOption> options,
        Guid? defaultOptionId,
        CancellationToken cancellationToken)
    {
        await _dbContext.EventCustomPropertyDefinitions.AddAsync(definition, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        if (options.Count > 0)
        {
            await _dbContext.EventCustomPropertyOptions.AddRangeAsync(options, cancellationToken);
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

    public async Task<EventCustomPropertyDefinition> UpdateWithOptions(
        EventCustomPropertyDefinition definition,
        IReadOnlyCollection<EventCustomPropertyOption> options,
        Guid? defaultOptionId,
        CancellationToken cancellationToken)
    {
        definition.DefaultOptionId = null;
        _dbContext.Entry(definition).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _dbContext.EventCustomPropertyOptions
            .IgnoreQueryFilters()
            .Where(x => x.EventCustomPropertyDefinitionId == definition.Id)
            .ExecuteDeleteAsync(cancellationToken);

        if (options.Count > 0)
        {
            await _dbContext.EventCustomPropertyOptions.AddRangeAsync(options, cancellationToken);
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
        await _dbContext.EventCustomPropertyValues
            .IgnoreQueryFilters()
            .Where(x => x.EventCustomPropertyDefinitionId == id)
            .ExecuteDeleteAsync(cancellationToken);

        await _dbContext.EventCustomPropertyOptions
            .IgnoreQueryFilters()
            .Where(x => x.EventCustomPropertyDefinitionId == id)
            .ExecuteDeleteAsync(cancellationToken);

        var affectedRows = await _dbContext.EventCustomPropertyDefinitions
            .IgnoreQueryFilters()
            .Where(x => x.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }

    public async Task<List<EventCustomPropertyValue>> GetValuesForEvent(Guid eventId)
    {
        return await _dbContext.EventCustomPropertyValues
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.EventId == eventId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync();
    }

    public async Task<List<EventCustomPropertyValue>> GetValuesForDefinition(Guid definitionId)
    {
        return await _dbContext.EventCustomPropertyValues
            .AsNoTrackingWithIdentityResolution()
            .Where(x => x.EventCustomPropertyDefinitionId == definitionId)
            .OrderBy(x => x.Ordinal)
            .ToListAsync();
    }

    public async Task<EventCustomPropertyValue> SetValue(EventCustomPropertyValue value, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.EventCustomPropertyValues
            .FirstOrDefaultAsync(x =>
                x.EventCustomPropertyDefinitionId == value.EventCustomPropertyDefinitionId
                && x.EventId == value.EventId
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

        await _dbContext.EventCustomPropertyValues.AddAsync(value, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return value;
    }

    public async Task SetMultiValues(
        Guid definitionId,
        Guid eventId,
        IReadOnlyCollection<EventCustomPropertyValue> values,
        CancellationToken cancellationToken)
    {
        await _dbContext.EventCustomPropertyValues
            .IgnoreQueryFilters()
            .Where(x => x.EventCustomPropertyDefinitionId == definitionId && x.EventId == eventId)
            .ExecuteDeleteAsync(cancellationToken);

        if (values.Count > 0)
        {
            await _dbContext.EventCustomPropertyValues.AddRangeAsync(values, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
