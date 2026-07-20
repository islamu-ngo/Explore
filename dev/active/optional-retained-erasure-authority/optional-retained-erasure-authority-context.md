<!-- ABOUTME: Working context for the optional retained erasure-authority correction. -->
<!-- ABOUTME: Records verified source anchors, approved decisions, repository constraints, and implementation hazards. -->

# Optional Retained Erasure Authority Context

**Status:** Planning complete; implementation not started

**Last Updated:** 2026-07-20 Europe/Brussels

**Canonical plan:** `dev/active/optional-retained-erasure-authority/optional-retained-erasure-authority-plan.md`

## 1. Approved Product Direction

- ISLAMU Event targets small and large self-hosted operators from one codebase.
- Default required topology is one application PostgreSQL database, API, Blazor, and migration service.
- Location erasure remains enabled and transactional by default.
- Default mode does not guarantee protection against restoring a pre-erasure application backup.
- A second PostgreSQL database is an explicit optional capability for operators requiring restore replay protection.
- `local-full` auto-provisions that database; `local-default`, `local-core`, and `local-lite` do not.
- Production/non-Aspire retained deployments provide the mode and named connection string through environment/secrets.
- The optional database must use a dedicated EF Core DbContext, repositories, entity configurations, and generated migrations.

## 2. Intent And Rule Routing

### Intent classification

| Intent | Why it applies | Required tests/docs |
|---|---|---|
| `add-ef-migration` | Main application ledger migration plus dedicated authority-context migration | `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests`, `schemas/islamu-event.md` |
| `update-repository-query` | Replaces raw Npgsql authority access with entity-returning EF repositories | `Event.Persistence.IntegrationTests`, `Event.Architecture.Tests` |
| Cross-cutting fallback | Current intent registry has no exact internal optional-DbContext/deployment-mode intent | Configuration, secrets, operations, self-hosting, backup/restore, Aspire, API startup, migration service |

`external-infrastructure-bootstrap` was inspected but is not selected as the primary contract: it is designed for setup-time external provider onboarding and UI/auth flows, while this capability is deployment-time server configuration with no browser workflow.

### Loaded rules and skills

- `implementation-plan`
- `dotnet-efcore-guidelines`
- `clean-architecture-rules`
- `aspire`
- `.claude/rules/efcore-migrations.md`
- `.claude/rules/efcore-persistence.md`
- `.claude/rules/application-layer.md`
- `.claude/rules/domain.md`
- `.claude/rules/tests.md`

### Hard project constraints

- Repositories return entities, never DTOs or `IQueryable`.
- Use EF migrations and synchronize snapshots; do not hand-create migration files.
- Never add destructive rollback that silently loses evidence.
- Provider-specific raw SQL is allowed only where EF cannot express the operation; it must be fixed, migration-owned, parameterized where applicable, and tested.
- No tenant-filter bypass expansion is required for the authority context.
- Every new file starts with two `ABOUTME:` lines.
- Planning edits stay under `dev/active/`; runtime code is not changed during this planning turn.

## 3. Verified Current State

### Current storage path

| Source | Verified behavior |
|---|---|
| `src/Explore.Infrastructure/Privacy/ErasureAuthority/LocationPrivacyErasureAuthorityOptions.cs` | Binds `LocationPrivacy:ErasureAuthority:ConnectionString`; comments explicitly keep storage outside EF lifecycle |
| `PostgreSqlLocationPrivacyErasureAuthority.cs` | Creates a raw `NpgsqlDataSource`, throws on missing connection, calls security-definer append/read functions |
| `LocationPrivacyErasureAuthoritySchema.sql` | Creates roles, schema, tables, counter, trigger, functions, and grants as an embedded runtime resource |
| `LocationPrivacyErasureAuthoritySchema.cs` | Reads the embedded SQL resource |
| `src/Explore.Infrastructure/Explore.Infrastructure.csproj` | Embeds the authority SQL file |

### Current composition and startup

| Source | Verified behavior |
|---|---|
| `InfrastructureServicesRegistration.ConfigureInfrastructureServices` | Always binds authority options and registers `ILocationPrivacyErasureAuthority` |
| `Explore.API/Program.cs` | Runs migrations/seeding, then unconditionally invokes the retained-authority startup gate outside Testing/OpenAPI generation; no deployment-mode validation exists yet |
| `Explore.API/BackgroundServices/LocationPrivacyStartupGate.cs` | Unconditionally resolves replay and blocks host start on any authority/replay failure; the approved `ApplicationDatabase` branch is not implemented yet |
| `Event.MigrationService/Program.cs` | Registers only `ExploreDbContext` and `DataProtectionKeyContext` |
| `Event.MigrationService/Worker.cs` | Migrates app DB, constraints, Data Protection, and seed data; no authority context |
| `Explore.AppHost/AppHost.cs` | Provisions only the application Postgres resource for local-data modes; no authority database or connection injection |

This mismatch explains the observed startup failure: the API requires a service/resource that AppHost and the migration worker do not supply.

### Current application transaction

`GlobalLocationPrivacyErasureService` currently:

1. loads the user and owner-bounded Private Homes;
2. appends a PII-free intent to the external authority;
3. replays retained facts;
4. applies Home/user/actor erasure, EventLocation audits, checkpoint, and correction outbox rows inside `IUnitOfWork.ExecuteSerializableAsync`;
5. invalidates cache tags after commit.

Useful behavior to preserve:

- UUIDv7 intent idempotency;
- authority-first ordering in retained mode;
- per-intent application atomicity;
- PII-free correction payloads;
- contiguous checkpoints;
- ambiguous acknowledgement retry;
- fail-closed startup before hosted services/Kestrel.

Behavior that must change:

- external authority cannot be mandatory;
- default mode needs a real local-ledger transaction rather than a fake authority;
- external storage/schema ownership moves from Infrastructure/raw SQL to Persistence/EF;
- startup replay must branch on explicit mode before resolving retained dependencies.

## 4. Verified Repository Patterns To Reuse

| Pattern | Source anchor | Use in this workstream |
|---|---|---|
| Pooled main context and scoped wrapper | `Explore.Persistence/PersistenceServicesRegistration.cs` | Keep application erasure inside existing `ExploreDbContext`/unit of work |
| Dedicated EF context | `DataProtectionKeyContext.cs` | Model authority as a narrow context rather than another full application context |
| Design-time factory | `ExploreDbContextFactory.cs`, `DataProtectionKeyContextFactory.cs` | Add authority factory for `dotnet ef` tooling |
| Explicit entity configuration | `LocationPrivacyErasureReplayCheckpointConfiguration.cs` | Map constraints/indexes in configuration classes |
| Entity-returning repository | `LocationPrivacyErasureReplayCheckpointRepository.cs`, `GlobalLocationPrivacyErasureRepository.cs` | Keep persistence contracts in Application and implementations in Persistence |
| Conditional optional health | existing API/Infrastructure health checks | Report selected mode safely without making default mode unhealthy |
| Aspire profile branching | `AspireRunMode`, `UsesLocalData`, `includeHeavyExtras` | Create retained resource only for `FullLocal` |
| Migration-before-API dependency | `AppHost.cs` migration project and `WaitForCompletion` | Migrate authority before retained API startup |

## 5. Existing Tests To Pin Or Move

| Test source | Current responsibility | Planned treatment |
|---|---|---|
| `tests/Event.Application.UnitTests/Features/Users/Commands/GlobalLocationPrivacyErasurePendingSpecs.cs` | Authority-first command behavior and pending replay | Pin, then split expectations by mode |
| `tests/Event.Application.UnitTests/Services/GlobalLocationPrivacyReplayCacheGateTests.cs` | Replay/checkpoint/cache behavior | Preserve retained-mode semantics |
| `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` | Real PostgreSQL application erasure | Extend for local-ledger atomicity and retained mirroring |
| `tests/Explore.Infrastructure.Tests/Infrastructure/Privacy/LocationPrivacyErasureAuthorityIntegrationTests.cs` | Raw schema/client idempotency, concurrency, permissions, restore isolation | Move persistence-owned coverage to `Event.Persistence.IntegrationTests` before deleting raw client/schema |
| `tests/Event.API.IntegrationTests/Privacy/LocationPrivacyStartupGateTests.cs` | Retained failure blocks hosted workers | Add default-mode no-resolution/startup cases |
| `tests/Event.Architecture.Tests/LocationPrivacyStartupGateArchitectureTests.cs` | Gate runs before host start | Make assertion conditional-mode aware |
| `tests/Event.Architecture.Tests/AspireLocalInfrastructureArchitectureTests.cs` | AppHost profile/resource graph | Add full-only authority resource/reference guards |

## 6. Current Documentation Conflict

- `docs/OPERATIONS.md` says `LocationPrivacy:ErasureAuthority:ConnectionString` is required and the replay gate is universal.
- `docs/BACKUP_RESTORE_UPGRADE.md` lists the authority database among assets that must always be backed up/restored.
- the active Event Location Privacy plan and `.omo` plan previously specified a separate database and universal fail-closed replay as mandatory; their topology statements are now re-baselined to this workstream.
- `docs/CONFIGURATION.md` does not list an erasure durability mode or canonical named authority connection.
- `docs/SECRETS.md` does not map authority secret ownership.
- `local-full` currently lacks the resource despite those requirements.

All must be reconciled in the implementation, not left as competing operator instructions.

## 7. Canonical Decisions

1. Mode names are `ApplicationDatabase` and `RetainedAuthority`; there is no `Disabled` mode.
2. Missing mode means `ApplicationDatabase`.
3. Secret presence never changes the mode.
4. The canonical secret key is `ConnectionStrings:LocationPrivacyAuthority`.
5. The old nested connection-string option is removed from authority.
6. The API and migration service may receive different credentials under the same connection-string name.
7. Default mode writes a local PII-free ledger and checkpoint in the same transaction as erasure/outbox.
8. Retained mode writes external authority first, then mirrors the fact and applies it in the application transaction.
9. No two-phase/distributed transaction is introduced.
10. Retained startup failure never falls back to application-only behavior.
11. The mode is selected at process startup and requires restart to change.
12. `local-full` uses a separate Postgres server/container and volume; other Aspire profiles provision none.
13. Blazor never receives authority configuration.
14. Health output contains only normalized mode and a restore-protection boolean.
15. Existing raw authority schema must be adopted non-destructively if present.
16. Provider SQL belongs to EF migrations, not an embedded runtime schema loader.
17. Retained facts are never automatically deleted, including after downgrade.

## 8. Transition Invariants

### Default to retained

- Application ledger is the historical source for erasures performed in default mode.
- Migration synchronization compares the ordered application ledger, application checkpoint chain, and retained ledger by sequence, intent ID, owner ID, normalized location IDs, and reason.
- An application-only suffix may be appended to an empty/matching retained ledger.
- On a pre-correction retained installation, an external prefix already proven applied by the checkpoint chain may be imported into the new local mirror without replaying it or duplicating outbox rows.
- An external suffix beyond the verified application checkpoint is left for API replay.
- Any same-sequence mismatch or gap blocks activation.
- For facts produced after this correction, application checkpoints must match the local ledger because both are committed atomically; the pre-correction exception is handled only through the verified checkpoint-prefix import above.

### Retained to default

- Operators first run retained mode until authority, local mirror, and checkpoint watermarks agree.
- The authority database is retained after switching.
- Default startup does not contact it.
- The operator explicitly accepts loss of independent restore replay for future restores/erasures.

## 9. Worktree And Sequencing Hazard

The shared worktree is heavily dirty from the broader Event Location Privacy and other workstreams. In particular, many historical main-context migrations are deleted and a replacement `init`/snapshot is present in the working tree.

Consequences:

- this planning turn creates only the three files in this directory;
- implementation must use a task-owned worktree or otherwise obtain exclusive migration ownership;
- no new main-context or authority-context migration is generated until the existing migration/snapshot state is reconciled;
- unrelated dirty documentation/code changes must be preserved, not reset or overwritten.

## 10. Verification Contract

Each implementation phase ends with one Release build and one fastest relevant project test command, as specified in the plan. Do not use solution-level `dotnet test`.

Required observable proofs include:

- default host composition succeeds without an authority secret and never resolves the authority context;
- retained composition rejects missing configuration;
- one-database erasure is atomic across local ledger, checkpoint, mutation, and outbox;
- retained failure after external append remains replayable;
- generated migrations cover new and legacy raw-schema databases without data loss;
- profile graph contains an authority resource only in `local-full`;
- health/log payloads contain no secret/provider details.

No application, Aspire, Docker, or browser process was started during planning. No runtime test or build result is claimed by these documents.

## 11. Open Risks, Not Open Architecture Decisions

- It is unknown whether any external operator already has a populated raw authority database. The plan therefore requires a safe adoption path rather than assuming none exists.
- The current shared migration history is unstable and is a hard sequencing dependency for implementation.
- A second database in the same production cluster may still share a restore failure domain. Documentation must not advertise full protection unless retention/restore is independent.
- Compose currently lacks an `Event.MigrationService` container. The implementation includes that topology correction because the approved minimal contract names the migration service as required.

The approved default/mode/profile decisions themselves are not open questions.
