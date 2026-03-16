# Plan: Multi-Database Support (PostgreSQL & MariaDB)

Last Updated: 2026-03-10

## Executive Summary
Transition the ISLAMU Event platform from a hardcoded PostgreSQL dependency to a multi-provider architecture. This enables self-hosters to choose between PostgreSQL (primary) and MariaDB (secondary) using either a dedicated database or a shared database namespace via PostgreSQL schemas or MariaDB table prefixes. This move increases platform accessibility and reduces infrastructure costs for users with existing database clusters.

## Current State Analysis
- **Persistence Layer**: `Explore.Persistence` project contains `ExploreDbContext` and all configurations.
- **Provider**: Hardcoded to `Npgsql` in `PersistenceServicesRegistration.cs` and `ExploreDbContextFactory.cs`.
- **Migrations**: Single set of PostgreSQL migrations in `Explore.Persistence/Migrations`.
- **Configuration**: Fetched via Infisical or standard `ConnectionStrings:DefaultConnection`.
- **Naming**: Using `UseSnakeCaseNamingConvention()`.

## Proposed Future State
- **Abstraction**: Unified `DatabaseOptions` configuration object.
- **Providers**: `Npgsql.EntityFrameworkCore.PostgreSQL` for Postgres and `Pomelo.EntityFrameworkCore.MySql` for MariaDB.
- **Isolation**: Supports PostgreSQL schemas and MariaDB table prefixes for shared DB scenarios.
- **Migrations**: Separate migration assemblies: `Explore.Persistence.Migrations.Postgres` and `Explore.Persistence.Migrations.MariaDb`.
- **Installation**: Environment-variable-driven setup with fail-fast validation.

## Implementation Phases

### Phase 1: Persistence Composition Root (Infrastructure Layer)
1. **Define `DatabaseOptions`**: Create a configuration object to manage provider, connection, schema, and prefix.
2. **Refactor Service Registration**: Move provider selection into `PersistenceServicesRegistration.cs` and handle both runtime and design-time factory.
3. **Handle Table Prefixing**: Add logic in `ExploreDbContext.OnModelCreating` to apply prefixes to all tables and the migrations history table.

### Phase 2: Migration Projects & Multi-Provider Sets
1. **Postgres Migration Project**: Move existing migrations to a new assembly `Explore.Persistence.Migrations.Postgres`.
2. **MariaDB Migration Project**: Create `Explore.Persistence.Migrations.MariaDb` and generate the initial migration set.
3. **Design-Time Selection**: Update `ExploreDbContextFactory` to use `DatabaseOptions` for provider-specific migration generation.

### Phase 3: Application Startup & Validation (API Layer)
1. **Startup Validation**: Add a `ValidateDatabaseConfig` step in `Program.cs` to check connectivity and provider requirements.
2. **Environment Variable Integration**: Ensure `Database__Provider`, `Database__Schema`, etc., are mapped correctly.

### Phase 4: CI/CD & Testing
1. **Integration Tests**: Update CI to run tests against both PostgreSQL and MariaDB.
2. **Migration Verification**: Add an automated test for full database schema application for both providers.

## Detailed Tasks

### Phase 1: Infrastructure Core
- [ ] Task 1.1: Create `DatabaseOptions` class in `Explore.Application.Contracts.Persistence` (or Persistence namespace).
- [ ] Task 1.2: Implement `IDbContextOptionsBuilder` helper to encapsulate provider selection.
- [ ] Task 1.3: Update `ExploreDbContext.OnModelCreating` to handle table prefixes and default schemas.

### Phase 2: Migration Assemblies
- [ ] Task 2.1: Move current Postgres migrations to `Explore.Persistence.Migrations.Postgres` project.
- [ ] Task 2.2: Create new `Explore.Persistence.Migrations.MariaDb` project.
- [ ] Task 2.3: Generate MariaDB migrations for current schema.

### Phase 3: API & Configuration
- [ ] Task 3.1: Map environment variables to `DatabaseOptions`.
- [ ] Task 3.2: Add health checks for DB connectivity on startup.
- [ ] Task 3.3: Update `Explore.AppHost` to support both database types for Aspire.

## Risk Assessment
- **Spatial Queries**: PostGIS vs MariaDB Spatial compatibility.
- **Migration Drift**: Keeping two sets of migrations in sync.
- **JSON Support**: Discrepancies in how providers handle JSON properties.

## Success Metrics
- Successful application of migrations for both Postgres and MariaDB.
- Integration tests passing on both providers in CI.
- Ability to share a database via table prefixes or PostgreSQL schemas.
