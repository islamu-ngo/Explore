# Repository Pattern

> **Project-Agnostic Repository Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

---

## Why Repository Pattern?

```
┌─────────────────────────────────────────────────────────────────────┐
│                    BENEFITS OF REPOSITORY PATTERN                   │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  ✅ Abstraction: Application layer doesn't depend on EF Core        │
│  ✅ Testability: Easy to mock repositories for unit tests          │
│  ✅ Clean Architecture: Maintains dependency inversion              │
│  ✅ Centralized Data Access: Common queries in one place            │
│  ✅ Consistent API: Uniform data access across entities             │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Generic Repository

Base repository with common CRUD operations.

### Interface (Application Layer)

```csharp
// File: {Project}.Application/Contracts/Persistence/IGenericRepository.cs
namespace {Project}.Application.Contracts.Persistence;

public interface IGenericRepository<T, TKey> where T : class
{
    Task<T> Create(T entity);
    Task<T?> GetById(TKey id);
    Task<IReadOnlyList<T>> GetAll();
    Task Update(T entity);
    Task Delete(T entity);
    Task<bool> Exists(TKey id);
}
```

### Implementation (Persistence Layer)

```csharp
// File: {Project}.Persistence/Repositories/GenericRepository.cs
using System;
using {Project}.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace {Project}.Persistence.Repositories;

public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
{
    private readonly {DbContext} _dbContext;

    public GenericRepository({DbContext} dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> Create(T entity)
    {
        try
        {
            await _dbContext.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == "23505")
        {
            // Duplicate key violation - detach the entity and rethrow with more context
            _dbContext.Entry(entity).State = EntityState.Detached;
            throw new InvalidOperationException(
                $"A record with the same unique key already exists. Constraint: {pgEx.ConstraintName}. Detail: {pgEx.Detail}",
                ex);
        }
    }

    public async Task<T?> GetById(TKey id)
    {
        return await _dbContext.Set<T>().FindAsync(id);
    }

    public async Task<IReadOnlyList<T>> GetAll()
    {
        return await _dbContext.Set<T>().ToListAsync();
    }

    public async Task Update(T entity)
    {
        _dbContext.Entry(entity).State = EntityState.Modified;
        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(T entity)
    {
        _dbContext.Set<T>().Remove(entity);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<bool> Exists(TKey id)
    {
        var entity = await GetById(id);
        return entity != null;
    }
}
```

---

## Entity-Specific Repository

Inherit from GenericRepository and add custom methods.

### Interface (Application Layer)

```csharp
// File: {Project}.Application/Contracts/Persistence/I{Entity}Repository.cs
using {Project}.Domain;

namespace {Project}.Application.Contracts.Persistence;

public interface I{Entity}Repository : IGenericRepository<{Entity}, {IdType}>
{
    Task<List<{Entity}>> Get{Entities}WithDetails();
    Task<{Entity}?> Get{Entity}WithDetails({IdType} id);
    Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId);
}
```

### Implementation (Persistence Layer)

```csharp
// File: {Project}.Persistence/Repositories/{Entity}Repository.cs
using Microsoft.EntityFrameworkCore;
using {Project}.Application.Contracts.Persistence;
using {Project}.Domain;

namespace {Project}.Persistence.Repositories;

public class {Entity}Repository : GenericRepository<{Entity}, {IdType}>, I{Entity}Repository
{
    private readonly {DbContext} _dbContext;

    public {Entity}Repository({DbContext} dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // ✅ REPOSITORIES RETURN ENTITIES, NOT DTOs
    public async Task<List<{Entity}>> Get{Entities}WithDetails()
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{ParentEntity})
            .Include(e => e.Status)
            .Include(e => e.Visibility)
            .Include(e => e.FeaturedImage)
            .ToListAsync();
    }

    public async Task<{Entity}?> Get{Entity}WithDetails({IdType} id)
    {
        return await _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{ParentEntity})
            .Include(e => e.Status)
            .Include(e => e.Visibility)
            .Include(e => e.FeaturedImage)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<{Entity}>> GetMy{Entities}WithDetails(string userId)
    {
        {IdType} userIdParsed;
        var isValid = {IdType}.TryParse(userId, out userIdParsed);

        var query = _dbContext.{Entities}
            .Include(e => e.{LookupEntity})
            .Include(e => e.{RelatedEntity1})
            .Include(e => e.{RelatedEntity2})
            .Include(e => e.{ParentEntity})
            .Include(e => e.Status)
            .Include(e => e.Visibility)
            .Include(e => e.FeaturedImage)
            .AsQueryable();

        if (isValid)
        {
            query = query.Where(e =>
                _dbContext.Users.Any(u => u.Id == userIdParsed && u.{ParentEntity}Id == e.{ParentEntity}Id));
        }

        return await query.ToListAsync();
    }

    // NOTE: In the Application layer handler, entities are mapped to DTOs:
    //
    // public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
    // {
    //     // Repository returns ENTITIES
    //     var {entities} = await _{entity}Repository.Get{Entities}WithDetails();
    //
    //     // AutoMapper maps ENTITIES to DTOs
    //     return _mapper.Map<List<{Entity}ListDto>>({entities});
    // }
}
```

---

## Repository Registration (DI)

```csharp
// File: {Project}.Persistence/PersistenceServicesRegistration.cs
using Microsoft.Extensions.DependencyInjection;
using {Project}.Application.Contracts.Persistence;
using {Project}.Persistence.Repositories;

namespace {Project}.Persistence;

public static class PersistenceServicesRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services)
    {
        // Register generic repository
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

        // Register specific repositories
        services.AddScoped<I{Entity}Repository, {Entity}Repository>();
        services.AddScoped<I{ParentEntity}Repository, {ParentEntity}Repository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<I{LinkEntity}Repository, {LinkEntity}Repository>();

        return services;
    }
}
```

**Usage in Program.cs**:
```csharp
builder.Services.AddPersistenceServices();
```

---

## Repository Method Patterns

### Return Entities (Standard Pattern)

**CRITICAL**: Repositories return ENTITIES, not DTOs. DTO mapping happens in Application layer handlers via AutoMapper.

```csharp
// ✅ CORRECT: Repository returns entities
public async Task<List<{Entity}>> Get{Entities}WithDetails()
{
    return await _dbContext.{Entities}
        .Include(e => e.{LookupEntity})
        .Include(e => e.{RelatedEntity1})
        .Include(e => e.{RelatedEntity2})
        .Include(e => e.{ParentEntity})
        .ToListAsync();
}

// ✅ CORRECT: Handler maps entities to DTOs
public class Get{Entity}ListRequestHandler : IRequestHandler<Get{Entity}ListRequest, List<{Entity}ListDto>>
{
    private readonly I{Entity}Repository _{entity}Repository;
    private readonly IMapper _mapper;

    public async Task<List<{Entity}ListDto>> Handle(Get{Entity}ListRequest request, CancellationToken cancellationToken)
    {
        // Repository returns ENTITIES
        var {entities} = await _{entity}Repository.Get{Entities}WithDetails();

        // AutoMapper maps ENTITIES to DTOs
        return _mapper.Map<List<{Entity}ListDto>>({entities});
    }
}
```

**Benefits**:
- ✅ Clean separation of concerns
- ✅ Repositories focus on data access only
- ✅ Handlers handle DTO mapping with AutoMapper
- ✅ Reusable repositories for different DTO shapes

### AsNoTracking for Read-Only Queries

```csharp
public async Task<List<{Entity}>> Get{Entities}ReadOnly()
{
    // ✅ No change tracking (faster for read-only)
    return await _dbContext.{Entities}
        .AsNoTracking()
        .Include(e => e.{LookupEntity})
        .Include(e => e.{ParentEntity})
        .ToListAsync();
}
```

---

## Advanced Repository Patterns

### Specification Pattern

```csharp
// File: Application/Specifications/ISpecification.cs
public interface ISpecification<T>
{
    Expression<Func<T, bool>> Criteria { get; }
    List<Expression<Func<T, object>>> Includes { get; }
}

// File: Application/Specifications/{Entity}Specification.cs
public class {Entities}By{ParentEntity}Spec : ISpecification<{Entity}>
{
    public {Entities}By{ParentEntity}Spec({IdType} {parentEntity}Id)
    {
        Criteria = e => e.{ParentEntity}Id == {parentEntity}Id;
        Includes = new List<Expression<Func<{Entity}, object>>>
        {
            e => e.{ParentEntity},
            e => e.{LookupEntity}
        };
    }

    public Expression<Func<{Entity}, bool>> Criteria { get; }
    public List<Expression<Func<{Entity}, object>>> Includes { get; }
}

// Repository method
public async Task<List<{Entity}>> Find(ISpecification<{Entity}> spec)
{
    var query = _dbContext.{Entities}.Where(spec.Criteria);

    query = spec.Includes
        .Aggregate(query, (current, include) => current.Include(include));

    return await query.ToListAsync();
}
```

### Pagination

```csharp
public async Task<List<{Entity}>> Get{Entities}Paginated(
    int page,
    int pageSize,
    string? searchTerm)
{
    var query = _dbContext.{Entities}
        .Include(e => e.{ParentEntity})
        .Include(e => e.{LookupEntity})
        .AsQueryable();

    if (!string.IsNullOrWhiteSpace(searchTerm))
    {
        query = query.Where(e =>
            e.Title.Contains(searchTerm) ||
            e.Description.Contains(searchTerm));
    }

    var totalCount = await query.CountAsync();

    var items = await query
        .OrderByDescending(e => e.StartDate)
        .Skip((page - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

    return items;
}
```

---

## Best Practices

| Practice | Reason |
|----------|--------|
| ✅ Interface in Application, implementation in Persistence | Clean Architecture |
| ✅ Inherit from GenericRepository | Code reuse |
| ✅ Return entities, not DTOs | Separation of concerns |
| ✅ Use `AsNoTracking()` for read-only | No unnecessary tracking |
| ✅ Include related entities with `Include` | Avoid N+1 queries |
| ✅ Register repositories in DI | Dependency injection |
| ✅ Use async methods (`Async` suffix) | Non-blocking I/O |
| ❌ Don't return IQueryable from repository | Encapsulation |
| ❌ Don't inject DbContext in Application layer | Breaks abstraction |
| ❌ Don't track entities for read-only queries | Performance overhead |

---

**Related Resources**:
- [dbcontext-patterns.md](dbcontext-patterns.md) - DbContext configuration
- [querying-patterns.md](querying-patterns.md) - Query optimization
- [entity-configuration.md](entity-configuration.md) - Entity configuration
