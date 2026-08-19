<!-- ABOUTME: Context and session ledger for Multi-Database Persistence Unification and API-First Architecture. -->
<!-- ABOUTME: Tracks progress, decisions, constraints, key files, and handoffs for future implementation agents. -->

# Multi-Database Persistence Unification & API-First Architecture — Context

Last Updated: 2026-08-19 Europe/Brussels

## SESSION PROGRESS (2026-08-19 Europe/Brussels)

### ✅ COMPLETED
- Repository analysis completed: cataloged all instances of raw SQL, stored procedures, triggers, and project sprawl.
- Implementation plan created and synchronized under `dev/active/multi-database-persistence-unification/`.
- Architecture Option 2 (1 core persistence + 4 dedicated provider migration assemblies) selected.

### 🟡 IN PROGRESS
- Awaiting user review and execution signal for the implementation plan.

### ⏭️ NEXT
1. User reviews and approves the plan.
2. Implementation agent executes Phase 1: `ImmutableEntityInterceptor`, counter seeding, and unified `CoLocatedPrivacyErasureAuthorityRepository`.
3. Verify Phase 1 with Release build and `Event.Architecture.Tests`.

### ⚠️ BLOCKERS
- None.

---

## Quick Resume

1. Read this context and `multi-database-persistence-unification-tasks.md`.
2. Read only the current phase and decisions from `multi-database-persistence-unification-plan.md`.
3. Start from the highest-priority unchecked task in `multi-database-persistence-unification-tasks.md`.
4. Keep `multi-database-persistence-unification-tasks.md` current as the hot progress ledger.

---

## Key Files And Responsibilities

| Path | Status | Layer | Purpose | Notes |
|---|---|---|---|---|
| `src/Explore.Persistence/Interceptors/ImmutableEntityInterceptor.cs` | New | Persistence | EF Core SaveChangesInterceptor | Prevents mutation/deletion of `PrivacyErasureIntent` in C# |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPrivacyErasureAuthorityRepository.cs` | New | Persistence | Unified Privacy Erasure Authority | Uses `RelationalNamedLock` and pure LINQ across all 5 DB engines |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Configurations/PrivacyErasureCounterConfiguration.cs` | Existing | Persistence | Model Configuration | Adds `builder.HasData()` to seed the singleton counter row |
| `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs` | Existing | Persistence | Provider Composition | Routes all migration targets to 1 assembly per provider |
| `src/Explore.Persistence/Repositories/EventAgendaItemRepository.cs` | Existing | Persistence | Repository | Replaces raw SQL `UPDATE` with LINQ `ExecuteUpdateAsync` |
| `src/Explore.Persistence/Repositories/RegistrationInventoryRepository.cs` | Existing | Persistence | Repository | Replaces raw SQL `UPDATE` with LINQ `ExecuteUpdateAsync` |
| `src/Explore.Persistence/Repositories/RegistrationFinalizationRepository.cs` | Existing | Persistence | Repository | Replaces direct `pg_advisory_xact_lock` with `RelationalNamedLock` |
| `src/Explore.Persistence.Migrations.SqlServer/` | Existing | Migrations | SQL Server Migrations | Houses all SQL Server migrations (Application, DataProtection, Erasure) |
| `src/Explore.Persistence.Migrations.Sqlite/` | Existing | Migrations | SQLite Migrations | Houses all SQLite migrations (Application, DataProtection, Erasure) |
| `src/Explore.Persistence.Migrations.MySql/` | Existing | Migrations | MySQL Migrations | Houses all MySQL migrations (Application, DataProtection, Erasure) |
| `src/Explore.Persistence.Migrations.MariaDb/` | Existing | Migrations | MariaDB Migrations | Houses all MariaDB migrations (Application, DataProtection, Erasure) |
| `src/Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite/` | Delete | Migrations | Deprecated SQLite Erasure | Project deleted; replaced by primary schema |
| `src/Explore.Persistence.DataProtection.Migrations.*/` (4 projects) | Delete | Migrations | Deprecated DataProtection | Projects deleted; merged into provider migration assemblies |

---

## Key Decisions

1. **Option 2 Architecture**: Consolidates 10 persistence projects to 5 (1 core `Explore.Persistence` + 4 provider migration assemblies `.SqlServer`, `.Sqlite`, `.MySql`, `.MariaDb`).
2. **API-Side Immutability**: Uses EF Core `ImmutableEntityInterceptor` in C# to replace database triggers (`tr_erasure_intents_immutable`).
3. **Unified CoLocated Authority Repository**: Combines SQLite and PostgreSQL implementations into a single `CoLocatedPrivacyErasureAuthorityRepository` using `RelationalNamedLock` and pure LINQ.
4. **Pure LINQ Batch Updates**: Uses EF Core 7+ `ExecuteUpdateAsync` to eliminate raw SQL update statements.

---

## Constraints And Rules To Remember

- Repositories return entities, never DTOs.
- No raw SQL strings in standard repositories; use `ExecuteUpdateAsync` and `RelationalNamedLock`.
- Invariant enforcement and validation must live in C# Domain/Application/Persistence layers.
- Every file must start with a two-line `ABOUTME:` comment summary.

---

## Validation Baseline

For every phase:
1. `dotnet build --configuration Release --verbosity quiet`
2. `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

---

## Current Known Risks / Unknowns

- Ensure `PrimaryDatabaseProviderComposition` properly configures migration history tables and assembly names for all 5 providers (owned by Task 3.1).

---

## Handoff Notes

### Handoff — 2026-08-19 Europe/Brussels
- **Current state:** Planning artifacts created and ready for implementation.
- **Next action:** Execute Phase 1 Task 1.1 (`ImmutableEntityInterceptor.cs`).
- **Blockers:** None.
- **Modified files:** Planning artifacts in `dev/active/multi-database-persistence-unification/`.
- **Validation:** Plan matches all architectural invariants and project rules.
