# Context: Multi-Database Support (PostgreSQL & MariaDB)

Last Updated: 2026-03-10

## Key Decisions & Architecture
1. **Providers**:
   - PostgreSQL: `Npgsql.EntityFrameworkCore.PostgreSQL`
   - MariaDB: `Pomelo.EntityFrameworkCore.MySql`
2. **Configuration**:
   - Primary contract: Environment Variables (`Database__Provider`, `Database__ConnectionString`, etc.)
   - Optional: Infisical for secrets management.
3. **Multi-Tenancy Isolation**:
   - PostgreSQL: True schemas (inside a DB) or table prefixes.
   - MariaDB: Table prefixes (as schema/DB are synonyms).
4. **Migration Assemblies**:
   - `Explore.Persistence.Migrations.Postgres`
   - `Explore.Persistence.Migrations.MariaDb`
5. **Fail-Fast Policy**: Application will not start if database connectivity or version compatibility checks fail.

## Core Interface / Option Signatures

```csharp
public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string Provider { get; init; } = "Postgres"; // "Postgres" | "MariaDb"
    public string ConnectionString { get; init; } = "";
    public string? Schema { get; init; }                // Postgres only
    public string? TablePrefix { get; init; }           // Shared DB mode
    public string? ServerVersion { get; init; }         // MariaDB/MySQL only (e.g. "11.2.2-MariaDB")
}
```

## Key Files to Modify/Create
- `Explore.Persistence/ExploreDbContext.cs`: Prefix/Schema application logic.
- `Explore.Persistence/PersistenceServicesRegistration.cs`: Dynamic provider registration.
- `Explore.Persistence/ExploreDbContextFactory.cs`: Configuration-driven migration generation.
- `Explore.API/Program.cs`: Startup configuration and validation.

## External Dependencies
- `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Pomelo.EntityFrameworkCore.MySql`
- `EFCore.NamingConventions` (Current snake_case convention)
