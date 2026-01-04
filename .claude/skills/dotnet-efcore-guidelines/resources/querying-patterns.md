# Querying Patterns

Efficient querying patterns with Entity Framework Core for ISLAMU Event.

---

## Basic Queries

### Get All

```csharp
// ✅ Simple get all
var events = await _dbContext.Events.ToListAsync();

// ✅ With AsNoTracking for read-only
var events = await _dbContext.Events
    .AsNoTracking()
    .ToListAsync();
```

### Get By ID

```csharp
// ✅ FindAsync (uses primary key)
var event = await _dbContext.Events.FindAsync(eventId);

// ✅ FirstOrDefaultAsync with condition
var event = await _dbContext.Events
    .FirstOrDefaultAsync(e => e.Id == eventId);
```

### Get Single

```csharp
// ✅ SingleOrDefaultAsync (throws if multiple)
var event = await _dbContext.Events
    .SingleOrDefaultAsync(e => e.Id == eventId);

// ⚠️ Use only when expecting exactly 0 or 1 result
```

---

## Filtering

### Where Clause

```csharp
// Simple filter
var upcomingEvents = await _dbContext.Events
    .Where(e => e.StartDate > DateTime.Now)
    .ToListAsync();

// Multiple conditions
var events = await _dbContext.Events
    .Where(e => e.OrganizationId == orgId &&
                e.StartDate > DateTime.Now &&
                !e.IsCancelled)
    .ToListAsync();

// String contains (case-sensitive in PostgreSQL)
var events = await _dbContext.Events
    .Where(e => e.Title.Contains(searchTerm))
    .ToListAsync();

// Case-insensitive search (PostgreSQL)
var events = await _dbContext.Events
    .Where(e => EF.Functions.ILike(e.Title, $"%{searchTerm}%"))
    .ToListAsync();
```

---

## Eager Loading (Include)

### Single Level

```csharp
// ✅ Load events with organization
var events = await _dbContext.Events
    .Include(e => e.Organization)
    .ToListAsync();
```

### Multiple Includes

```csharp
// ✅ Multiple related entities
var events = await _dbContext.Events
    .Include(e => e.Organization)
    .Include(e => e.EventType)
    .Include(e => e.FeaturedImage)
    .Include(e => e.AudienceAge)
    .ToListAsync();
```

### Nested Include (ThenInclude)

```csharp
// ✅ Load events with organization and organization members
var events = await _dbContext.Events
    .Include(e => e.Organization)
        .ThenInclude(o => o.Members)
    .ToListAsync();

// ✅ Multiple levels
var events = await _dbContext.Events
    .Include(e => e.Organization)
        .ThenInclude(o => o.Members)
            .ThenInclude(m => m.User)
    .ToListAsync();
```

### Filtered Include (EF Core 5+)

```csharp
// ✅ Only include approved members
var organizations = await _dbContext.Organizations
    .Include(o => o.Members.Where(m => m.IsApproved))
    .ToListAsync();

// ✅ Only include upcoming events
var organizations = await _dbContext.Organizations
    .Include(o => o.Events.Where(e => e.StartDate > DateTime.Now))
    .ToListAsync();
```

---

## Projection (Select)

### Project to DTO

```csharp
// ✅ RECOMMENDED: Project directly to DTO
var eventDtos = await _dbContext.Events
    .Include(e => e.Organization)
    .Select(e => new EventListDto
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        OrganizationName = e.Organization.FullName,  // ✅ No N+1 query
        StartDate = e.StartDate
    })
    .ToListAsync();
```

**Benefits**:
- ✅ Only selected columns retrieved
- ✅ No change tracking
- ✅ Faster than loading entire entities

### Anonymous Type

```csharp
// ✅ Project to anonymous type
var events = await _dbContext.Events
    .Select(e => new
    {
        e.Id,
        e.Title,
        OrganizationName = e.Organization.FullName
    })
    .ToListAsync();
```

### Conditional Projection

```csharp
// ✅ Conditional fields in projection
var events = await _dbContext.Events
    .Select(e => new EventListDto
    {
        Id = e.Id,
        Title = e.Title,
        IsUpcoming = e.StartDate > DateTime.Now,  // Computed
        DaysUntil = (e.StartDate - DateTime.Now).Days  // Computed
    })
    .ToListAsync();
```

---

## Sorting

### OrderBy

```csharp
// ✅ Ascending
var events = await _dbContext.Events
    .OrderBy(e => e.StartDate)
    .ToListAsync();

// ✅ Descending
var events = await _dbContext.Events
    .OrderByDescending(e => e.StartDate)
    .ToListAsync();

// ✅ Multiple sort criteria
var events = await _dbContext.Events
    .OrderBy(e => e.OrganizationId)
    .ThenByDescending(e => e.StartDate)
    .ToListAsync();
```

### Dynamic Sorting

```csharp
public async Task<List<EventListDto>> GetEvents(string sortBy, bool descending)
{
    var query = _dbContext.Events.AsQueryable();

    query = sortBy switch
    {
        "title" => descending ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
        "date" => descending ? query.OrderByDescending(e => e.StartDate) : query.OrderBy(e => e.StartDate),
        "views" => descending ? query.OrderByDescending(e => e.TotalViews) : query.OrderBy(e => e.TotalViews),
        _ => query.OrderByDescending(e => e.StartDate)  // Default
    };

    return await query
        .Select(e => new EventListDto { /* ... */ })
        .ToListAsync();
}
```

---

## Pagination

### Skip and Take

```csharp
public async Task<List<EventListDto>> GetEventsPaginated(int page, int pageSize)
{
    return await _dbContext.Events
        .OrderByDescending(e => e.StartDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new EventListDto { /* ... */ })
        .ToListAsync();
}
```

### With Total Count

```csharp
public async Task<(List<EventListDto> Events, int TotalCount)> GetEventsPaginated(
    int page,
    int pageSize)
{
    var query = _dbContext.Events.AsQueryable();

    var totalCount = await query.CountAsync();

    var events = await query
        .OrderByDescending(e => e.StartDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new EventListDto { /* ... */ })
        .ToListAsync();

    return (events, totalCount);
}
```

---

## Aggregation

### Count

```csharp
// Total count
var totalEvents = await _dbContext.Events.CountAsync();

// Conditional count
var upcomingCount = await _dbContext.Events
    .CountAsync(e => e.StartDate > DateTime.Now);
```

### Sum, Average, Min, Max

```csharp
// Sum
var totalViews = await _dbContext.Events.SumAsync(e => e.TotalViews);

// Average
var avgPrice = await _dbContext.Events.AverageAsync(e => e.Price);

// Min and Max
var earliestEvent = await _dbContext.Events.MinAsync(e => e.StartDate);
var latestEvent = await _dbContext.Events.MaxAsync(e => e.StartDate);
```

### GroupBy

```csharp
// Group events by organization
var eventsByOrg = await _dbContext.Events
    .GroupBy(e => e.OrganizationId)
    .Select(g => new
    {
        OrganizationId = g.Key,
        EventCount = g.Count(),
        TotalViews = g.Sum(e => e.TotalViews)
    })
    .ToListAsync();
```

---

## Joining

### Explicit Join

```csharp
var result = await (
    from e in _dbContext.Events
    join o in _dbContext.Organizations on e.OrganizationId equals o.Id
    select new EventListDto
    {
        Id = e.Id,
        Title = e.Title,
        OrganizationName = o.FullName
    })
    .ToListAsync();
```

### Include vs Join

```csharp
// ❌ Include (loads entire related entity)
var events = await _dbContext.Events
    .Include(e => e.Organization)
    .ToListAsync();

// ✅ Select (only needed fields)
var events = await _dbContext.Events
    .Select(e => new EventListDto
    {
        Id = e.Id,
        Title = e.Title,
        OrganizationName = e.Organization.FullName  // Automatic join
    })
    .ToListAsync();
```

---

## Advanced Patterns

### AsNoTracking

```csharp
// ✅ Use for read-only queries (faster)
var events = await _dbContext.Events
    .AsNoTracking()
    .Include(e => e.Organization)
    .ToListAsync();
```

### AsSplitQuery

```csharp
// ✅ Avoid cartesian explosion with multiple Includes
var organizations = await _dbContext.Organizations
    .Include(o => o.Events)
    .Include(o => o.Members)
    .AsSplitQuery()  // Executes as 3 separate SQL queries
    .ToListAsync();

// ❌ Single query (can cause performance issues)
var organizations = await _dbContext.Organizations
    .Include(o => o.Events)
    .Include(o => o.Members)
    .ToListAsync();
```

### FromSqlRaw (Raw SQL)

```csharp
// ✅ Use for complex queries not supported by LINQ
var events = await _dbContext.Events
    .FromSqlRaw(@"
        SELECT * FROM ""Events""
        WHERE ""StartDate"" > {0}
        ORDER BY ""TotalViews"" DESC
    ", DateTime.Now)
    .ToListAsync();

// ⚠️ Use parameters to prevent SQL injection
var searchTerm = "conference";
var events = await _dbContext.Events
    .FromSqlRaw(@"
        SELECT * FROM ""Events""
        WHERE ""Title"" ILIKE {0}
    ", $"%{searchTerm}%")
    .ToListAsync();
```

### Any / All / Contains

```csharp
// Any: Check if any records exist
var hasUpcomingEvents = await _dbContext.Events
    .AnyAsync(e => e.StartDate > DateTime.Now);

// All: Check if all records match condition
var allFree = await _dbContext.Events
    .AllAsync(e => e.Price == 0);

// Contains: Check if value in list
var organizationIds = new[] { guid1, guid2, guid3 };
var events = await _dbContext.Events
    .Where(e => organizationIds.Contains(e.OrganizationId))
    .ToListAsync();
```

---

## Performance Optimization

### N+1 Query Problem

```csharp
// ❌ N+1 PROBLEM: Loads organization for each event
var events = await _dbContext.Events.ToListAsync();
foreach (var evt in events)
{
    Console.WriteLine(evt.Organization.FullName);  // ❌ Separate query per event!
}

// ✅ FIX 1: Use Include
var events = await _dbContext.Events
    .Include(e => e.Organization)
    .ToListAsync();

// ✅ FIX 2: Use Select (RECOMMENDED)
var events = await _dbContext.Events
    .Select(e => new
    {
        e.Id,
        e.Title,
        OrganizationName = e.Organization.FullName  // ✅ Single query
    })
    .ToListAsync();
```

### Avoid Loading Entire Entity

```csharp
// ❌ Loads entire entity (all columns)
var titles = await _dbContext.Events
    .ToListAsync()
    .Select(e => e.Title);  // ❌ Executes in memory

// ✅ Project to only needed columns
var titles = await _dbContext.Events
    .Select(e => e.Title)
    .ToListAsync();  // ✅ Only selects Title column
```

### Compiled Queries

```csharp
// Define compiled query (cached)
private static readonly Func<ExploreDbContext, Guid, Task<Event?>> _getEventById =
    EF.CompileAsyncQuery((ExploreDbContext context, Guid id) =>
        context.Events.FirstOrDefault(e => e.Id == id));

// Use compiled query
var event = await _getEventById(_dbContext, eventId);
```

---

## Best Practices

| Practice | Reason |
|----------|--------|
| ✅ Use `Select` for projections | Only retrieves needed columns |
| ✅ Use `AsNoTracking()` for read-only | Faster, no change tracking |
| ✅ Use `Include` for related data | Avoid N+1 queries |
| ✅ Use `AsSplitQuery()` for multiple includes | Avoid cartesian explosion |
| ✅ Filter before sorting/paging | Reduces data processed |
| ✅ Use `CountAsync()` instead of `Count()` | Non-blocking |
| ✅ Use parameters in raw SQL | Prevent SQL injection |
| ❌ Don't use `ToList()` before filtering | Loads all data into memory |
| ❌ Don't access navigation properties without Include | Causes N+1 |
| ❌ Don't track entities for read-only queries | Unnecessary overhead |

---

**Related Resources**:
- [repository-pattern.md](repository-pattern.md) - Repository implementations
- [dbcontext-patterns.md](dbcontext-patterns.md) - DbContext configuration
- [entity-configuration.md](entity-configuration.md) - Entity relationships
