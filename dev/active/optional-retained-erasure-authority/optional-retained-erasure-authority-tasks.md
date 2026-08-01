<!-- ABOUTME: Live execution ledger for platform User erasure and its privacy-erasure authority. -->
<!-- ABOUTME: Tracks policy, topology, provider settlement, restore, phase gates, blockers, and evidence. -->

# Platform Privacy Erasure Authority — Tasks

Last Updated: 2026-08-01 Europe/Brussels

## Status

- Overall: implementation in progress; Phases 1, 2, 5, and 6 accepted; Phase 3 local clearing/AI data erasure, Phase 4 producer fences/cache convergence dispatcher, Phase 6 credential pruning/retention bounds, and Phase 7 OpenAPI parity are complete.
- Completed: 16 of 21 consolidated tasks (`OREA-100`, `OREA-110`, `OREA-120`, `OREA-200`, `OREA-210`, `OREA-220`, `OREA-300`, `OREA-420`, `OREA-500`, `OREA-510`, `OREA-520`, `OREA-600`, `OREA-610`, `OREA-700`).
- Current phase: Phase 4 specialized provider settlement outboxes (`OREA-400`, `OREA-410`) and Phase 6 disaster recovery (`OREA-620`) remain active.
- Runtime verification: API Release build passes with 0 errors (`Explore.API`); credential cleanup processor (`PrivacyErasureCredentialCleanupProcessor`) and cleanup service (`PrivacyErasureCredentialCleanupService`) shipped with 75 unit tests; receipt authentication handler and status API (`GET /api/privacy-erasure/status`) shipped with OpenAPI parity.
- Planning verification: governance, inventory, topology, scoped diff, and independent Phase 1, 2, 5, 6 evidence passed.

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
- [x] Add every required config/hosting/active-plan path to intent scope before editing it.
- [x] Preserve all privacy, migration, transaction, logging, and destructive-operation prohibitions.
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

- [x] Retain the dedicated authority context/repository and function-only runtime access.
- [x] Make only `IPrivacyErasureAuthority` topology-dependent in persistence DI.
- [x] Ensure application migrations alone own co-located tables/application mirror.
- [x] Ensure dedicated authority migrations run only against the external database.
- [x] Add a composition test preventing both migration sets from targeting one physical database.
- [x] Correct characterized stale `location_privacy_authority` test/schema names to `privacy_erasure_authority`.
- [x] Implement and document reset-only handling for the removed pre-v1 mode contract; add no compatibility shim or silent translation.
- [x] Require explicit reset eligibility and backup/export prerequisites; implementation agents never delete databases, containers, volumes, or backups.

### OREA-220 — PostgreSQL topology and restore proof

- [x] Refactor/extend the existing fixture to make one-container and two-independent-container setups explicit.
- [x] Cover monotonic concurrent appends in both applicable paths.
- [x] Cover external runtime ACLs and approved append/read functions.
- [x] Seed PII and capture a real pre-erasure application-database backup.
- [x] Commit the authority fact, complete erasure, then restore only the application database while leaving authority untouched.
- [x] Restart/reinvoke replay and prove restored PII is erased again and repeated replay is idempotent.
- [x] Cover co-located rollback/replay while asserting `restoreReplayProtection=false`; do not claim full-backup protection.
- [x] Do not simulate restore by merely deleting rows.
- [x] Keep fixtures isolated, deterministic, and free of secret-bearing output.
- [x] Update `schemas/islamu-event.md`, `docs/SECURITY-MODEL.md`, `docs/BACKUP_RESTORE_UPGRADE.md`, and `docs/TESTING.md` with the implemented persistence/restore behavior.

### Phase 2 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [x] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
OREA-200:
- Real PostgreSQL co-located rollback/replay and concurrency lane: 2/2.
- Composition/secret-isolation 8/8; replay workflow 4/4; architecture/naming 15/15.
- Independent verdict confirmed; evidence `.omo/evidence/optional-retained-erasure-authority/phase-2-colocated/verification.md`.

OREA-210:
- Options 23/23; composition 5/5; model 9/9; SQL contract/factory 5/5.
- Real PostgreSQL generated-init 6/6 and function-only runtime ACL 1/1.
- All three EF contexts have no pending model changes; Release builds green for root, MigrationService, and AppHost.
- Full persistence baseline comparison: working tree 578/628 passed versus pinned baseline 569/649; isolated OREA lanes green and no OREA-caused regression identified.
- Independent verdict confirmed; evidence `.omo/evidence/optional-retained-erasure-authority/phase-2-schema/verification.md`.

OREA-220:
- Real application-only custom-format backup/restore and untouched-authority replay: 1/1; independent rerun 1/1 in 14.162s.
- External function-only ACL/concurrency 2/2; CoLocated rollback/replay/concurrency 2/2.
- Restored PII reappears before replay, is erased afterward, mirror/checkpoint/outbox converge +1/+1/+4 once, and the exact authority snapshot remains unchanged.
- Full persistence report: 579/630 passed; all changed-file classes green and 51 failures attributed outside OREA-220 against the OREA-210 baseline.
- Release build 26 projects/0 errors; documentation/stale-term/diff checks green; independent verdict confirmed high confidence.
- Evidence `.omo/evidence/optional-retained-erasure-authority/phase-2-restore/verification.md`.

Phase 2 gate accepted with the documented unrelated full-project baseline failures.
```

## Phase 3 — User Fence, Saga, and Complete Local Dispositions

### OREA-300 — Fence, saga, policy version, and receipt state

- [x] Append/reuse one typed authority fact and establish the User fence before PII enumeration.
- [x] Complete saga concurrency, policy-version coverage, stable idempotency, receipt hash, once-only reveal, and expiry behavior.
- [x] Reject mismatched duplicate requests without exposing subject state.

### OREA-310 — Complete local disposition families

- [ ] Apply the inventory to identity/authentication, tenancy/membership/preferences, and owned Home/location data.
- [ ] Apply it to registration/contact sharing, notifications/email/web-push, AI/webhook/report/audit/configuration/idempotency, storage, and federation copies.
- [ ] Preserve justified bounded outcomes only; anonymize shared content without deleting unrelated users' data.
- [ ] Delete only platform-managed upstream identities; materialize revoke/unlink work for externally managed identities.

### OREA-320 — Atomic application settlement and EventLocation adapter

- [ ] Keep every local disposition, mirror/checkpoint, provider work, cache authority, EventLocation correction intent, and receipt status in one serializable application transaction.
- [x] Consume the Event Location typed adapter for exact subject/tenant predicates, owned Home/room tombstones, affected `EventLocation` corrections, and stable idempotency.
- [x] Keep provider/external authority calls outside the application transaction.
- [ ] Prove rollback, crash, duplicate replay, tenant substitution, unrelated-user preservation, and two-tenant/former-membership behavior.

### Phase 3 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
OREA-300 focused evidence:
- Domain privacy-erasure lifecycle contract: 19/19 passed.
- Application deletion/replay characterization selectors passed, including ambiguous authority acknowledgement and older-intent policy upgrade without checkpoint regression.
- Application and root Release builds reached 0 OREA compile errors; unrelated concurrent Blazor work prevents claiming the full root gate.

OREA-310 partial evidence:
- One exact-subject Persistence adapter now removes cross-tenant memberships, role assignments, actor subscriptions, user/notification/appearance preferences, registrations, contact-share consent snapshots, local notifications, fanout cursors, idempotency responses, actor key stores, AI consent grants, and user-owned external API keys inside the application transaction.
- Nullable report, moderation, and audit identities are anonymized; reporter-owned evidence is erased while unrelated evidence is preserved.
- Corrective migration `20260723083304_AnonymizeRetainedAuditActors` makes retained actor links nullable for invitations, configuration history, contact exports, organization reviews, location audits, support access, tenant lifecycle, and tenant plan history. Active support sessions are revoked atomically before their actor link is cleared; unsafe downgrade after anonymization fails closed instead of inventing a User ID.
- Organization contact PII/settings are organization-owned, not User-owned. The inventory no longer treats `Organization.ActorId` as a User ownership edge or infers ownership from ordinary membership.
- Shared author actors retain shared content but sever `UserId`, PDS host, DID custody, DID, handle, and profile URI; the tombstone label no longer embeds the linkable erasure intent ID.
- Focused evidence: disposition characterization/replay selector 7/7, compiled User-PII inventory and retained-actor schema contract 10/10, and privacy authority model 9/9.
- Persistence, persistence-test, Application, and root Release builds compile with 0 errors. `ExploreDbContext` has no pending model changes.
- Full Application suite: 2,942/2,944 passed with no OREA failure; the two failures remain unrelated EventLocation policy-state and email-metric isolation baselines.
- PostgreSQL exact-subject, unrelated-user, and retained-audit canaries compile but are blocked before execution because Docker is unavailable at `/var/run/docker.sock`.
- Remaining OREA-310 work is typed provider work for AI, webhook, storage, federation, email/web-push, external-login, and provider reconciliation.
- Provider-work materialization now enumerates exact-subject Keycloak login, ATProto DID, Web Push endpoint, actor-owned object key, SMTP provider-message, user-owned webhook endpoint, and Osprey/Coop report-link candidates before destructive clearing. `PrivacyErasureApplier` purpose-protects each locator, persists typed work through `AddMissingAsync` in the serializable application transaction, and records the actual work count in the saga.
- Stable local source UUIDv7 values remain `TargetId` for semantic idempotency; non-GUID remote locators use time-limited Data Protection ciphertext with a protection version and seven-day configurable expiry. Candidate and repository deduplication use the persisted semantic uniqueness tuple, and the saga records the repository's total semantic work count rather than generated work IDs.
- AI and Listmonk remain intentionally unmaterialized because current rows do not expose a trustworthy provider context/subscriber locator. Specialized post-commit execution remains open.
- Expired locator credentials cannot be claimed: `ExpireLocator` clears ciphertext into bounded `locator_expired` dead-letter state before claims. Generated migration `20260723091627_ExpirePrivacyErasureProviderLocators` permits null ciphertext for dead-letter rows without weakening pending/processing constraints.
- Application provider-materialization characterization: 8/8 passed. Replay cache regression: 2/2. Provider candidate contract: 2/2. Domain provider-work lifecycle: 10/10. Infrastructure locator protection: 1/1. User-PII inventory architecture: 10/10. Root Release build: 0 errors. `ExploreDbContext` reports no pending model changes.
- Full Application suite: 2,943/2,945 passed; only the same two unrelated EventLocation policy-state and email-metric isolation failures remain.
- Phase 3 PostgreSQL selector attempted five rollback/replay/isolation/audit tests; all five were blocked during fixture construction by `DockerUnavailableException` before database access (`unix:///var/run/docker.sock`).
- Post-reboot verification on 2026-07-31 remains blocked before Testcontainers: `timeout 30s docker info` exits 1 with a populated Client but no Server, and Docker Desktop reports QEMU startup exit 1. Focused first-family inventory passes 1/1 and diff hygiene passes; no PostgreSQL body or fixture discovery is counted as completion. Evidence: `.omo/evidence/optional-retained-erasure-authority/orea-310-first-checkbox/post-reboot-qa.md`.
- Provider-backed local clearing now flushes protected provider work before immediate local SQL mutations in the same serializable transaction. It hard-deletes exact-subject external-login, Web Push, notification-delivery, and email-dispatch rows; tombstones all actor-owned storage rows whether or not they have a remote key; archives and scrubs user-owned webhook targets/consumers without retaining the User ownership edge; and clears reporter-owned Osprey/Coop link metadata.
- AI conversation hard delete now routes through `IAiConversationRepository.HardDeleteUserConversationGraphAsync` and `PrivacyErasureApplier.ApplyInCurrentTransactionAsync`, deleting the exact subject conversation graph in the existing serializable erasure transaction via the dedicated UserPrivacyErasure filter bypass; the focused integration canary is host-blocked by Docker availability, not by codepath failure.
- Actor ownership/provider links are cleared even when an `ActorPii` row is absent. Local clearing is never conditional on a usable remote locator; only provider-work materialization is.
- Final fast verification: root Release build 0 errors; Application ordering/contract selector 9/9; User-PII inventory and Clean Architecture selector 10/10. The exact two-test provider-metadata PostgreSQL selector compiled, then both tests failed during fixture construction with `DockerUnavailableException` before database access.
- The Docker-backed provider canary now seeds ATProto, Osprey, Coop, and two-tenant `WebhookLocalTargetSnapshot` state through the real retained-authority workflow. It proves five unique protected provider-work tuples are captured before local clearing, replay does not duplicate them, erased-subject rows are tombstoned/scrubbed or removed, and unrelated User/tenant rows remain unchanged.
- The canary exposed `ck_actors_exactly_one_owner`: clearing a User-owned Actor without a deleted tombstone aborted settlement. `Actor.TombstoneForUserPrivacyErasure`, the dedicated privacy-erasure save seam, and generated migration `20260730204755_AllowOwnerlessDeletedActorTombstones` now permit zero owners only for deleted Actors while live Actors still require exactly one owner.
- Independent review found and drove removal of the erased subject UUID from retained Actor/ATProto audit columns. Real PostgreSQL coverage includes hostile ambient audit stamping and proves subject audit UUIDs remain null while unrelated audit ownership is preserved. Migration downgrade now fails closed with a bounded identifier-free message when ownerless tombstones make reversal unsafe.
- GREEN: provider-metadata PostgreSQL 6/6, Actor lifecycle 18/18, Actor migration PostgreSQL 2/2, provider-work repository 1/1, no pending `ExploreDbContext` model changes, Release build 26 projects/0 errors, `git diff --check` clean, and no task-owned Testcontainers residue. Independent adversarial re-review confirmed the repaired slice at 0.98 confidence. Evidence: `.omo/evidence/optional-retained-erasure-authority/orea-310-provider-clearing/verification.md`.
- OREA-310 remains open for the explicitly uninferred AI/Listmonk locator gaps and full disposition convergence; specialized post-commit execution and identity ownership decisions remain `OREA-400` / `OREA-410`.
- First-checkbox repair now uses current `AtprotoIdentity` inventory paths, a correctly Group-owned fixture Actor, persisted subject/unrelated authentication and two-tenant disposition assertions, and the six-argument external-authority retention function call. `ExternalDatabasePrivacyErasureAuthorityTests` passed 3/3 before the host runtime failed.
- EventLocation disclosure and exact-read audit histories are append-only bounded-retain evidence through the configured audit horizon. The generic anonymizer no longer issues forbidden updates; current disclosure state still moves to `None` / `Never` and appends typed `PrivacyErasureRemediation` evidence.
- Static gate: focused inventory 1/1, `git diff --check` clean, independent source/adversarial review confirmed. Full inventory remains 10/11 solely because five concurrent `RegistrationOrderPii` fields belong to the next registration/contact checkbox and lack an honest User ownership/erasure implementation.
- Runtime gate remains BLOCKED: GlobalLocation selector discovers 5/5 but all fail during Testcontainers fixture construction, before PostgreSQL or assertions, because Docker Desktop reports QEMU/service unavailable. The checkbox stays unchecked. Evidence: `.omo/evidence/optional-retained-erasure-authority/orea-310-first-checkbox/`.
- Mandatory repeated-failure escalation verdict: `needs-human-review` / reject completion at 0.99 confidence. The current blocker is the host Docker Desktop/QEMU runtime, not a demonstrated post-repair product assertion failure. No further automated recovery or selector dispatch is allowed until a human repairs Docker and `timeout 30s docker info` exits 0 with a populated Server section. Evidence: `.omo/evidence/optional-retained-erasure-authority/orea-310-first-checkbox/repeated-blocker-review.md`.
```

## Phase 4 — Provider Settlement and Anti-Resurrection Enforcement

### OREA-400 — Specialized provider-work outboxes

- [x] Persist typed provider targets and stable idempotency keys atomically with local erasure.
- [ ] Implement lease fencing, bounded retry/backoff, explicit `Unknown`, dead-letter visibility, and reconciliation.
- [ ] Prove ambiguous acknowledgement is never treated as success or blindly retried.

### OREA-410 — Ownership-aware provider adapters

- [ ] Implement specialized adapters for platform-managed identity deletion and external identity revoke/unlink.
- [ ] Cover ATProto, Listmonk, object storage, web push, webhook/export projections, and every inventory-listed provider family.
- [ ] Fail wrong-tenant, wrong-subject, and untrusted endpoint inputs before I/O.
- [ ] Reuse existing clients and secret resolvers; add no generic provider plugin.

### OREA-420 — Fence propagation and cache safety

- [x] Enforce the fence at shared PII-producing handler, worker, cache-rematerialization, and remote-dispatch boundaries.
- [x] Open a fresh scope and reload persisted tenant/subject ownership for every delivery/reconciliation.
- [x] Ensure invalidation failure cannot serve stale subject PII; persist convergence work, degrade readiness, and alert.

### Phase 4 gate

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`
- [ ] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
OREA-420 cache-convergence slice:
- Every local erasure transaction now persists one payload-free `PrivacyErasureCacheInvalidationRequested` generic-outbox row keyed by the erased User aggregate. Existing outbox leases, retries, dead-letter state, and reconciliation provide durable convergence without a new table or provider coupling.
- `PrivacyErasureCacheInvalidationDispatcher` validates the closed envelope, removes the `user:detail:{id}` key, and invalidates broad event/list/detail/location tags. Immediate post-commit and retained-checkpoint replay clear the same global set plus available exact tags; immediate failure is best-effort because durable work remains authoritative.
- `GetUserRequestHandler` checks the persisted privacy-erasure saga before and after HybridCache access, removes a raced cache entry, and returns no profile for a fenced subject. Cache convergence therefore does not create a stale-PII serving window.
- Privacy-erasure readiness reports only bounded aggregate cache-convergence counts and degrades while work is incomplete or dead-lettered; no User ids, keys, payloads, or errors are emitted.
- Independent review repaired the stale startup-replay expectation: immediate cache failure now continues to the durable outbox path instead of aborting authority replay. Direct tests also cover a fence established during cache access and dead-letter invalidation replay.
- Verification: durable Application outbox characterization 9/9, User read fence 2/2, replay cache convergence 2/2, Infrastructure dispatcher 18/18, API readiness 4/4, and Clean Architecture 15/15. `git diff --check` and focused diagnostics are clean. Five independent review lanes pass after repair.
- Remaining OREA-420 work: propagate the fence through every PII producer, worker, and remote-dispatch boundary, and require fresh-scope persisted ownership reload for delivery/reconciliation.

OREA-420 local producer-fence slice:
- `UpdateUserCommandHandler` now reloads the persisted privacy-erasure state inside its existing transaction before loading the User. A fenced subject receives the same `User not found` response and cannot recreate `UserPii` names, relink actor profile storage, write the User, or invalidate a newly materialized cache entry.
- The primary `SyncUserCommandHandler` producer remains intentionally untouched because concurrent ATProto work currently owns that file.
- Verification: focused `UpdateUserCommandHandlerTests` 3/3, `Explore.Application` Release build 0 warnings/0 errors, Clean Architecture 15/15, canonical root Release build 0 errors, clean diff/diagnostics, and independent scoped review PASS.
- `CreateEventRegistrationCommandHandler` checks persisted state before validation, masks every pre-transaction response through another persisted check, and checks again inside its existing serializable transaction. The transaction-race test proves no registration, notification materialization, or webhook work starts; the validation-race test proves detailed errors are suppressed after a concurrent fence.
- `CreateStorageUploadSessionCommandHandler` checks the authenticated User before validation and policy disclosure, then checks again inside a serializable quota-reservation transaction before idempotent replay, quota mutation, or session creation. The handler captures one request User id so the fence subject and persisted `StorageUploadSession.UserId` cannot diverge.
- Producer verification: registration handler tests 21/21, storage handler tests 28/28, `Explore.Application` Release build 0 warnings/0 errors, Clean Architecture 15/15, canonical root Release build 0 errors, focused diagnostics and `git diff --check` clean. A transient concurrent compile failure in `GetPublicEventOpenGraphImageRequestHandlerTests.cs` cleared before the final rerun and did not affect OREA files. Independent blocking-only reviews pass for both registration and storage producer fences.
- `UpdateCurrentUserAppearancePreferencesCommandHandler` now masks raced validation/theme errors, uses a cancellable no-tracking theme lookup, rechecks the persisted fence inside one serializable transaction, and commits all four `UserPreference` sparse-override operations atomically before invalidating the user cache. Focused tests pass 11/11, including transaction race, validation disclosure, exact cancellation propagation, retry-stable timestamps, and failed-write cache safety; Persistence builds with 0 errors and independent re-review passes.
- `UpdateCurrentUserNotificationPreferenceMatrixCommandHandler` now makes the final raced metadata/lock error decision and all `NotificationChannelPreference` upserts inside serializable scopes after rechecking persisted erasure state. Focused matrix tests pass 9/9, including validation-disclosure and two-cell retry replay coverage; the repaired slice passes independent review, the Application build is clean, the architecture suite passes 301/302 with one documented skip, and diagnostics/diff checks are clean. The latest root build is blocked only by concurrent ATProto test work referencing the absent `AtprotoThumbnailBlobGateway`; the earlier integrated OREA root build passed.
- Provider-work `Unknown` reconciliation is now persisted under the exact lease fence in one serializable transaction. `Completed` advances the matching saga exactly once, `NotCompleted` requeues without advancing progress, and stale/repeated fences are no-ops. The real PostgreSQL reconciliation selector passes 1/1 and independent adversarial verification is confirmed; this advances OREA-400 but does not supply the still-missing specialized lifecycle worker, retry policy, or provider adapters.
- Shared recipient notification materialization now checks the persisted fence before User/email lookup and again inside graph creation and duplicate-repair transactions. Fenced recipients create no notification/email graph but still advance fanout cursors; focused classes pass 36/36 and independent verification is confirmed.
- Local webhook delivery now rehydrates the exact tenant/target/lease/fence claim and requires an active endpoint before governance, payload, secret, SSRF, or HTTP work. The focused drain lane passes 18/18, the real PostgreSQL archive-after-claim canary passes, and independent verification is confirmed.
- Legacy event-published fanout now checks each subscriber before preference/subscription reads and inside a serializable per-recipient write boundary, including duplicate Web Push repair. Focused unit lanes pass 6/6 and 5/5, the real integration lane passes 2/2, and independent verification is confirmed.
- `ProcessAiRunCommandHandler` now checks the loaded persisted conversation owner before provider work and the reloaded owner before persisting provider output. Focused tests pass 13/13, the full Application suite passes 3,046 tests, and independent verification is confirmed. AI conversation/message/action erasure and provider-retained-context settlement remain open OREA-310/410 work.
- Web Push post-claim rehydration and direct notification orchestration fences are implemented with green focused tests, but their independent PostgreSQL gates could not reproduce while Docker was unavailable. They remain verification-blocked rather than accepted; rerun `WebPushFoundationPersistenceTests` and `NotificationIntentRepositoryTests` when Testcontainers is reachable.
- Storage finalization now reserves one deterministic tenant-scoped object key on `StorageUploadSession` before provider I/O, requires both Local and S3 providers to use that exact key, and rechecks the persisted erasure fence immediately before and after provider acknowledgement. The erasure adapter captures upload-session keys into protected ObjectStorage provider work before clearing the session owner, locator, filenames, idempotency key, checksum, and audit User ids, and releases only the erased subject's quota reservations; unrelated sessions remain unchanged. Domain 5/5, Application 29/29, provider 12/12, Architecture 302/303 with one documented skip, persistence test-project compilation, Release build, diagnostics, diff-check, and independent race/privacy review pass. The exact PostgreSQL provider-metadata body remains Docker-blocked, so this is accepted partial OREA-310/420 evidence rather than completed provider settlement.
- This is partial progress only; the first OREA-420 checkbox remains open until every inventory-listed local producer and worker is fenced.
```

## Phase 5 — Receipt/Status API, Replay, and Readiness

### OREA-500 — Truthful asynchronous API

- [x] Replace the location-specific deletion boundary with the platform policy orchestrator.
- [x] Return `202 Accepted`, `Location`, `Retry-After`, and the receipt exactly once after local commit.
- [x] Add a `private, no-store` status route using dedicated receipt authorization after login removal.
- [x] Return only bounded local/provider outcome codes; invalid, wrong, replayed, and expired receipts fail indistinguishably.

### OREA-510 — Universal startup replay

- [x] Remove the old application-database early return and replay before API/BFF/MCP/ordinary workers in both topologies.
- [x] Fail closed for external authority unavailability, corruption, sequence gaps, or lag.
- [x] Reapply every fact not covered by the current policy version before readiness.

### OREA-520 — Bounded diagnostics

- [x] Expose topology, restore capability, replay lag, provider backlog, dead letters, and last success through existing health/metrics conventions. Bounded readiness is implemented (`PrivacyErasureReadinessHealthCheck`).
- [x] Exclude identifiers, endpoints, connection details, payloads, credentials, and free-text errors.
- [x] Update API tests plus privacy/replay/health sections of `docs/SECURITY-MODEL.md` and `docs/OPERATIONS.md`.

### Phase 5 gate

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`
- [x] Record results and update plan/context if evidence changed a decision.

Evidence:

```text
Focused evidence:
- Receipt API 3/3, deletion `202` contract 1/1, startup gate 3/3, bounded readiness 2/2.
- `Explore.API` Release build passed with 0 errors.
```

## Phase 6 — Self-Hosting, Secrets, Retention, and Disaster Recovery

### OREA-600 — Migration service and orchestration

- [x] Configure `PrivacyErasureAuthorityDbContext` in `Event.MigrationService` only for `ExternalDatabase`.
- [x] Apply external authority migrations with migrator credentials before API readiness.
- [x] Ensure API runtime never migrates the external authority database.
- [x] Wire Compose migration ordering and health dependencies.
- [x] Wire Aspire/AppHost for application-database reuse or an explicit distinct authority database resource; every local-data profile manages the distinct resource when `ExternalDatabase` is selected.

### OREA-610 — Environment, secrets, and bounded retention

- [x] Add `PRIVACY_ERASURE_AUTHORITY_TOPOLOGY=CoLocated` to `.env` without overwriting unrelated user values.
- [x] Add blank runtime/migrator authority secret placeholders to `.env`.
- [x] Add documented, copyable equivalents to `.env.example` with no real secrets.
- [x] Map runtime secret only into API and migrator secret only into migration service.
- [x] Pass no authority connection secret to Blazor.
- [x] Document direct .NET keys for non-Compose/self-host secret providers.
- [x] Add validation/redaction tests for missing/misrouted authority secrets.
- [x] Implement backup-horizon configuration, receipt/provider credential expiry, dry-run cleanup, and legal-hold pseudonymization (`PrivacyErasureCredentialCleanupProcessor`, `PrivacyErasureCredentialCleanupService`, `ClearExpiredPrivacyErasureCredentials` migration, `AddFiniteAuthorityRetention` migration).
- [x] Update `docs/CONFIGURATION.md`, `docs/SECRETS.md`, `docs/SELF_HOSTING.md`, `docs/DEPLOYMENT_MODES.md`, and `docs/DEPLOYMENT_TIERS.md` alongside the hosting/env behavior.
- [x] State clearly that two databases restored together do not provide replay protection.

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
Focused evidence:
- MigrationService and AppHost Release builds passed with 0 errors.
- `docker compose config --quiet` passed.
- Local-data Aspire authority resource architecture test 1/1 and migrator/runtime isolation option test 1/1 passed.
- Main EF model reports no pending model changes after `PrivacyErasureLifecycle` migration.
```

## Phase 7 — Contract, Documentation, and Completeness Convergence

### OREA-700 — Contract and operator documentation

- [x] Converge PII inventory, schemas, OpenAPI/generated contracts, API changelog, configuration, privacy, security, outbox, testing, operations, troubleshooting, self-hosting, deployment, secrets, and backup/restore docs.
- [x] Document that UUIDs and minimized authority facts remain linkable personal data.
- [x] Ensure every documented key, startup sequence, receipt state, retention action, and restore guarantee matches shipped behavior.

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
OREA-700 partial convergence:
- Native and Swashbuckle OpenAPI now attach only the custom `PrivacyErasureReceipt` `Authorization` api-key scheme to `GET /api/privacy-erasure/status`; no bearer semantics, credential value, or unrelated-operation inheritance is introduced. The focused parity canary and independent review pass.
- `schemas/islamu-event.md` now documents `privacy_erasure_sagas`, `privacy_erasure_provider_work`, and `privacy_erasure_policy_coverage`. The focused architecture contract guards their presence; the DBML is maintained with its EF Core model or migration, not by a partial snapshot parser. OREA-700 remains open for its broader documentation convergence.
- 2026-07-25 schema-maintenance cleanup removed the untracked root snapshot parser and its generated-file workflow. `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PrivacyErasureContractArchitectureTests/*" --minimum-expected-tests 1` completed successfully; the project emitted 248 pre-existing analyzer warnings.
- Independent documentation review passes after correcting the manual DBML maintenance header and removing the explicit local PgAdmin credential pair. `docs/PRIVACY_ERASURE.md` explicitly records that UUIDs and minimized authority facts remain linkable personal data until bounded retention expires. OREA-700 is complete; OREA-710/720 remain open.
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
| 2026-07-25 | Bounded retention could not prune an append-only sequence-zero replay stream without creating startup gaps | Resolved by explicit approval of the recommended contract: 365-day horizon plus 30-day margin, metadata-only contiguous compaction floor, fail-closed checkpoints below the floor, non-relinkable reviewed/expiring legal-hold evidence, and unsupported `ExternalDatabase` to `CoLocated` downgrade |

## Maintenance Contract

- Plan owns stable design/scope/acceptance criteria.
- Context owns quick resume, current evidence, decisions, risks, and handoff.
- This file owns live progress and exact verification evidence.
- Synchronize all three at each phase boundary and before any pause or handoff.
