# Tasks: Multi-Database Support (PostgreSQL & MariaDB)

Last Updated: 2026-03-10

## Phase 1: Infrastructure Core (S)
- [ ] Task 1.1: Create `DatabaseOptions.cs` in `Explore.Persistence/Settings/`
  - [ ] Acceptance: Includes Provider, ConnectionString, Schema, TablePrefix, ServerVersion.
- [ ] Task 1.2: Refactor `PersistenceServicesRegistration.cs` for dynamic provider registration.
  - [ ] Acceptance: Swaps between `UseNpgsql` and `UseMySql` based on `DatabaseOptions`.
  - [ ] Acceptance: Configures `MigrationsAssembly` dynamically.
- [ ] Task 1.3: Update `ExploreDbContext.cs` `OnModelCreating` for prefix/schema application.
  - [ ] Acceptance: Table names prefixed if `TablePrefix` is set.
  - [ ] Acceptance: `HasDefaultSchema` called if `Schema` is set (Postgres only).
  - [ ] Acceptance: Migrations history table configured with same schema/prefix.

## Phase 2: Migration Management (M)
- [ ] Task 2.1: Extract Postgres migrations to a new assembly `Explore.Persistence.Migrations.Postgres`.
- [ ] Task 2.2: Create `Explore.Persistence.Migrations.MariaDb` project and reference `Explore.Persistence`.
- [ ] Task 2.3: Generate initial MariaDB migration set using `dotnet ef migrations add Initial`.
- [ ] Task 2.4: Update `ExploreDbContextFactory.cs` to handle multi-provider generation.

## Phase 3: Runtime Configuration & Fail-Fast (S)
- [ ] Task 3.1: Add `DatabaseOptions` validation logic (e.g., check for ServerVersion if MariaDB).
- [ ] Task 3.2: Configure `Program.cs` to load `DatabaseOptions` from Environment Variables.
- [ ] Task 3.3: Implement startup check for database connectivity.

## Phase 4: Verification & Tests (M)
- [ ] Task 4.1: Update unit tests for `PersistenceServicesRegistration` to verify correct provider registration.
- [ ] Task 4.2: Add integration tests for MariaDB in CI (Docker-based).
- [ ] Task 4.3: Manual verification of "Shared Database Mode" (table prefixes).

## Legend
- S: Small (1-2 days)
- M: Medium (3-5 days)
- L: Large (1-2 weeks)
- XL: Extra Large (2+ weeks)
