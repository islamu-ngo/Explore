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
    Task<List<EventListDto>> GetEventsWithDetails();
    Task<EventDto> GetEventWithDetails(Guid id);
    Task<List<EventListDto>> GetMyEventsWithDetails(string userId);
    Task<List<EventListDto>> GetEventsByOrganization(Guid organizationId);
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

    public async Task<List<EventListDto>> GetEventsWithDetails()
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.Organization)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AudienceAge)
            .Include(e => e.AudienceGender)
            .Select(e => new EventListDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                OrganizationId = e.OrganizationId,
                OrganizationFullName = e.Organization.FullName,
                EventTypeFullName = e.EventType.FullName,
                FeaturedImageUri = e.FeaturedImage.Uri,
                AudienceAgeFullName = e.AudienceAge.FullName,
                AudienceGenderFullName = e.AudienceGender.FullName,
                TotalViews = e.TotalViews,
                Country = e.Country,
                City = e.City
            })
            .ToListAsync();
    }

    public async Task<EventDto> GetEventWithDetails(Guid id)
    {
        var eventDto = await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.Organization)
            .Include(e => e.FeaturedImage)
            .Include(e => e.AudienceAge)
            .Include(e => e.AudienceGender)
            .Where(e => e.Id == id)
            .Select(e => new EventDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                OrganizationId = e.OrganizationId,
                OrganizationFullName = e.Organization.FullName,
                EventTypeId = e.EventTypeId,
                EventTypeFullName = e.EventType.FullName,
                FeaturedImageUri = e.FeaturedImage.Uri,
                TotalViews = e.TotalViews,
                Country = e.Country,
                City = e.City,
                Address = e.Address,
                PostCode = e.PostCode,
                Price = e.Price,
                IsRegistrationRequired = e.IsRegistrationRequired,
                AudienceAttendees = e.AudienceAttendees
            })
            .FirstOrDefaultAsync();

        return eventDto ?? throw new KeyNotFoundException($"Event with ID {id} not found");
    }

    public async Task<List<EventListDto>> GetEventsByOrganization(Guid organizationId)
    {
        return await _dbContext.Events
            .Where(e => e.OrganizationId == organizationId)
            .Include(e => e.EventType)
            .Include(e => e.FeaturedImage)
            .Select(e => new EventListDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                EventTypeFullName = e.EventType.FullName,
                FeaturedImageUri = e.FeaturedImage.Uri
            })
            .ToListAsync();
    }

    public async Task<List<EventListDto>> GetMyEventsWithDetails(string userId)
    {
        Guid userGuid = Guid.TryParse(userId, out var guid) ? guid : Guid.Empty;

        var query = _dbContext.Events
            .Include(e => e.Organization)
                .ThenInclude(o => o.Members)
            .Include(e => e.EventType)
            .Include(e => e.FeaturedImage)
            .AsQueryable();

        if (userGuid != Guid.Empty)
        {
            query = query.Where(e =>
                e.Organization.CreatedByUserId == userId ||
                e.Organization.Members.Any(m => m.UserId == userGuid));
        }
        else
        {
            query = query.Where(e => e.Organization.CreatedByUserId == userId);
        }

        return await query
            .Select(e => new EventListDto
            {
                Id = e.Id,
                Title = e.Title,
                Description = e.Description,
                StartDate = e.StartDate,
                OrganizationFullName = e.Organization.FullName,
                EventTypeFullName = e.EventType.FullName,
                FeaturedImageUri = e.FeaturedImage.Uri
            })
            .ToListAsync();
    }
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

### Projection to DTO (Recommended)

```csharp
public async Task<List<EventListDto>> GetEventsWithDetails()
{
    // ✅ Project directly to DTO (no entities tracked)
    return await _dbContext.Events
        .Include(e => e.Organization)
        .Select(e => new EventListDto
        {
            Id = e.Id,
            Title = e.Title,
            OrganizationName = e.Organization.FullName
        })
        .ToListAsync();
}
```

**Benefits**:
- ✅ No change tracking (faster)
- ✅ Only selected columns retrieved
- ✅ Returns DTO directly

### Return Entity (Use Sparingly)

```csharp
public async Task<Event?> GetEventWithIncludes(Guid id)
{
    // ⚠️ Returns tracked entity
    return await _dbContext.Events
        .Include(e => e.Organization)
        .Include(e => e.FeaturedImage)
        .FirstOrDefaultAsync(e => e.Id == id);
}
```

**When to use**:
- ⚠️ Only when you need to update the entity
- ⚠️ Use `AsNoTracking()` if read-only

### AsNoTracking for Read-Only

```csharp
public async Task<Event?> GetEventReadOnly(Guid id)
{
    // ✅ No change tracking
    return await _dbContext.Events
        .AsNoTracking()
        .Include(e => e.Organization)
        .FirstOrDefaultAsync(e => e.Id == id);
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
public class EventsByOrganizationSpec : ISpecification<Event>
{
    public EventsByOrganizationSpec(Guid organizationId)
    {
        Criteria = e => e.OrganizationId == organizationId;
        Includes = new List<Expression<Func<Event, object>>>
        {
            e => e.Organization,
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
public async Task<PaginatedList<EventListDto>> GetEventsPaginated(
    int page,
    int pageSize,
    string? searchTerm)
{
    var query = _dbContext.Events
        .Include(e => e.Organization)
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
        .Select(e => new EventListDto
        {
            Id = e.Id,
            Title = e.Title,
            OrganizationName = e.Organization.FullName
        })
        .ToListAsync();

    return new PaginatedList<EventListDto>(items, totalCount, page, pageSize);
}
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
