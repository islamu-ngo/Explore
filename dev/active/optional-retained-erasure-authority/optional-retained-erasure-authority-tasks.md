<!-- ABOUTME: Executable task ledger for the optional retained erasure-authority correction. -->
<!-- ABOUTME: Sequences failing-first proofs, EF persistence, conditional startup, Aspire wiring, and operator documentation. -->

# Optional Retained Erasure Authority Tasks

**Status:** Not started

**Last Updated:** 2026-07-20 Europe/Brussels

**Current focus:** `OREA-000`

**Plan:** `optional-retained-erasure-authority-plan.md`

**Context:** `optional-retained-erasure-authority-context.md`

## Execution Rules

- Follow PIN → RED → GREEN for every existing behavior changed by this workstream.
- Do not generate migrations until the shared main-context migration chain is reconciled and exclusively owned.
- Generate migrations with `dotnet ef migrations add`; do not hand-create migration or snapshot files.
- Do not remove the raw authority implementation until equivalent EF-backed integration evidence is green.
- Do not use a fake/no-op authority to make default mode pass.
- Do not permit silent retained-to-default fallback.
- Keep one top-level implementation task in progress at a time when tasks share `ExploreDbContext`, migrations, `Program.cs`, `Worker.cs`, or `AppHost.cs`.
- Record exact commands, counts, failure observations, and artifact paths beneath each task before checking it complete.

## Phase 1 — Pin Behavior And Introduce Explicit Workflows

- [ ] **OREA-000 — Re-baseline authority topology sources**
  - Paths: `dev/active/event-location-privacy/*`, `.omo/plans/event-location-privacy.md`, and the three documents in this workstream.
  - Deliverable: replace mandatory-topology claims with the two-mode contract and link the focused plan without altering unrelated privacy work.
  - Acceptance: one-database default, explicit retained mode, no fallback, local-full-only provisioning, and EF ownership are consistent across all planning sources.
  - Verify: targeted `rg` finds no remaining statement that every deployment requires an authority connection/database.
  - Evidence: pending.

- [ ] **OREA-010 — PIN current erasure semantics before refactoring**
  - Paths: existing Global Location Privacy erasure tests and `GlobalLocationPrivacyErasureService` behavior.
  - Deliverable: characterization tests for Home/user/actor erasure, policy audit, checkpoint, outbox, cache invalidation, ambiguous append retry, and retained failure ordering.
  - Acceptance: tests pass against unchanged behavior and assert observable state rather than mock call order alone.
  - Verify: focused Application tests recorded before production edits.
  - Evidence: pending.

- [ ] **OREA-020 — RED the deployment-mode contract**
  - Paths: new/updated Application/API options and composition tests.
  - Deliverable: failing tests for absent config => `ApplicationDatabase`, stray connection => no activation, invalid mode => validation failure, retained without connection => failure, and default => no retained dependency resolution.
  - Acceptance: every failure is caused by missing two-mode behavior, not a compile error or pinned constant.
  - Verify: exact failing test names/output recorded.
  - Evidence: pending.

- [ ] **OREA-030 — Add mode/options and startup-only workflow selection**
  - Paths: new Application mode/options contract, `ApplicationServicesRegistration.cs`, `InfrastructureServicesRegistration.cs`, and API composition as required.
  - Deliverable: `ApplicationDatabase` default, `RetainedAuthority` opt-in, canonical `ConnectionStrings:LocationPrivacyAuthority`, strict validation, and conditional construction.
  - Acceptance: connection presence never activates retained mode; default composition does not build an authority context/client; old nested key does not act as authority.
  - Verify: OREA-020 tests green.
  - Evidence: pending.

- [ ] **OREA-040 — Extract shared erasure applier and two real workflows**
  - Paths: `GlobalLocationPrivacyErasureService.cs`, new workflow/applier classes and contracts, `DeleteUserCommandHandler.cs`, outbox/checkpoint tests.
  - Deliverable: shared mutation logic, application-database transaction workflow, retained authority-first workflow, unchanged handler boundary.
  - Acceptance: default path is a real local-ledger workflow; retained append failure never invokes it; no nested transaction; no fake authority.
  - Verify: PIN and RED suites green with mode-specific cases.
  - Evidence: pending.

### Phase 1 Gate

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
```

## Phase 2 — Move Authority Storage Into EF Core

- [ ] **OREA-100 — Add mode-neutral ledger entities and configurations**
  - Paths: `src/Explore.Domain/LocationPrivacyErasureAuthorityIntent.cs`, new counter/state entity, `ExploreDbContext.DbSets.cs`, new authority configuration folder.
  - Deliverable: PII-free ledger/counter mapped into the application database and reusable by the dedicated authority context.
  - Acceptance: UUIDv7, opaque-ID, reason, timestamp, uniqueness, sequence, and no-PII constraints are modelled; authority context applies only its own configurations.
  - Verify: relational model tests prove exact tables/columns/indexes/checks.
  - Evidence: pending.

- [ ] **OREA-110 — Implement application-database ledger repository**
  - Paths: new Application persistence contract and `ApplicationDatabaseLocationPrivacyErasureLedgerRepository`.
  - Deliverable: entity-returning append/read repository that participates in the existing `ExploreDbContext` transaction.
  - Acceptance: normalized duplicate is idempotent, mismatched duplicate rejects, sequence allocation rolls back with failed erasure, and no update/delete surface exists.
  - Verify: real PostgreSQL atomicity/concurrency tests.
  - Evidence: pending.

- [ ] **OREA-120 — Add dedicated authority DbContext, factory, and retained repository**
  - Paths: new files under `src/Explore.Persistence/Privacy/ErasureAuthority/`.
  - Deliverable: `LocationPrivacyAuthorityDbContext`, design-time factory, retained EF repository, conditional DI extension.
  - Acceptance: named connection string only; no full application model; bounded ordered reads; runtime repository exposes append/read only.
  - Verify: context can be created independently and migrations target only authority objects.
  - Evidence: pending.

- [ ] **OREA-130 — Generate main application ledger migration**
  - Blocking dependency: shared `ExploreDbContext` migration/snapshot lane is reconciled.
  - Command: `dotnet ef migrations add AddApplicationDatabaseLocationPrivacyErasureLedger --context ExploreDbContext --project src/Explore.Persistence/Explore.Persistence.csproj --startup-project src/Explore.API/Explore.API.csproj`.
  - Deliverable: generated migration/designer/snapshot for local ledger and counter.
  - Acceptance: additive Up; guarded Down aborts if evidence exists; no unrelated model drift; schema doc updated in the same phase.
  - Verify: main-context pending-model check and real PostgreSQL apply/rollback tests.
  - Evidence: pending.

- [ ] **OREA-140 — Generate dedicated authority migration and adopt the raw schema**
  - Command: `dotnet ef migrations add InitialLocationPrivacyAuthority --context LocationPrivacyAuthorityDbContext --project src/Explore.Persistence/Explore.Persistence.csproj --startup-project src/Explore.API/Explore.API.csproj --output-dir Migrations/LocationPrivacyAuthority`.
  - Deliverable: authority migration/designer/snapshot plus migration-owned provider SQL for trigger/functions/permissions that EF cannot model.
  - Acceptance: fresh DB and current raw-schema-with-data upgrade both succeed; facts/counter remain unchanged; Down refuses nonempty evidence.
  - Verify: authority pending-model check, fresh apply, legacy adoption, repeat apply, and guarded rollback on PostgreSQL.
  - Evidence: pending.

- [ ] **OREA-150 — Remove runtime schema bootstrap and raw authority client**
  - Paths: `src/Explore.Infrastructure/Privacy/ErasureAuthority/*`, `Explore.Infrastructure.csproj`, registration, moved tests.
  - Deliverable: delete embedded SQL loader/resource and direct `NpgsqlDataSource` authority adapter after EF replacements pass.
  - Acceptance: no runtime/test path executes schema DDL; no authority storage implementation remains in Infrastructure; security invariants remain covered.
  - Verify: source scan plus moved Persistence integration suite.
  - Evidence: pending.

### Phase 2 Gate

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## Phase 3 — Make Migration, Startup, And Health Conditional

- [ ] **OREA-200 — Make Event.MigrationService mode-aware**
  - Paths: `src/Event.MigrationService/Program.cs`, `Worker.cs`, new synchronization service/tests.
  - Deliverable: default migrates only app/Data Protection; retained also migrates authority and performs ordered ledger synchronization.
  - Acceptance: default never resolves authority config/context; retained failure exits 1; divergence/gap stops; application-only suffix promotes; a pre-correction checkpointed authority prefix backfills the new local mirror without duplicate outbox; authority facts beyond the verified checkpoint remain for API replay.
  - Verify: worker/composition tests use exact configuration variants.
  - Evidence: pending.

- [ ] **OREA-210 — Make API startup replay conditional**
  - Paths: `Explore.API/Program.cs`, `LocationPrivacyStartupGate.cs`, API integration tests.
  - Deliverable: mode branch occurs before replay-service resolution; retained gate remains between host build and host start.
  - Acceptance: default host starts with no authority service/secret; retained outage/mismatch/cancellation prevents hosted-worker invocation; no fallback.
  - Verify: expanded `LocationPrivacyStartupGateTests`.
  - Evidence: pending.

- [ ] **OREA-220 — Add bounded durability health reporting**
  - Paths: new API health check, `Program.cs`, shared safe health writer tests/docs.
  - Deliverable: `location-privacy-erasure-durability` health entry.
  - Acceptance: default healthy/false, retained healthy/true only after validation, retained failures unhealthy; no endpoint, DB name, watermark, IDs, exception text, or secret.
  - Verify: API health integration tests inspect serialized response.
  - Evidence: pending.

- [ ] **OREA-230 — Add migration-service container path for self-hosting**
  - Paths: new `src/Event.MigrationService/Dockerfile`, `docker-compose.yml`, `.env.example`, self-hosting tests/docs.
  - Deliverable: Compose migration service runs before API, receives application config always and authority config only when explicitly retained.
  - Acceptance: default Compose values require no authority secret; failed migration prevents API dependency satisfaction; API retains direct-run app migration fallback but never migrates authority with runtime credentials.
  - Verify: deterministic Compose configuration/architecture tests; no live container launch in this phase gate.
  - Evidence: pending.

### Phase 3 Gate

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
```

## Phase 4 — Wire `local-full` And Close Operator Contracts

- [ ] **OREA-300 — Provision authority only in Aspire `local-full`**
  - Paths: `src/Explore.AppHost/AppHost.cs`, PgAdmin config as needed, launch-profile architecture tests.
  - Deliverable: separate Postgres server/database/volume in `FullLocal`; connection/mode injected only into migration service/API; correct wait edges.
  - Acceptance: no second DB inside app Postgres; no authority resource/reference in default/core/lite; no authority config in Blazor.
  - Verify: `AspireLocalInfrastructureArchitectureTests` parse/guard all four profiles.
  - Evidence: pending.

- [ ] **OREA-310 — Add DI and secret-flow architecture guards**
  - Paths: `tests/Event.Architecture.Tests/`.
  - Deliverable: guards for explicit mode selection, no unconditional retained registration, no embedded schema SQL, context/migration ownership, and no Blazor secret flow.
  - Acceptance: tests fail against the old structure and pass only after the target boundaries exist.
  - Verify: focused Architecture tests.
  - Evidence: pending.

- [ ] **OREA-320 — Rewrite configuration, operations, self-hosting, and backup contracts**
  - Paths: all documents listed in plan Section 11 plus `schemas/islamu-event.md`.
  - Deliverable: one-database default, retained opt-in, exact keys, profile matrix, health semantics, promotion/downgrade, independent backup requirement, default restore limitation, and troubleshooting.
  - Acceptance: no document calls authority mandatory for every deployment; no document calls default erasure disabled; release checklist captures both migrations/config/backup impact.
  - Verify: documentation quality tests and contradiction search are part of the Architecture project gate.
  - Evidence: pending.

- [ ] **OREA-330 — Final cross-mode contract audit**
  - Paths: complete change set.
  - Deliverable: trace each acceptance criterion from mode config through DI, persistence, migration, startup, health, Aspire, Compose, and docs.
  - Acceptance: every criterion has a test/evidence owner; no silent fallback, secret leak, migration drift, or competing plan statement remains.
  - Verify: final source searches plus Phase 4 gate.
  - Evidence: pending.

### Phase 4 Gate

```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

## Completion Evidence Checklist

- [ ] Default mode starts with no authority mode/secret and one PostgreSQL database.
- [ ] Default erasure proves atomic local ledger + checkpoint + mutation + outbox.
- [ ] Stray authority secret does not activate retained mode.
- [ ] Retained mode missing/unreachable/mismatched authority fails closed.
- [ ] External append followed by application failure replays successfully.
- [ ] Fresh and legacy authority databases migrate without evidence loss.
- [ ] Nonempty ledger/authority rollback is blocked.
- [ ] `local-full` alone contains the separate authority resource/volume.
- [ ] Blazor receives no authority connection or mode-control surface.
- [ ] Health/log evidence contains no secret/provider/identifier details.
- [ ] Promotion and downgrade procedures are documented and tested at their seams.
- [ ] Broader privacy plans and canonical operator docs agree with this workstream.
