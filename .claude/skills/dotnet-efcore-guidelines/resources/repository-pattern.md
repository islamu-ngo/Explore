# Repository Pattern

Repository pattern implementation for ISLAMU Event project.

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
// File: Explore.Application/Contracts/Persistence/IGenericRepository.cs
namespace Explore.Application.Contracts.Persistence;

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
// File: Explore.Persistence/Repositories/GenericRepository.cs
using Microsoft.EntityFrameworkCore;
using Explore.Application.Contracts.Persistence;

namespace Explore.Persistence.Repositories;

public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
{
    private readonly ExploreDbContext _dbContext;

    public GenericRepository(ExploreDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<T> Create(T entity)
    {
        await _dbContext.AddAsync(entity);
        await _dbContext.SaveChangesAsync();
        return entity;
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
// File: Explore.Application/Contracts/Persistence/IEventRepository.cs
using Explore.Application.DTOs.Event;
using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public interface IEventRepository : IGenericRepository<Event, Guid>
{
    Task<List<Event>> GetEventsWithDetails();
    Task<Event?> GetEventWithDetails(Guid id);
    Task<List<Event>> GetMyEventsWithDetails(string userId);
    Task<List<Event>> GetEventsByActor(Guid actorId);
}
```

### Implementation (Persistence Layer)

```csharp
// File: Explore.Persistence/Repositories/EventRepository.cs
using Microsoft.EntityFrameworkCore;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Domain;

namespace Explore.Persistence.Repositories;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // ✅ REPOSITORIES RETURN ENTITIES, NOT DTOs
    public async Task<List<Event>> GetEventsWithDetails()
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AtprotoRecord)
            .ToListAsync();
    }

    public async Task<Event?> GetEventWithDetails(Guid id)
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AtprotoRecord)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> GetEventsByActor(Guid actorId)
    {
        return await _dbContext.Events
            .Where(e => e.ActorId == actorId)
            .Include(e => e.EventType)
            .Include(e => e.FeaturedImage)
            .ToListAsync();
    }

    public async Task<List<Event>> GetMyEventsWithDetails(string userId)
    {
        // Returns entities; handler will map to DTOs
        return await _dbContext.Events
            .Where(e => e.Organization.Members.Any(m => m.UserId == userId))
            .Include(e => e.EventType)
            .Include(e => e.FeaturedImage)
            .ToListAsync();
    }

    // NOTE: In the Application layer handler, entities are mapped to DTOs:
    //
    // public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    // {
    //     // Repository returns ENTITIES
    //     var events = await _eventRepository.GetEventsWithDetails();
    //
    //     // AutoMapper maps ENTITIES to DTOs
    //     return _mapper.Map<List<EventListDto>>(events);
    // }
}
```

---

## Repository Registration (DI)

```csharp
// File: Explore.Persistence/PersistenceServicesRegistration.cs
using Microsoft.Extensions.DependencyInjection;
using Explore.Application.Contracts.Persistence;
using Explore.Persistence.Repositories;

namespace Explore.Persistence;

public static class PersistenceServicesRegistration
{
    public static IServiceCollection AddPersistenceServices(
        this IServiceCollection services)
    {
        // Register generic repository
        services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));

        // Register specific repositories
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IOrganizationRepository, OrganizationRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IEventRegistrationRepository, EventRegistrationRepository>();

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
public async Task<List<Event>> GetEventsWithDetails()
{
    return await _dbContext.Events
        .Include(e => e.EventType)
        .Include(e => e.AudienceGender)
        .Include(e => e.AudienceAge)
        .Include(e => e.Actor)
        .ToListAsync();
}

// ✅ CORRECT: Handler maps entities to DTOs
public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        // Repository returns ENTITIES
        var events = await _eventRepository.GetEventsWithDetails();

        // AutoMapper maps ENTITIES to DTOs
        return _mapper.Map<List<EventListDto>>(events);
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
public async Task<List<Event>> GetEventsReadOnly()
{
    // ✅ No change tracking (faster for read-only)
    return await _dbContext.Events
        .AsNoTracking()
        .Include(e => e.EventType)
        .Include(e => e.Organization)
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

// File: Application/Specifications/EventSpecification.cs
public class EventsByActorSpec : ISpecification<Event>
{
    public EventsByActorSpec(Guid actorId)
    {
        Criteria = e => e.ActorId == actorId;
        Includes = new List<Expression<Func<Event, object>>>
        {
            e => e.Actor,
            e => e.EventType
        };
    }

    public Expression<Func<Event, bool>> Criteria { get; }
    public List<Expression<Func<Event, object>>> Includes { get; }
}

// Repository method
public async Task<List<Event>> Find(ISpecification<Event> spec)
{
    var query = _dbContext.Events.Where(spec.Criteria);

    query = spec.Includes
        .Aggregate(query, (current, include) => current.Include(include));

    return await query.ToListAsync();
}
```

### Pagination

```csharp
public async Task<List<Event>> GetEventsPaginated(
    int page,
    int pageSize,
    string? searchTerm)
{
    var query = _dbContext.Events
        .Include(e => e.Actor)
        .Include(e => e.EventType)
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
```

---

## Best Practices

| Practice | Reason |
|----------|--------|
| ✅ Interface in Application, implementation in Persistence | Clean Architecture |
| ✅ Inherit from GenericRepository | Code reuse |
| ✅ Project to DTOs with `Select` | Performance |
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
