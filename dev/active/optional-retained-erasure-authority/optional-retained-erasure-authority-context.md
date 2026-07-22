<!-- ABOUTME: Quick-resume context for the canonical platform User-erasure authority workstream. -->
<!-- ABOUTME: Captures policy, topology, provider settlement, restore, enterprise operations, risks, and next action. -->

# Platform Privacy Erasure Authority — Context

Last Updated: 2026-07-22 Europe/Brussels

## Progress Snapshot

- Status: Phase 1 accepted; `OREA-100`, `OREA-110`, and `OREA-120` are complete, and Phase 2 persistence adapters/migration ownership is next.
- Active intent: `platform-privacy-erasure` now requires one authority-first workflow, `CoLocated` / `ExternalDatabase`, separate runtime/migrator credentials, and no Blazor authority secret.
- Workstream: canonical owner of complete platform User erasure, authority topology, receipt/status, provider settlement, replay, retention, and restore behavior.
- Supersedes: privacy-erasure implementation ownership in `.omo/plans/platform-wide-privacy-erasure-authority.md` and `dev/active/event-location-privacy/`.
- Runtime changes: configuration now exposes only `CoLocated` / `ExternalDatabase`, rejects the legacy mode key, isolates the external connection, and registers one authority-first workflow; the real CoLocated authority adapter remains the first Phase 2 dependency.
- Verification: Release build passed with 26 projects, 0 errors, and 41 documented warnings; focused governance 6/6, inventory 9/9, topology/composition 22/22, and request contract 3/3 passed. Full Architecture reproduced only its three documented unrelated failures (292/296 passed, 1 skipped).

## Quick Resume

Start here:

1. Read `optional-retained-erasure-authority-plan.md` Sections 1–5 and 13.
2. Read the current `platform-privacy-erasure` intent in `.claude/contract/intents.yaml`.
3. Treat the recorded baseline and Phase 1 gate as complete.
4. Implement `OREA-200`: a real CoLocated `IPrivacyErasureAuthority` adapter with a short-lived `ExploreDbContext` and an independent commit boundary.
5. Prove rollback survival, replay/idempotency, and zero external-connection access before proceeding to schema/migration ownership.

The target is not “two durability modes.” It is one authority-first workflow with two authority-storage topologies:

- `CoLocated`: authority ledger and application mirror reside in the application database.
- `ExternalDatabase`: authority ledger resides in a separately connected database; the application mirror remains local.

Only the second topology can protect against restoring a pre-erasure application backup, and only if the authority database is outside the application database’s restore operation.

Topology is only one part of this workstream. The same plan owns the User fence, policy-versioned saga, complete PII disposition inventory, atomic local erasure, short-lived receipt/status API, specialized provider work, anti-resurrection checks, retention/legal hold, and enterprise recovery contract.

## Key Files

### Governance and planning

- `.claude/contract/intents.yaml`
- `.omo/plans/platform-wide-privacy-erasure-authority.md`
- `dev/active/event-location-privacy/`
- `dev/active/optional-retained-erasure-authority/`

### Configuration and composition

- `src/Explore.Application/Configuration/PrivacyErasureDurabilityOptions.cs`
- `src/Explore.Application/ApplicationServicesRegistration.cs`
- `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`
- `src/Explore.Persistence/PersistenceServicesRegistration.cs`
- `src/Explore.API/BackgroundServices/PrivacyErasureStartupGate.cs`

### Workflow and persistence

- `src/Explore.Application/Features/Users/Handlers/Commands/DeleteUserCommandHandler.cs`
- `src/Explore.Application/Services/ApplicationDatabasePrivacyErasureWorkflow.cs`
- `src/Explore.Application/Services/RetainedAuthorityPrivacyErasureWorkflow.cs`
- `src/Explore.Application/Services/PrivacyErasureApplier.cs`
- `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/ApplicationDatabasePrivacyErasureLedgerRepository.cs`
- `src/Explore.Persistence/Privacy/ErasureAuthority/Repositories/EfCorePrivacyErasureAuthorityRepository.cs`
- `src/Explore.Persistence/Privacy/ErasureAuthority/PrivacyErasureAuthorityDbContext.cs`
- `src/Explore.Persistence/Migrations/`
- `src/Explore.Persistence/Migrations/PrivacyErasureAuthority/`
- `src/Explore.Domain/PrivacyErasureSaga.cs`
- `src/Explore.Domain/PrivacyErasurePolicyCoverage.cs`
- `src/Explore.Domain/PrivacyErasureIntent.cs`
- `src/Explore.Domain/PrivacyErasureReplayCheckpoint.cs`
- `tests/Event.Architecture.Tests/Privacy/UserPiiInventory.cs`
- `tests/Event.Architecture.Tests/Privacy/UserPiiInventoryArchitectureTests.cs`

### Hosting and operations

- `src/Event.MigrationService/Program.cs`
- `src/Event.MigrationService/Worker.cs`
- `src/Explore.AppHost/AppHost.cs`
- `docker-compose.yml`
- `.env`
- `.env.example`
- `src/Explore.Secrets/Bootstrap/BootstrapSecretLoader.cs`

### Tests

- `tests/Event.Application.UnitTests/Configuration/PrivacyErasureDurabilityOptionsTests.cs`
- `tests/Event.Application.UnitTests/Configuration/PrivacyErasureModelCompositionTests.cs`
- `tests/Event.API.IntegrationTests/Privacy/PrivacyErasureStartupGateTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/GlobalLocationPrivacyErasureTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityCompositionValidationTests.cs`
- `tests/Event.Persistence.IntegrationTests/Privacy/PrivacyErasureAuthorityDbContextFactoryTests.cs`

## Current-State Findings

1. `PrivacyErasureDurabilityOptions` currently defines `ApplicationDatabase` and `RetainedAuthority`, with `ApplicationDatabase` as default.
2. Application DI selects between two workflow implementations; infrastructure DI registers replay only in retained mode.
3. The startup gate skips replay in application-database mode.
4. The retained workflow already has the desired shape: commit authority fact, then replay/apply.
5. `PrivacyErasureApplier` already writes the application-side mirror and checkpoint within the application transaction.
6. The application and dedicated authority migrations both own `privacy_erasure_authority` tables. Applying both migration sets to one database would collide.
7. The dedicated migration additionally owns PostgreSQL functions, roles, grants, and tamper-resistant access rules.
8. `Event.MigrationService` currently does not migrate `PrivacyErasureAuthorityDbContext`.
9. `.env` and `.env.example` mention the old mode, while Compose does not provide complete authority wiring.
10. The bootstrap secret loader is specialized for the main application PostgreSQL settings. The lean plan keeps authority as a named connection string supplied separately to API and migration processes.
11. Existing persistence integration infrastructure already starts two independent PostgreSQL containers. Extend it; do not create a competing fixture stack.
12. Some test assertions still reference `location_privacy_authority`; confirm and replace stale generalized-schema names during the persistence phase.
13. `tests/Event.Architecture.Tests/Privacy/UserPiiInventory.cs` classifies broad User-PII families across identity/authentication, tenancy/membership, registration/contact sharing, notifications/email/web-push, AI/webhook/report/audit/configuration, storage, federation, and external providers; completeness against current source must be re-proven.
14. Generalized authority, applier, startup-gate, saga/policy-coverage, and inventory pieces exist, but code presence is not consolidated-plan acceptance evidence.
15. Event Location contributes only a typed disposition adapter: exact subject/tenant predicates, owned Home/room tombstoning, affected `EventLocation` correction intents, stable idempotency, and integration tests. This workstream owns the platform orchestration.

## Decisions

### Accepted

- This directory is the sole active implementation workstream for platform User erasure and its authority.
- `User` is the only executable subject kind; Organization and Tenant require separate typed policies and handlers.
- A machine-checked inventory proves completeness but never drives runtime SQL or arbitrary instructions.
- Fence before enumeration; apply complete local dispositions, mirror/checkpoint, provider work, cache authority, and receipt state in one serializable application transaction.
- Return `202` with a once-revealed short-lived receipt; persist only its fixed-time-verifiable hash and serve status as `private, no-store`.
- Provider cleanup is specialized, idempotent, fenced, retry/unknown/dead-letter aware, and always after local commit.
- Delete only platform-managed upstream identities; revoke or unlink identities the platform does not own.
- Remove production behavior-mode selection.
- Introduce `PrivacyErasure:Authority:Topology` with `CoLocated` and `ExternalDatabase`.
- Default to `CoLocated`, and show that choice explicitly in sample env files.
- Reject a present legacy `PrivacyErasure:Durability:Mode` key with upgrade guidance.
- Never infer external topology from a connection string.
- Keep one authority-first workflow and run startup replay in both topologies.
- Keep the application-side mirror/checkpoint in both topologies.
- Use a separately committed, short-lived application context for the co-located authority append.
- Keep dedicated authority migrations out of the application database.
- Use different public runtime/migrator secret variables, each mapped to `ConnectionStrings__PrivacyErasureAuthority` only in its owning process.
- Test external restore safety with two independent PostgreSQL containers and a real pre-erasure application backup restore.
- Report `restoreReplayProtection=false` for co-located storage.
- Use a pre-v1 reset-only policy for the removed behavior-mode contract. Breaking compatibility is accepted; silent data loss and agent-driven deletion of operator resources are not.
- Retain linkable authority identifiers only through the maximum resurrection-capable backup horizon plus the approved margin; pseudonymize legal-hold evidence and destroy expired credentials/receipts.

### Rejected

- Keeping `ApplicationDatabase` as a production workflow mode.
- Auto-selecting external storage when a connection exists.
- Applying the dedicated authority migration to the application database.
- Giving API runtime credentials migration privileges.
- Using a distributed transaction.
- Claiming that a second database automatically means an independent restore domain.
- Rebuilding Testcontainers infrastructure already present in the persistence integration project.
- Generic provider plugins, arbitrary JSON/table/column erasure instructions, and reflection-driven destructive SQL.
- Inline provider calls or synchronous claims that external deletion is complete.
- Compatibility shims for the old behavior-mode configuration.

## Configuration Target

Public self-host variables:

```dotenv
PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=CoLocated
PRIVACY_ERASURE_AUTHORITY_RUNTIME_CONNECTION_STRING=
PRIVACY_ERASURE_AUTHORITY_MIGRATOR_CONNECTION_STRING=
```

Process mapping:

- API receives topology plus runtime authority connection.
- Migration service receives topology plus migrator authority connection.
- Blazor receives neither authority secret.
- In `CoLocated`, neither authority connection is required or opened.
- In `ExternalDatabase`, the process-specific connection is mandatory and errors must name only the missing configuration key.

## Canonical Erasure Flow

1. Authorize the request, append/reuse one typed policy-versioned authority fact, and fence the User before PII enumeration.
2. Apply every classified local disposition in one serializable application transaction.
3. In that transaction, confirm the application mirror/checkpoint, materialize specialized provider work and EventLocation corrections, invalidate cache authority, and persist receipt/status.
4. Commit locally, return `202` with the once-revealed receipt, then let specialized fenced workers settle remote work.
5. Keep `Unknown` provider outcomes reconcilable; never restore local PII or claim remote completion prematurely.
6. Before traffic after startup/restore, replay every authority fact not covered by the current policy version and reject PII recreation for fenced/deleted users.

## Invariants and Constraints

- Authority facts contain bounded metadata only, never live PII, identifiers, selectors, or payloads.
- Every durable User-PII copy and producer maps to exactly one compiled disposition/fence owner in the machine inventory.
- The User is fenced before PII enumeration; shared write, worker, cache, and dispatch boundaries prevent recreation.
- Append authority fact before application mutation; provider calls stay outside the application transaction.
- Application dispositions, mirror, checkpoint, provider work, cache authority, EventLocation corrections, and receipt state remain atomic and serializable.
- Replay is idempotent and monotonic.
- Repositories return entities, not DTOs.
- EF migrations are additive; never edit deployed migration history destructively.
- External runtime access remains function-only; migrator credentials stay outside API/Blazor.
- Logs, health, errors, examples, tests, and planning artifacts never expose credentials or PII.
- Preserve unrelated dirty-worktree changes.
- Every changed source file must retain the two-line `ABOUTME:` header requirement.
- Every delivery and reconciliation opens a fresh scope and reloads persisted tenant/subject ownership; queued/caller identifiers are never authority.
- Sensitive caches are `no-store` or partitioned by tenant/subject/policy version; failed invalidation cannot serve stale PII.

## Restore Guarantee Boundary

`CoLocated` protects against an application transaction failure after the authority append because the append commits separately. It does not protect against restoring the entire application database from a backup created before the erasure.

`ExternalDatabase` protects against that restore only when the application database is restored without also rolling back the authority database. Two database names on the same PostgreSQL server are operationally valid, but a cluster/volume snapshot that restores both together defeats the guarantee.

## Validation Baseline

Planning evidence inspected:

- options and DI selection;
- workflow/applier/repository boundaries;
- application and dedicated authority migrations;
- migration-service registration;
- env/Compose/AppHost/secrets surfaces;
- unit, API integration, and persistence Testcontainers coverage;
- canonical intent, rules, operations, testing, and implementation-plan guidance.

Planning-session baseline:

```bash
dotnet build --configuration Release --verbosity quiet
```

Result: passed on 2026-07-22 with 26 projects, 0 errors, and 41 warnings. The warnings include pre-existing `NU1903` advisories for `System.Security.Cryptography.Xml` 10.0.7. Record a fresh result plus starting SHA/status in `optional-retained-erasure-authority-tasks.md` immediately before runtime edits.

The Senior CTO planning hook is a known non-green baseline: 286 architecture tests, 282 passed, 3 unrelated existing failures, and 1 skipped. The failures concern repository naming, the organization-centric scope-file guardrail finding multiple matches, and explicit HATEOAS permission metadata in existing EventReport/EmailDispatch policies.

## Risks to Watch

- A co-located adapter accidentally sharing the applier transaction.
- Authority/app migrations targeting the same physical database.
- Old mode keys silently changing the deployment guarantee.
- External secrets being passed to the wrong process.
- A synthetic restore test that only deletes rows instead of exercising PostgreSQL restore behavior.
- Active docs/plans retaining contradictory `ApplicationDatabase` guidance.
- Existing deployments needing sequence/fact migration during co-located-to-external cutover.
- An unclassified PII family or producer escaping erasure/fence coverage.
- Provider `Unknown` being mistaken for success or blindly retried.
- Receipt, health, or telemetry becoming a new identifier disclosure surface.
- Authority loss/corruption, credential rotation, topology cutover, unsafe downgrade, or RPO/RTO procedures remaining untested.
- The pre-v1 reset-only policy being documented ambiguously enough to cause operator data loss.

## Handoff

The next action is the unchecked `OREA-100` live EF/provider PII inventory reconciliation, followed by every still-unchecked `OREA-110` runtime/product task. The intent amendment and workstream ownership convergence are complete. Do not implement topology adapters or more erasure families before the Phase 1 contract gate is green.

After Phase 1, update:

- this file with new evidence/decisions;
- the plan if the architecture changes;
- `optional-retained-erasure-authority-tasks.md` with checked items and exact verification output.

## Maintenance Contract

- Keep progress and the next action at the top.
- Add only evidence needed to resume; durable discoveries belong in `dev/_journal/journal.md`.
- Synchronize terminology and status with the plan and tasks ledger after each phase.
- Never store secret values, private connection details, or erased data here.
