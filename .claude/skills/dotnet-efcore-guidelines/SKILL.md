---
name: dotnet-efcore-guidelines
description: Entity Framework Core patterns for ISLAMU Event. Covers DbContext, entity configurations, repository pattern, migrations, and PostgreSQL-specific features.
type: domain
enforcement: suggest
priority: high
---

# .NET + Entity Framework Core Guidelines

## 🎯 Purpose

Provides Entity Framework Core best practices for the ISLAMU Event project using PostgreSQL.

## ⚡ When This Skill Activates

**Triggered by**:
- Keywords: "ef core", "entity framework", "dbcontext", "repository", "migration", "database", "postgres", "postgresql"
- File patterns: `**/Persistence/**/*.cs`, `**/Repositories/**/*.cs`, `**/*DbContext.cs`, `**/Configurations/**/*.cs`
- Content patterns: `DbContext`, `IEntityTypeConfiguration`, `modelBuilder`, `Include`, `DbSet`

## 🏗️ ISLAMU Event EF Core Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                    EF Core Architecture                             │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  Explore.Domain                                                     │
│  ├── Entities (Event, Organization, User, etc.)                    │
│  └── Enums                                                          │
│                                                                     │
│  Explore.Application                                                │
│  ├── Contracts/Persistence/                                         │
│  │   ├── IGenericRepository<T, TKey>                               │
│  │   ├── IEventRepository : IGenericRepository<Event, Guid>        │
│  │   └── IOrganizationRepository : IGenericRepository<Org, Guid>   │
│  └── DTOs/ (EventDto, EventListDto, etc.)                          │
│                                                                     │
│  Explore.Persistence                                                │
│  ├── ExploreDbContext.cs                                            │
│  ├── Configurations/Entities/                                       │
│  │   ├── EventConfiguration.cs : IEntityTypeConfiguration<Event>   │
│  │   ├── OrganizationConfiguration.cs                              │
│  │   └── ... (one per entity)                                      │
│  ├── Repositories/                                                  │
│  │   ├── GenericRepository<T, TKey> : IGenericRepository<T, TKey>  │
│  │   ├── EventRepository : GenericRepository<Event, Guid>          │
│  │   └── ... (one per entity with custom methods)                  │
│  └── Migrations/                                                    │
│      └── YYYYMMDDHHMMSS_MigrationName.cs                            │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

## 📚 Resources

| Resource | Description |
|----------|-------------|
| [dbcontext-patterns.md](resources/dbcontext-patterns.md) | DbContext configuration, SaveChangesAsync override |
| [entity-configuration.md](resources/entity-configuration.md) | IEntityTypeConfiguration, TPT, PostgreSQL functions |
| [repository-pattern.md](resources/repository-pattern.md) | GenericRepository, custom repositories |
| [querying-patterns.md](resources/querying-patterns.md) | Include, Select, projections, performance |
| [migrations.md](resources/migrations.md) | Creating and applying migrations |

## ⚡ Quick Reference

### DbContext Pattern

```csharp
public class ExploreDbContext : DbContext
{
    public ExploreDbContext(DbContextOptions<ExploreDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Audit logging, timestamps, etc.
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added)
            {
                // Handle creation
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }

    public DbSet<Event> Events { get; set; }
    public DbSet<Organization> Organizations { get; set; }
}
```

### Entity Configuration

```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.UseTptMappingStrategy();

        // PostgreSQL: UUIDv7 primary keys
        builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");

        // DB-level defaults are OK in EF configuration (do not add defaults in entity property initializers)
        builder.Property(e => e.TotalViews).HasDefaultValue(0);

        builder.Property(e => e.Title).HasMaxLength(200).IsRequired();
        builder.Property(e => e.Description).HasMaxLength(5000);
        builder.Property(e => e.Slug).HasMaxLength(500);

        builder.HasOne(e => e.Actor)
            .WithMany()
            .HasForeignKey(e => e.ActorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### Repository Pattern

**CRITICAL RULE**: Repositories return ENTITIES, not DTOs. DTO mapping happens in Application layer handlers via AutoMapper.

**Real Example from Explore.Persistence/Repositories/GenericRepository.cs:**

```csharp
namespace Explore.Persistence.Repositories;

using System;
using System.Collections.Generic;
using Explore.Application.Contracts.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;

public class GenericRepository<T, TKey> : IGenericRepository<T, TKey> where T : class
{
    private readonly ExploreDbContext _dbContext;

    public GenericRepository(ExploreDbContext dbContext)
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
            // Duplicate key violation: detach and rethrow with more context
            _dbContext.Entry(entity).State = EntityState.Detached;
            throw new InvalidOperationException(
                $"A record with the same unique key already exists. Constraint: {pgEx.ConstraintName}. Detail: {pgEx.Detail}",
                ex);
        }
    }

    public async Task<T?> GetById(TKey id) =>
        await _dbContext.Set<T>().FindAsync(id);

    public async Task<IReadOnlyList<T>> GetAll() =>
        await _dbContext.Set<T>().ToListAsync();

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
}
```

**Real Example from Explore.Persistence/Repositories/EventRepository.cs:**

```csharp
namespace Explore.Persistence.Repositories;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Explore.Application.Contracts.Persistence;
using Explore.Domain;
using Microsoft.EntityFrameworkCore;

public class EventRepository : GenericRepository<Event, Guid>, IEventRepository
{
    private readonly ExploreDbContext _dbContext;

    public EventRepository(ExploreDbContext dbContext) : base(dbContext)
    {
        _dbContext = dbContext;
    }

    // Returns ENTITIES, not DTOs
    public async Task<List<Event>> GetEventsWithDetails()
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .ToListAsync();
    }

    public async Task<Event?> GetEventWithDetails(Guid id)
    {
        return await _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ProfilePicture)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .Include(e => e.AtprotoRecord)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<List<Event>> GetMyEventsWithDetails(string userId)
    {
        Guid userGuid;
        bool isGuid = Guid.TryParse(userId, out userGuid);

        var query = _dbContext.Events
            .Include(e => e.EventType)
            .Include(e => e.AudienceGender)
            .Include(e => e.AudienceAge)
            .Include(e => e.Actor)
                .ThenInclude(a => a.ActorType)
            .Include(e => e.FeaturedImage)
            .Include(e => e.EventStatus)
            .Include(e => e.VisibilityType)
            .Include(e => e.EventFormat)
            .Include(e => e.Madhab)
            .AsQueryable();

        if (isGuid)
        {
            query = query.Where(e =>
                _dbContext.Users.Any(u => u.Id == userGuid && u.ActorId == e.ActorId) ||
                _dbContext.OrganizationMembers.Any(om =>
                    om.UserId == userGuid &&
                    _dbContext.Organizations.Any(o => o.Id == om.OrganizationId && o.ActorId == e.ActorId)));
        }

        return await query.ToListAsync();
    }
}
```

**Handler Example - Repository returns ENTITIES → AutoMapper → DTOs:**

**Real Example from Explore.Application/Features/Events/Handlers/Queries/GetEventListRequestHandler.cs:**

```csharp
namespace Explore.Application.Features.Events.Handlers.Queries;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using Explore.Application.Contracts.Persistence;
using Explore.Application.DTOs.Event;
using Explore.Application.Features.Events.Requests.Queries;
using MediatR;

public class GetEventListRequestHandler : IRequestHandler<GetEventListRequest, List<EventListDto>>
{
    private readonly IEventRepository _eventRepository;
    private readonly IMapper _mapper;

    public GetEventListRequestHandler(IEventRepository eventRepository, IMapper mapper)
    {
        _eventRepository = eventRepository;
        _mapper = mapper;
    }

    public async Task<List<EventListDto>> Handle(GetEventListRequest request, CancellationToken cancellationToken)
    {
        // Repository returns ENTITIES
        var events = await _eventRepository.GetEventsWithDetails();

        // AutoMapper maps ENTITIES to DTOs
        return _mapper.Map<List<EventListDto>>(events);
    }
}
```

### Querying with Include

```csharp
// Multiple levels
var events = await _dbContext.Events
    .Include(e => e.EventType)
    .Include(e => e.AudienceGender)
    .Include(e => e.AudienceAge)
    .Include(e => e.Actor)
    .Include(e => e.EventSessions)
        .ThenInclude(s => s.Location)
    .ToListAsync();

// Filtered Include (EF Core 5+)
var events = await _dbContext.Organizations
    .Include(o => o.Members.Where(m => m.IsActive))
    .ToListAsync();
```

## ✅ Do's

- ✅ **DO** use `IEntityTypeConfiguration<T>` for entity configuration
- ✅ **DO** use `ApplyConfigurationsFromAssembly` in DbContext
- ✅ **DO** use repository pattern (interfaces in Application, implementations in Persistence)
- ✅ **DO** project to DTOs with `Select` for queries
- ✅ **DO** use `Include` for eager loading related entities
- ✅ **DO** use `AsNoTracking()` for read-only queries
- ✅ **DO** use `FindAsync` for lookups by primary key
- ✅ **DO** override `SaveChangesAsync` for cross-cutting concerns
- ✅ **DO** use PostgreSQL-specific features (`HasDefaultValueSql`, `uuidv7()`)
- ✅ **DO** use migrations for schema changes

## ❌ Don'ts

- ❌ **DON'T** use DbContext directly in Application layer (use repositories)
- ❌ **DON'T** configure entities in OnModelCreating (use IEntityTypeConfiguration)
- ❌ **DON'T** return DTOs from repositories (return entities only)
- ❌ **DON'T** load entire entities when you only need specific fields
- ❌ **DON'T** use lazy loading (explicit Include or Select instead)
- ❌ **DON'T** track entities for read-only queries (use AsNoTracking)
- ❌ **DON'T** use `ToList()` before filtering (use IQueryable)
- ❌ **DON'T** call SaveChanges multiple times in a loop (use transactions)
- ❌ **DON'T** ignore navigation property configuration

## 🎨 Common Patterns

### Create Entity

```csharp
public async Task<Event> Create(Event entity)
{
    await _dbContext.AddAsync(entity);
    await _dbContext.SaveChangesAsync();
    return entity;
}
```

### Update Entity

```csharp
public async Task Update(Event entity)
{
    _dbContext.Entry(entity).State = EntityState.Modified;
    await _dbContext.SaveChangesAsync();
}
```

### Delete Entity

```csharp
public async Task Delete(Event entity)
{
    _dbContext.Set<Event>().Remove(entity);
    await _dbContext.SaveChangesAsync();
}
```

### Query with Projection

```csharp
public async Task<List<EventListDto>> GetEventsWithDetails()
{
    return await _dbContext.Events
        .Include(e => e.Organization)
        .Select(e => new EventListDto
        {
            Id = e.Id,
            Title = e.Title,
            OrganizationName = e.Organization.FullName  // ✅ No N+1 query
        })
        .ToListAsync();
}
```

## 🔧 PostgreSQL-Specific Features

### UUIDv7 Primary Keys

```csharp
builder.Property(e => e.Id).HasDefaultValueSql("uuidv7()");
```

### PostGIS for Spatial Data

```csharp
builder.Property(e => e.Location)
       .HasColumnType("geography(point)");
```


## 📖 Deep Dive

For comprehensive guidance:
- **DbContext Patterns**: [dbcontext-patterns.md](resources/dbcontext-patterns.md)
- **Entity Configuration**: [entity-configuration.md](resources/entity-configuration.md)
- **Repository Pattern**: [repository-pattern.md](resources/repository-pattern.md)
- **Querying Patterns**: [querying-patterns.md](resources/querying-patterns.md)
- **Migrations**: [migrations.md](resources/migrations.md)

---

**Related Skills**:
- `clean-architecture-rules` - Ensures repositories are in Persistence layer
- `cqrs-mediatr-guidelines` - Handlers use repositories for data access
- `backend-dev-guidelines` - Overall backend architecture

**Enforcement Level**: 💡 SUGGEST (Provides guidance, doesn't block)
