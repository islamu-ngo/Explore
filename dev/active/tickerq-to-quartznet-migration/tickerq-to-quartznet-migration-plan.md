# TickerQ → Quartz.NET Migration — Implementation Plan

Last Updated: 2026-08-15 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Replace TickerQ with Quartz.NET for all background job scheduling.
- **Task directory:** `dev/active/tickerq-to-quartznet-migration/`
- **Planning status:** ✅ Implemented (all 4 phases complete — see `tasks.md` for the delivered state and deviations)
- **Matched intents:** Fallback contract — no existing intent covers a scheduler-library replacement. Consider adding a `replace-scheduler-library` intent if this pattern recurs.
- **Relevant skills:** `clean-architecture-rules`, `ip-clean-room`, `dotnet-efcore-guidelines`
- **Relevant rules:** `.agents/rules/api-controllers.md` (for dashboard route), `.agents/rules/tests.md`
- **Primary layers touched:** API (primary), Application (contracts — minor), Infrastructure (settings), Standalone, ServiceDefaults, Tests, Documentation
- **Complexity:** **L** — contained API-layer swap of ~15 source files + ~8 test files + ~13 documentation files, but touches hosting, persistence, health checks, and the standalone image.

## 1. Executive Summary

Replace TickerQ (MIT/Apache-2.0 dual, young, PostgreSQL-only operational store, separate EF Core migrations) with Quartz.NET (Apache 2.0, 15+ years enterprise maturity, first-party SQLite/PostgreSQL/SQL Server/MySQL support via ADO.NET `AdoJobStore`, no EF Core DbContext required).

**Why it matters:**
- Eliminates the separate `ApiTickerQDbContext`, `Migrations/TickerQ/` directory, and PostgreSQL-only persistence constraint.
- Enables the standalone Tier 1 SQLite deployment to have durable background job scheduling — currently impossible with TickerQ.
- Aligns with the project's multi-database strategy (PostgreSQL, SQLite, SQL Server, MariaDB, MySQL) via first-party Quartz ADO.NET delegates.
- Reduces self-hosting friction: no separate schema, no separate migration DbContext, no optional SaaS dependency.

**Non-goals:**
- No backward compatibility shims — development mode, clean swap.
- No clustering configuration — will be configured but not tested until Tier 3 need.
- No custom dashboard UI. **Corrected during implementation:** `Quartz.AspNetCore` has no dashboard (health checks + hosted service only). A first-party `Quartz.Dashboard` package does exist (3.18+, Apache-2.0) but is a Blazor Server + SignalR app; it was deferred and a read-only JSON status endpoint ships instead. See `tasks.md` → Deferred Work.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---|---|
| TickerQ NuGet packages: `TickerQ`, `TickerQ.Dashboard`, `TickerQ.EntityFrameworkCore`, `TickerQ.Instrumentation.OpenTelemetry` v10.4.0 | Verified: `Directory.Packages.props:168-171`, `Explore.API.csproj:71-74` | High | Central package management |
| TickerQ code confined to API layer `Scheduling/` and `Extensions/` | Verified by search: `using TickerQ` matched 12 lines, all in `src/Explore.API/` | High | No Domain/Application/Persistence/Infrastructure direct coupling |
| Application contracts are scheduler-neutral | Verified: `IScheduledJobRegistry`, `ScheduledJobDescriptor`, `ScheduledJobNames`, `IScheduledEmailDispatchTrigger` — no TickerQ imports | High | Clean Architecture boundary intact |
| 8 scheduled jobs defined | Verified: `ScheduledJobNames.cs` — `email-dispatch-drain`, `general-outbox-drain`, `pds-sync-drain`, `email-dispatch-recovery-scan`, `dead-letter-summary`, `event-reminder-dispatch`, `waitlist-promotion-scan`, `tenant-maintenance-scan` | High | |
| Infrastructure `EmailDispatchProcessorMode` enum contains `TickerQ` value | Verified: `Explore.Infrastructure/EmailDispatchProcessorSettings.cs` and `EmailDispatchProcessorSettingsValidator.cs` | High | Enum/messages must update to Quartz |
| Auth policy for TickerQ dashboard defined in `AuthenticationExtensions.cs` | Verified: `Explore.API/Extensions/AuthenticationExtensions.cs:255` | High | Policy name must change |
| OTel `.AddSource("TickerQ")` in ServiceDefaults | Verified: `Explore.ServiceDefaults/Extensions.cs` | High | Source name must change |
| 2 TickerQ job classes | Verified: `EmailDispatchTickerQJobs.cs` (2 jobs), `EventLifecycleTickerQJobs.cs` (1 job) | High | 3 implemented, 5 planned |
| Separate TickerQ EF Core DbContext and migrations | Verified: `ApiTickerQDbContext.cs`, `ApiTickerQDbContextFactory.cs`, `Migrations/TickerQ/` (3 files) | High | PostgreSQL-only |
| No TickerQ in docker-compose or .env | Verified by search: zero results in `docker-compose.yml` and `.env.example` | High | Runtime config only |
| TickerQ referenced in 13 documentation files | Verified by search: `docs/ARCHITECTURE.md`, `CONFIGURATION.md`, `DEPLOYMENT_TIERS.md`, `EMAIL_NOTIFICATIONS.md`, `FEDERATION.md`, `OPERATIONS.md`, `OUTBOX_PATTERN.md`, `RELEASE_CHECKLIST.md`, `SECURITY-MODEL.md`, `SELF_HOSTING.md`, `TESTING.md`, `TROUBLESHOOTING.md`, `docs-website/Installation/docker-compose.md` | High | All need text updates |
| 8 test files reference TickerQ | Verified by search in `tests/` | High | All need rewriting |

### 2.2 Existing Implementation

**API Layer (`src/Explore.API/`):**
- `Scheduling/EmailDispatchTickerQJobs.cs` — Two `[TickerFunction]` methods: cron drain (every 10s) and recovery scan (every 1m). Uses `TickerFunctionContext` with `SkipIfAlreadyRunning()`.
- `Scheduling/EventLifecycleTickerQJobs.cs` — One `[TickerFunction]` method for delayed event-reminder dispatch. Uses `TickerFunctionContext<ScheduledEmailDispatchPointer>` for typed payload.
- `Scheduling/TickerQScheduledEmailDispatchTrigger.cs` — Implements `IScheduledEmailDispatchTrigger` from Application. Schedules one-off delayed jobs via TickerQ's `ITickerManager`.
- `Scheduling/ApiTickerQDbContext.cs` — EF Core DbContext for TickerQ operational store.
- `Scheduling/ApiTickerQDbContextFactory.cs` — Design-time factory for TickerQ migrations.
- `Configuration/TickerQSchedulerOptions.cs` — Options class with `ConcurrencyLevel`, `EnableDashboard`, `DashboardPath`.
- `Configuration/TickerQSchedulerOptionsValidator.cs` — FluentValidation validator for above.
- `Extensions/TickerQSchedulerExtensions.cs` — DI registration: `AddTickerQ()`, `UseTickerQEntityFrameworkCorePersistence()`, `AddTickerQDashboard()`, `AddTickerQOpenTelemetry()`.
- `Migrations/TickerQ/` — 3 files: initial migration, designer, model snapshot.

**Application Layer (`src/Explore.Application/`):**
- `Contracts/Scheduling/IScheduledJobRegistry.cs` — Scheduler-neutral catalog interface. **No TickerQ dependency.**
- `Contracts/Scheduling/ScheduledJobDescriptor.cs` — Scheduler-neutral record. **No TickerQ dependency.**
- `Contracts/Scheduling/ScheduledJobNames.cs` — Centralized job name constants. **No TickerQ dependency.**
- `Contracts/Infrastructure/IScheduledEmailDispatchTrigger.cs` — Application contract for delayed dispatch. **No TickerQ dependency.**

**Standalone (`src/Event.Standalone/`):**
- `Hosting/StandaloneHostApplicationExtensions.cs` — References TickerQ in standalone host composition.

**Service Defaults (`src/Explore.ServiceDefaults/`):**
- `Extensions.cs` — Contains `.AddSource("TickerQ")` for OpenTelemetry tracing. Must replace with Quartz source name.

**Auth Policy:**
- `Explore.API/Extensions/AuthenticationExtensions.cs` — Line 255 defines an authorization policy string referencing the TickerQ dashboard. Must update to Quartz dashboard policy.

### 2.3 Existing Tests And Verification Coverage

| Test File | What It Tests | Impact |
|---|---|---|
| `EmailDispatchTickerQJobsTests.cs` | Job drain and recovery integration | Rewrite to Quartz IJob |
| `TickerQDashboardRouteTests.cs` | Dashboard route accessibility | Rewrite for Quartz dashboard route |
| `TickerQSchedulerOperationalStoreTests.cs` | EF Core store setup | Delete — Quartz uses AdoJobStore |
| `TickerQSchedulerOptionsValidatorTests.cs` | Options validation | Rewrite for Quartz options |
| `ApiTickerQDbContextFactoryTests.cs` | Design-time factory | Delete — no EF Core DbContext needed |
| `EmailDispatchHealthCheckTests.cs` | Health check behavior | Modify if health check references TickerQ |
| `DurableSideEffectBoundaryTests.cs` | Architecture boundary for durable side effects | Modify TickerQ references |
| `StandaloneHostGraphTests.cs` | Standalone service graph | Modify TickerQ references |
| `AuthorizationProductionGuardrailTests.cs` | Auth guardrails on dashboard | Rewrite for Quartz dashboard path |

### 2.4 Existing Documentation And Contracts

13 documentation files reference TickerQ and need text updates:
`ARCHITECTURE.md`, `CONFIGURATION.md`, `DEPLOYMENT_TIERS.md`, `EMAIL_NOTIFICATIONS.md`, `FEDERATION.md`, `OPERATIONS.md`, `OUTBOX_PATTERN.md`, `RELEASE_CHECKLIST.md`, `SECURITY-MODEL.md`, `SELF_HOSTING.md`, `TESTING.md`, `TROUBLESHOOTING.md`, `docs-website/Installation/docker-compose.md`.

### 2.5 Current Pain Points / Improvement Areas

1. **PostgreSQL-only operational store** — TickerQ's EF Core persistence uses PostgreSQL. Standalone Tier 1 (SQLite) cannot have durable job scheduling.
2. **Separate migration DbContext** — `ApiTickerQDbContext` + `Migrations/TickerQ/` adds migration complexity for self-hosters.
3. **Maturity risk** — TickerQ is a young library; long-term .NET version support is uncertain.
4. **Fixed `ticker` schema** — TickerQ owns a fixed PostgreSQL schema, creating namespace collision risk for multi-instance deployments sharing a database.

### 2.6 Unknowns After Investigation

| Unknown | Searched | Resolution Task |
|---|---|---|
| Exact Quartz.NET DDL for SQLite with `QRTZ_` prefix | Web search confirmed scripts exist in Quartz GitHub `/database/tables/` | Task 1.1 will embed DDL scripts |
| Whether `Quartz.AspNetCore` dashboard auth supports existing `[Authorize]` patterns | Web search shows it supports middleware pipeline auth | Task 2.2 will implement |
| How `TickerQScheduledEmailDispatchTrigger` dynamically schedules via `ITickerManager` | Must read file to map to `ISchedulerFactory` + `IScheduler` | Task 1.3 will implement |

## 3. Proposed Future State

**Scheduling ownership:**
- Quartz.NET runs fully in-process via `IHostedService` (`AddQuartzHostedService`).
- `AdoJobStore` persists job and trigger state in the application's primary database (PostgreSQL, SQLite, SQL Server, MariaDB, MySQL) using a configurable table prefix (`QRTZ_`).
- DDL scripts for each database provider are embedded in the application and applied by `MigrationService` (split deployment) or in-process at startup (standalone).

**Job implementation:**
- Each job implements `IJob` with constructor-injected services (scoped DI per execution).
- `[DisallowConcurrentExecution]` replaces `SkipIfAlreadyRunning()`.
- `JobDataMap` replaces `TickerFunctionContext<T>` for typed payloads.
- Cron schedules use `CronTrigger`; one-off delayed dispatch uses `SimpleTrigger` with `StartAt()`.

**Application contracts unchanged:**
- `IScheduledJobRegistry`, `ScheduledJobDescriptor`, `ScheduledJobNames` — no changes.
- `IScheduledEmailDispatchTrigger` — implementation changes (TickerQ → Quartz), contract stays.

**Self-hosting benefit:**
- Standalone Tier 1 (SQLite) gets durable scheduling with zero additional infrastructure.
- Split deployments (PostgreSQL) use the same `QRTZ_` tables in the application database.
- No separate schema, no separate migration DbContext, no `ticker` namespace.

## 4. Non-Negotiable Constraints

1. **Clean Architecture inward-only dependencies** — Quartz.NET must be referenced only in `Explore.API` (API layer). Application contracts remain scheduler-neutral.
2. **No EF Core migrations for scheduler tables** — Quartz uses its own DDL scripts. Do not create an EF Core DbContext for Quartz.
3. **Every file starts with two-line ABOUTME** — per `QUICK_REFERENCE.md`.
4. **File-scoped namespaces** — per governance conventions.
5. **Apache 2.0 license compatibility** — Quartz.NET is Apache 2.0, compatible with AGPL-3.0 outbound.
6. **No backward compatibility shims** — development mode, clean swap.
7. **Validators are manually instantiated** — any options validation follows project convention.

## 5. Architecture And Design Decisions

### Decision 1: Use `AdoJobStore` (not RAMJobStore)
- **Why:** Durable scheduling survives process restarts. Required for production event reminders and outbox drains.
- **Alternatives considered:** `RAMJobStore` (simpler, but jobs lost on restart — unacceptable for email dispatch).
- **Consequences:** Requires DDL tables in the application database.
- **Files/layers affected:** API layer configuration, MigrationService DDL application.

### Decision 2: Embed DDL scripts as resources, apply via `MigrationService` / startup
- **Why:** Quartz tables are not EF Core managed. Embedding scripts keeps them versioned with the application. Application startup (standalone) or MigrationService (split) applies them idempotently.
- **Alternatives considered:** EF Core `migrationBuilder.Sql()` (breaks the "no EF migrations for scheduler" constraint), manual operator script (adds self-hosting friction).
- **Consequences:** Need a helper that applies DDL scripts with `IF NOT EXISTS` guards per provider.
- **Files/layers affected:** API resources, MigrationService startup, Standalone startup.

### Decision 3: Quartz tables co-located in application database with `QRTZ_` prefix
- **Why:** Simplest for self-hosters. One database, one backup, one restore. The `QRTZ_` prefix prevents collision with application tables.
- **Alternatives considered:** Separate schema (adds PostgreSQL namespace complexity), separate database (adds infrastructure for Tier 1).
- **Consequences:** Quartz tables appear in the application database with `QRTZ_` prefix.
- **Files/layers affected:** Quartz configuration, DDL scripts.

### Decision 4: `SystemTextJsonSerializer` for `JobDataMap` serialization
- **Why:** Aligns with the project's use of `System.Text.Json`. Avoids Newtonsoft dependency.
- **Alternatives considered:** `NewtonsoftJsonObjectSerializer` (adds unnecessary dependency).
- **Consequences:** `JobDataMap` values serialized as JSON strings.
- **Files/layers affected:** Quartz DI configuration.

### Decision 5: One scheduling options class replacing TickerQ options
- **Why:** Maintain the existing pattern of validated configuration.
- **Alternatives considered:** Direct Quartz config via `appsettings.json` (loses validator pattern).
- **Consequences:** `QuartzSchedulerOptions` replaces `TickerQSchedulerOptions` with the same validation approach.
- **Files/layers affected:** `Configuration/`, `Extensions/`.

## 6. Implementation Phases

### Phase 1: Core Quartz.NET Infrastructure — NuGet, Configuration, Job Store, DDL

- **Goal:** Replace TickerQ NuGet packages with Quartz.NET packages, configure `AdoJobStore`, embed DDL scripts, wire DI.
- **Depends on:** Nothing
- **Related skills/rules:** `clean-architecture-rules`, `ip-clean-room`
- **Acceptance criteria:** Build compiles with Quartz.NET references; TickerQ packages removed.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Revert NuGet changes in `Directory.Packages.props` and `.csproj`.

#### Task 1.1: Replace NuGet packages
- **Type:** modify
- **Layer:** API / Build
- **Files:**
  - `Directory.Packages.props` (existing) — remove 4 TickerQ entries, add Quartz entries
  - `src/Explore.API/Explore.API.csproj` (existing) — remove 4 TickerQ `<PackageReference>`, add Quartz references
- **Description:** Remove `TickerQ`, `TickerQ.Dashboard`, `TickerQ.EntityFrameworkCore`, `TickerQ.Instrumentation.OpenTelemetry` v10.4.0. Add `Quartz`, `Quartz.Extensions.Hosting`, `Quartz.Serialization.SystemTextJson`, `Quartz.AspNetCore`. Pin versions in `Directory.Packages.props`.
- **Acceptance Criteria:**
  - [ ] No TickerQ packages remain in `Directory.Packages.props`
  - [ ] No TickerQ `<PackageReference>` remains in any `.csproj`
  - [ ] Quartz packages resolve successfully
- **Dependencies:** None
- **Effort:** S
- **Required Skills/Rules:** ip-clean-room (license check: Apache 2.0 ✅)

#### Task 1.2: Create Quartz configuration and options
- **Type:** create / delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Configuration/QuartzSchedulerOptions.cs` (new)
  - `src/Explore.API/Configuration/QuartzSchedulerOptionsValidator.cs` (new)
  - `src/Explore.API/Configuration/TickerQSchedulerOptions.cs` (existing) — delete
  - `src/Explore.API/Configuration/TickerQSchedulerOptionsValidator.cs` (existing) — delete
- **Description:** Create `QuartzSchedulerOptions` with `EnableDashboard`, `DashboardPath`, `UsePersistentStore`, `TablePrefix` properties. Create validator with manually-instantiated FluentValidation (per project convention). Delete TickerQ equivalents.
- **Acceptance Criteria:**
  - [ ] `QuartzSchedulerOptions` exists with all necessary properties
  - [ ] Validator is manually instantiated (not DI-injected)
  - [ ] TickerQ options files deleted
- **Dependencies:** None
- **Effort:** S
- **Required Skills/Rules:** cqrs-mediatr-guidelines (validator convention)

#### Task 1.3: Create Quartz DI extension method
- **Type:** create / delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs` (new)
  - `src/Explore.API/Extensions/TickerQSchedulerExtensions.cs` (existing) — delete
- **Description:** Create `AddQuartzScheduler(this IServiceCollection, IConfiguration)` that:
  - Calls `services.AddQuartz(q => { ... })` with `AdoJobStore` configuration
  - Configures provider-specific delegate (SQLite, PostgreSQL, SQL Server, MySQL) based on `Database:Provider`
  - Sets `SystemTextJsonSerializer`, `UseProperties = true`, table prefix `QRTZ_`
  - Registers all `IJob` implementations
  - Registers cron triggers for recurring jobs
  - Calls `services.AddQuartzHostedService(o => o.WaitForJobsToComplete = true)`
  - Optionally maps Quartz dashboard via `Quartz.AspNetCore`
  - Delete `TickerQSchedulerExtensions.cs`
- **Acceptance Criteria:**
  - [ ] `AddQuartzScheduler` registers Quartz with persistent store for all 5 database providers
  - [ ] All recurring jobs registered with cron triggers
  - [ ] Dashboard conditionally mapped
  - [ ] TickerQ extension deleted
- **Dependencies:** 1.1, 1.2
- **Effort:** L
- **Required Skills/Rules:** clean-architecture-rules

#### Task 1.4: Embed Quartz DDL scripts and create schema initializer
- **Type:** create
- **Layer:** API
- **Files:**
  - `src/Explore.API/Resources/Quartz/tables_sqlite.sql` (new)
  - `src/Explore.API/Resources/Quartz/tables_postgres.sql` (new)
  - `src/Explore.API/Resources/Quartz/tables_sqlServer.sql` (new)
  - `src/Explore.API/Resources/Quartz/tables_mysql.sql` (new)
  - `src/Explore.API/Scheduling/QuartzSchemaInitializer.cs` (new)
- **Description:** Download DDL scripts from the Quartz.NET GitHub repository `/database/tables/`. Embed as resources. Create `QuartzSchemaInitializer` that applies the appropriate DDL idempotently on startup (for standalone) or via MigrationService (for split). Use `IF NOT EXISTS` guards or provider-specific equivalents.
- **Acceptance Criteria:**
  - [ ] DDL scripts embedded for all 4 provider families
  - [ ] Schema initializer applies correct script based on `Database:Provider`
  - [ ] Idempotent — safe to run multiple times
- **Dependencies:** 1.1
- **Effort:** M
- **Required Skills/Rules:** dotnet-efcore-guidelines (for understanding provider detection)

#### Task 1.5: Delete TickerQ EF Core artifacts
- **Type:** delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/ApiTickerQDbContext.cs` (existing) — delete
  - `src/Explore.API/Scheduling/ApiTickerQDbContextFactory.cs` (existing) — delete
  - `src/Explore.API/Migrations/TickerQ/20260528182509_AddTickerQOperationalStore.Designer.cs` (existing) — delete
  - `src/Explore.API/Migrations/TickerQ/20260528182509_AddTickerQOperationalStore.cs` (existing) — delete
  - `src/Explore.API/Migrations/TickerQ/ApiTickerQDbContextModelSnapshot.cs` (existing) — delete
- **Description:** Delete all TickerQ EF Core DbContext, factory, and migration files. The entire `Migrations/TickerQ/` directory is removed.
- **Acceptance Criteria:**
  - [ ] No TickerQ migration files remain
  - [ ] No `ApiTickerQDbContext` remains
  - [ ] No `ApiTickerQDbContextFactory` remains
- **Dependencies:** 1.3 (extension must not reference these)
- **Effort:** S
- **Required Skills/Rules:** None

### Phase 2: Job Implementation — Rewrite TickerQ Jobs as Quartz IJob

- **Goal:** Rewrite all TickerQ job classes as Quartz.NET `IJob` implementations with equivalent behavior.
- **Depends on:** Phase 1
- **Related skills/rules:** `clean-architecture-rules`
- **Acceptance criteria:** All 3 implemented jobs work as Quartz `IJob` with identical Application-layer delegation.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Restore TickerQ job files from git.

#### Task 2.1: Rewrite EmailDispatchTickerQJobs as Quartz IJob
- **Type:** create / delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/EmailDispatchDrainJob.cs` (new)
  - `src/Explore.API/Scheduling/EmailDispatchRecoveryScanJob.cs` (new)
  - `src/Explore.API/Scheduling/EmailDispatchTickerQJobs.cs` (existing) — delete
- **Description:** Split into two `IJob` classes (Quartz requires one class per job for `[DisallowConcurrentExecution]`). Each injects `IEmailDispatchDrainService` and `ILogger<T>` via constructor DI. Use `[DisallowConcurrentExecution]` attribute. Cron schedule registered in extension (Task 1.3). Delete TickerQ version.
- **Acceptance Criteria:**
  - [ ] `EmailDispatchDrainJob` runs on `*/10 * * * * ?` (every 10 seconds) cron
  - [ ] `EmailDispatchRecoveryScanJob` runs on `0 */1 * * * ?` (every minute) cron
  - [ ] Both use `[DisallowConcurrentExecution]`
  - [ ] Both delegate to `IEmailDispatchDrainService` identically to TickerQ version
  - [ ] TickerQ job file deleted
- **Dependencies:** 1.3
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules

#### Task 2.2: Rewrite EventLifecycleTickerQJobs as Quartz IJob
- **Type:** create / delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/EventReminderDispatchJob.cs` (new)
  - `src/Explore.API/Scheduling/EventLifecycleTickerQJobs.cs` (existing) — delete
- **Description:** Create `EventReminderDispatchJob : IJob` that reads `ScheduledEmailDispatchPointer` from `JobDataMap` (serialized as JSON string). Validates use case. Delegates to `IEmailDispatchDrainService.ProcessSingleAsync()`. Delete TickerQ version.
- **Acceptance Criteria:**
  - [ ] `EventReminderDispatchJob` deserializes pointer from `JobDataMap`
  - [ ] Validates use case matches `EventLifecycleAutomationUseCases.EventReminder`
  - [ ] Delegates to `drainService.ProcessSingleAsync()` identically
  - [ ] TickerQ job file deleted
- **Dependencies:** 1.3
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules

#### Task 2.3: Rewrite TickerQScheduledEmailDispatchTrigger
- **Type:** create / delete
- **Layer:** API
- **Files:**
  - `src/Explore.API/Scheduling/QuartzScheduledEmailDispatchTrigger.cs` (new)
  - `src/Explore.API/Scheduling/TickerQScheduledEmailDispatchTrigger.cs` (existing) — delete
- **Description:** Implement `IScheduledEmailDispatchTrigger` using `ISchedulerFactory` to get `IScheduler`, then schedule a `SimpleTrigger` with `StartAt(scheduledTime)` and `JobDataMap` containing serialized `ScheduledEmailDispatchPointer`. Replace `ITickerManager` usage. Delete TickerQ version.
- **Acceptance Criteria:**
  - [ ] Implements `IScheduledEmailDispatchTrigger` interface unchanged
  - [ ] Uses `ISchedulerFactory` → `IScheduler.ScheduleJob()` for one-off delayed dispatch
  - [ ] Serializes `ScheduledEmailDispatchPointer` into `JobDataMap` as JSON
  - [ ] TickerQ trigger file deleted
- **Dependencies:** 1.3, 2.2
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules

#### Task 2.4: Update hosting extensions, health checks, and infrastructure settings
- **Type:** modify
- **Layer:** API / Infrastructure / Standalone / ServiceDefaults
- **Files:**
  - `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs` (existing) — modify
  - `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` (existing) — modify
  - `src/Explore.API/Hosting/ApiHostStartupExtensions.cs` (existing) — modify
  - `src/Explore.API/HealthChecks/EmailDispatchHealthCheck.cs` (existing) — modify if needed
  - `src/Explore.API/Extensions/AuthenticationExtensions.cs` (existing) — modify dashboard auth policy name
  - `src/Explore.Infrastructure/EmailDispatchProcessorSettings.cs` (existing) — rename `TickerQ` enum to `Quartz`
  - `src/Explore.Infrastructure/EmailDispatchProcessorSettingsValidator.cs` (existing) — update validation messages
  - `src/Event.Standalone/Hosting/StandaloneHostApplicationExtensions.cs` (existing) — modify
  - `src/Explore.ServiceDefaults/Extensions.cs` (existing) — replace `.AddSource("TickerQ")` with Quartz source
- **Description:** Replace all TickerQ references in hosting pipeline with Quartz equivalents. Rename `EmailDispatchProcessorMode.TickerQ` to `EmailDispatchProcessorMode.Quartz` in Infrastructure settings. Update standalone to call `QuartzSchemaInitializer` before starting the host. Replace TickerQ dashboard mapping with Quartz dashboard. Update dashboard auth policy name in `AuthenticationExtensions.cs`. Replace TickerQ OTel source with Quartz source in ServiceDefaults.
- **Acceptance Criteria:**
  - [ ] No `TickerQ` references remain in any hosting extension
  - [ ] `EmailDispatchProcessorMode.TickerQ` renamed to `.Quartz`
  - [ ] Validator messages updated
  - [ ] Auth policy name updated for Quartz dashboard
  - [ ] OTel source updated to Quartz
  - [ ] Standalone applies Quartz DDL before HTTP listener starts
  - [ ] Dashboard route uses same authorization pattern as before
  - [ ] Health check works with Quartz (or Quartz.AspNetCore built-in health)
- **Dependencies:** 1.3, 1.4
- **Effort:** L
- **Required Skills/Rules:** clean-architecture-rules

### Phase 3: Tests — Rewrite and verify

- **Goal:** Rewrite all TickerQ-specific tests for Quartz.NET equivalents. Update architecture tests.
- **Depends on:** Phase 2
- **Related skills/rules:** `.agents/rules/tests.md`
- **Acceptance criteria:** All rewritten tests pass. No TickerQ references remain in test code.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Restore test files from git.

#### Task 3.1: Rewrite API integration tests
- **Type:** create / delete / modify
- **Layer:** Tests
- **Files:**
  - `tests/Event.API.IntegrationTests/Features/EmailDispatchTickerQJobsTests.cs` (existing) — rewrite as `EmailDispatchQuartzJobsTests.cs`
  - `tests/Event.API.IntegrationTests/Features/TickerQDashboardRouteTests.cs` (existing) — rewrite as `QuartzDashboardRouteTests.cs`
  - `tests/Event.API.IntegrationTests/Features/TickerQSchedulerOperationalStoreTests.cs` (existing) — delete (no EF store to test)
  - `tests/Event.API.IntegrationTests/Features/TickerQSchedulerOptionsValidatorTests.cs` (existing) — rewrite as `QuartzSchedulerOptionsValidatorTests.cs`
  - `tests/Event.API.IntegrationTests/Scheduling/ApiTickerQDbContextFactoryTests.cs` (existing) — delete
  - `tests/Event.API.IntegrationTests/Features/EmailDispatchHealthCheckTests.cs` (existing) — modify
  - `tests/Event.API.IntegrationTests/Features/AuthorizationProductionGuardrailTests.cs` (existing) — modify TickerQ dashboard references
- **Description:** Rewrite tests to verify Quartz job behavior, dashboard routing, and options validation. Delete tests for TickerQ EF Core artifacts that no longer exist.
- **Acceptance Criteria:**
  - [ ] No TickerQ references remain in any test file
  - [ ] Job drain and recovery tests verify Quartz IJob execution
  - [ ] Dashboard route test verifies authorization on Quartz dashboard
  - [ ] Options validator tests verify new `QuartzSchedulerOptions`
  - [ ] Health check tests pass with Quartz
- **Dependencies:** 2.1, 2.2, 2.3, 2.4
- **Effort:** L
- **Required Skills/Rules:** `.agents/rules/tests.md`

#### Task 3.2: Update architecture tests
- **Type:** modify
- **Layer:** Tests
- **Files:**
  - `tests/Event.Architecture.Tests/DurableSideEffectBoundaryTests.cs` (existing) — modify
  - `tests/Event.Standalone.IntegrationTests/StandaloneHostGraphTests.cs` (existing) — modify
- **Description:** Update architecture tests that reference TickerQ to reference Quartz.NET instead. The durable-side-effect boundary tests should enforce that Quartz.NET is confined to the API layer.
- **Acceptance Criteria:**
  - [ ] Architecture tests verify Quartz is API-layer only (not leaked to Domain/Application/Persistence)
  - [ ] Standalone graph tests reflect Quartz service registrations
  - [ ] No TickerQ references remain
- **Dependencies:** 2.4
- **Effort:** M
- **Required Skills/Rules:** clean-architecture-rules

### Phase 4: Documentation — Update all TickerQ references

- **Goal:** Update all 13 documentation files that reference TickerQ to reference Quartz.NET. Update SELF_HOSTING.md to reflect SQLite durable scheduling capability.
- **Depends on:** Phase 2
- **Related skills/rules:** None (documentation only)
- **Acceptance criteria:** No TickerQ references remain in documentation. Self-hosting docs reflect Quartz.NET.
- **Phase-end verification (run once after all tasks):**
  - `dotnet build --configuration Release --verbosity quiet`
  - `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- **Rollback / failure handling:** Restore docs from git.

#### Task 4.1: Update core documentation files
- **Type:** modify
- **Layer:** Docs
- **Files:**
  - `docs/SELF_HOSTING.md` (existing)
  - `docs/ARCHITECTURE.md` (existing)
  - `docs/CONFIGURATION.md` (existing)
  - `docs/DEPLOYMENT_TIERS.md` (existing)
  - `docs/EMAIL_NOTIFICATIONS.md` (existing)
  - `docs/OPERATIONS.md` (existing)
  - `docs/OUTBOX_PATTERN.md` (existing)
  - `docs/SECURITY-MODEL.md` (existing)
  - `docs/TESTING.md` (existing)
  - `docs/TROUBLESHOOTING.md` (existing)
  - `docs/FEDERATION.md` (existing)
  - `docs/RELEASE_CHECKLIST.md` (existing)
  - `docs/docs-website/Installation/docker-compose.md` (existing)
- **Description:** Replace all TickerQ mentions with Quartz.NET equivalents. Update SELF_HOSTING.md to remove the "TickerQ is PostgreSQL-only" caveat and document that standalone SQLite now has durable scheduling. Update CONFIGURATION.md with Quartz-specific settings. Update ARCHITECTURE.md diagrams if they reference TickerQ.
- **Acceptance Criteria:**
  - [ ] Zero `TickerQ` occurrences in any documentation file
  - [ ] SELF_HOSTING.md reflects Quartz.NET with SQLite support
  - [ ] CONFIGURATION.md documents new Quartz scheduler options
  - [ ] All 13 files updated
- **Dependencies:** 2.4
- **Effort:** M
- **Required Skills/Rules:** None

## 7. Testing Strategy

| Phase | Test Project | Rationale |
|---|---|---|
| 1 | `Event.Architecture.Tests` | Verifies Clean Architecture boundaries after package swap |
| 2 | `Event.API.IntegrationTests` | Verifies job execution, dashboard, hosting pipeline |
| 3 | `Event.API.IntegrationTests` | Verifies rewritten test suite itself |
| 4 | `Event.Architecture.Tests` | Verifies no doc-related regressions |

Contract-required additional projects to distribute: `Event.Architecture.Tests` (Phase 1, 4), `Event.API.IntegrationTests` (Phase 2, 3).

## 8. Documentation, Configuration, And Operations Impact

- **Configuration:** Replace `TickerQ:*` settings with `Quartz:*` or `Scheduler:*` settings in `appsettings.json`.
- **Environment variables:** No TickerQ-specific env vars exist in docker-compose or .env.
- **Aspire/Compose:** No changes needed — TickerQ had no Compose service.
- **13 documentation files:** Listed in Task 4.1.
- **Generated artifacts:** No OpenAPI or NSwag client changes — scheduler is internal.

## 9. Security, Authorization, Privacy, And Abuse Considerations

- **Dashboard authorization:** Quartz dashboard must maintain the same `[Authorize]` protection as the TickerQ dashboard. Verified by `AuthorizationProductionGuardrailTests`.
- **No new trust boundaries:** Scheduler runs in-process, same trust boundary as the API.
- **No new secrets:** Quartz uses the existing application database connection string.
- **Tenant isolation:** Not applicable — scheduler is infrastructure, not tenant-scoped.

## 10. Multi-Tenancy, Federation, Localization, Accessibility, And Product Considerations

| Concern | Classification | Explanation |
|---|---|---|
| Multi-tenancy | Not Applicable | Scheduler is infrastructure; jobs use tenant context from the Application layer |
| Federation | Not Applicable | Scheduler is internal to the API process |
| Localization | Not Applicable | No user-facing strings in scheduler |
| Accessibility | Not Applicable | Dashboard is admin-only, not end-user-facing |
| Product | Not Applicable | No product behavior change; identical job execution |

## 11. Observability And Operations

- **Logging:** Quartz.NET logs via `Microsoft.Extensions.Logging` (built-in). Job execution logs remain identical (Application layer logging unchanged).
- **Metrics:** Quartz 3.x has built-in OpenTelemetry instrumentation. No separate `TickerQ.Instrumentation.OpenTelemetry` package needed.
- **Health checks:** `Quartz.AspNetCore` provides built-in scheduler health checks. Replaces any TickerQ-specific health registration.
- **Dashboard:** Quartz.NET in-process dashboard at configurable path (default `/quartz`), behind `[Authorize]`.

## 12. Migration And Compatibility Plan

- **No backward compatibility** — development mode, clean swap.
- **Database:** TickerQ's `ticker` schema tables become orphaned. Operators can drop them manually. No automated cleanup.
- **Quartz tables:** Created fresh by DDL scripts. No data migration from TickerQ state.
- **Deployment order:** Apply Quartz DDL → deploy updated application. Single-step for standalone.
- **Breaking changes:** TickerQ configuration keys no longer recognized. New Quartz keys required.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection Signal | Owner/Task |
|---|---|---|---|---|---|
| Quartz DDL scripts for SQLite have compatibility issues | Low | Medium | Test DDL application in standalone integration tests | Startup failure, health check fails | Task 1.4 |
| Quartz cron expression syntax difference from TickerQ | Low | Low | Quartz cron is standard (6 fields with seconds); verify each expression | Jobs don't fire on schedule | Task 1.3, 2.1 |
| Dashboard authorization differs from TickerQ pattern | Low | Medium | Verify with `AuthorizationProductionGuardrailTests` | Unauthorized access or 403 for admins | Task 2.4, 3.1 |
| `JobDataMap` serialization incompatibility with `ScheduledEmailDispatchPointer` | Medium | Medium | Use `System.Text.Json` serialization with `UseProperties = true`; test round-trip | Deserialization failure in event-reminder job | Task 2.2, 2.3 |
| Quartz 10-second cron may behave differently than TickerQ 10-second cron | Low | Low | Verify Quartz supports sub-minute cron expressions (it does: seconds field) | Email dispatch drain interval changes | Task 2.1 |

## 14. Success Metrics And Definition Of Done

- [ ] Zero TickerQ references in source code (verified by `grep -r "TickerQ" src/ tests/`)
- [ ] Zero TickerQ references in documentation (verified by `grep -r "TickerQ" docs/`)
- [ ] Zero TickerQ packages in `Directory.Packages.props`
- [ ] `dotnet build --configuration Release --verbosity quiet` passes
- [ ] `Event.Architecture.Tests` passes — Clean Architecture boundaries enforced
- [ ] `Event.API.IntegrationTests` passes — all job, dashboard, health, and options tests green
- [ ] Standalone integration tests pass
- [ ] All 3 implemented jobs fire correctly under Quartz
- [ ] DDL schema initializer works for SQLite and PostgreSQL

## 15. Implementation Agent Contract — KEEP DEV DOCS CURRENT

Require future implementation agents to:

1. At the first implementation start, read plan, context, and tasks once; on a cold resume, read context and tasks first, then only the plan sections needed for the current phase or changed decision.
2. During an uninterrupted session, do not reread unchanged plan/context/tasks after every task; keep the current task in working context and reopen only the exact section needed.
3. Start from the highest-priority unchecked task unless the user overrides it.
4. Treat `tasks.md` as the hot execution ledger: check a substantial task immediately after its implementation acceptance criteria are met, and reconcile smaller completed tasks together no later than phase end.
5. Keep implementation-task and phase-verification checkboxes separate; a task may be checked when its implementation is complete, but the phase is complete only after its build and selected test checkboxes pass.
6. Update the task status summary, completed count, current priority, next recommended slice, discovered tasks, deferred work, and `Last Updated` whenever task state changes.
7. Update context after a completed phase, meaningful decision, blocker, failed validation, material discovery, or before pause/compaction/transfer; do not rewrite it for trivial edits.
8. Update the plan only when scope, architecture, phase order, acceptance criteria, risks, or validation strategy changes; do not churn it for ordinary progress.
9. Record failed validation with the known cause and next recovery action in tasks/context without marking the phase complete.
10. Before pausing, compaction, transfer, or PR creation, reconcile the affected tasks, add a concise dated handoff, and identify unrelated dirty files that the next contributor must avoid.
11. Run phase verification only after all phase tasks, with one Release build and at most one selected project test; do not repeat successful commands or start the application/browser.
12. Never report completion when repository reality and the task ledger disagree.

Require every implementation summary to teach:

- what changed and why;
- architecture/design patterns, libraries, infrastructure, protocols, and project abstractions used;
- important files, classes, handlers, services, and components with their responsibilities;
- data/control flow;
- relevant repository conventions and reliability/security practices;
- verification performed, remaining work, next work, and dev-doc update status.

## 16. Progress Reporting Contract

Require this response shape after each implementation slice:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: yes/no with reason
```

For completed implementation work, `Docs updated` must confirm that `tasks.md` was reconciled. Report context and plan separately as updated or unchanged because no trigger occurred.

## 16b. Implementation Outcome & Approved Deviations (2026-08-15)

All four phases are implemented and verified. The following deviations from this plan were made
deliberately during implementation and are the authoritative record:

| # | Plan said | Delivered | Why |
|---|---|---|---|
| 1 | `QuartzSchedulerOptions` | `QuartzSchedulerSettings` | Hard name collision with the public `Quartz.QuartzSchedulerOptions` type. |
| 2 | Dashboard via `Quartz.AspNetCore` | Read-only JSON status endpoint at the same guarded path | `Quartz.AspNetCore` ships no dashboard. The first-party `Quartz.Dashboard` is a Blazor Server + SignalR app that would collide with `Event.Standalone`'s Blazor composition, for a surface disabled by default. |
| 3 | DDL downloaded from the Quartz repository | DDL authored independently from schema interface facts | AGENTS.md Rule #8 / `ip-clean-room` forbid ingesting third-party SQL. Delivered scripts are also **non-destructive** (no `DROP`/`TRUNCATE`), which upstream initialization scripts are not, making startup application safe. |
| 4 | `Event.MigrationService` applies DDL for split deployments | API startup applies DDL in all deployments | Mirrors the TickerQ path being replaced; avoids duplicating embedded resources into a second project. Gated by `Scheduler:Quartz:ApplySchemaOnStartup`. |
| 5 | Trigger retries mirror TickerQ's `[10, 60, 300]` | No scheduler-level retry for one-off reminders | The recurring drain is already the retry authority; a second competing retry policy would double-dispatch reasoning. A failed wake-up leaves the outbox row due. |
| 6 | Dashboard options `DashboardEnabled` / `DashboardPath` / `DashboardSessionTimeoutMinutes` | `StatusEndpointEnabled` / `StatusEndpointPath` / `StatusEndpointAuthorizationPolicy` | Names now describe what the surface actually is. Session timeout is meaningless for a stateless endpoint. |

Two defects in the pre-existing system were found and fixed as part of this work:

1. `ScheduledJobRegistry` advertised cron expressions (`*/10 * * * * *`) that Quartz rejects — a 6-field
   expression may not set both day-of-month and day-of-week to `*`. Corrected to `*/10 * * * * ?`.
2. `JobDataMap.GetString` throws on a missing key, which would have turned a payload-free trigger into a
   scheduler retry loop. `EventReminderDispatchJob` now probes with `TryGetValue`.

The risk in §17 below is **resolved for SQLite** (executed end-to-end against a real database file, including
restart durability and real job firing) and **structurally asserted** for PostgreSQL, SQL Server, and MySQL.

## 17. Potential Risks & Unknowns

The part most likely to require iteration is the **DDL script embedding and multi-provider schema initialization** (Task 1.4). Quartz.NET ships DDL scripts per database, but the `IF NOT EXISTS` guard approach differs across providers (PostgreSQL uses `CREATE TABLE IF NOT EXISTS`, SQLite supports it natively, SQL Server requires `IF NOT EXISTS (SELECT * FROM sys.objects WHERE ...)`). The schema initializer must be tested against at least SQLite (standalone) and PostgreSQL (split) to be confident. The multi-database support strategy should ideally be coordinated with the existing `dev/active/multi-database-support/` workstream if that work is still active.

A secondary concern is the **Quartz.NET cron expression format**. Quartz uses 6–7 fields (seconds included), while some TickerQ cron expressions may use 6 fields with different semantics. The email-dispatch-drain cron `*/10 * * * * *` (every 10 seconds) must be validated against Quartz's `*/10 * * * * ?` format (day-of-week uses `?` when day-of-month is specified or vice versa).
