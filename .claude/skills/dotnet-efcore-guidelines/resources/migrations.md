# Migrations

Entity Framework Core migrations for schema management in ISLAMU Event.

---

## What are Migrations?

Migrations are version-controlled schema changes that allow you to evolve your database schema over time.

```
┌─────────────────────────────────────────────────────────────────────┐
│                       MIGRATION WORKFLOW                            │
├─────────────────────────────────────────────────────────────────────┤
│                                                                     │
│  1. Change Domain Model (Add/Remove/Modify entities)                │
│     ↓                                                               │
│  2. Create Migration (dotnet ef migrations add)                     │
│     ↓                                                               │
│  3. Review Generated SQL                                            │
│     ↓                                                               │
│  4. Apply Migration (dotnet ef database update)                     │
│     ↓                                                               │
│  5. Database Schema Updated                                         │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘
```

---

## Creating Migrations

### Add Migration

```bash
# Basic migration
dotnet ef migrations add AddEventEntity --project Explore.Persistence

# With specific context
dotnet ef migrations add AddEventEntity \
  --project Explore.Persistence \
  --context ExploreDbContext

# With output directory
dotnet ef migrations add AddEventEntity \
  --project Explore.Persistence \
  --output-dir Migrations
```

### Migration Naming Conventions

| Pattern | Example | Use Case |
|---------|---------|----------|
| `Add{Entity}` | `AddEvent` | New entity |
| `Add{Property}To{Entity}` | `AddDescriptionToEvent` | New property |
| `Remove{Property}From{Entity}` | `RemoveOldFieldFromEvent` | Remove property |
| `Update{Entity}{Property}` | `UpdateEventTitleMaxLength` | Modify property |
| `Add{Entity}{Relationship}` | `AddEventOrganizationRelationship` | New relationship |
| `Initial` | `InitialCreate` | First migration |

---

## Migration Structure

A migration consists of two methods:

```csharp
public partial class AddEventEntity : Migration
{
    // Applied when migrating up (forward)
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuidv7()"),
                Title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                StartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                TotalViews = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
            });
    }

    // Applied when rolling back (backward)
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Events");
    }
}
```

---

## Applying Migrations

### Update Database

```bash
# Apply all pending migrations
dotnet ef database update --project Explore.Persistence

# Apply to specific migration
dotnet ef database update AddEventEntity --project Explore.Persistence

# Rollback to previous migration
dotnet ef database update PreviousMigrationName --project Explore.Persistence

# Rollback all migrations
dotnet ef database update 0 --project Explore.Persistence
```

### Generate SQL Script

```bash
# Generate SQL for all migrations
dotnet ef migrations script \
  --project Explore.Persistence \
  --output migration.sql

# Generate SQL for specific migration
dotnet ef migrations script AddEventEntity \
  --project Explore.Persistence \
  --output add-event.sql

# Generate SQL from specific migration to another
dotnet ef migrations script Migration1 Migration2 \
  --project Explore.Persistence \
  --output migration-1-to-2.sql

# Idempotent script (can run multiple times)
dotnet ef migrations script \
  --project Explore.Persistence \
  --idempotent \
  --output migration.sql
```

---

## Custom Migration Code

### Add Custom SQL

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // Create table
    migrationBuilder.CreateTable(/* ... */);

    // ✅ Add custom SQL
    migrationBuilder.Sql(@"
        CREATE OR REPLACE FUNCTION uuidv7() RETURNS uuid AS $$
        DECLARE
            unix_ts_ms BIGINT;
            uuid_bytes BYTEA;
        BEGIN
            unix_ts_ms = (EXTRACT(EPOCH FROM CLOCK_TIMESTAMP()) * 1000)::BIGINT;
            uuid_bytes = E'\\x' ||
                LPAD(TO_HEX(unix_ts_ms), 12, '0') ||
                GEN_RANDOM_BYTES(10)::TEXT;
            RETURN uuid_bytes::UUID;
        END;
        $$ LANGUAGE plpgsql;
    ");
}
```

### Create Index

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Create index
    migrationBuilder.CreateIndex(
        name: "IX_Events_StartDate",
        table: "Events",
        column: "StartDate");

    // ✅ Create composite index
    migrationBuilder.CreateIndex(
        name: "IX_Events_OrganizationId_StartDate",
        table: "Events",
        columns: new[] { "OrganizationId", "StartDate" });

    // ✅ Create unique index
    migrationBuilder.CreateIndex(
        name: "IX_Users_Email",
        table: "Users",
        column: "Email",
        unique: true);
}
```

### Add Column

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Add nullable column (safe)
    migrationBuilder.AddColumn<string>(
        name: "Subtitle",
        table: "Events",
        type: "character varying(200)",
        maxLength: 200,
        nullable: true);

    // ⚠️ Add non-nullable column (requires default)
    migrationBuilder.AddColumn<int>(
        name: "Priority",
        table: "Events",
        type: "integer",
        nullable: false,
        defaultValue: 0);
}
```

### Modify Column

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Change column type
    migrationBuilder.AlterColumn<string>(
        name: "Title",
        table: "Events",
        type: "character varying(500)",  // Changed from 200
        maxLength: 500,
        nullable: false,
        oldClrType: typeof(string),
        oldType: "character varying(200)",
        oldMaxLength: 200);
}
```

### Rename Column/Table

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Rename column
    migrationBuilder.RenameColumn(
        name: "OldName",
        table: "Events",
        newName: "NewName");

    // ✅ Rename table
    migrationBuilder.RenameTable(
        name: "OldTableName",
        newName: "NewTableName");
}
```

---

## Data Migrations

### Seed Data

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Insert seed data
    migrationBuilder.InsertData(
        table: "AudienceAges",
        columns: new[] { "Id", "FullName", "MinAge", "MaxAge" },
        values: new object[,]
        {
            { 1, "Children", 0, 12 },
            { 2, "Youth", 13, 17 },
            { 3, "Adults", 18, 64 },
            { 4, "Seniors", 65, 150 }
        });
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    // ✅ Remove seed data
    migrationBuilder.DeleteData(
        table: "AudienceAges",
        keyColumn: "Id",
        keyValues: new object[] { 1, 2, 3, 4 });
}
```

### Data Transformation

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Transform existing data
    migrationBuilder.Sql(@"
        UPDATE ""Events""
        SET ""Title"" = UPPER(""Title"")
        WHERE ""Title"" IS NOT NULL;
    ");
}
```

---

## PostgreSQL-Specific Migrations

### PostGIS Extension

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Enable PostGIS extension
    migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");

    migrationBuilder.AddColumn<NetTopologySuite.Geometries.Point>(
        name: "Location",
        table: "Events",
        type: "geography(point)",
        nullable: true);
}
```

### JSONB Column

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Add JSONB column
    migrationBuilder.AddColumn<string>(
        name: "Metadata",
        table: "Events",
        type: "jsonb",
        nullable: true);

    // ✅ Create GIN index for JSONB
    migrationBuilder.Sql(@"
        CREATE INDEX IX_Events_Metadata
        ON ""Events"" USING GIN (""Metadata"");
    ");
}
```

### Array Column

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    // ✅ Add array column
    migrationBuilder.AddColumn<string[]>(
        name: "Tags",
        table: "Events",
        type: "text[]",
        nullable: true);
}
```

---

## Migration Management

### List Migrations

```bash
# List all migrations
dotnet ef migrations list --project Explore.Persistence
```

### Remove Last Migration

```bash
# ⚠️ ONLY if not applied to database
dotnet ef migrations remove --project Explore.Persistence

# ❌ DON'T remove if already applied - create a new migration instead
```

### Check Pending Migrations

```csharp
// In code
var pendingMigrations = await _dbContext.Database.GetPendingMigrationsAsync();
if (pendingMigrations.Any())
{
    // Migrations need to be applied
    Console.WriteLine($"Pending migrations: {string.Join(", ", pendingMigrations)}");
}
```

### Apply Migrations Programmatically

```csharp
// Program.cs - Apply migrations on startup
using var scope = app.Services.CreateScope();
var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();

// ✅ Apply pending migrations
await dbContext.Database.MigrateAsync();
```

---

## Best Practices

| Practice | Reason |
|----------|--------|
| ✅ Review generated SQL before applying | Catch issues early |
| ✅ Use descriptive migration names | Easy to understand history |
| ✅ Add indexes for foreign keys | Query performance |
| ✅ Make nullable columns when adding to existing tables | Avoid data loss |
| ✅ Use transactions for data migrations | Data integrity |
| ✅ Test migrations on copy of production data | Validate before production |
| ✅ Keep migrations small and focused | Easier to review and rollback |
| ✅ Generate idempotent SQL scripts for production | Safe deployment |
| ❌ Don't remove migrations after applied | Breaks history |
| ❌ Don't modify migrations after applied | Use new migration |
| ❌ Don't include sensitive data in migrations | Security risk |

---

## Troubleshooting

### Migration Already Applied

```bash
# Error: "The migration '20240101120000_MigrationName' has already been applied to the database."

# ✅ Solution: Create a new migration for changes
dotnet ef migrations add UpdateMigration --project Explore.Persistence
```

### Conflicting Migrations

```bash
# Error: "Your migrations are out of sync"

# ✅ Solution 1: Pull latest and rebase
git pull origin main
dotnet ef migrations remove  # If not yet applied

# ✅ Solution 2: Create new migration
dotnet ef migrations add ResolveMergeConflict --project Explore.Persistence
```

### Connection String Not Found

```bash
# Error: "No connection string named 'DefaultConnection' was found"

# ✅ Solution: Specify connection string
dotnet ef database update \
  --project Explore.Persistence \
  --connection "Host=localhost;Database=islamu_event;Username=postgres;Password=password"
```

---

## Production Deployment

### Generate SQL Script for DBA

```bash
# ✅ Generate idempotent SQL script
dotnet ef migrations script \
  --project Explore.Persistence \
  --idempotent \
  --output migrations/release-1.0.0.sql
```

### Manual vs Automatic

**Manual (Recommended for Production)**:
```bash
# 1. Generate SQL script
dotnet ef migrations script --idempotent --output release.sql

# 2. Review script with DBA
# 3. DBA applies script to production database
```

**Automatic (Development Only)**:
```csharp
// Program.cs
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ExploreDbContext>();
    await dbContext.Database.MigrateAsync();  // ✅ Auto-apply in dev only
}
```

---

**Related Resources**:
- [dbcontext-patterns.md](dbcontext-patterns.md) - DbContext configuration
- [entity-configuration.md](entity-configuration.md) - Entity configuration
- [repository-pattern.md](repository-pattern.md) - Repository implementations
