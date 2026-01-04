# Entity Configuration

Entity configuration using `IEntityTypeConfiguration<T>` for ISLAMU Event.

---

## IEntityTypeConfiguration Pattern

Separate configuration classes for each entity.

### Basic Configuration

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Explore.Domain;

namespace Explore.Persistence.Configurations.Entities;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // Table name
        builder.ToTable("Events");

        // Primary key
        builder.HasKey(e => e.Id);

        // Properties
        builder.Property(e => e.Title)
               .IsRequired()
               .HasMaxLength(200);

        builder.Property(e => e.Description)
               .HasMaxLength(2000);

        // Relationships
        builder.HasOne(e => e.Organization)
               .WithMany(o => o.Events)
               .HasForeignKey(e => e.OrganizationId);
    }
}
```

---

## ISLAMU Event Patterns

### TPT (Table Per Type) Strategy

```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // ✅ TPT Strategy (project standard)
        builder.UseTptMappingStrategy();

        // Each derived type gets its own table
        // Base Event table + derived tables (Conference, Workshop, etc.)
    }
}
```

**Why TPT?**
- ✅ Normalized data (no discriminator column)
- ✅ No null columns for unused properties
- ✅ Clean separation of types

**Alternatives**:
- `UseTphMappingStrategy()` - Table Per Hierarchy (single table with discriminator)
- `UseTpcMappingStrategy()` - Table Per Concrete (one table per concrete type)

### PostgreSQL UUIDv7 Primary Keys

```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // ✅ PostgreSQL function for UUIDv7
        builder.Property(e => e.Id)
               .HasDefaultValueSql("uuidv7()");
    }
}
```

**Why UUIDv7?**
- ✅ Time-ordered (better index performance than random UUIDs)
- ✅ Globally unique
- ✅ No identity column contention

### Default Values

```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // Default value in database
        builder.Property(e => e.TotalViews)
               .HasDefaultValue(0);

        // Default value using SQL
        builder.Property(e => e.CreatedAt)
               .HasDefaultValueSql("NOW()");

        // Computed column
        builder.Property(e => e.FullAddress)
               .HasComputedColumnSql("[Address] + ', ' + [City]", stored: false);
    }
}
```

---

## Property Configuration

### Required Properties

```csharp
builder.Property(e => e.Title)
       .IsRequired()
       .HasMaxLength(200);

builder.Property(e => e.Email)
       .IsRequired()
       .HasMaxLength(255);
```

### Optional Properties

```csharp
// Nullable reference type (C# 10+)
builder.Property(e => e.Description)
       .HasMaxLength(2000);  // Optional - no IsRequired()

// Nullable value type
builder.Property(e => e.EndDate)
       .HasColumnType("timestamp");  // DateTime? is automatically optional
```

### Max Length

```csharp
builder.Property(e => e.Title).HasMaxLength(200);
builder.Property(e => e.ShortDescription).HasMaxLength(500);
builder.Property(e => e.Description).HasMaxLength(2000);
builder.Property(e => e.Email).HasMaxLength(255);
```

### Column Types (PostgreSQL)

```csharp
// Text
builder.Property(e => e.Description)
       .HasColumnType("text");

// Timestamp
builder.Property(e => e.CreatedAt)
       .HasColumnType("timestamp with time zone");

// JSON
builder.Property(e => e.Metadata)
       .HasColumnType("jsonb");

// Decimal with precision
builder.Property(e => e.Price)
       .HasColumnType("decimal(18,2)");

// Geography (PostGIS)
builder.Property(e => e.Location)
       .HasColumnType("geography(point)");
```

---

## Relationships

### One-to-Many

```csharp
// Organization has many Events
public class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> builder)
    {
        builder.HasMany(o => o.Events)
               .WithOne(e => e.Organization)
               .HasForeignKey(e => e.OrganizationId)
               .OnDelete(DeleteBehavior.Cascade);  // Delete events when organization deleted
    }
}
```

### Many-to-One

```csharp
// Event belongs to one Organization
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.HasOne(e => e.Organization)
               .WithMany(o => o.Events)
               .HasForeignKey(e => e.OrganizationId)
               .IsRequired();  // Every event must have an organization
    }
}
```

### Many-to-Many

```csharp
// Event <-> Category (through EventCategories)
public class EventCategoriesConfiguration : IEntityTypeConfiguration<EventCategories>
{
    public void Configure(EntityTypeBuilder<EventCategories> builder)
    {
        // Composite primary key
        builder.HasKey(ec => new { ec.EventId, ec.CategoryId });

        // Event side
        builder.HasOne(ec => ec.Event)
               .WithMany(e => e.EventCategories)
               .HasForeignKey(ec => ec.EventId);

        // Category side
        builder.HasOne(ec => ec.Category)
               .WithMany(c => c.EventCategories)
               .HasForeignKey(ec => ec.CategoryId);
    }
}
```

### One-to-One

```csharp
// User has one Profile
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(u => u.Profile)
               .WithOne(p => p.User)
               .HasForeignKey<UserProfile>(p => p.UserId);
    }
}
```

---

## Delete Behavior

```csharp
builder.HasOne(e => e.Organization)
       .WithMany(o => o.Events)
       .HasForeignKey(e => e.OrganizationId)
       .OnDelete(DeleteBehavior.Cascade);  // Delete events when org deleted
```

| Behavior | Description |
|----------|-------------|
| `Cascade` | Delete dependents when principal deleted |
| `Restrict` | Prevent delete if dependents exist |
| `SetNull` | Set foreign key to null when principal deleted |
| `NoAction` | No action (database handles it) |

---

## Indexes

### Simple Index

```csharp
builder.HasIndex(e => e.StartDate);
builder.HasIndex(e => e.OrganizationId);
```

### Composite Index

```csharp
builder.HasIndex(e => new { e.OrganizationId, e.StartDate });
```

### Unique Index

```csharp
builder.HasIndex(e => e.Email).IsUnique();
```

### Named Index

```csharp
builder.HasIndex(e => e.Title)
       .HasDatabaseName("IX_Events_Title");
```

### Filtered Index (PostgreSQL)

```csharp
builder.HasIndex(e => e.Email)
       .IsUnique()
       .HasFilter("[Email] IS NOT NULL");
```

---

## Value Conversions

### Enum to String

```csharp
builder.Property(e => e.Status)
       .HasConversion<string>();  // Store enum as string in database
```

### JSON Column

```csharp
builder.Property(e => e.Metadata)
       .HasConversion(
           v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
           v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions)null!)!)
       .HasColumnType("jsonb");
```

### Custom Value Converter

```csharp
var converter = new ValueConverter<List<string>, string>(
    v => string.Join(',', v),
    v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList());

builder.Property(e => e.Tags)
       .HasConversion(converter);
```

---

## Owned Types

For value objects that don't have their own identity.

```csharp
public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        // Address is an owned type
        builder.OwnsOne(e => e.Address, address =>
        {
            address.Property(a => a.Street).HasMaxLength(200);
            address.Property(a => a.City).HasMaxLength(100);
            address.Property(a => a.Country).HasMaxLength(100);
        });
    }
}
```

---

## Table Splitting

Multiple entities mapped to same table.

```csharp
// Event and EventDetails share same table
builder.ToTable("Events");
builder.HasOne(e => e.Details)
       .WithOne()
       .HasForeignKey<EventDetails>(d => d.EventId);

// EventDetails configuration
builder.ToTable("Events");  // Same table as Event
```

---

## Temporal Tables (SQL Server)

```csharp
// Enable temporal table (SQL Server only)
builder.ToTable("Events", b => b.IsTemporal(
    temporal =>
    {
        temporal.HasPeriodStart("ValidFrom");
        temporal.HasPeriodEnd("ValidTo");
        temporal.UseHistoryTable("EventsHistory");
    }));
```

---

## Best Practices

| Practice | Reason |
|----------|--------|
| ✅ One configuration class per entity | Separation of concerns |
| ✅ Use `HasMaxLength` for strings | Database optimization |
| ✅ Specify `IsRequired` explicitly | Clear intent |
| ✅ Use `TPT` for inheritance (project standard) | Clean separation |
| ✅ Use PostgreSQL functions (`uuidv7()`) | Better performance |
| ✅ Configure indexes for foreign keys | Query performance |
| ✅ Use `DeleteBehavior.Cascade` carefully | Data integrity |
| ❌ Don't configure in OnModelCreating | Use IEntityTypeConfiguration |
| ❌ Don't use magic strings for column types | Use HasColumnType |

---

**Related Resources**:
- [dbcontext-patterns.md](dbcontext-patterns.md) - DbContext configuration
- [repository-pattern.md](repository-pattern.md) - Repository implementations
- [migrations.md](migrations.md) - Creating migrations
