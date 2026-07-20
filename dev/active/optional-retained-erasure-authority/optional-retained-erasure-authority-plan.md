<!-- ABOUTME: Implementation plan for making retained location-erasure authority an explicit optional capability. -->
<!-- ABOUTME: Preserves a one-database default while defining EF-managed retained mode, migrations, startup gates, and operator transitions. -->

# Optional Retained Erasure Authority Implementation Plan

**Status:** Proposed; architecture direction approved; implementation in progress; completion tracked per OREA task evidence

**Last Updated:** 2026-07-20 Europe/Brussels

**Primary intents:** `add-ef-migration`, `update-repository-query`, plus a cross-cutting configuration/deployment fallback contract

**Owning workstream:** `dev/active/optional-retained-erasure-authority/`

## 1. Outcome

ISLAMU Event must remain self-hostable for small and large operators from the same codebase.

The default deployment requires one application PostgreSQL database, the API, the Blazor BFF/application, and the migration service. Location erasure remains functional and transactional in that topology. The default does **not** claim that erased physical-location PII cannot reappear when an operator restores an application backup taken before the erasure.

Operators that need protection against that restore scenario can explicitly enable a second, independently retained PostgreSQL database. That database becomes the retained erasure authority: it stores only immutable PII-free erasure facts and is replayed over a restored application database before traffic or hosted workers start.

The optional database must follow repository conventions:

- a dedicated EF Core `LocationPrivacyAuthorityDbContext` in `Explore.Persistence`;
- dedicated EF repositories and entity configurations;
- migrations generated with `dotnet ef migrations add` and applied by `Event.MigrationService`;
- a named .NET connection string supplied through environment variables, user secrets, or the configured secret provider;
- no runtime schema bootstrap from an embedded SQL file;
- local Aspire provisioning only in `local-full`.

## 2. Issue And Root Cause

### 2.1 Baseline issue at plan approval

At plan approval, the retained-only implementation treated the retained database as mandatory for every installation:

- `InfrastructureServicesRegistration.ConfigureInfrastructureServices` always registers `PostgreSqlLocationPrivacyErasureAuthority`.
- `Explore.API/Program.cs` always executes `LocationPrivacyStartupGate` outside tests and OpenAPI generation.
- `PostgreSqlLocationPrivacyErasureAuthority` throws when `LocationPrivacy:ErasureAuthority:ConnectionString` is missing.
- `docs/OPERATIONS.md` describes that connection string and replay gate as required.

At that baseline, `src/Explore.AppHost/AppHost.cs` did not provision or inject an authority database in any profile, and `Event.MigrationService` knew only `ExploreDbContext` and `DataProtectionKeyContext`. These statements explain the approved correction; after implementation work begins, current status must come from the owning OREA task evidence rather than this historical baseline.

### 2.2 Why the decision happened

The Event Location Privacy workstream optimized first for the strongest disaster-recovery property: an older application backup must not be able to make erased Home PII live again. That property genuinely requires a ledger outside the application restore set. A table in the application database—or a second database on the same restored cluster/volume—cannot provide it.

The mistake was not the existence of retained authority. The mistake was promoting the strongest deployment topology into a universal application invariant instead of an explicit operator-selected capability. That led directly to unconditional dependency injection, unconditional startup replay, and a non-EF schema path.

### 2.3 Convention mismatch at plan approval

At plan approval, the authority implementation also bypassed normal persistence ownership:

- `src/Explore.Infrastructure/Privacy/ErasureAuthority/PostgreSqlLocationPrivacyErasureAuthority.cs` owned a raw `NpgsqlDataSource`.
- `LocationPrivacyErasureAuthoritySchema.sql` was an embedded runtime provisioning resource.
- the authority had no DbContext, design-time factory, EF migration history, or migration-service step.

That baseline justified the approved correction: keep the security semantics while bringing storage and schema lifecycle back under `Explore.Persistence` and EF Core. This subsection does not claim downstream completion; current implementation status comes only from the owning OREA task evidence.

## 3. Source Ownership And Supersession

This focused plan owns the deployment-mode correction for retained erasure authority. It does not replace the broader Event Location Privacy disclosure, authorization, API, calendar, AI, federation, or discovery work.

The broader Event Location Privacy planning sources now defer deployment-mode, persistence, migration, startup, and provisioning ownership to this plan:

- `dev/active/event-location-privacy/event-location-privacy-plan.md`;
- `dev/active/event-location-privacy/event-location-privacy-context.md`;
- `dev/active/event-location-privacy/event-location-privacy-tasks.md`;
- `.omo/plans/event-location-privacy.md`.

OREA-000 completed that planning re-baseline. Canonical operator docs, including `docs/OPERATIONS.md` and `docs/BACKUP_RESTORE_UPGRADE.md`, remain intentionally unchanged until OREA-320 updates them against implemented behavior. Work on the authority topology must not continue concurrently in both workstreams.

## 4. Deployment Modes And Defaults

| Concern | `ApplicationDatabase` | `RetainedAuthority` |
|---|---|---|
| Default | Yes | No; explicit opt-in |
| PostgreSQL databases | One application database | Application database plus independent authority database |
| Normal erasure | Atomic local ledger, PII erasure, audit, checkpoint, and outbox transaction | Authority append first; application mirror, PII erasure, audit, checkpoint, and outbox transaction second |
| Live-request safety | Full transactional erasure | Full transactional erasure plus replayable retained intent |
| Protection from pre-erasure app backup restore | Not guaranteed | Protected within the documented restore boundary when the authority has an independent retention/restore set and replay succeeds |
| Startup replay | Not required; no authority service is resolved | Mandatory before host start |
| Missing authority configuration | Irrelevant; application starts | Configuration error; startup fails closed |
| Authority outage | Irrelevant | Startup and new erasures fail closed; never fall back |
| Health posture | Healthy selected mode; `restoreReplayProtection=false` | Healthy only after authority validation/replay; `restoreReplayProtection=true` |

`ApplicationDatabase` does not mean erasure is disabled. It means the erasure ledger is stored in the same restore domain as the erased data. UI behavior and public API semantics do not change between modes.

## 5. Configuration Contract

### 5.1 Canonical keys

```text
LocationPrivacy:ErasureDurability:Mode
ConnectionStrings:LocationPrivacyAuthority
```

Environment-variable forms:

```text
LocationPrivacy__ErasureDurability__Mode=ApplicationDatabase
ConnectionStrings__LocationPrivacyAuthority=<secret PostgreSQL connection string>
```

Rules:

1. `LocationPrivacy:ErasureDurability:Mode` defaults to `ApplicationDatabase` when absent.
2. The only accepted values are `ApplicationDatabase` and `RetainedAuthority`, matched case-insensitively but normalized in diagnostics.
3. A connection string never activates retained mode by itself.
4. `RetainedAuthority` requires `ConnectionStrings:LocationPrivacyAuthority` in the API and migration-service process.
5. `ApplicationDatabase` neither validates nor opens the authority connection.
6. Mode selection is startup/deployment configuration through `IOptions`, not a hot-reload setting. A mode change requires a controlled restart.
7. The connection string is deployment-managed. It is never stored in governance tables, returned by an API, sent to Blazor, logged, traced, included in health data, or exposed in Aspire diagnostics as plain configuration.
8. The old `LocationPrivacy:ErasureAuthority:ConnectionString` key is removed from runtime authority. Retained mode with only the old key must fail with bounded migration guidance; it must not silently activate or silently fall back.

The same canonical connection-string name is used by both processes. Operators may give `Event.MigrationService` a migration-owner credential and the API an execute/read-only runtime credential by supplying different secret values to each process.

## 6. Target Architecture

### 6.1 Local application ledger in both modes

The application database always contains a PII-free erasure ledger and monotonic counter. This is not extra infrastructure; it is part of `ExploreDbContext`.

The local ledger solves three problems:

- default mode retains an auditable, idempotent sequence without requiring another database;
- the existing correction-outbox contract keeps a positive intent sequence in both modes;
- transitions between modes can synchronize exact facts instead of declaring all historical erasures unprotected.

In retained mode, the application ledger is a mirror of the external authority for every applied intent. In default mode, it is the only ledger and therefore shares the application's backup/restore fate.

### 6.2 Application-database flow

1. Validate the deletion/erasure request and discover the owner-bounded Home set.
2. Create one UUIDv7 PII-free erasure intent.
3. Inside one `ExploreDbContext` serializable transaction:
   - append the intent to the local erasure ledger and allocate the next sequence;
   - erase Home PII and tombstone dependent room/label state;
   - update EventLocation policy/audit state;
   - erase user/actor PII and authentication tokens;
   - append the local checkpoint;
   - append PII-free correction outbox rows.
4. Commit once.
5. Invalidate affected cache tags after commit.

If any database operation fails, both the ledger append and erasure roll back. There is no startup replay from an independent source after a backup restore, and documentation must say so plainly.

### 6.3 Retained-authority flow

1. Validate the request and construct the same PII-free intent.
2. Append it to `LocationPrivacyAuthorityDbContext` in an independent transaction.
3. Inside one `ExploreDbContext` serializable transaction:
   - mirror the exact authority fact into the local ledger;
   - apply the erasure mutations;
   - append the checkpoint;
   - append correction outbox rows.
4. Commit and invalidate caches.

There is intentionally no distributed transaction. If step 2 succeeds and step 3 fails, the retained fact remains pending and startup/runtime replay applies it later. Retained-mode failures never invoke the application-database workflow as a fallback.

### 6.4 Shared application logic, explicit workflows

Refactor the current `GlobalLocationPrivacyErasureService` so storage-mode coordination and application mutations are separate:

- a shared application erasure applier owns entity mutation, audits, checkpoints, and outbox creation but does not open a transaction itself;
- `ApplicationDatabaseLocationPrivacyErasureWorkflow` owns the single application transaction;
- `RetainedAuthorityLocationPrivacyErasureWorkflow` owns authority-first append and replay;
- `IGlobalLocationPrivacyErasureService` remains the handler-facing boundary;
- composition selects one workflow at startup without constructing the retained workflow in default mode.

Do not add a fake/no-op authority implementation. Default mode is a real workflow with a real local ledger, not an authority client that pretends to succeed.

### 6.5 Layer ownership

| Layer | Ownership |
|---|---|
| Domain | Mode-neutral PII-free erasure fact/counter invariants; no connection or deployment knowledge |
| Application | Erasure workflow contracts, shared applier, transaction ordering, replay rules, and outbox payloads |
| Persistence | Both ledger repositories, `LocationPrivacyAuthorityDbContext`, entity configurations, factories, and migrations |
| Infrastructure | No authority schema ownership; retain only adapters that genuinely belong to external/runtime infrastructure |
| API | Options validation, workflow selection, conditional startup gate, and bounded health result |
| Migration service | Conditional authority migration and mode-transition synchronization |
| AppHost | Profile-owned resource graph and secret/reference injection |
| Blazor | No authority configuration, connection, mode selection, or local authorization logic |

## 7. EF Core Persistence Design

### 7.1 New context and repositories

Add a focused persistence area under `src/Explore.Persistence/Privacy/ErasureAuthority/`:

- `LocationPrivacyAuthorityDbContext.cs` — applies only authority configurations;
- `LocationPrivacyAuthorityDbContextFactory.cs` — design-time factory using `ConnectionStrings:LocationPrivacyAuthority`;
- `Configurations/LocationPrivacyErasureAuthorityIntentConfiguration.cs`;
- `Configurations/LocationPrivacyErasureAuthorityCounterConfiguration.cs`;
- `Repositories/EfCoreLocationPrivacyErasureAuthorityRepository.cs`;
- `Repositories/ApplicationDatabaseLocationPrivacyErasureLedgerRepository.cs`.

The repositories return domain entities and expose append/read operations only. No DTO or `IQueryable` crosses the boundary.

`ExploreDbContext` also maps the ledger entity/counter so default mode can append within the existing unit of work. `LocationPrivacyAuthorityDbContext` maps the same data contract in its own database. The external context must not scan and accidentally include the full application model.

Both contexts retain the existing `location_privacy_authority` schema and table names. In default mode that schema lives inside the single application database; in retained mode the same schema contract also exists in the independent authority database. Reusing the physical contract makes raw-schema adoption and exact ledger synchronization testable without inventing a second representation.

### 7.2 Schema invariants

Preserve the current useful authority properties:

- UUIDv7 RFC-variant intent IDs;
- non-empty opaque owner/location IDs;
- normalized distinct ordered location IDs;
- closed erasure-reason values;
- positive monotonic sequences with transactional allocation;
- idempotent duplicate append only when the normalized payload matches;
- mismatched duplicate rejection;
- server-owned UTC recording time;
- bounded ordered reads;
- no update/delete repository surface;
- database-enforced mutation rejection for retained facts;
- no address, label, room name, coordinates, postcode, free text, credentials, or other PII.

Fixed provider SQL that EF cannot model—such as the mutation-rejection trigger, security-definer functions, fixed `search_path`, and role grants—must live inside the generated EF migration lifecycle and be explicitly reviewed. It must not remain an embedded resource executed by application or test startup.

### 7.3 Migration ownership

Generate two migrations from the repository root; do not hand-create migration classes or snapshots:

```bash
dotnet ef migrations add AddApplicationDatabaseLocationPrivacyErasureLedger \
  --context ExploreDbContext \
  --project src/Explore.Persistence/Explore.Persistence.csproj \
  --startup-project src/Explore.API/Explore.API.csproj

dotnet ef migrations add InitialLocationPrivacyAuthority \
  --context LocationPrivacyAuthorityDbContext \
  --project src/Explore.Persistence/Explore.Persistence.csproj \
  --startup-project src/Explore.API/Explore.API.csproj \
  --output-dir Migrations/LocationPrivacyAuthority
```

The authority migration must support both a new empty database and non-destructive adoption of the current raw `location_privacy_authority` schema. Existing facts, counter position, functions, and permissions cannot be dropped or renumbered.

Both `Down()` paths must abort when erasure facts exist. Development rollback may remove an unused empty ledger, but no rollback may silently destroy retained or local erasure evidence.

The current worktree contains an in-progress replacement of the main migration chain and snapshot. Implementation must not generate either migration until that shared chain is reconciled and owned by one migration task.

## 8. Migration Service And Mode Transitions

### 8.1 Startup migration behavior

`Event.MigrationService` always migrates the application and Data Protection contexts. It registers and migrates `LocationPrivacyAuthorityDbContext` only when `RetainedAuthority` is selected.

In default mode:

- no authority DbContext is registered;
- no authority connection string is read or validated;
- the worker completes normally with only the application database.

In retained mode:

- the authority database is migrated before API startup;
- schema/adoption failure stops the migration service with exit code 1;
- the API waits for migration completion and then runs retained replay before host start.

The API may keep its existing application-database migration fallback for direct execution, but it must never use the runtime authority credential to apply authority migrations. Retained deployments run `Event.MigrationService` or an explicit `dotnet ef database update --context LocationPrivacyAuthorityDbContext` with a migration credential first.

### 8.2 Upgrade from default to retained

Mode activation is a controlled synchronization, not merely adding a connection string:

1. Back up the application database.
2. Provision an independent PostgreSQL database and independent backup/retention policy.
3. Run `Event.MigrationService` with mode `RetainedAuthority` and the authority migration credential.
4. Compare the application ledger, application checkpoint chain, and retained ledger by sequence, intent ID, and normalized payload.
5. For erasures produced by corrected default mode, append any application-ledger suffix to the retained database in the same order.
6. For a pre-correction retained deployment whose external facts and verified checkpoints predate the new local mirror, import only the authority prefix already proven applied by the checkpoint chain into the local ledger; do not replay that prefix or duplicate its outbox rows.
7. Leave any authority suffix beyond the verified application checkpoint for normal API replay.
8. Reject any divergence, fork, or gap; never overwrite or renumber.
9. Start the API; retained replay applies every authority fact beyond the verified application prefix before traffic.

Because corrected default mode writes the local ledger and checkpoint atomically with erasure, its historical erasures can be promoted rather than treated as future-only protection. Existing retained installations are also migrated without re-emitting already-applied corrections: the external authority plus verified checkpoint chain establishes the safe local-mirror prefix.

### 8.3 Downgrade from retained to default

1. Keep retained mode enabled until the authority watermark, local mirror, and application checkpoint match.
2. Back up both databases.
3. Change mode to `ApplicationDatabase` and restart.
4. Retain the authority database and its backups; do not delete it automatically.
5. Document that future restores no longer receive independent replay protection.

There is no automatic fallback or auto-downgrade during an outage.

## 9. API Startup And Health

`LocationPrivacyStartupGate` becomes mode-aware before resolving `ILocationErasureReplayService`:

- `ApplicationDatabase`: return without resolving any authority service or opening any authority connection;
- `RetainedAuthority`: verify the current checkpoint against the external authority, replay every later sequence, revalidate cache/correction state, and fail before host start on any error.

Add a bounded readiness check named `location-privacy-erasure-durability`:

- default mode: `Healthy`, with only normalized mode and `restoreReplayProtection=false`;
- retained mode after successful validation: `Healthy`, with normalized mode and `restoreReplayProtection=true`;
- retained mode failure: `Unhealthy`, and startup remains blocked where the failure occurs before host start.

Health output must never include connection strings, hosts, database names, role names, watermarks, opaque IDs, raw provider exceptions, or retained counts.

## 10. Aspire And Self-Hosting Topology

### 10.1 Aspire profiles

| Profile | Authority resource | Effective mode |
|---|---|---|
| `local-full` | Separate PostgreSQL server/container, separate named volume, authority database | `RetainedAuthority` |
| `local-default` | None | `ApplicationDatabase` |
| `local-core` | None | `ApplicationDatabase` |
| `local-lite` | None | `ApplicationDatabase` |

`local-full` must use a separate PostgreSQL server resource and volume, not a second database inside the application `postgres` resource. Otherwise resetting/restoring the application server could also reset the authority and defeat the feature being exercised.

AppHost injects `ConnectionStrings:LocationPrivacyAuthority` only into `Event.MigrationService` and `Explore.API`, sets the retained mode for those two projects, and adds the required wait edges. It never sends the connection to `Explore.Blazor`.

All other Aspire profiles explicitly select `ApplicationDatabase`, create no authority resource, create no authority volume, and add no authority reference/wait edge.

### 10.2 Non-Aspire deployments

Outside Aspire, default Compose/direct deployments need no authority variables. A retained deployment supplies the explicit mode and named connection string through per-process environment/secrets and runs the migration service before the API.

The implementation must add a container path for `Event.MigrationService` to the repository Compose topology so the self-hosting contract matches the stated required components. The optional authority database itself may be operator-managed; only `local-full` is required to auto-provision it in this workstream.

## 11. Documentation Contract

Update implementation-facing and operator-facing sources in the same change:

- `docs/CONFIGURATION.md` — canonical mode, default, valid values, and named connection string;
- `docs/SECRETS.md` — deployment-managed secret ownership and per-process credential separation;
- `docs/OPERATIONS.md` — conditional startup gate, health behavior, and Aspire matrix;
- `docs/SELF_HOSTING.md` — one-database default and retained opt-in;
- `docs/BACKUP_RESTORE_UPGRADE.md` — separate procedures for both modes, promotion/downgrade, and honest default limitation;
- `docs/TROUBLESHOOTING.md` — invalid mode, missing secret, failed migration, replay divergence, and downgrade guidance;
- `docs/DEPLOYMENT_TIERS.md` — retained authority as an optional higher-assurance capability, not a different codebase;
- `docs/RELEASE_CHECKLIST.md` — mode/config/migration/backup impact;
- `schemas/islamu-event.md` — application-local ledger and separate authority schema ownership;
- the broader Event Location Privacy plan/context/tasks and `.omo` plan — replace mandatory-topology claims.

Documentation must never call `ApplicationDatabase` “disabled” or imply that ordinary erasure is unavailable. It must state exactly which restore guarantee is absent.

## 12. Constraints And Non-Goals

### Required constraints

- One application database is the default and must start with no authority connection string.
- Retained mode is explicit and fail closed.
- No fallback from retained mode to default mode.
- No activation based only on secret presence.
- No runtime/hot switching.
- No distributed transaction across databases.
- No authority secret in Blazor, API responses, health, logs, traces, screenshots, or support bundles.
- No PII in either local or retained erasure ledger.
- No raw runtime schema bootstrap.
- No hand-created EF migration/snapshot files.
- Repositories return entities, not DTOs.
- Provider-specific SQL is fixed, parameterized where applicable, migration-owned, and reviewed.
- Retained protection requires an independent restore set; a second database on the same restored volume is not sufficient.

### Non-goals

- Redesigning EventLocation disclosure policy, HAL, calendar, AI, federation, or discovery.
- Making every existing Compose dependency optional in this workstream.
- Adding a database/provider abstraction for non-PostgreSQL authorities.
- Exposing a UI switch for erasure durability.
- Automatically deleting or pruning retained authority facts.
- Promising deletion from historical backups in default mode.

## 13. Delivery Phases

### Phase 1 — Pin Behavior And Introduce Explicit Workflows

1. Add characterization tests for current erasure mutation, checkpoint, correction outbox, ambiguous append retry, and fail-closed retained behavior.
2. Add failing tests proving missing configuration starts in `ApplicationDatabase`, a stray connection string does not activate retained mode, and retained mode without a connection fails validation.
3. Add the mode/options contract and startup-only selection.
4. Extract the shared application erasure applier and the two explicit workflows.
5. Re-baseline the broader Event Location Privacy planning statements in the same slice.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

### Phase 2 — Move Authority Storage Into EF Core

1. Add the local ledger/counter to `ExploreDbContext` and application repository.
2. Add `LocationPrivacyAuthorityDbContext`, explicit configurations, design-time factory, and retained repository.
3. Generate the two EF migrations with the commands in Section 7.3.
4. Adopt existing raw-schema data without loss and move non-model PostgreSQL invariants into migration-owned SQL.
5. Remove the embedded schema resource and direct Infrastructure `NpgsqlDataSource` client only after EF integration tests are green.
6. Prove atomic local append+erasure, retained idempotency/concurrency, append-only enforcement, PII-free shape, guarded rollback, and context-specific model parity.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

### Phase 3 — Make Migration, Startup, And Health Conditional

1. Register/migrate the authority context only in retained mode.
2. Add ledger synchronization and divergence checks for mode promotion.
3. Make API workflow registration and startup replay conditional without resolving retained services in default mode.
4. Add bounded durability health reporting.
5. Add the migration-service container path and make Compose API startup wait for successful migrations.
6. Prove default startup with one database, retained fail-closed startup, migration failure propagation, promotion-prefix handling, cancellation, and secret redaction.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

### Phase 4 — Wire `local-full` And Close Operator Contracts

1. Add the separate authority PostgreSQL server/database/volume only in `FullLocal`.
2. Inject the mode and connection only into migration service and API; update PgAdmin inventory without copying credentials.
3. Prove `local-default`, `local-core`, and `local-lite` have no authority resource/reference and remain `ApplicationDatabase`.
4. Update every document in Section 11, including promotion/downgrade and the default backup disclaimer.
5. Add architecture guards preventing unconditional authority registration, Blazor secret flow, embedded schema SQL, and non-full Aspire provisioning.

Phase gate:

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## 14. Acceptance Criteria

- With no erasure-durability configuration and no authority secret, API/migration/Blazor use only the application database and start normally.
- Default-mode account erasure atomically writes the local ledger/checkpoint, removes PII, writes audits, and queues PII-free corrections.
- A configured authority connection without explicit retained mode is never opened.
- Retained mode cannot start with a missing/unreachable/unmigrated/divergent authority.
- Retained append success followed by application failure remains replayable and never falls back.
- Restoring an older application database in retained mode replays missing facts before traffic/workers.
- `local-full` contains a separate authority Postgres resource/volume; the other three named profiles contain none.
- Blazor has no authority connection or durability-selection code.
- The authority has a dedicated DbContext, factory, configurations, repositories, snapshot, and migration history.
- The embedded runtime schema resource and raw Infrastructure authority client are removed.
- Migration `Down()` cannot destroy nonempty erasure evidence.
- Upgrade/downgrade workflows detect divergence and never renumber or overwrite facts.
- Health and logs expose only bounded mode/capability data.
- Operator docs distinguish transactional erasure from backup-resurrection protection without overstating either mode.

## 15. Risks And Mitigations

| Risk | Mitigation |
|---|---|
| Main migration chain is currently being rewritten in the shared worktree | Do not generate migrations until that lane is reconciled; use a task-owned worktree and one migration owner |
| Existing raw authority database has facts but no EF history | Test a non-destructive adoption path and schema signature before removing the embedded bootstrap |
| Local and retained ledgers diverge during mode transition | Compare exact ordered facts; stop on first mismatch; never overwrite, delete, or renumber |
| Same-cluster “separate DB” creates a false guarantee | Require independent restore domain in docs and use a separate server/volume in `local-full` |
| DI accidentally constructs retained services in default mode | Conditional composition tests assert no context/client resolution and no connection attempt |
| Retained outage silently weakens privacy | No fallback; startup/new erasure fail closed until operator repair |
| Runtime role can mutate authority tables | Migration-owned permissions plus update/delete trigger; integration tests use the runtime credential |
| Mode name is misunderstood as erasure on/off | Use `ApplicationDatabase`/`RetainedAuthority`; never use `Disabled` |
| Promotion checkpoints hide unapplied facts | Local ledger append, application mutation, checkpoint, and outbox share one transaction; transition validates the prefix |

## 16. Definition Of Done

The work is complete when the default topology is verifiably one-database and healthy, retained mode is explicitly opt-in and independently replayable, both schemas are EF-managed, all four phase gates pass, planning/operator sources agree, and no runtime path can silently trade retained guarantees for availability.
