<!-- ABOUTME: Live execution ledger for platform User erasure and its privacy-erasure authority. -->
<!-- ABOUTME: Tracks policy, topology, provider settlement, restore, phase gates, blockers, and evidence. -->

# Platform Privacy Erasure Authority — Tasks

Last Updated: 2026-07-22 Europe/Brussels

## Status

- Overall: implementation in progress; Phase 1 governance, inventory, and topology contract accepted with attributed baseline failures.
- Completed: 4 of 21 consolidated tasks (`OREA-100`, `OREA-110`, `OREA-120`, `OREA-200`).
- Current phase: Phase 2 — Persistence adapters and migration ownership.
- Current blocker: none; governance now authorizes the topology and inventory implementation work.
- Ownership blocker: resolved by `OREA-120`; the historical `.omo` plan is no longer active.
- Runtime verification: Phase 1 Release build and all focused selectors passed; full Architecture reproduced only three documented unrelated failures.
- Planning verification: governance, inventory, topology, scoped diff, and independent Phase 1 evidence passed.

## Maintenance Rules

- This file is the hot execution ledger; update it after every completed task, blocker, or verification run.
- Check a task only when code, colocated tests, documentation/configuration, and required evidence for that task are complete.
- Record exact commands and outcomes under the relevant phase gate.
- Keep the plan stable unless architecture/scope changes; keep context current with evidence and handoff state.
- Preserve unrelated user changes and stop on conflicting in-scope edits.
- Never record secrets, connection-string values, or PII.

## Baseline Checklist

- [x] Record starting branch and SHA.
- [x] Record scoped and full `git status --short`; identify pre-existing in-scope changes.
- [x] Run the canonical root Release build before runtime edits.
- [x] Read the current `platform-privacy-erasure` intent and matching rules/skills.
- [x] Use the code-review graph to refresh impact radius and tests for the exact symbols being changed.

Baseline evidence:

```text
Planning session, 2026-07-22:
- dotnet build --configuration Release --verbosity quiet
- PASS: 26 projects, 0 errors, 41 warnings.
- Existing warnings include NU1903 advisories for System.Security.Cryptography.Xml 10.0.7.

Implementation agent must still record starting branch/SHA/status and rerun the build immediately before runtime edits.

Implementation baseline, 2026-07-22:
- Branch/SHA: `develop` at `ee847015a97bef389ef8208003ca185895548c74`.
- Full status: dirty shared worktree; broad intent paths overlap unrelated changes that must be preserved.
- Scoped status: only this workstream's three planning artifacts were modified; matching privacy-erasure runtime source and tests were clean.
- `dotnet build --configuration Release --verbosity quiet`
- PASS: 26 projects, 0 errors, 41 pre-existing warnings in 5.08 seconds.
- Code-review graph: topology-option change classified high risk; direct configuration symbol found and dependent composition/tests identified for failing-first coverage.
```

## Phase 1 — Governance, Inventory, and Contract Semantics

### OREA-100 — Governance and complete User-PII inventory

- [x] Amend `.claude/contract/intents.yaml` so `platform-privacy-erasure` owns complete User erasure, provider settlement, receipt/status, one authority-first workflow, both topologies, retention, and restore.
- [ ] Add every required config/hosting/active-plan path to intent scope before editing it.
- [ ] Preserve all privacy, migration, transaction, logging, and destructive-operation prohibitions.
- [x] Reconcile the machine User-PII inventory with the current EF model and provider registries; require one disposition, producer/fence owner, retention rule, provider action, and policy version per copy.
- [x] Reject arbitrary executable instructions in the inventory.

### OREA-110 — Typed authority and topology contract

- [x] Replace `PrivacyErasureDurabilityMode` with an authority topology model.
- [x] Set `CoLocated` as the default and validate only `CoLocated` / `ExternalDatabase`.
- [x] Detect and reject `PrivacyErasure:Durability:Mode` with actionable upgrade guidance.
- [x] Require `ConnectionStrings:PrivacyErasureAuthority` only in `ExternalDatabase`.
- [x] Prove a stray authority connection does not activate external topology or get read in `CoLocated`.
- [x] Register one workflow for both topologies; defer only the authority adapter choice to persistence composition.
- [x] Keep `User` as the only executable subject kind and reject arbitrary metadata/selectors.
- [x] Update option/composition tests in the same change.

### OREA-120 — Canonical workstream ownership

- [x] Mark `.omo/plans/platform-wide-privacy-erasure-authority.md` historical after all still-valid requirements/evidence are represented here.
- [x] Remove platform-erasure implementation ownership from Event Location plan/context/tasks.
- [x] Retain only the typed EventLocation disposition/correction adapter boundary in the Event Location workstream.
- [x] Confirm this workstream is the sole active owner of receipt/status, provider settlement, replay, retention, and restore behavior.

### Phase 1 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
- [x] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
OREA-100 inventory:
- RED: 8 tests, 6 passed, 2 failed for missing explicit FenceOwner and executable-instruction rejection.
- Repair RED: 9 tests, 8 passed, 1 failed for curl-to-shell bypass.
- GREEN: 9/9 focused tests; 147 hard-delete, 85 anonymize, 4 bounded-retain, 16 external-action, unclassified=0.
- Exact omission probe fails with `missing: OrganizationPii.Email`.
- Independent adversarial verdict: confirmed (0.99) after curl/wget hostile probes and benign-control coverage.

OREA-110 topology:
- PIN: legacy configuration characterization 8/8.
- RED: new topology tests failed on missing PrivacyErasureAuthorityTopology.
- GREEN: 22/22 focused topology/composition tests and 3/3 bounded request-contract tests.
- Release build: 26 projects, 0 errors, 41 pre-existing package warnings.
- Independent adversarial verdict: confirmed (0.97).
- Explicit next dependency: OREA-200 supplies the real CoLocated IPrivacyErasureAuthority adapter; no fake, no-op, or fallback was introduced.

Phase 1 integrated gate:
- Release build: 26 projects, 0 errors, 41 documented warnings.
- Focused selectors: governance 6/6, inventory 9/9, topology/composition 22/22, request contract 3/3.
- Full Architecture: 296 total, 292 passed, 3 documented unrelated failures, 1 skipped.
- All 11 changed C# files have clean LSP diagnostics; manual YAML, inventory, hostile-instruction, and CoLocated secret-isolation observables passed.
- Independent DoneClaim verdict: confirmed; evidence `.omo/evidence/optional-retained-erasure-authority/phase-1-gate/verification.md`.
```

## Phase 2 — Persistence Adapters and Migration Ownership

### OREA-200 — Co-located and external adapters

- [x] Keep the application ledger repository as the mirror/checkpoint store.
- [x] Implement a co-located authority adapter using a short-lived `ExploreDbContext` and separate commit boundary.
- [x] Prove the authority append survives a forced application mutation rollback.
- [x] Prove replay applies the pending fact exactly once and idempotently confirms the mirror/checkpoint.
- [x] Prove no external authority connection is opened in `CoLocated`.

### OREA-210 — Schema ownership and pre-v1 reset policy

- [ ] Retain the dedicated authority context/repository and function-only runtime access.
- [ ] Make only `IPrivacyErasureAuthority` topology-dependent in persistence DI.
- [ ] Ensure application migrations alone own co-located tables/application mirror.
- [ ] Ensure dedicated authority migrations run only against the external database.
- [ ] Add a composition test preventing both migration sets from targeting one physical database.
- [ ] Correct characterized stale `location_privacy_authority` test/schema names to `privacy_erasure_authority`.
- [ ] Implement and document reset-only handling for the removed pre-v1 mode contract; add no compatibility shim or silent translation.
- [ ] Require explicit reset eligibility and backup/export prerequisites; implementation agents never delete databases, containers, volumes, or backups.

### OREA-220 — PostgreSQL topology and restore proof

- [ ] Refactor/extend the existing fixture to make one-container and two-independent-container setups explicit.
- [ ] Cover monotonic concurrent appends in both applicable paths.
- [ ] Cover external runtime ACLs and approved append/read functions.
- [ ] Seed PII and capture a real pre-erasure application-database backup.
- [ ] Commit the authority fact, complete erasure, then restore only the application database while leaving authority untouched.
- [ ] Restart/reinvoke replay and prove restored PII is erased again and repeated replay is idempotent.
- [ ] Cover co-located rollback/replay while asserting `restoreReplayProtection=false`; do not claim full-backup protection.
- [ ] Do not simulate restore by merely deleting rows.
- [ ] Keep fixtures isolated, deterministic, and free of secret-bearing output.
- [ ] Update `schemas/islamu-event.md`, `docs/SECURITY-MODEL.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, and `docs/TESTING.md` with the implemented persistence/restore behavior.

### Phase 2 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
Pending.
```

## Phase 3 — User Fence, Saga, and Complete Local Dispositions

### OREA-300 — Fence, saga, policy version, and receipt state

- [ ] Append/reuse one typed authority fact and establish the User fence before PII enumeration.
- [ ] Complete saga concurrency, policy-version coverage, stable idempotency, receipt hash, once-only reveal, and expiry behavior.
- [ ] Reject mismatched duplicate requests without exposing subject state.

### OREA-310 — Complete local disposition families

- [ ] Apply the inventory to identity/authentication, tenancy/membership/preferences, and owned Home/location data.
- [ ] Apply it to registration/contact sharing, notifications/email/web-push, AI/webhook/report/audit/configuration/idempotency, storage, and federation copies.
- [ ] Preserve justified bounded outcomes only; anonymize shared content without deleting unrelated users' data.
- [ ] Delete only platform-managed upstream identities; materialize revoke/unlink work for externally managed identities.

### OREA-320 — Atomic application settlement and EventLocation adapter

- [ ] Keep every local disposition, mirror/checkpoint, provider work, cache authority, EventLocation correction intent, and receipt status in one serializable application transaction.
- [ ] Consume the Event Location typed adapter for exact subject/tenant predicates, owned Home/room tombstones, affected `EventLocation` corrections, and stable idempotency.
- [ ] Keep provider/external authority calls outside the application transaction.
- [ ] Prove rollback, crash, duplicate replay, tenant substitution, unrelated-user preservation, and two-tenant/former-membership behavior.

### Phase 3 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
Pending.
```

## Phase 4 — Provider Settlement and Anti-Resurrection Enforcement

### OREA-400 — Specialized provider-work outboxes

- [ ] Persist typed provider targets and stable idempotency keys atomically with local erasure.
- [ ] Implement lease fencing, bounded retry/backoff, explicit `Unknown`, dead-letter visibility, and reconciliation.
- [ ] Prove ambiguous acknowledgement is never treated as success or blindly retried.

### OREA-410 — Ownership-aware provider adapters

- [ ] Implement specialized adapters for platform-managed identity deletion and external identity revoke/unlink.
- [ ] Cover ATProto, Listmonk, object storage, web push, webhook/export projections, and every inventory-listed provider family.
- [ ] Fail wrong-tenant, wrong-subject, and untrusted endpoint inputs before I/O.
- [ ] Reuse existing clients and secret resolvers; add no generic provider plugin.

### OREA-420 — Fence propagation and cache safety

- [ ] Enforce the fence at shared PII-producing handler, worker, cache-rematerialization, and remote-dispatch boundaries.
- [ ] Open a fresh scope and reload persisted tenant/subject ownership for every delivery/reconciliation.
- [ ] Ensure invalidation failure cannot serve stale subject PII; persist convergence work, degrade readiness, and alert.

### Phase 4 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
Pending.
```

## Phase 5 — Receipt/Status API, Replay, and Readiness

### OREA-500 — Truthful asynchronous API

- [ ] Replace the location-specific deletion boundary with the platform policy orchestrator.
- [ ] Return `202 Accepted`, `Location`, `Retry-After`, and the receipt exactly once after local commit.
- [ ] Add a `private, no-store` status route using dedicated receipt authorization after login removal.
- [ ] Return only bounded local/provider outcome codes; invalid, wrong, replayed, and expired receipts fail indistinguishably.

### OREA-510 — Universal startup replay

- [ ] Remove the old application-database early return and replay before API/BFF/MCP/ordinary workers in both topologies.
- [ ] Fail closed for external authority unavailability, corruption, sequence gaps, or lag.
- [ ] Reapply every fact not covered by the current policy version before readiness.

### OREA-520 — Bounded diagnostics

- [ ] Expose topology, restore capability, replay lag, provider backlog, dead letters, and last success through existing health/metrics conventions.
- [ ] Exclude identifiers, endpoints, connection details, payloads, credentials, and free-text errors.
- [ ] Update API tests plus privacy/replay/health sections of `docs/SECURITY-MODEL.md` and `docs/OPERATIONS.md`.

### Phase 5 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
Pending.
```

## Phase 6 — Self-Hosting, Secrets, Retention, and Disaster Recovery

### OREA-600 — Migration service and orchestration

- [ ] Configure `PrivacyErasureAuthorityDbContext` in `Event.MigrationService` only for `ExternalDatabase`.
- [ ] Apply external authority migrations with migrator credentials before API readiness.
- [ ] Ensure API runtime never migrates the external authority database.
- [ ] Wire Compose migration ordering and health dependencies.
- [ ] Wire Aspire/AppHost for application-database reuse or an explicit distinct authority database resource.

### OREA-610 — Environment, secrets, and bounded retention

- [ ] Add `PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=CoLocated` to `.env` without overwriting unrelated user values.
- [ ] Add blank runtime/migrator authority secret placeholders to `.env`.
- [ ] Add documented, copyable equivalents to `.env.example` with no real secrets.
- [ ] Map runtime secret only into API and migrator secret only into migration service.
- [ ] Pass no authority connection secret to Blazor.
- [ ] Document direct .NET keys for non-Compose/self-host secret providers.
- [ ] Add validation/redaction tests for missing/misrouted authority secrets.
- [ ] Implement backup-horizon configuration, receipt/provider credential expiry, dry-run cleanup, and legal-hold pseudonymization.
- [ ] Update `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/DEPLOYMENT_MODES.md`, and `docs/DEPLOYMENT_TIERS.md` alongside the hosting/env behavior.
- [ ] State clearly that two databases restored together do not provide replay protection.

### OREA-620 — Enterprise disaster recovery

- [ ] Define and test backup ordering, RPO/RTO, authority loss/corruption recovery, and the exact readiness-resume point.
- [ ] Define and test runtime/migrator credential rotation.
- [ ] Define and test `CoLocated` to `ExternalDatabase` cutover and explicit acknowledgement for unsafe downgrade.
- [ ] Record the pre-v1 reset-only policy, reset eligibility, backup/export prerequisites, forward repair, and old-backup rehearsal.

### Phase 6 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Secrets.Tests/Explore.Secrets.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
Pending.
```

## Phase 7 — Contract, Documentation, and Completeness Convergence

### OREA-700 — Contract and operator documentation

- [ ] Converge PII inventory, schemas, OpenAPI/generated contracts, API changelog, configuration, privacy, security, outbox, testing, operations, troubleshooting, self-hosting, deployment, secrets, and backup/restore docs.
- [ ] Document that UUIDs and minimized authority facts remain linkable personal data.
- [ ] Ensure every documented key, startup sequence, receipt state, retention action, and restore guarantee matches shipped behavior.

### OREA-710 — Ownership and completeness enforcement

- [ ] Remove obsolete location-specific authority names and legacy behavior-mode configuration.
- [ ] Confirm every current local/external User-PII copy has one implemented disposition and every producer uses the shared fence.
- [ ] Confirm Event Location owns only its typed adapter/correction behavior and this workstream owns platform orchestration.
- [ ] Review the scoped diff for secrets, PII-bearing authority fields, destructive migration behavior, privilege leakage, and unrelated edits.

### OREA-720 — Final behavior evidence

- [ ] Record normal deletion, concurrency, rollback, duplicate/ambiguous append, provider unknown/reconciliation, tenant substitution, receipt expiry, and policy upgrade evidence.
- [ ] Record both topology paths, old-backup replay, unrelated-user preservation, zero PII recreation, and zero unclassified copies.
- [ ] Add only durable non-obvious findings to `dev/_journal/journal.md` through the canonical finding workflow.

### Phase 7 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Domain.UnitTests/Event.Domain.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
Pending.
```

## Deferred / Explicitly Out of Scope

- [ ] Distributed transactions between authority and application databases.
- [ ] Arbitrary operator-authored erasure SQL or selector payloads.
- [ ] Automatic proof that two configured databases have independent backup lifecycles.
- [ ] External-to-co-located downgrade automation.
- [ ] New secret-loader abstraction unless implementation evidence proves process-specific named connections insufficient.
- [ ] New Testcontainers framework when the existing persistence fixture can be extended.

## Blockers and Decision Changes

| Date | Item | Status / resolution |
|---|---|---|
| 2026-07-22 | Canonical intent mandated the old `ApplicationDatabase` default | Resolved; intent now requires one authority-first workflow with `CoLocated` / `ExternalDatabase`, while `OREA-100` inventory reconciliation and every `OREA-110` runtime/product task remain incomplete |
| 2026-07-22 | Existing broader `.omo` plan defaulted to the old mode model and owned unfinished platform erasure | Resolved by `OREA-120`; valid scope/evidence is represented here and the `.omo` plan is historical |
| 2026-07-22 | Event Location planning owned global erasure/topology work | Resolved by `OREA-120`; only the typed EventLocation adapter boundary remains there |

## Maintenance Contract

- Plan owns stable design/scope/acceptance criteria.
- Context owns quick resume, current evidence, decisions, risks, and handoff.
- This file owns live progress and exact verification evidence.
- Synchronize all three at each phase boundary and before any pause or handoff.
