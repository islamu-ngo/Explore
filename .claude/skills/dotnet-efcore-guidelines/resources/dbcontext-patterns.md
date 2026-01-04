# DbContext Patterns

DbContext configuration and best practices for ISLAMU Event.

---

## DbContext Structure

The DbContext is the central class for Entity Framework Core data access.

### Basic DbContext

```csharp
using Microsoft.EntityFrameworkCore;
using Explore.Domain;

namespace Explore.Persistence;

public class ExploreDbContext : DbContext
{
    public ExploreDbContext(DbContextOptions<ExploreDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all IEntityTypeConfiguration<T> from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);
    }

    // DbSet properties for each entity
    public DbSet<Event> Events { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<User> Users { get; set; }
}
```

---

## DbSet Properties

DbSets represent tables in the database.

```csharp
public class ExploreDbContext : DbContext
{
    // ✅ Public DbSet properties
    public DbSet<Event> Events { get; set; }
    public DbSet<Organization> Organizations { get; set; }
    public DbSet<EventRegistration> EventRegistrations { get; set; }

    // ⚠️ Nullable for optional entities
    public DbSet<EventCategories>? ProgramCategories { get; set; }

    // ✅ Non-nullable for required entities (compiler nullability)
    public DbSet<User> Users { get; set; } = null!;
}
```

**Naming Convention**:
- Plural form: `Events`, `Organizations`, `Users`
- PascalCase
- Descriptive names matching domain language

---

## OnModelCreating - Entity Configuration

### Apply Configurations from Assembly

**✅ Recommended** (ISLAMU Event pattern):

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ✅ Automatically applies all IEntityTypeConfiguration<T> in assembly
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExploreDbContext).Assembly);
}
```

### Manual Configuration Application

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // ❌ Manual (not recommended - use ApplyConfigurationsFromAssembly)
    modelBuilder.ApplyConfiguration(new EventConfiguration());
    modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
    // ... many more
}
```

**Why ApplyConfigurationsFromAssembly?**
- ✅ Automatic discovery of all configurations
- ✅ No need to manually register each configuration
- ✅ Easier to maintain as new entities are added

---

## SaveChangesAsync Override

Override `SaveChangesAsync` for cross-cutting concerns.

### Audit Logging Pattern

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.State == EntityState.Added)
        {
            // Log entity creation
            Console.WriteLine($"Creating {entry.Entity.GetType().Name}");
        }
        else if (entry.State == EntityState.Modified)
        {
            // Log entity update
            Console.WriteLine($"Updating {entry.Entity.GetType().Name}");
        }
        else if (entry.State == EntityState.Deleted)
        {
            // Log entity deletion
            Console.WriteLine($"Deleting {entry.Entity.GetType().Name}");
        }
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

### Automatic Timestamps

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entries = ChangeTracker.Entries()
        .Where(e => e.Entity is BaseEntity &&
                    (e.State == EntityState.Added || e.State == EntityState.Modified));

    foreach (var entry in entries)
    {
        var entity = (BaseEntity)entry.Entity;

        if (entry.State == EntityState.Added)
        {
            entity.CreatedAt = DateTime.UtcNow;
        }

        entity.UpdatedAt = DateTime.UtcNow;
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

### Soft Delete Pattern

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var deletedEntries = ChangeTracker.Entries()
        .Where(e => e.Entity is ISoftDeletable && e.State == EntityState.Deleted);

    foreach (var entry in deletedEntries)
    {
        // Convert Delete to Update with IsDeleted flag
        entry.State = EntityState.Modified;
        ((ISoftDeletable)entry.Entity).IsDeleted = true;
        ((ISoftDeletable)entry.Entity).DeletedAt = DateTime.UtcNow;
    }

    return base.SaveChangesAsync(cancellationToken);
}
```

---

## DbContext Configuration (Program.cs)

### PostgreSQL Configuration

```csharp
// Program.cs
using Npgsql.EntityFrameworkCore.PostgreSQL;

builder.Services.AddDbContext<ExploreDbContext>(options =>
{
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions =>
        {
            // Enable retry on failure
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorCodesToAdd: null);

            // Use NetTopologySuite for PostGIS spatial data
            npgsqlOptions.UseNetTopologySuite();

            // Set command timeout
            npgsqlOptions.CommandTimeout(30);
        });

    // Enable sensitive data logging in development
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

### Connection String

**appsettings.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=islamu_event;Username=postgres;Password=yourpassword;Include Error Detail=true"
  }
}
```

**appsettings.Development.json**:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=islamu_event_dev;Username=postgres;Password=dev;Include Error Detail=true"
  }
}
```

---

## DbContext Lifetime and Scoping

### Scoped Lifetime (Default - Recommended)

```csharp
// ✅ Scoped (one instance per HTTP request)
builder.Services.AddDbContext<ExploreDbContext>(options =>
    options.UseNpgsql(connectionString));
```

**Why Scoped?**
- ✅ One DbContext instance per HTTP request
- ✅ Proper change tracking
- ✅ Automatic disposal
- ✅ Thread-safe within a request

### Transient Lifetime (Not Recommended)

```csharp
// ❌ DON'T use Transient for DbContext
builder.Services.AddDbContext<ExploreDbContext>(
    options => options.UseNpgsql(connectionString),
    ServiceLifetime.Transient);
```

**Why Not Transient?**
- ❌ Multiple instances per request
- ❌ Change tracking issues
- ❌ Performance overhead

### Singleton Lifetime (Never!)

```csharp
// ❌ NEVER use Singleton for DbContext
builder.Services.AddDbContext<ExploreDbContext>(
    options => options.UseNpgsql(connectionString),
    ServiceLifetime.Singleton);
```

**Why Never Singleton?**
- ❌ Not thread-safe
- ❌ Memory leaks
- ❌ Stale data
- ❌ Connection pool exhaustion

---

## Change Tracker

The ChangeTracker tracks entity state changes.

### Accessing Tracked Entities

```csharp
public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // Get all tracked entities
    var entries = ChangeTracker.Entries();

    // Filter by state
    var addedEntities = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Added);

    var modifiedEntities = ChangeTracker.Entries()
        .Where(e => e.State == EntityState.Modified);

    // Filter by type
    var events = ChangeTracker.Entries<Event>();

    return base.SaveChangesAsync(cancellationToken);
}
```

### Entity States

| State | Description |
|-------|-------------|
| `Detached` | Entity not tracked by DbContext |
| `Unchanged` | Entity tracked, no changes |
| `Added` | Entity will be inserted |
| `Modified` | Entity will be updated |
| `Deleted` | Entity will be deleted |

### Modifying Entity State

```csharp
// Attach existing entity and mark as modified
var existingEvent = new Event { Id = eventId };
_dbContext.Attach(existingEvent);
_dbContext.Entry(existingEvent).State = EntityState.Modified;

// Mark specific properties as modified
_dbContext.Entry(existingEvent).Property(e => e.Title).IsModified = true;
_dbContext.Entry(existingEvent).Property(e => e.Description).IsModified = true;

await _dbContext.SaveChangesAsync();
```

---

## Query Filters (Global Filters)

Global filters apply to all queries.

### Soft Delete Filter

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply soft delete filter to all ISoftDeletable entities
    modelBuilder.Entity<Event>()
        .HasQueryFilter(e => !e.IsDeleted);

    modelBuilder.Entity<Organization>()
        .HasQueryFilter(o => !o.IsDeleted);
}
```

### Multi-Tenancy Filter

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);

    // Apply tenant filter (requires current tenant from DI)
    modelBuilder.Entity<Event>()
        .HasQueryFilter(e => e.TenantId == _currentTenantId);
}
```

### Ignoring Query Filters

```csharp
// Include soft-deleted entities
var allEvents = await _dbContext.Events
    .IgnoreQueryFilters()
    .ToListAsync();
```

---

## Performance Optimization

### Disable Change Tracking for Read-Only Queries

```csharp
// ✅ No tracking (faster for read-only)
var events = await _dbContext.Events
    .AsNoTracking()
    .ToListAsync();

// ❌ With tracking (unnecessary for read-only)
var events = await _dbContext.Events.ToListAsync();
```

### Split Queries for Large Includes

```csharp
// ✅ Split into multiple queries (avoids cartesian explosion)
var organizations = await _dbContext.Organizations
    .Include(o => o.Events)
    .Include(o => o.Members)
    .AsSplitQuery()
    .ToListAsync();

// ❌ Single query (can cause performance issues with multiple includes)
var organizations = await _dbContext.Organizations
    .Include(o => o.Events)
    .Include(o => o.Members)
    .ToListAsync();
```

---

## Best Practices

| Practice | Reason |
|----------|--------|
| ✅ Use `ApplyConfigurationsFromAssembly` | Automatic configuration discovery |
| ✅ Override `SaveChangesAsync` for audit logging | Centralized cross-cutting concerns |
| ✅ Use scoped lifetime for DbContext | One instance per request |
| ✅ Use `AsNoTracking()` for read-only queries | Improved performance |
| ✅ Enable detailed errors in development | Better debugging |
| ✅ Use connection pooling | Reuse connections |
| ❌ Don't use DbContext directly in Application layer | Use repositories |
| ❌ Don't use singleton DbContext | Not thread-safe |
| ❌ Don't track entities for read-only queries | Unnecessary overhead |

---

**Related Resources**:
- [entity-configuration.md](entity-configuration.md) - IEntityTypeConfiguration patterns
- [repository-pattern.md](repository-pattern.md) - Repository implementations
- [querying-patterns.md](querying-patterns.md) - Query optimization
