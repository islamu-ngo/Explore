<!-- ABOUTME: Resumable context for restore-safe privacy-erasure authority lifecycle work. -->
<!-- ABOUTME: Records the approved topology matrix, verified implementation, superseded five-provider scope, and next tasks. -->

# Optional Retained Erasure Authority — Context

Last Updated: 2026-08-20 Europe/Brussels

## SESSION PROGRESS (2026-08-20 Europe/Brussels)

### ✅ COMPLETED

- Reconciled this workstream with the canonical `platform-privacy-erasure` intent and the Senior CTO-approved multi-database contract.
- Verified current topology composition, authority-first workflow, embedded storage hardening, startup/readiness behavior, external PostgreSQL ACLs, and embedded/external old-primary restore tests.
- Removed five-provider `CoLocated` from active scope.
- Preserved PostgreSQL/SQLite delivered behavior and mapped historical open goals into six current tasks.
- Added the required Tier 2 release/changelog closing task.

### 🟡 IN PROGRESS

- Awaiting user review or approval of the re-baselined plan.

### ⏭️ NEXT

1. Land or jointly coordinate multi-database Phase 1 so the provider/topology matrix is pinned once.
2. Resolve the master privacy-erasure legal-hold pseudonymization policy required by OREA-1400.
3. Start OREA-1400: add typed authority high-water/floor and maintenance boundaries.

### ⚠️ BLOCKERS

- OREA-1400 must not invent legal-hold semantics. It depends on the explicit master-policy decision described in platform privacy-erasure task 18.
- Multi-database Phase 1 owns provider/topology composition and migration-ownership tests; avoid parallel duplicate edits.

## Quick Resume

1. Read this context and `optional-retained-erasure-authority-tasks.md`.
2. Read only the current phase, architecture decisions, and constraints in `optional-retained-erasure-authority-plan.md`.
3. Start from the first unchecked task after its dependencies.
4. Do not resume OREA-1010–1018 or the superseded five-provider decisions.

## Objective

Finish restore-safe authority lifecycle behavior for the supported placements:

- `EmbeddedSqlite`: dedicated local SQLite authority file with any primary provider;
- `CoLocated`: primary PostgreSQL or primary SQLite only;
- `ExternalDatabase`: separate PostgreSQL database.

The remaining work is explicit floor/high-water state, hold-aware bounded retention, below-floor rollback handling, supported-adapter maintenance, truthful diagnostics, operator recovery guidance, and release evidence.

## Current Verified State

### Implemented

- Three mutually exclusive topology values with `EmbeddedSqlite` default.
- Authority-first append before primary erasure settlement.
- Ordered replay with sequence-gap and checkpoint-identity checks.
- Dedicated embedded SQLite file with local-filesystem, WAL, integrity, permission, and symlink controls.
- PostgreSQL and SQLite co-located authority composition.
- External PostgreSQL function-only runtime access and separate migration ownership.
- Startup replay before traffic and bounded readiness diagnostics.
- Embedded and external old-primary-backup replay tests.
- `CoLocated` reports `restoreReplayProtection=false`.

### Missing or incomplete

- Durable retained floor/high-water API.
- Below-floor restore rejection after compaction.
- Authority fact compaction/pruning.
- Legal-hold pseudonymization.
- Topology-specific maintenance contracts and generated schema changes.
- Complete operator/release evidence for bounded retention.

## Approved Support Matrix

| Topology | Placement | Provider rule |
|---|---|---|
| `EmbeddedSqlite` | dedicated SQLite file | independent of primary provider |
| `CoLocated` | primary DB | PostgreSQL or SQLite only |
| `ExternalDatabase` | separate DB | PostgreSQL only |

SQL Server, MariaDB, and MySQL remain supported primary application/Data Protection providers. They use embedded SQLite or external PostgreSQL authority, not co-located authority.

## Ownership Boundaries

- `multi-database-persistence-unification`: capability matrix, fail-fast composition, migration-owner assertions, CI/provider documentation.
- `optional-retained-erasure-authority`: state/floor, compaction, legal-hold storage behavior, rollback detection, authority recovery, operator posture.
- `.omo/plans/platform-wide-privacy-erasure-authority.md`: privacy policy, legal-hold meaning, full User-PII inventory, provider settlement, API/receipt, and final platform acceptance.

## Key Files and Responsibilities

| Path | Existing/New | Responsibility |
|---|---|---|
| `src/Explore.Application/Contracts/PrivacyErasure/IPrivacyErasureAuthority.cs` | Existing | append/read Application boundary |
| `src/Explore.Application/Contracts/PrivacyErasure/IPrivacyErasureAuthorityMaintenance.cs` | New | bounded dry-run/apply maintenance boundary |
| `src/Explore.Domain/PrivacyErasureAuthorityState.cs` | New | high-water/floor state without identifiers |
| `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs` | Existing | authority-first workflow and replay |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EmbeddedPrivacyErasureAuthorityRepository.cs` | Existing | embedded/co-located SQLite behavior |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/CoLocatedPostgresPrivacyErasureAuthorityRepository.cs` | Existing | co-located PostgreSQL behavior |
| `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs` | Existing | external PostgreSQL function-only behavior |
| `src/Explore.API/HealthChecks/PrivacyErasureReadinessHealthCheck.cs` | Existing | bounded operator recovery posture |
| `src/Explore.Persistence/Schema/ExploreDatabaseMigrator.cs` | Existing | selected topology migration ownership |
| `docs/releases/changes/CHG-2026-0002.yaml` | New | Tier 2 privacy/operator release fragment |

## Key Decisions

1. Preserve three topology choices but only the approved authority placements.
2. Keep topology-specific Persistence adapters behind shared Application contracts.
3. Add explicit retained-floor state before any compaction.
4. Consume legal-hold policy from the master platform workstream.
5. Keep external runtime function-only and MigrationService migration-only.
6. Keep pre-v1 topology changes reset/export/restore-only with no compatibility shim.
7. Do not create five-provider co-located migrations, generic lock/repository code, or project consolidation.

## Historical Reconciliation

### Preserved delivered evidence

OREA-100/110/120, 200/210/220, 300, 420, 500/510/520, 600/610, 700, 900–903, and 1000–1006.

### Superseded planning

- OREA-1009: five-provider rebaseline.
- OREA-D12/D13/D14: five-provider namespace and universal repository decisions.
- OREA-1010–1018: provider-neutral model/repository, three new co-located provider lanes, five-provider matrix, and matching docs.

These are not implementation evidence and must not be resumed.

### Historical open goals mapped forward

| Historical goals | Current owner |
|---|---|
| OREA-310/320 | OREA-1400/1401 |
| OREA-400 | OREA-1500 |
| OREA-410 | OREA-1501 |
| OREA-620/710/720 | OREA-1600/1601 plus master platform evidence |

## Constraints and Rules

- Repositories return entities; handlers map DTOs.
- Generated migrations and snapshots are never hand-edited.
- Authority data is linkable personal data and must not appear in logs, metrics, health, or evidence.
- Normal request paths cannot invoke maintenance or bypass tenant filters.
- No provider I/O occurs inside the primary erasure transaction or startup replay transaction.
- Provider settlement remains specialized, fenced, idempotent, and post-commit.
- No new dependency, provider plugin, interpreter, message broker, dual write, or distributed transaction.
- Phase gates use one Release build and at most one deterministic non-browser project test.
- Do not start Docker, Aspire, the app, browsers, or live services for local phase verification.

## Validation Baseline

- Latest Release baseline from 2026-08-20: passed, 39 projects, 0 errors.
- The baseline reported substantial pre-existing warnings, including repeated `NU1903` for `SSH.NET` 2025.1.0.
- This rebaseline changes planning documentation only; no product tests are required for the planning turn.

## Current Risks / Unknowns

- Legal-hold pseudonymization semantics are a real blocker for OREA-1400 schema design.
- Floor advancement mistakes could make valid retained facts unreplayable.
- External maintenance could weaken function-only ACLs if it is implemented as table access.
- Generated migration ownership must remain distinct for embedded SQLite, co-located PostgreSQL, and external PostgreSQL.
- Existing docs intentionally make no compaction or RPO/RTO guarantee.

## Handoff Notes

### Handoff — 2026-08-20 Europe/Brussels

- **Current state:** Planning re-baseline complete; no implementation started.
- **Next action:** Resolve dependencies, then OREA-1400.
- **Blockers:** Master legal-hold policy and coordination with multi-database Phase 1.
- **Modified files:** Three planning files under `dev/active/optional-retained-erasure-authority/`.
- **Validation:** Release baseline inherited; planning diff verification remains to run.
- **Documentation impact:** Canonical docs remain implemented-current; Phase 16 adds retention/recovery guarantees only after code exists.
- **Risks:** floor/hold correctness and external least privilege.
- **Notes for next contributor/agent:** Keep PostgreSQL/SQLite adapters separate and treat OREA-1010–1018 as cancelled.
