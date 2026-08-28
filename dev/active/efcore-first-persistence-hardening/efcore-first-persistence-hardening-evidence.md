<!-- ABOUTME: Shared repository evidence packet for the EF Core-first persistence hardening workstream. -->
<!-- ABOUTME: Binds local architecture facts, provider research, active-workstream constraints, and resolved planning decisions. -->

# EF Core-First Persistence Hardening — Evidence Packet

Last Updated: 2026-08-27 Europe/Brussels

## Purpose And Revision

This packet is the single reviewed input for the implementation plan, tasks ledger,
context, and planning-mode I-VSD assessment. It records current repository evidence
only; it does not authorize implementation or modify runtime behavior.

The packet covers `src/Explore.Persistence`, primary database configuration under
`src/Explore.Secrets/Database`, provider migration projects, relevant architecture
and persistence integration tests, and overlapping active workstreams.

## Contract Classification

| Contract | Criticality | Planning consequence |
| --- | --- | --- |
| `update-repository-query` | Domain state | Preserve repository entity boundaries, named filters, navigation semantics, and behavioral integration coverage. |
| `add-ef-migration` | Tier 1 security | Generated migrations only, exhaustive provider blast-radius review, adversarial invariant tests, and anonymized MAD review. |
| Payment, admission, inventory, notification, webhook, and privacy path rules | Tiers 0–2 overlays | Preserve monetary state, tenant isolation, authority-first erasure, anti-resurrection fencing, idempotency, lease fences, and zero-PII telemetry. |

Applicable repository authorities:

- `AGENTS.md`
- `docs/QUICK_REFERENCE.md`
- `docs/ARCHITECTURE.md`
- `docs/DOMAIN.md`
- `docs/MULTI_TENANCY.md`
- `docs/CODEBASE_INSIGHTS.md`
- `docs/OPERATIONS.md`
- `docs/TESTING.md`
- `.agents/rules/efcore-persistence.md`
- `.agents/rules/efcore-migrations.md`
- `.agents/skills/dotnet-efcore-guidelines/SKILL.md`
- `.agents/skills/optimize-ef-core-queries/SKILL.md`
- `.agents/skills/criticality-guardrail/SKILL.md`

## Current Architecture Facts

1. The application uses one ORM, EF Core, with five primary provider modes:
   PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL.
2. Provider composition is centralized in
   `src/Explore.Persistence/Database/PrimaryDatabaseProviderComposition.cs`.
3. `EFCore.NamingConventions` is enabled through
   `UseSnakeCaseNamingConvention()` in runtime, migration, and retained-authority
   context composition.
4. PostgreSQL and SQL Server use a configurable schema. SQLite, MariaDB, and
   MySQL use the fixed `ie_` table prefix through
   `src/Explore.Persistence/Schema/RelationalModelNamespace.cs`.
5. PostgreSQL uses the primary `Explore.Persistence` migration set. Other
   primary providers own separate generated migration assemblies; Data
   Protection and retained-authority storage also have provider-owned migration
   sets.
6. Repositories already use native EF set-based mutation extensively:
   222 `ExecuteUpdateAsync` sites and 75 `ExecuteDeleteAsync` sites.

## Static Audit Baseline

The 2026-08-27 non-generated persistence scan found:

| Signal | Count |
| --- | ---: |
| EF raw-SQL API calls | 51 across 26 files |
| Direct ADO command/`CommandText` markers | 24 across 9 files |
| Literal `ToTable("...")` mappings | 228 |
| Literal `HasColumnName("...")` mappings | 79 |
| Literal `HasDatabaseName("...")` mappings | 428 |
| Imports from provider or EF `.Internal` namespaces | 4 |

Generated migration bodies are excluded from these runtime raw-SQL counts.

## Confirmed Problem Classes

### Physical-Name Bypass

Provider-agnostic SQL in these paths embeds unqualified, unprefixed table names
instead of using EF model metadata:

- `src/Explore.Persistence/Repositories/EventAgendaItemRepository.cs`
- `src/Explore.Persistence/Repositories/EventSessionRepository.cs`
- `src/Explore.Persistence/Repositories/NotificationFanoutOccurrenceRepository.cs`
- `src/Explore.Persistence/Repositories/RegistrationInventoryRepository.cs`

This conflicts with configurable schemas and the `ie_` prefix and therefore is
an observable provider-portability defect, not merely a style concern.

### Native-EF Bypass

Ordinary conditional updates, deletes, state transitions, and several upsert
flows use SQL despite existing tracked-entity or `ExecuteUpdateAsync` patterns.
The largest concentration is
`src/Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs`, which
maintains PostgreSQL SQL branches beside portable EF implementations.

### Convention Duplication

Literal table, column, index, and constraint names duplicate naming-convention
output in many configurations. Some column mappings remain semantically
necessary, especially flattened owned-value columns; they require explicit,
machine-reviewable exceptions rather than blanket removal.

### Provider Leakage

Repositories contain repeated provider-name literals and engine branches.
Provider detection and unavoidable engine behavior are not consistently
isolated behind provider primitives.

### Migration Extensibility Risk

`ApplicationMigrationsModelDiffer` and configurable-schema SQL generators depend
on EF/provider internal namespaces. Official EF guidance supports custom
migration operations and service replacement, but the Npgsql public generator
constructor itself currently requires
`Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.Internal.INpgsqlSingletonOptions`.
This seam therefore needs strict isolation and package-compatibility tests.

### Missing Enforcement

The architecture suite has no general gate preventing new raw-SQL APIs, direct
ADO commands, redundant literal table mappings, internal provider APIs, or
repository-level provider-name comparisons.

## Existing Behavioral Authorities

- Registration Data Collection is complete and remains authoritative for
  registration order, payment/refund, inventory, admission, and erasure
  invariants.
- Event Ticketing Lifecycle is plan-blocked. It must consume the persistence
  guardrails from this workstream before adding runtime persistence behavior.
- Existing tenant filters, explicit bypass reasons, concurrency stamps, lease
  fences, idempotency keys, outbox semantics, authority-first erasure ordering,
  and zero-PII telemetry requirements are preserved.

## External Functional Research

Only official framework/provider documentation and package metadata were used.
No external implementation source was imported.

| Source | Repository-relevant conclusion |
| --- | --- |
| [EF Core ExecuteUpdate and ExecuteDelete](https://learn.microsoft.com/en-us/ef/core/saving/execute-insert-update-delete) | Native set-based updates support LINQ predicates, multiple setters, calculated values, rows-affected checks, and explicit transaction composition; they do not provide insert/upsert, multi-table mutation, or returned original values. |
| [EF Core SQL queries](https://learn.microsoft.com/en-us/ef/core/querying/sql-queries) | Raw SQL is supported but must be parameterized and remains responsible for physical query shape and mapping correctness. |
| [EF Core database functions](https://learn.microsoft.com/en-us/ef/core/querying/database-functions) | Providers may translate additional .NET expressions; provider translation must be verified before introducing SQL. |
| [EF Core custom migration operations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/operations) | Public custom operations and `IMigrationsSqlGenerator` replacement are the supported migration extension path. |
| [EF Core multiple-provider migrations](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers) | Separate migration sets per provider are the official pattern and match the repository direction. |
| [EF Core testing strategy](https://learn.microsoft.com/en-us/ef/core/testing/choosing-a-testing-strategy) | Provider behavior must be tested against real engines; fake providers do not establish translation or engine parity. |
| [Npgsql translations](https://www.npgsql.org/efcore/mapping/translations.html) | Npgsql already translates many PostgreSQL functions and .NET expressions; use those translations before raw SQL. |
| [Npgsql migration generator API](https://www.npgsql.org/efcore/api/Npgsql.EntityFrameworkCore.PostgreSQL.Migrations.NpgsqlMigrationsSqlGenerator.html) | The generator is public but its current constructor includes an internal provider options contract. |
| [EFCore.NamingConventions](https://github.com/efcore/EFCore.NamingConventions/blob/main/README.md) | Snake case applies across relational providers and includes table, column, key, and index naming; the plugin is community-maintained and needs model parity tests. |
| [Microting.EntityFrameworkCore.MySql 10.0.10](https://www.nuget.org/packages/Microting.EntityFrameworkCore.MySql/10.0.10) | Microting is an EF Core provider over MySqlConnector, not another ORM; its package describes itself as fast-moving, so real-engine compatibility evidence is mandatory. |
| [PostgreSQL advisory locks](https://www.postgresql.org/docs/current/functions-admin.html#FUNCTIONS-ADVISORY-LOCKS) | Advisory locks are an engine primitive with no general EF LINQ equivalent and may remain behind a narrow provider adapter. |

## Resolved Planning Decisions

| ID | Decision |
| --- | --- |
| `PERSIST-DEC-001` | Retain PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL as supported primary providers. |
| `PERSIST-DEC-002` | Retain configurable schemas for PostgreSQL/SQL Server and the fixed `ie_` prefix for schema-less providers. |
| `PERSIST-DEC-003` | Keep `EFCore.NamingConventions` as the physical naming authority; remove redundant mappings and make true semantic exceptions explicit. |
| `PERSIST-DEC-004` | Repositories use tracked entities, LINQ, specifications, `ExecuteUpdate/Delete`, transactions, and concurrency tokens before any provider primitive. |
| `PERSIST-DEC-005` | Provider APIs/translations are the second rung. Parameterized SQL is allowed only in an isolated provider-primitive namespace after a documented capability check. |
| `PERSIST-DEC-006` | Physical table/column identifiers in unavoidable SQL come from EF metadata and `ISqlGenerationHelper`; values remain parameters. |
| `PERSIST-DEC-007` | Tracked migration files and snapshots remain generated artifacts. Existing merged history is not hand-edited; generated corrective migrations may be breaking, and disposable development databases may be recreated. |
| `PERSIST-DEC-008` | Historical scaffold-time compatibility backfills are removed once generated corrective migrations make them unnecessary. Provider-internal migration APIs that cannot yet be removed are isolated and package-version tested. |
| `PERSIST-DEC-009` | No new persistence dependency is planned. A provider extension package requires a separate license and capability decision. |
| `PERSIST-DEC-010` | The implementation is test-first at each behavioral seam and concludes with real-engine parity, mutation testing, and anonymized MAD review. |

## Intake Outcome

Repository evidence resolved the material planning branches. There are no
deferrable open questions that alter scope, architecture, phase order, or test
strategy. Implementation approval remains with the user; destructive database
recreation remains an implementation-time operator action, not a planning side
effect.
