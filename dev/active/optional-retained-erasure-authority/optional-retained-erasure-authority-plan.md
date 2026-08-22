<!-- ABOUTME: Re-baselined implementation plan for restore-safe privacy-erasure authority storage and lifecycle. -->
<!-- ABOUTME: Keeps PostgreSQL/SQLite topology boundaries explicit while planning rollback detection, retention, recovery, and release evidence. -->

# Optional Retained Erasure Authority — Implementation Plan

Last Updated: 2026-08-20 Europe/Brussels

## 0. Planning Metadata

- **Original request:** Re-baseline the optional retained-authority workstream to the latest approved multi-database and privacy-erasure decisions.
- **Task directory:** `dev/active/optional-retained-erasure-authority/`
- **Planning status:** Re-baselined; awaiting user approval
- **Matched intent:** `platform-privacy-erasure`
- **Relevant skills:** `implementation-plan`, `clean-architecture-rules`, `dotnet-efcore-guidelines`, `outbox-pattern`, `auth-patterns`, `ip-clean-room`
- **Relevant rules:** `.agents/rules/domain.md`, `.agents/rules/application-layer.md`, `.agents/rules/efcore-persistence.md`, `.agents/rules/efcore-migrations.md`, `.agents/rules/api-controllers.md`, `.agents/rules/tests.md`, `.agents/rules/ip-clean-room.md`
- **Related workstream:** `dev/active/multi-database-persistence-unification/`
- **Primary layers:** Domain, Application, Persistence, API readiness/health, MigrationService, tests, configuration, operations, and release metadata
- **Complexity:** L. Supported storage implementations already exist, but retention-floor evolution and restore safety cross multiple DbContexts, generated migration lanes, least-privilege SQL functions, and operator recovery contracts.
- **External influence:** None. Repository code, tests, governance, and existing operator documentation are the only sources.

## 1. Executive Summary

This workstream no longer expands `CoLocated` authority storage to SQL Server, MariaDB, or MySQL. The approved and implemented topology matrix is:

| Topology | Supported authority placement | Restore behavior |
|---|---|---|
| `EmbeddedSqlite` | Dedicated local SQLite authority file; works with any supported primary provider | Protects against stale primary restore only when its file/volume is preserved independently |
| `CoLocated` | Primary PostgreSQL schema or primary SQLite file | Restored atomically with the primary; `restoreReplayProtection=false` |
| `ExternalDatabase` | Separate PostgreSQL database | Protects against stale primary restore only when independently restored |

Primary application and Data Protection persistence still support PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL. That broader primary-provider matrix does not imply broader co-located authority support.

Most topology selection, authority-first ordering, embedded storage hardening, startup replay, external PostgreSQL least privilege, and old-primary-backup replay already exist. Remaining work is narrowed to explicit authority high-water/floor state, bounded retention and legal-hold behavior, rollback detection after compaction, topology-specific maintenance through the existing adapters, truthful operator diagnostics, and final release evidence.

### Non-goals

- no five-provider `CoLocated` expansion;
- no universal co-located DbContext or repository;
- no migration-project consolidation;
- no generic provider plugin or new dependency;
- no distributed transaction;
- no compatibility shim for removed pre-v1 configuration;
- no redesign of receipt/status HTTP, provider outboxes, HAL, BFF, or authorization.

## 2. Source-Grounded Current State Report

### 2.1 Evidence Log

| Claim | Evidence | Confidence | Notes |
|---|---|---:|---|
| Exactly three topology values exist and `EmbeddedSqlite` is the default | `PrivacyErasureDurabilityOptions.cs` | High | `CoLocated` reports `RestoreReplayProtection=false` |
| Authority facts are appended before primary erasure settlement | `RetainedAuthorityPrivacyErasureWorkflow.EraseUserAsync` | High | Retry covers ambiguous append acknowledgement |
| Replay validates sequence continuity and checkpoint identity | `RetainedAuthorityPrivacyErasureWorkflow.ReplayPendingAsync` | High | No explicit retained-floor state exists |
| Embedded storage enforces local filesystem, WAL, integrity, symlink rejection, and restrictive permissions | `EmbeddedPrivacyErasureAuthorityStorage.cs` | High | Implemented for the dedicated file |
| Embedded old-primary-backup replay is automated | `EmbeddedPrivacyErasureRecoveryTests.cs` | High | Dedicated authority remains untouched |
| External PostgreSQL uses function-only runtime access and separate migration ownership | `EfCorePrivacyErasureAuthorityRepository.cs`; `ExternalDatabasePrivacyErasureAuthorityTests` | High | Direct table access is denied |
| External old-primary-backup replay is automated | `ExternalDatabasePrivacyErasureRestoreTests` | High | Authority snapshot remains unchanged |
| PostgreSQL/SQLite `CoLocated` composition exists and unsupported providers fail closed | `PersistenceServicesRegistration.cs`; composition tests | High | Provider expansion is not current scope |
| Readiness exposes bounded topology and replay status | `PrivacyErasureReadinessHealthCheck.cs` | High | It does not expose authority floor/rollback state |
| Authority pruning/compaction is not implemented | `IPrivacyErasureAuthority.cs`; `docs/BACKUP_RESTORE_UPGRADE.md` | High | Docs explicitly state no update/delete/pruning surface |
| Below-floor compaction and complete DR rehearsals remain pending | `docs/BACKUP_RESTORE_UPGRADE.md`; `docs/TESTING.md` | High | No RPO/RTO or compaction guarantee may be claimed |

### 2.2 Existing Implementation

- **Domain/Application:** Typed `PrivacyErasureIntent`, monotonic sequence counter, authority-first User workflow, serializable local settlement, replay checkpoints, fenced provider work, cache invalidation, and once-revealed receipt hashing.
- **Persistence:**
  - embedded/co-located SQLite uses `EmbeddedPrivacyErasureAuthorityDbContext` and `EmbeddedPrivacyErasureAuthorityRepository`;
  - co-located PostgreSQL uses `CoLocatedPrivacyErasureAuthorityDbContext` and `CoLocatedPostgresPrivacyErasureAuthorityRepository`;
  - external PostgreSQL uses `PrivacyErasureAuthorityDbContext` and function-only `EfCorePrivacyErasureAuthorityRepository`.
- **Migration ownership:**
  - embedded/co-located SQLite authority tables: `Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite`;
  - co-located PostgreSQL authority tables: `Explore.Persistence` co-located authority migration lane;
  - external PostgreSQL functions, roles, grants, and tables: dedicated authority migration lane;
  - application migrations own only the replay checkpoint outside the selected co-located authority context.
- **API/Operations:** Startup replay blocks traffic on failure; readiness emits bounded aggregate state and `restoreReplayProtection`.

### 2.3 Existing Tests and Verification Coverage

- `EmbeddedPrivacyErasureRecoveryTests` covers old-primary restore replay, unsafe permissions, and symlink rejection against a real SQLite file.
- `ExternalDatabasePrivacyErasureAuthorityTests` covers function-only ACLs, finite retention timestamps, retry idempotency, and concurrent monotonic allocation.
- `ExternalDatabasePrivacyErasureRestoreTests` covers independently retained external authority during an old-primary restore.
- `PrivacyErasureStartupGateTests` and `PrivacyErasureReadinessHealthCheckTests` cover startup blocking, cancellation, and bounded health data.
- Provider composition and migration ownership tests are owned by `multi-database-persistence-unification`.

Coverage gap: no explicit retained floor, no hold-aware compaction, no below-floor restore rejection, and no bounded maintenance contract shared by all supported authority placements.

### 2.4 Existing Documentation and Contracts

- Canonical operator topology: `docs/PRIVACY_ERASURE.md`.
- Backup/restore and current no-pruning limitation: `docs/BACKUP_RESTORE_UPGRADE.md`.
- Configuration and secret ownership: `docs/CONFIGURATION.md`, `docs/SECRETS.md`.
- Provider and restore test lanes: `docs/TESTING.md`.
- Primary support matrix and migration ownership hardening: `dev/active/multi-database-persistence-unification/`.
- Master platform erasure requirements, including bounded retention/legal hold: `.omo/plans/platform-wide-privacy-erasure-authority.md`, task 18.

### 2.5 Current Pain Points

- The previous OREA plan incorrectly converted five-provider primary persistence support into five-provider co-located authority support.
- OREA-1010–1018 duplicated work owned by the multi-database workstream and proposed a generic repository that would erase meaningful PostgreSQL/SQLite safety differences.
- Replay can detect missing/gapped facts but has no explicit high-water/floor contract for safe compaction or authority rollback diagnosis.
- Finite `RetentionExpiresAtUtc` values exist, but authority facts are never compacted and legal-hold pseudonymization is not implemented.
- Operator docs correctly refuse compaction/RPO claims, so the workstream cannot be closed until lifecycle evidence exists.

### 2.6 Unknowns After Investigation

| Unknown | Resolution owner |
|---|---|
| Exact typed legal-hold source and release policy for authority facts | Master platform privacy-erasure task 18; consumed by OREA-1400 |
| Whether maintenance runs under runtime or separately scoped authority credentials | OREA-1501, preserving function-only external access |
| Exact migration shape for retained floor and pseudonymized held evidence | OREA-1500/1501 through model/configuration changes and generated migrations |

## 3. Proposed Future State

The three topology choices remain mutually exclusive and expose one Application boundary. Each adapter additionally reports bounded authority state:

```text
authority state = high watermark + retained floor + maintenance posture
```

Startup/replay behavior:

1. read authority state before normal replay;
2. reject impossible state (`floor < 0`, `high < floor`, gaps, or checkpoint ahead);
3. replay retained facts when the checkpoint is within the replayable range;
4. fail readiness when a restored primary checkpoint is below the retained floor;
5. expose bounded recovery reason codes without identifiers or connection details.

Retention behavior:

1. use the approved maximum resurrection-capable backup horizon plus safety margin;
2. perform a dry-run eligibility pass;
3. preserve active legal holds and pseudonymize held evidence according to the master policy;
4. compact eligible facts and advance the retained floor atomically;
5. never claim that a primary backup older than the floor can be replayed;
6. keep topology-specific repository and migration implementations.

## 4. Non-Negotiable Constraints

- `EmbeddedSqlite`, PostgreSQL/SQLite `CoLocated`, and external PostgreSQL are the only supported authority placements.
- Exactly one authority sink is active; no dual write or shadow ledger.
- Authority facts contain minimal pseudonymous/linkable data, never live PII.
- The authority append precedes primary mutation acknowledgement.
- Normal repositories keep tenant filters; only the dedicated erasure adapter may use exact cross-tenant subject predicates.
- Provider calls remain post-commit through specialized fenced outboxes.
- Generated migrations/snapshots are never hand-edited.
- External runtime access remains function-only and cannot migrate or access tables directly.
- Breaking pre-v1 changes fail fast; no silent translation or compatibility shim.
- No new dependency, provider plugin, generic interpreter, or message broker.

## 5. Architecture and Design Decisions

### Decision 1 — Preserve topology-specific adapters

- **Why:** PostgreSQL row locks/functions/ACLs and SQLite single-writer/file semantics are different correctness boundaries.
- **Rejected alternative:** one provider-neutral repository based on `RelationalNamedLock`.
- **Consequence:** `IPrivacyErasureAuthority` remains the shared boundary; Persistence keeps three concrete adapters.

### Decision 2 — Add explicit retained-floor state before compaction

- **Why:** A finite retention timestamp without a durable floor cannot distinguish safe compaction from authority rollback.
- **Rejected alternative:** infer floor from the first row or treat an empty authority as safe.
- **Consequence:** state, replay, readiness, migrations, and tests change together.

### Decision 3 — Policy ownership stays in the master privacy-erasure workstream

- **Why:** Legal-hold meaning and retention horizon are product/privacy policy, not storage-provider behavior.
- **Consequence:** this workstream implements the approved typed policy in supported adapters and does not invent a second legal-hold model.

### Decision 4 — No topology migration workflow before v1

- **Why:** Breaking changes are allowed and automatic fact copying creates a high-risk dual-authority period.
- **Consequence:** topology changes remain operator-managed reset/export/restore decisions with explicit backups and no compatibility shim.

## 6. Implementation Phases

## Phase 14 — Authority State and Replay Safety

**Goal:** Establish an explicit high-water/floor contract and fail-closed replay semantics before any destructive maintenance exists.

### OREA-1400 — Add typed authority state and maintenance boundaries

- **Type:** create/modify
- **Layer:** Domain/Application
- **Files:**
  - `src/Explore.Domain/PrivacyErasureAuthorityState.cs` (new)
  - `src/Explore.Domain/PrivacyErasureCounter.cs` (existing)
  - `src/Explore.Application/Contracts/PrivacyErasure/IPrivacyErasureAuthority.cs` (existing)
  - `src/Explore.Application/Contracts/PrivacyErasure/IPrivacyErasureAuthorityMaintenance.cs` (new)
  - `src/Explore.Application/Configuration/PrivacyErasureOptions.cs` (existing)
  - `tests/Event.Domain.UnitTests/PrivacyErasureContractTests.cs` (existing)
  - `tests/Event.Application.UnitTests/Configuration/PrivacyErasureModelCompositionTests.cs` (existing)
- **Description:** Add bounded high-water/floor state and a typed dry-run/apply maintenance boundary that consumes, rather than redefines, the master retention/legal-hold policy.
- **Acceptance criteria:**
  - [ ] Invalid state and early-compaction requests fail before persistence I/O.
  - [ ] State and maintenance results contain no subject, tenant, credential, or connection data.
  - [ ] No update/delete API is added to normal request-path repositories.
- **Dependencies:** Multi-database Phase 1; master privacy-erasure retention policy
- **Effort:** M
- **Required skills/rules:** `clean-architecture-rules`, `cqrs-mediatr-guidelines`, domain/application rules

### OREA-1401 — Make replay and readiness floor-aware

- **Type:** modify
- **Layer:** Application/API
- **Files:**
  - `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs` (existing)
  - `src/Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs` (existing)
  - `src/Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs` (existing)
  - `tests/Event.Application.UnitTests/Services/GlobalLocationPrivacyReplayCacheGateTests.cs` (existing)
  - `tests/Event.API.IntegrationTests/Privacy/PrivacyErasureStartupGateTests.cs` (existing)
  - `tests/Event.API.IntegrationTests/Privacy/PrivacyErasureReadinessHealthCheckTests.cs` (existing)
- **Description:** Validate state before replay, reject checkpoint-ahead/below-floor/gap conditions, and expose bounded recovery reason codes while preserving startup cancellation.
- **Acceptance criteria:**
  - [ ] A checkpoint ahead of high-water or below retained floor blocks startup.
  - [ ] Replay within the retained range remains ordered and idempotent.
  - [ ] Health data reports topology, restore capability, floor/high-water posture, and bounded status only.
  - [ ] `CoLocated` never reports restore-isolated protection.
- **Dependencies:** OREA-1400
- **Effort:** M
- **Required skills/rules:** `clean-architecture-rules`, `auth-patterns`, application/API/test rules

### Phase 14 Verification — run once after all Phase 14 tasks

1. `dotnet build --configuration Release --verbosity quiet`
2. `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`

## Phase 15 — Topology-Specific Retention and Recovery

**Goal:** Implement hold-aware bounded maintenance through existing SQLite and PostgreSQL authority adapters without widening provider support.

### OREA-1500 — Implement embedded and co-located SQLite maintenance

- **Type:** modify/create generated migration
- **Layer:** Persistence
- **Files:**
  - `src/Explore.Persistence/Privacy/ErasureAuthority/EmbeddedPrivacyErasureAuthorityDbContext.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EmbeddedPrivacyErasureAuthorityRepository.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Configurations/EmbeddedPrivacyErasureCounterConfiguration.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Configurations/EmbeddedPrivacyErasureIntentConfiguration.cs` (existing)
  - `src/Explore.Persistence.PrivacyErasureAuthority.Migrations.Sqlite/Migrations/` (generated output)
  - `tests/Event.Persistence.IntegrationTests/Privacy/EmbeddedPrivacyErasureRecoveryTests.cs` (existing)
- **Description:** Add atomic dry-run/hold-aware compaction, retained-floor advancement, rollback detection, and old-backup failure behavior for the dedicated file and primary SQLite file.
- **Acceptance criteria:**
  - [ ] Facts remain replayable through the configured horizon and safety margin.
  - [ ] Active holds prevent destructive removal and preserve only approved pseudonymized evidence.
  - [ ] Compaction and floor advancement commit atomically.
  - [ ] An old primary below floor fails closed; a supported old primary within range replays once.
  - [ ] Generated SQLite migrations are produced through `dotnet ef`, never patched.
- **Dependencies:** Phase 14
- **Effort:** L
- **Required skills/rules:** `dotnet-efcore-guidelines`, EF persistence/migration/test rules

### OREA-1501 — Implement co-located and external PostgreSQL maintenance

- **Type:** modify/create generated migration
- **Layer:** Persistence/MigrationService
- **Files:**
  - `src/Explore.Persistence/Privacy/ErasureAuthority/CoLocatedPrivacyErasureAuthorityDbContext.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDbContext.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDatabaseContract.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPostgresPrivacyErasureAuthorityRepository.cs` (existing)
  - `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs` (existing)
  - `src/Explore.Persistence/Migrations/CoLocatedPrivacyErasureAuthority/` (generated output)
  - `src/Explore.Persistence/Migrations/PrivacyErasureAuthority/` (generated output)
  - `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` (existing)
  - `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs` (existing)
- **Description:** Implement the same approved maintenance semantics separately for co-located PostgreSQL and external PostgreSQL, preserving function-only external runtime access and independent restore behavior.
- **Acceptance criteria:**
  - [ ] External runtime maintenance is possible only through approved functions; table DML and migration remain denied.
  - [ ] Co-located maintenance stays inside the primary PostgreSQL transaction/backup boundary.
  - [ ] Concurrent append and maintenance cannot duplicate sequences, skip retained facts, or move the floor past an ineligible fact.
  - [ ] External old-primary restore remains replayable while the independently retained authority is unchanged.
  - [ ] Generated PostgreSQL migrations/functions/grants come from model/generator sources, never hand edits.
- **Dependencies:** OREA-1400, OREA-1401
- **Effort:** L
- **Required skills/rules:** `dotnet-efcore-guidelines`, `clean-architecture-rules`, EF migration/test rules

### Phase 15 Verification — run once after all Phase 15 tasks

1. `dotnet build --configuration Release --verbosity quiet`
2. `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release -- --treenode-filter "/*/*/EmbeddedPrivacyErasureRecoveryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

Real PostgreSQL authority and restore tests remain required CI/release evidence; implementation agents do not start Docker or live services as a local phase-end gate.

## Phase 16 — Operator Contract and Release Closure

**Goal:** Expose truthful recovery posture, synchronize self-hosting guidance, and contribute governed release metadata.

### OREA-1600 — Converge operator diagnostics, deployment, and recovery guidance

- **Type:** modify
- **Layer:** API/DevOps/Docs
- **Files:**
  - `src/Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs` (existing)
  - `src/Explore.AppHost/AppHost.cs` (existing)
  - `docker-compose.yml` (existing)
  - `.env.example` (existing)
  - `docs/PRIVACY_ERASURE.md` (existing)
  - `docs/CONFIGURATION.md` (existing)
  - `docs/SECRETS.md` (existing)
  - `docs/SELF_HOSTING.md` (existing)
  - `docs/BACKUP_RESTORE_UPGRADE.md` (existing)
  - `docs/TROUBLESHOOTING.md` (existing)
  - `docs/TESTING.md` (existing)
- **Description:** Document the supported matrix, maintenance lifecycle, backup units, restore limits, failure codes, credential ownership, and CI-owned recovery evidence without claiming unsupported RPO/RTO.
- **Acceptance criteria:**
  - [ ] Every surface says PostgreSQL/SQLite `CoLocated`, not five-provider `CoLocated`.
  - [ ] Embedded and external backup independence is conditional and operationally explicit.
  - [ ] `CoLocated` atomic restore and `restoreReplayProtection=false` are explicit.
  - [ ] Logs/health/troubleshooting expose no identifiers, DSNs, secrets, URLs, or exception text.
  - [ ] No Blazor process receives authority credentials.
- **Dependencies:** Phase 15; multi-database Phase 2
- **Effort:** M
- **Required skills/rules:** `auth-patterns`, `ip-clean-room`, API/test rules, documentation style guide

### OREA-1601 — Changelog contribution and final commit composition

- **Type:** create/prepare
- **Layer:** Release
- **Files:**
  - `docs/releases/changes/CHG-2026-0002.yaml` (new)
- **Description:** Add the Tier 2 privacy/operator change fragment after implementation and tests are green, validate it against release policy, and prepare one outcome-led commit. Do not execute a commit without explicit authorization.
- **Acceptance criteria:**
  - [ ] Fragment includes structured Breaking, Security, Migration, Configuration, OpenAPI, and Operator dispositions.
  - [ ] `Type` and `Scope: privacy` pass `ReleaseInputPolicy`.
  - [ ] Commit subject describes the privacy/recovery outcome, not code layers.
  - [ ] Terminal footer contains `Change-Id: CHG-2026-0002`.
  - [ ] Add `BREAKING CHANGE:` and `!` only if the implemented deployed contract requires operator action.
- **Dependencies:** OREA-1600 and all prior phase verification
- **Effort:** S
- **Required skills/rules:** `conventional-commit`, release policy and scope registry

### Phase 16 Verification — run once after all Phase 16 tasks

1. `dotnet build --configuration Release --verbosity quiet`
2. `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## 7. Testing Strategy

Phase gates use one deterministic project each: Application unit tests, focused file-backed Persistence integration tests, and Architecture tests.

The `platform-privacy-erasure` intent additionally requires nonzero Domain, Application, Architecture, Persistence, Infrastructure, API, Secrets, MigrationService/AppHost, and approved real-provider/recovery evidence before merge/release. Existing CI/runtime lanes own those broader checks; they are not additional local phase commands.

## 8. Documentation, Configuration, and Operations Impact

- **Applicable:** privacy-erasure topology, retention, health, secret ownership, migration ownership, self-hosting, backup/restore, troubleshooting, testing, and release metadata.
- **No HTTP schema redesign:** receipt/status routes and HAL contracts remain unchanged.
- **No new configuration topology:** existing structured keys remain; only maintenance/retention policy keys approved by the master workstream may be added.
- **Release strategy:** Tier 2 because privacy retention, migrations, and operator recovery are high impact. Use `CHG-2026-0002` and `Scope: privacy`.

## 9. Security, Authorization, Privacy, and Abuse

- Authority identifiers remain linkable personal data until safely compacted.
- Legal holds prevent destructive cleanup and retain only approved pseudonymized evidence.
- Maintenance is dry-run first, bounded, auditable, and unavailable to ordinary request paths.
- External runtime access remains function-only; migrator credentials stay MigrationService-only.
- Receipt authorization, rate limiting, HAL affordances, and provider outbox fencing are unchanged.
- Health, logs, metrics, evidence, and support artifacts use bounded reason codes and counts only.

## 10. Cross-Cutting Product Classifications

| Concern | Classification | Reason |
|---|---|---|
| Multi-tenancy | Applicable | Replay/maintenance must preserve unrelated tenants and exact subject predicates |
| Federation/provider cleanup | Not changed | Existing specialized post-commit outboxes remain authoritative |
| Localization | Not applicable | No user-facing localized content changes |
| Accessibility | Not applicable | No UI changes |
| HAL/BFF | Not applicable | No action-affordance or token-flow changes |
| Self-hosting | Applicable | Volume, credentials, backup, restore, and readiness behavior change |
| Observability | Applicable | Floor/high-water and maintenance posture must be bounded and PII-free |

## 11. Observability and Operations

- readiness: topology, restore capability, replay posture, floor/high-water relation, bounded maintenance status;
- logs/traces: operation type and bounded outcome only;
- metrics: counts and duration with bounded topology/result labels;
- recovery: startup remains blocked for corrupt, rolled-back, unavailable, ahead, below-floor, or unreplayable authority state;
- no RPO/RTO claim until CI/release recovery evidence supports it.

## 12. Migration and Compatibility

- Add model/configuration changes first; generate migrations with `dotnet ef`.
- Never rewrite applied migration history or manually edit snapshots/designers.
- Deployment order remains backup, MigrationService, startup replay/readiness, then traffic.
- Pre-v1 topology/provider changes have no compatibility shim or automatic cross-mode copy.
- SQL Server/MariaDB/MySQL primary databases may use `EmbeddedSqlite` or external PostgreSQL authority; they may not select `CoLocated`.

## 13. Risk Register

| Risk | Likelihood | Impact | Mitigation | Detection signal | Owner |
|---|---:|---:|---|---|---|
| Floor advances beyond replay-safe facts | Medium | Critical | Atomic eligibility, hold check, compaction, and floor update | below-floor/restore tests | OREA-1500/1501 |
| Legal hold leaks live identifiers indefinitely | Medium | High | Typed policy and pseudonymized held evidence | catalog/text canaries | OREA-1400/1500/1501 |
| External maintenance weakens function-only ACLs | Medium | Critical | SECURITY DEFINER functions with explicit grants and denied table DML | ACL integration tests | OREA-1501 |
| Co-located deployment claims restore isolation | Low | High | fixed health flag and operator docs | architecture/API assertions | OREA-1401/1600 |
| Stale five-provider tasks are restarted | Medium | High | supersession ledger and dependency on multi-database contract | task/doc scan | this rebaseline |
| Generated migration histories drift | Medium | High | provider/context ownership tests; no hand edits | pending-model/ownership evidence | OREA-1500/1501 |

## 14. Success Metrics and Definition of Done

1. Supported topology/provider combinations remain exactly the approved matrix.
2. Authority state exposes a durable high-water and retained floor without PII.
3. Startup/replay rejects ahead, gap, rollback, and below-floor states.
4. Hold-aware retention is implemented for embedded/co-located SQLite, co-located PostgreSQL, and external PostgreSQL.
5. External runtime remains function-only; MigrationService remains migration owner.
6. Embedded and external old-primary restore evidence remains green; co-located recovery is documented as atomic/non-isolated.
7. Operator docs, health, CI claims, planning docs, and release fragment agree.
8. Each phase passes one Release build and its selected project test.

## 15. Historical and Superseded Work

- Verified delivered behavior remains credited: OREA-100/110/120, 200/210/220, 300, 420, 500/510/520, 600/610, 700, 900–903, and 1000–1006.
- OREA-1009 and decisions OREA-D12/D13/D14 are preserved as superseded planning history, not implementation evidence.
- OREA-1010–1018 are cancelled by the latest support decision; they must not be resumed.
- Open historical goals OREA-310/320/400/410/620/710/720 map to OREA-1400–1600 and the master platform plan rather than remaining duplicate checklists.

## 16. Implementation Agent Contract

1. Read all three artifacts once at implementation start; on resume, read context/tasks then only the relevant plan sections.
2. Start from the first unchecked task after its dependencies.
3. Keep `tasks.md` as the hot ledger and update substantial tasks immediately.
4. Update context only after a phase, decision, blocker, validation failure, material discovery, or handoff.
5. Update this plan only when scope, architecture, phase order, acceptance, risk, or verification changes.
6. Keep implementation and phase-verification checkboxes separate.
7. Run phase verification once; do not start the app, browser, Docker, Aspire, or live services locally.
8. Preserve unrelated dirty work and never delete databases, containers, volumes, backups, or migration history.
9. Stop if work requires five-provider `CoLocated`, a universal repository, a second authority sink, or a new dependency.

## 17. Progress Reporting Contract

After each slice report:

```text
Implemented: developer teaching summary
Verified: exact evidence
Remaining: incomplete or deferred work
Next: recommended next slice
Docs updated: tasks/context/plan status and reason
```

## 18. Potential Risks and Unknowns

The hardest unresolved issue is legal-hold pseudonymization after the replay horizon: retained evidence must remain useful for the approved legal purpose without preserving a live identifier or pretending it can still replay an erased subject. OREA-1400 must consume an explicit master-policy decision before Persistence schema work begins.
