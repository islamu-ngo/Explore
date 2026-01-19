# Querying Patterns

> **Project-Agnostic Querying Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

---

## Basic Queries

### Get All

```csharp
// ✅ Simple get all
var {entities} = await _dbContext.{Entities}.ToListAsync();

// ✅ With AsNoTracking for read-only
var {entities} = await _dbContext.{Entities}
    .AsNoTracking()
    .ToListAsync();
```

### Get By ID

```csharp
// ✅ FindAsync (uses primary key)
var {entity} = await _dbContext.{Entities}.FindAsync({entity}Id);

// ✅ FirstOrDefaultAsync with condition
var {entity} = await _dbContext.{Entities}
    .FirstOrDefaultAsync(e => e.Id == {entity}Id);
```

### Get Single

```csharp
// ✅ SingleOrDefaultAsync (throws if multiple)
var {entity} = await _dbContext.{Entities}
    .SingleOrDefaultAsync(e => e.Id == {entity}Id);

// ⚠️ Use only when expecting exactly 0 or 1 result
```

---

## Filtering

### Where Clause

```csharp
// Simple filter
var upcoming{Entities} = await _dbContext.{Entities}
    .Where(e => e.StartDate > DateTime.Now)
    .ToListAsync();

// Multiple conditions
var {entities} = await _dbContext.{Entities}
    .Where(e => e.{ParentEntity}Id == parentId &&
                e.StartDate > DateTime.Now &&
                !e.IsCancelled)
    .ToListAsync();

// String contains (case-sensitive in PostgreSQL)
var {entities} = await _dbContext.{Entities}
    .Where(e => e.Title.Contains(searchTerm))
    .ToListAsync();

// Case-insensitive search (PostgreSQL)
var {entities} = await _dbContext.{Entities}
    .Where(e => EF.Functions.ILike(e.Title, $"%{searchTerm}%"))
    .ToListAsync();
```

---

## Eager Loading (Include)

### Single Level

```csharp
// ✅ Load {entities} with {parentEntity}
var {entities} = await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
    .ToListAsync();
```

### Multiple Includes

```csharp
// ✅ Multiple related entities
var {entities} = await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
    .Include(e => e.{LookupEntity})
    .Include(e => e.FeaturedImage)
    .Include(e => e.Status)
    .ToListAsync();
```

### Nested Include (ThenInclude)

```csharp
// ✅ Load {entities} with {parentEntity} and members
var {entities} = await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
        .ThenInclude(o => o.Members)
    .ToListAsync();

// ✅ Multiple levels
var {entities} = await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
        .ThenInclude(o => o.Members)
            .ThenInclude(m => m.User)
    .ToListAsync();
```

### Filtered Include (EF Core 5+)

```csharp
// ✅ Only include approved members
var {parentEntities} = await _dbContext.{ParentEntities}
    .Include(o => o.Members.Where(m => m.IsApproved))
    .ToListAsync();

// ✅ Only include upcoming {entities}
var {parentEntities} = await _dbContext.{ParentEntities}
    .Include(o => o.{Entities}.Where(e => e.StartDate > DateTime.Now))
    .ToListAsync();
```

---

## Projection (Select)

### Project to DTO

```csharp
// ✅ RECOMMENDED: Project directly to DTO
var {entity}Dtos = await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
    .Select(e => new {Entity}ListDto
    {
        Id = e.Id,
        Title = e.Title,
        Description = e.Description,
        {ParentEntity}Name = e.{ParentEntity}.FullName,  // ✅ No N+1 query
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
var {entities} = await _dbContext.{Entities}
    .Select(e => new
    {
        e.Id,
        e.Title,
        {ParentEntity}Name = e.{ParentEntity}.FullName
    })
    .ToListAsync();
```

### Conditional Projection

```csharp
// ✅ Conditional fields in projection
var {entities} = await _dbContext.{Entities}
    .Select(e => new {Entity}ListDto
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
var {entities} = await _dbContext.{Entities}
    .OrderBy(e => e.StartDate)
    .ToListAsync();

// ✅ Descending
var {entities} = await _dbContext.{Entities}
    .OrderByDescending(e => e.StartDate)
    .ToListAsync();

// ✅ Multiple sort criteria
var {entities} = await _dbContext.{Entities}
    .OrderBy(e => e.{ParentEntity}Id)
    .ThenByDescending(e => e.StartDate)
    .ToListAsync();
```

### Dynamic Sorting

```csharp
public async Task<List<{Entity}ListDto>> Get{Entities}(string sortBy, bool descending)
{
    var query = _dbContext.{Entities}.AsQueryable();

    query = sortBy switch
    {
        "title" => descending ? query.OrderByDescending(e => e.Title) : query.OrderBy(e => e.Title),
        "date" => descending ? query.OrderByDescending(e => e.StartDate) : query.OrderBy(e => e.StartDate),
        "views" => descending ? query.OrderByDescending(e => e.ViewCount) : query.OrderBy(e => e.ViewCount),
        _ => query.OrderByDescending(e => e.StartDate)  // Default
    };

    return await query
        .Select(e => new {Entity}ListDto { /* ... */ })
        .ToListAsync();
}
```

---

## Pagination

### Skip and Take

```csharp
public async Task<List<{Entity}ListDto>> Get{Entities}Paginated(int page, int pageSize)
{
    return await _dbContext.{Entities}
        .OrderByDescending(e => e.StartDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new {Entity}ListDto { /* ... */ })
        .ToListAsync();
}
```

### With Total Count

```csharp
public async Task<(List<{Entity}ListDto> {Entities}, int TotalCount)> Get{Entities}Paginated(
    int page,
    int pageSize)
{
    var query = _dbContext.{Entities}.AsQueryable();

    var totalCount = await query.CountAsync();

    var {entities} = await query
        .OrderByDescending(e => e.StartDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .Select(e => new {Entity}ListDto { /* ... */ })
        .ToListAsync();

    return ({entities}, totalCount);
}
```

---

## Aggregation

### Count

```csharp
// Total count
var total{Entities} = await _dbContext.{Entities}.CountAsync();

// Conditional count
var upcomingCount = await _dbContext.{Entities}
    .CountAsync(e => e.StartDate > DateTime.Now);
```

### Sum, Average, Min, Max

```csharp
// Sum
var totalViews = await _dbContext.{Entities}.SumAsync(e => e.ViewCount);

// Average
var avgPrice = await _dbContext.{Entities}.AverageAsync(e => e.Price);

// Min and Max
var earliest{Entity} = await _dbContext.{Entities}.MinAsync(e => e.StartDate);
var latest{Entity} = await _dbContext.{Entities}.MaxAsync(e => e.StartDate);
```

### GroupBy

```csharp
// Group {entities} by {parentEntity}
var {entities}By{ParentEntity} = await _dbContext.{Entities}
    .GroupBy(e => e.{ParentEntity}Id)
    .Select(g => new
    {
        {ParentEntity}Id = g.Key,
        {Entity}Count = g.Count(),
        TotalViews = g.Sum(e => e.ViewCount)
    })
    .ToListAsync();
```

---

## Joining

### Explicit Join

```csharp
var result = await (
    from e in _dbContext.{Entities}
    join o in _dbContext.{ParentEntities} on e.{ParentEntity}Id equals o.Id
    select new {Entity}ListDto
    {
        Id = e.Id,
        Title = e.Title,
        {ParentEntity}Name = o.FullName
    })
    .ToListAsync();
```

### Include vs Join

```csharp
// ❌ Include (loads entire related entity)
var {entities} = await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
    .ToListAsync();

// ✅ Select (only needed fields)
var {entities} = await _dbContext.{Entities}
    .Select(e => new {Entity}ListDto
    {
        Id = e.Id,
        Title = e.Title,
        {ParentEntity}Name = e.{ParentEntity}.FullName  // Automatic join
    })
    .ToListAsync();
```

---

## Advanced Patterns

### AsNoTracking

```csharp
// ✅ Use for read-only queries (faster)
var {entities} = await _dbContext.{Entities}
    .AsNoTracking()
    .Include(e => e.{ParentEntity})
    .ToListAsync();
```

### AsSplitQuery

```csharp
// ✅ Avoid cartesian explosion with multiple Includes
var {parentEntities} = await _dbContext.{ParentEntities}
    .Include(o => o.{Entities})
    .Include(o => o.Members)
    .AsSplitQuery()  // Executes as 3 separate SQL queries
    .ToListAsync();

// ❌ Single query (can cause performance issues)
var {parentEntities} = await _dbContext.{ParentEntities}
    .Include(o => o.{Entities})
    .Include(o => o.Members)
    .ToListAsync();
```

### FromSqlRaw (Raw SQL)

```csharp
// ✅ Use for complex queries not supported by LINQ
var {entities} = await _dbContext.{Entities}
    .FromSqlRaw(@"
        SELECT * FROM ""{Entities}""
        WHERE ""StartDate"" > {0}
        ORDER BY ""ViewCount"" DESC
    ", DateTime.Now)
    .ToListAsync();

// ⚠️ Use parameters to prevent SQL injection
var searchTerm = "conference";
var {entities} = await _dbContext.{Entities}
    .FromSqlRaw(@"
        SELECT * FROM ""{Entities}""
        WHERE ""Title"" ILIKE {0}
    ", $"%{searchTerm}%")
    .ToListAsync();
```

### Any / All / Contains

```csharp
// Any: Check if any records exist
var hasUpcoming{Entities} = await _dbContext.{Entities}
    .AnyAsync(e => e.StartDate > DateTime.Now);

// All: Check if all records match condition
var allFree = await _dbContext.{Entities}
    .AllAsync(e => e.Price == 0);

// Contains: Check if value in list
var {parentEntity}Ids = new[] { guid1, guid2, guid3 };
var {entities} = await _dbContext.{Entities}
    .Where(e => {parentEntity}Ids.Contains(e.{ParentEntity}Id))
    .ToListAsync();
```

---

## Performance Optimization

### N+1 Query Problem

```csharp
// ❌ N+1 PROBLEM: Loads {parentEntity} for each {entity}
var {entities} = await _dbContext.{Entities}.ToListAsync();
foreach (var item in {entities})
{
    Console.WriteLine(item.{ParentEntity}.FullName);  // ❌ Separate query per item!
}

// ✅ FIX 1: Use Include
var {entities} = await _dbContext.{Entities}
    .Include(e => e.{ParentEntity})
    .ToListAsync();

// ✅ FIX 2: Use Select (RECOMMENDED)
var {entities} = await _dbContext.{Entities}
    .Select(e => new
    {
        e.Id,
        e.Title,
        {ParentEntity}Name = e.{ParentEntity}.FullName  // ✅ Single query
    })
    .ToListAsync();
```

### Avoid Loading Entire Entity

```csharp
// ❌ Loads entire entity (all columns)
var titles = await _dbContext.{Entities}
    .ToListAsync()
    .Select(e => e.Title);  // ❌ Executes in memory

// ✅ Project to only needed columns
var titles = await _dbContext.{Entities}
    .Select(e => e.Title)
    .ToListAsync();  // ✅ Only selects Title column
```

### Compiled Queries

```csharp
// Define compiled query (cached)
private static readonly Func<{DbContext}, {IdType}, Task<{Entity}?>> _get{Entity}ById =
    EF.CompileAsyncQuery(({DbContext} context, {IdType} id) =>
        context.{Entities}.FirstOrDefault(e => e.Id == id));

// Use compiled query
var {entity} = await _get{Entity}ById(_dbContext, {entity}Id);
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
