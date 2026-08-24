<!-- ABOUTME: Domain journal for persistence, EF Core, database migrations, and schema patterns. -->
<!-- ABOUTME: Captures durable findings, pitfalls, and decisions in Explore.Persistence and database stores. -->

# Persistence & Database Knowledge Ledger

> **Scope**: `Explore.Persistence`, EF Core configurations, migrations, query filters, PostgreSQL, SQLite, and Quartz schema.

---

## 1. Architectural Decisions

- **Clean-room adoption of third-party ADO schemas**: Treat external database schemas as interoperability interface facts rather than copying third-party SQL/DDL. Author DDL independently with project-native guards, parameterized tokens (e.g. `{prefix}`), and non-destructive operations safe for idempotent startup execution.
- **Auditing and Soft Delete**: Auditable entities use `CreatedAt/By` and `UpdatedAt/By`. Soft-deletable entities use `IsDeleted` and the global query filter `SoftDelete`.
- **Numeric Version Concurrency on PostgreSQL**: Use an application-managed integer `Version` token (or UUIDv7 concurrency stamp) for optimistic concurrency on PostgreSQL rather than SQL Server-style `byte[] RowVersion`.

---

## 2. Technical Insights & Patterns

- **EF Core tracking optimization**: Repositories return entities; projection and DTO mapping happen in handlers. Use `AsNoTracking()` on read-only queries.
- **DTO EventId propagation requires repository include paths**: When mapping child entities (e.g., registrations, session agenda items) for HAL or authorization, AutoMapper expressions like `src.EventSession.EventId` require repositories to explicitly include parent entities on all read paths.
- **EF migrations in dirty working trees**: `dotnet ef migrations add` snapshots the entire current model state. In dirty trees with concurrent model changes, inspect generated migration files and prune unrelated operations before applying.
- **EAV Projection Updaters**: EAV projection metrics extend `Explore.Projections` counters (`explore.projections.inline_updates_total`, `dirty_scope_skips_total`) with bounded labels (`tenant_id`, `projection_type`, `operation`). Never emit dynamic property namespaces, definition IDs, or session IDs as metrics tags.

---

## 3. Failed Approaches & Lessons

- **Hand-editing EF Migrations**: Banned across the repository. If a migration is wrong, fix the entity configuration or seeder, delete the unapplied development migration, and regenerate it with `dotnet ef migrations`.
- **Destructive Statements in Startup DDL**: Adding `DROP` or `TRUNCATE` statements to startup schema initializers breaks live environments. All startup initializers must use `CREATE TABLE IF NOT EXISTS` or idempotent metadata checks.
