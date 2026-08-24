<!-- ABOUTME: Executable RED-GREEN-CUTOVER ledger for moving API-hosted interval drains to Quartz.NET without moving durable authority. -->
<!-- ABOUTME: Keeps safety, tenant isolation, I-VSD, scheduler persistence, observability, and release-owner evidence dependency-complete. -->

# Periodic Queue-Drain Migration to Quartz.NET - Task Checklist

Last Updated: 2026-08-25 Europe/Brussels

## Status Summary

- **Overall status:** All 39 implementation tasks are complete; release-owner rehearsal R.1 remains outside implementation.
- **Implementation tasks completed:** 39/39.
- **Final implementation evidence:** Release build succeeded with 0 errors/585 warnings; focused matrices passed IntegrationSync drain 8/8, Listmonk adapter 5/5, IntegrationSync SQLite persistence 3/3, PDS drain 1/1, scheduler composition 12/12, API timer ratchets 7/7, and Clean Architecture 15/15.
- **Phase 1 RED:** complete. Task 1.1 confirmed the existing production safety path; Task 1.4 fails only on the forbidden `general-outbox-drain` machine contract.
- **Phase 1 verification:** Release build exit 0 with 13,411 non-clean analyzer warnings and 0 errors; selected clustered Email tests passed 2/2.
- **Phase 2:** GREEN. RequestLoggingPrivacyTests passed 2/2; focused scheduler audit/privacy passed 1/1; QuartzSchedulerCompositionTests passed 7/7; SchedulerHealthCheckTests passed 6/6; ApiLiabilityRatchetTests passed 15/15; the Release build had 0 errors and 305 warnings; and diff check passed. Real concurrent PostgreSQL reconciliation/foreign-key/idempotency, real HTTP administrator/non-administrator/anonymous plus unrelated-denial isolation, and canary-free logs/scopes/metrics are confirmed by two independent reviews. Cleanup left zero relevant processes, running containers, or reports.
- **Phase 3 evidence:** Registration Provider Drains is GREEN. Task 3.6 final counts are 5/5; combined Task 3.5/3.6/current authority 11/11; SQLite lifecycle safety/health 13/13; PostgreSQL lifecycle recovery 2/2; lifecycle architecture/query shape 5/5; retained Release build 304 warnings/0 errors. Portable [`evidence/3.6-cutover.md`](evidence/3.6-cutover.md) has SHA-256 `cc737fe1f16b9a70d57e7042b103197404fa50feaa651705076705b23e5fca5a`; local raw `.omo/start-work/evidence/3.6-cutover.txt` has SHA-256 `b0b8a195b0456a0644d97b4101d3e2b881d5763eb0e0637e9a0aca1adad1ca36` and is not claimed portable.
- **Task 3.3 behavior:** one payload-free UTC 10-second Quartz trigger is the sole cadence authority; durable exact-fence parking precedes provider I/O; authorized event-scoped HAL exposes three evidence-based recovery decisions with idempotent transactional success/refusal audit; delivery/recovery/health use split query contracts; health executes four COUNT commands plus one MIN command; semantic ratchets cover indirect interface/repository/DI/factory/alias paths; and global disablement performs exact one-shot owned-key cleanup through a private never-started scheduler with foreign preservation and no runtime operator surface.
- **Independent review:** Task 3.3 runtime reviewer `st_01a033f6` and architecture/privacy/operations reviewer `st_01a033f7` confirmed that cutover. Task 3.4 reviewer `st_01a03495` confirmed the RED. Task 3.5 runtime reviewer `st_01a034c5` and architecture reviewer `st_01a034c6` independently confirmed GREEN. Task 3.6 runtime reviewer `st_01a034fe` and architecture/privacy/operations reviewer `st_01a034ff` independently confirmed CUTOVER.
- **Cumulative architecture repair:** Task 3.2 made `SchedulerAdminAuthorizationPolicy` the sole instance-admin policy authority, removed `QuartzSchedulerSettings.InstanceAdminPolicyName`, and removed Quartz coupling from `SchedulerAdminController`.
- **Hard gate:** no GREEN starts before its RED fails for the stated reason. Task 3.3 atomically replaced the hosted worker with the live trigger; no dual cadence, compatibility shim, or mixed-version authority is permitted. This is the durable safety/no-dual-cadence boundary.
- **I-VSD record:** [queue-driven worker migration I-VSD](../../../islamic-value-sensitive-design/i-vsd-queue-driven-worker-migration.md) is synchronized to the authoritative Q1-Q12 mapping below; P.1 was accepted on 2026-08-24 through the Senior CTO feedback workflow.
- **Current blockers:** no implementation blocker. Stryker could not run because `dotnet-stryker` is neither installed nor tool-manifested; coordinated deployment/rollback rehearsal remains release-owner task R.1.


## Binding Execution Rules

- Follow RED -> GREEN -> CUTOVER inside every lane. Never register Quartz and its replaced hosted loop together; no compatibility shim, dual authority, or mixed-version authority is permitted.
- Quartz owns cadence only. Claims, explicit tenant-filter bypasses, tenant scopes, leases, fences, cancellation, retries, ambiguity, payload authority, and settlement stay below the API job.
- Every recurring drain uses a stable catalog name/key, `[DisallowConcurrentExecution]`, empty `JobDataMap`, and backlog-collapsing misfire handling. Unexpected failures bubble to Quartz.
- RED tasks add focused executable tests before production edits. Do not use sleeps or solution-level tests; await exact signals with bounded timeouts.
- Each CUTOVER atomically removes the old authority, registers the new authority, reconciles only platform-owned QRTZ keys, advances ratchets, and ships relevant docs, health, metrics, and zero-PII telemetry changes.
- No EF migration is planned. If a RED proves one necessary, stop and reclassify instead of generating one.
- Each phase end has exactly one Release build and at most one selected test-project command.
- Capture comparable hosted-worker and Quartz measurements before each CUTOVER deletion; the context's proposed performance budgets remain unmeasured gates, never completion claims.
- **Research disclosure:** code-review-graph and Context7 were used. AnySearch MCP was not exposed in this environment, so no AnySearch result is claimed.

## Planning / I-VSD Preparation - Completed Gate


- [x] **P.1 Reconcile and approve I-VSD-to-task traceability**
  - **Dependencies:** None; completed as the planning prerequisite before any implementation RED.
  - **Effort:** S.
  - **Guidance/skills:** `islamic-value-sensitive-design`; map amanah/accountability, privacy, non-duplication, tenant justice/fair bounded draining, interruption behavior, observability, and coordinated rollback to concrete RED/GREEN/CUTOVER IDs below without claiming implementation evidence.
  - **Files:** `islamic-value-sensitive-design/i-vsd-queue-driven-worker-migration.md`, `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-plan.md`, `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-tasks.md`, `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-context.md` [MODIFY only to make the traceability and status agree].
  - **Observable acceptance:** met on 2026-08-24. The report and all three workstream artifacts agree on stakeholder harms, tenant/PII boundaries, exact ambiguity outcomes, cancellation/interruption, bounded lane observability, release-owner duties, hard RED gates, implementation count 0, and the next action of 1.1 plus 1.4 RED only; every `I-VSD-Q1`-`I-VSD-Q12` resolves through the authoritative map below.
  - **Evidence:** Senior CTO feedback workflow review dated 2026-08-24; reciprocal links and Q1-Q12 mappings present; `git diff --check` and final artifact-consistency audit required by this planning change.

## Authoritative I-VSD Q1-Q12 Task Mapping

The consultation report, plan, context, and this ledger use the same mapping. **This table is authoritative for `I-VSD-Q1`-`I-VSD-Q12`; no Q-ID below is dangling.**

| I-VSD obligation | Authoritative current task IDs | Executable evidence boundary |
|---|---|---|
| `I-VSD-Q1` - accepted-but-unsettled Email becomes `Unknown` once | 1.1-1.3 | Cluster RED, minimum production correction, then evidence/docs cutover |
| `I-VSD-Q2` - no Quartz general-outbox correctness promise | 1.4-1.6 | Catalog RED/GREEN and hosted-exception ratchet |
| `I-VSD-Q3` - truthful disablement and lane backlog consequence | 2.1-2.3; CUTOVERs 3.3, 3.6, 4.3, 5.3, 5.6, 5.9, 6.3, 6.6, 8.3 | Global scheduler posture plus lane-specific health/metrics/docs |
| `I-VSD-Q4` - submission `Parked`/`DeadLettered` and IntegrationSync retry/dead-letter remain lane-owned | 3.1-3.3; 4.1-4.3 | Registration ambiguity tests and fenced IntegrationSync repair/job/cutover |
| `I-VSD-Q5` - claim-derived tenant execution and cleanup | 3.1-3.6; 4.1-4.3; 5.1-5.9; 6.1-6.6; 7.1-8.3 | Per-lane wrong/missing tenant, named bypass, fresh scope/principal, and exact settlement proofs |
| `I-VSD-Q6` - `PublicationUnknown`, reconciliation, and replay exclusion | 6.1-6.6 | Bulk replay exclusion/audit plus publication/reconciliation RED/GREEN/CUTOVER |
| `I-VSD-Q7` - PDS retry/dead-letter/lease/stale-fence safety | 7.1-7.3; 8.1-8.3 | Extraction parity followed by clustered Quartz ownership proof |
| `I-VSD-Q8` - empty maps and payload/PII-free scheduler/telemetry | 1.1-1.3; 2.1-2.3; 3.1-8.3 | QRTZ inspection, bounded labels, lane privacy assertions, and ratchets |
| `I-VSD-Q9` - authorized pause/resume and refused-control audit with retained evidence | 2.1-2.3; R.1 | Composition/admin audit proof, retained destination contract, and rehearsal retrieval evidence |
| `I-VSD-Q10` - upgrade, node failure, recovery, pause/resume, rollback | R.1 | Dated SQLite/PostgreSQL coordinated stop/observe/start/rollback evidence |
| `I-VSD-Q11` - bounded catch-up and resource use | 3.1-8.3; R.1 | Fixed cadence/batch/concurrency/non-overlap proof plus comparable measured release evidence |
| `I-VSD-Q12` - support responsibility and escalation paths | 2.3; CUTOVERs 3.3, 3.6, 4.3, 5.3, 5.6, 5.9, 6.3, 6.6, 7.3, 8.3; R.1 | Scheduler/lane runbooks, named ownership/escalation, safe diagnostics, rehearsal sign-off |

---


## Phase 1 - Email Crash Window and GeneralOutbox Contract

### Lane: EmailDispatch crash-window proof

- [x] **1.1 RED - Specify clustered EmailDispatch accepted-before-settlement recovery**
  - **Dependencies:** P.1.
  - **Effort:** M.
  - **Guidance/skills:** Quartz clustering, deterministic integration tests; subscribe to transport acceptance before triggering and use a bounded timeout.
  - **Files:** `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzClusterRecoveryTests.cs` [NEW]; `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs` [MODIFY].
  - **Observable acceptance:** RED proves the missing real two-node path: one trigger/transport call, accepted transport followed by lost settlement becomes exactly `Unknown` and is not automatically resent; wrong/missing tenant, cancellation, lease/fence, empty Quartz payload, and zero-PII telemetry are asserted.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchQuartzClusterRecoveryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Evidence (2026-08-24):** confirmed in the isolated execution worktree. The new production-path test passed 1/1: node A executed the real drain through accepted transport and blocked settlement, node B executed the real recovery scan and drain, all durable ledgers became `Unknown`, no second attempt or resend occurred, maps were empty, tenant/fence refusals held, and telemetry contained no dynamic PII. Independent adversarial verification returned `confirmed`; current production behavior is already safe, so Task 1.2 is a no-op candidate rather than a manufactured correction.

- [x] **1.2 GREEN - Preserve or minimally repair EmailDispatch ambiguity safety**
  - **Dependencies:** 1.1 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** durable outbox recovery and transport ambiguity; change production only where RED proves a defect.
  - **Files:** `src/Explore.Infrastructure/EmailDispatchDrainService.cs`, `src/Explore.Persistence/Repositories/EmailDispatchOutboxRepository.cs`, `src/Explore.Application/Contracts/Persistence/IEmailDispatchOutboxRepository.cs`, `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzClusterRecoveryTests.cs` [MODIFY only as required].
  - **Observable acceptance:** exact tenant/id/token/fence rejects stale settlement; cancellation stays recoverable; accepted-but-unsettled is `Unknown`, definitely-not-accepted retains its existing retry outcome; one transport call and no recipient/content/tenant in QRTZ or telemetry.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchQuartzClusterRecoveryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Evidence (2026-08-24):** confirmed with no production-file change. The exact filter passed 2/2. One production-path test proves post-acceptance node interruption, exact tenant/outbox-id/lease-token/fence refusal, observed Quartz cancellation-token propagation, node-B recovery to `Unknown`, one call/no resend, empty maps, and zero-PII telemetry. The second proves a definitely-not-accepted transport failure remains `RetryScheduled` with the existing 3600-second policy and never becomes `Unknown`. Independent adversarial verification returned `confirmed`.

- [x] **1.3 CUTOVER - Publish the Email safety gate without changing authority**
  - **Dependencies:** 1.2.
  - **Effort:** S.
  - **Guidance/skills:** operations, privacy, architecture ratchets; this is evidence cutover, not an Email mode change.
  - **Files:** `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `src/Explore.API/Scheduling/QuartzScheduledDeadlineDispatcher.cs`, `src/Explore.Application/Contracts/Scheduling/ScheduledDeadlinePointerKeys.cs`, `src/Explore.API/Scheduling/EventReminderDispatchJob.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, and focused API tests [MODIFY where the proven contract requires].
  - **Observable acceptance:** docs name exact `Unknown`/retry outcomes, reconciliation, cancellation and active-lease evidence; ratchet prevents scheduler payload/tenant labels; no second Email authority is registered.
  - **GREEN test command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`
  - **Evidence (2026-08-24):** independently confirmed. Docs publish the exact Email ambiguity, retry, identity-fence, cancellation, recovery, and empty-map contract. Scheduler metric groups are bounded to known values plus `other`. One-off deadlines now accept only exact per-job canonical GUID pointer sets and reject extra/missing/malformed/stale keys before scheduler access; recurring Email maps remain empty. Full API relative-path scanning, exact single scheduler enablement, and payload/authority ratchets passed 10/10. Focused dispatcher, reminder, and inventory tests passed 7/7 each; API build completed with 0 warnings and 0 errors. Verifier-generated reports were removed.

### Lane: GeneralOutbox catalog correction

- [x] **1.4 RED - Pin absence of a GeneralOutbox scheduler promise**
  - **Dependencies:** P.1.
  - **Effort:** S.
  - **Guidance/skills:** Application catalog contracts.
  - **Files:** `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** tests fail while `general-outbox-drain` remains in names, bounded labels, or registry, and require `OutboxProcessor` to remain the hosted exception.
  - **RED test command:** `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ScheduledJobRegistryTests/*" --minimum-expected-tests 1 --no-progress`
  - **Evidence (2026-08-24):** confirmed in the isolated execution worktree. The exact command discovered four tests and failed one intended public-catalog assertion because `ScheduledJobNames.All` still contains `general-outbox-drain`; compilation, discovery, PDS descriptors, and unrelated control-plane terminology were unaffected. The later registry assertion is sequenced after the catalog assertion and was not reached because the catalog assertion failed. Independent adversarial verification returned `confirmed`.

- [x] **1.5 GREEN - Remove GeneralOutbox from machine-consumed scheduling contracts**
  - **Dependencies:** 1.4 observed RED.
  - **Effort:** S.
  - **Guidance/skills:** scheduler-neutral contracts; do not alter the unrelated control-plane `general-outbox` status key.
  - **Files:** `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** no drain constant, telemetry key, descriptor, replacement job, or service remains; operational general-outbox status remains.
  - **GREEN test command:** `dotnet test --project tests/Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ScheduledJobRegistryTests/*" --minimum-expected-tests 1 --no-progress`
  - **Evidence (2026-08-24):** independently confirmed. `GeneralOutboxDrain` and its descriptor/catalog exposure are removed; the public names and registry tests pass 4/4; no replacement job/service, alias, shim, or bounded scheduler label remains. PDS planned descriptors and unrelated operational `general-outbox` status terminology are preserved.

- [x] **1.6 CUTOVER - Ratchet and document the hosted GeneralOutbox exception**
  - **Dependencies:** 1.5.
  - **Effort:** S.
  - **Guidance/skills:** architecture tests and operator docs.
  - **Files:** `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/OUTBOX_PATTERN.md` [MODIFY].
  - **Observable acceptance:** source/docs have no `general-outbox-drain` key/registration/promise; ratchet requires `OutboxProcessor` registration and rejects `GeneralOutboxDrainJob`; no alias/shim exists.
  - **GREEN test command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`
  - **Evidence (2026-08-24):** independently confirmed. Source/docs contain no `general-outbox-drain`; operational `general-outbox`, PDS, and managed-control-plane exceptions remain. Semantic Roslyn ratchets require one hosted `OutboxProcessor` and reject GeneralOutbox alias/descriptor/shim/drain/job/service identifiers, aliased hosted/job bases, indirect concrete or registered DI authority paths, and service resolution. Synthetic RED exposed nine escapes; final gate passed 12/12. The test-only Roslyn 5.0.0 dependency is centrally pinned, `PrivateAssets=all`, locked, MIT, signed, license-policy approved, and clean-room attested.

### Phase 1 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/EmailDispatchQuartzClusterRecoveryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- **Evidence (2026-08-24):** independently confirmed. Build exit 0 in 73.342s with 13,411 non-clean analyzer warnings and 0 errors. Selected tests exit 0 in 33.147s with 2 passed, 0 failed, 0 skipped. Before/after worktree status matched; package-lock mutation was restored; reports/temp files were removed; no task process remained.

---

## Phase 2 - Global Scheduler Composition

### Lane: global scheduler composition

- [x] **2.1 RED - Specify global composition and persisted-key behavior**
  - **Dependencies:** Phase 1 verification.
  - **Effort:** L.
  - **Guidance/skills:** ASP.NET composition and Quartz ADO-store inspection.
  - **Files:** `tests/Event.API.IntegrationTests/Features/QuartzSchedulerCompositionTests.cs` [NEW]; `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs` [MODIFY].
  - **Observable acceptance:** RED covers global enablement, Testing/OpenAPI suppression, conditional Email jobs, maintenance under hosted Email, disabled-feature absence, duplicate-trigger prevention, stable keys, empty persisted payload, collapsed misfires, owned-QRTZ-key cleanup that preserves foreign keys, and authorized/refused pause/resume audit records with bounded fields plus a configured retained destination.

  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/QuartzSchedulerCompositionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Evidence (2026-08-24):** independently confirmed in the isolated worktree. Six production-shaped tests yielded 2 passes and 4 intended failures. Actual MSBuild OpenAPI generation/Testing suppression and two production PostgreSQL Quartz nodes with one collapsed misfire acquisition, stable keys, empty maps, and zero persisted payload passed. Global lifecycle under hosted Email, hosted/disabled Email job omission, aggregate stale owned job+trigger cleanup with foreign preservation, and exact 403/401 refusal-audit bounds/privacy failed only on current product gaps. The real API pipeline uses persisted `platform.admin` authority, real fallback authorization, a shared retained external logging provider, dynamic canaries, and SHA-bound terminal evidence. No production files changed.

- [x] **2.2 GREEN - Make Quartz lifecycle global and Email registration conditional**
  - **Dependencies:** 2.1 observed RED.
  - **Effort:** L.
  - **Guidance/skills:** composition roots, Quartz options; replace `UseQuartzEmailDispatch`, with no compatibility property.
  - **Files:** `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs`, `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`, `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `tests/Event.API.IntegrationTests/Features/QuartzSchedulerCompositionTests.cs` [MODIFY].
  - **Observable acceptance:** `Scheduler:Quartz:Enabled` solely controls scheduler services/schema/middleware/status/admin/health; Email mode solely controls Email keys; recurring jobs have one stable payload-free trigger and non-replay misfires.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/QuartzSchedulerCompositionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Evidence (2026-08-24):** independently confirmed. Five Task 2.2-owned tests pass, including a durable Production-host test that starts Quartz, resolves operations, reports healthy scheduler health, maps admin/status routes, retains maintenance, and omits Email jobs under hosted Email. The authority ratchet enforces scheduler-wide `useQuartzScheduler`, rejects `UseQuartzEmailDispatch`, and confines Email mode to drain/recovery registration. The full class is 5 passed/2 intended Task 2.3 failures. No owned-key reconciler or refusal-audit implementation was added.

- [x] **2.3 CUTOVER - Reconcile owned QRTZ keys and publish scheduler operations**
  - **Dependencies:** 2.2.
  - **Effort:** M.
  - **Guidance/skills:** persistent-store reconciliation, health, bounded metrics; use an explicit owned-key allowlist only.
  - **Files:** `src/Explore.API/Scheduling/QuartzOwnedRecurringJobReconciler.cs` [NEW]; `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`, `src/Explore.API/HealthChecks/SchedulerHealthCheck.cs`, `src/Explore.API/Controllers/SchedulerAdminController.cs`, `src/Explore.Application/Contracts/Scheduling/ISchedulerAdminAuditSink.cs`, `src/Explore.API/Scheduling/LoggingSchedulerAdminAuditSink.cs`, `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `tests/Event.API.IntegrationTests/Features/QuartzSchedulerCompositionTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` [MODIFY].
  - **Observable acceptance:** startup removes obsolete/disabled owned jobs/triggers without touching foreign QRTZ rows; tests prove owned-key reconciliation, duplicate suppression, stable keys, misfires and empty payload; health distinguishes disabled/standby/error; metrics have bounded job/outcome labels and zero tenant/PII; authorized and refused scheduler controls emit bounded audit records, and docs require a retained access-controlled destination rather than claiming console logging is durable evidence.

  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/QuartzSchedulerCompositionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`
  - **Evidence (2026-08-24):** independently confirmed. Finite owned-key reconciliation removed only configured-disabled owned keys, preserved foreign scheduler keys, and remained idempotent during a real concurrent PostgreSQL two-node proof. The real production HTTP path returned administrator success, authenticated non-administrator and anonymous denial, and emitted no scheduler audit record for an unrelated denied endpoint. Dynamic canaries were absent from retained rendered logs, structured properties, ambient scopes, and scheduler metric tags. Documentation requires immutable/tamper-evident WORM-or-equivalent retained storage, but the backend is operator-owned and no deployment-specific configuration or certification is claimed.

### Phase 2 Verification

- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/QuartzSchedulerCompositionTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
- **Evidence (2026-08-24):** Release build 0 errors/305 warnings; RequestLoggingPrivacyTests 2/2; focused scheduler audit/privacy 1/1; QuartzSchedulerCompositionTests 7/7; SchedulerHealthCheckTests 6/6; ApiLiabilityRatchetTests 15/15; diff check passed. Two independent reviews confirmed the result. Cleanup left zero relevant processes, running containers, or reports.

---

## Phase 3 - Registration Provider Drains

### Lane: registration submission write

- [x] **3.1 RED - Specify submission-write job and durable safety**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** M.
  - **Guidance/skills:** MediatR delegation, registration claims, tenant isolation.
  - **Files:** `tests/Event.API.IntegrationTests/Features/RegistrationProviderSubmissionWriteQuartzJobTests.cs` [NEW]; `tests/Event.Application.UnitTests/Features/RegistrationSubmissions/RegistrationProviderSubmissionWriteEffectServiceTests.cs`, `tests/Event.Persistence.IntegrationTests/RegistrationProviderFoundationPersistenceTests.cs` [MODIFY].
  - **Observable acceptance:** RED covers one pass, cancellation, duplicate trigger/non-overlap, empty payload, wrong/missing tenant, explicit filter bypass, token/fence settlement, and exact ambiguity: empty persisted mapped answers is terminal ambiguous/manual with no sink call; definite rejection is non-ambiguous.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RegistrationProviderSubmissionWriteTask*Tests/*" --minimum-expected-tests 9 --no-progress`
  - **Evidence (2026-08-24):** confirmed RED: 9 total / 8 intended failures / 1 pass. The required durable safety and future-boundary contract is recorded in `.omo/start-work/evidence/3.1-red.txt`; no Task 3.1 production edit is attributed.

- [x] **3.2 GREEN - Add the submission-write one-pass job**
  - **Dependencies:** 3.1 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** thin Quartz jobs and zero-PII logging.
  - **Files:** `src/Explore.API/Scheduling/RegistrationProviderSubmissionWriteDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobDescriptor.cs`, `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/RegistrationProviderSubmissionWriteQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** `registration-provider-submission-write-drain` sends one drain command, propagates cancellation/failure, is non-concurrent/payload-free, and emits stable name plus bounded counts only.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RegistrationProviderSubmissionWriteTask32JobBoundaryTests/*" --minimum-expected-tests 5 --no-progress`
  - **Evidence (2026-08-24):** confirmed GREEN at the Task 3.2 checkpoint: the exact boundary filter was 5 total / 5 passed / 0 failed. The combined phase filter was 9 total / 6 passed / 3 intended Task 3.3 CUTOVER failures. `.omo/start-work/evidence/3.2-green.txt` records that historical phase split; no trigger, health registration, or hosted-worker removal had been added at that checkpoint.

- [x] **3.3 CUTOVER - Transfer submission-write cadence atomically**
  - **Dependencies:** 3.2.
  - **Effort:** M.
  - **Guidance/skills:** composition cutover, ratchets, operations.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/RegistrationProviderSubmissionWriteWorker.cs` [DELETE]; `tests/Event.API.IntegrationTests/Features/RegistrationProviderSubmissionWriteQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` [MODIFY].
  - **Observable acceptance:** one 10-second trigger replaces the worker in the same change; disabled scheduler has no key; owned stale keys reconcile; docs/health/metrics cover backlog, lease and ambiguity with zero PII; ratchet rejects reintroduction.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RegistrationProviderSubmissionWriteTask33CutoverTests/*" --minimum-expected-tests 3 --no-progress`
  - **Evidence (2026-08-24):** independently confirmed CUTOVER: Task 3.3 3/3, combined phase 10/10, and production-composed PostgreSQL/Quartz interruption, durable pre-invocation restart, and two-tenant scope cleanup 3/3. The live payload-free trigger and hosted-worker deletion landed together. Exact-fence HAL recovery/audit, split queries, five-command health, semantic ratchets, and disabled one-shot exact-key cleanup are confirmed. Portable evidence: [`evidence/3.3-cutover.md`](evidence/3.3-cutover.md), SHA-256 `b5ffbf9eee9dd60a34e4e87a037e44948af4f7e03f02008e4b3b9faa5d42453d`; local raw SHA-256 `9074ce7587338438eb75a03232e77e6c29598e011e9b487e8e07c066d219be5b`. Reviewers: runtime `st_01a033f6`; architecture/privacy/operations `st_01a033f7`.
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Lane: registration subscription lifecycle

- [x] **3.4 RED - Specify subscription-lifecycle job safety**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** M.
  - **Guidance/skills:** provider renewal/sweep lifecycle and fenced aggregates.
  - **Files:** `tests/Event.API.IntegrationTests/Features/RegistrationProviderSubscriptionLifecycleQuartzJobTests.cs`, `tests/Event.Architecture.Tests/RegistrationProviderSubscriptionLifecycleArchitectureTests.cs`, `tests/Event.Persistence.IntegrationTests/RegistrationProviderSubscriptionLifecyclePersistenceSafetyTests.cs` [NEW]; `tests/Event.Application.UnitTests/Services/Registration/RegistrationProviderSubscriptionLifecycleServiceTests.cs`, `tests/Explore.Infrastructure.Tests/Registration/Formbricks/FormbricksRegistrationProviderAdapterTests.cs`, `tests/Explore.Infrastructure.Tests/Registration/GoogleForms/GoogleFormsRegistrationProviderAdapterTests.cs` [MODIFY]. Portable evidence: [`evidence/3.4-red.md`](evidence/3.4-red.md); local raw evidence: `.omo/start-work/evidence/3.4-red.txt` [ignored local record].
  - **Observable acceptance:** RED covers one pass, cancellation, duplicate/non-overlap, empty payload, wrong/missing tenant, explicit bypass, expired lease/new generation, stale token/generation refusal, and exact renewal/sweep outcomes `binding_missing`, `subscription_unsupported`, `renewal_failed`, `sweep_unsupported`, `sweep_failed`.
  - **Task 3.5 boundary RED command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RegistrationProviderSubscriptionLifecycleTask35JobBoundaryTests/*" --minimum-expected-tests 5 --no-progress --maximum-parallel-tests 1`
  - **Task 3.6 CUTOVER RED command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RegistrationProviderSubscriptionLifecycleTask36CutoverTests/*" --minimum-expected-tests 4 --no-progress --maximum-parallel-tests 1`
  - **Evidence (2026-08-24):** independently confirmed by `st_01a03495`. Exact phase filters: `RegistrationProviderSubscriptionLifecycleTask35JobBoundaryTests` 0/5, `RegistrationProviderSubscriptionLifecycleTask36CutoverTests` 0/4, and `RegistrationProviderSubscriptionLifecycleTask34CurrentAuthorityControlTests` 1/1. Supporting counts: real SQLite repository/service 2/5, Application control flow 9/10, provider protocol 2/2, exact named bypass architecture 2/2, bounded zero-PII metrics 1/1, and Release build 0 warnings/errors. Four product-safety REDs are real: (1) accepted-then-cancelled reclaim repeats unkeyed provider creation, (2) accepted-before-settlement process loss repeats creation after reclaim/restart, (3) two-tenant claims lack claim-derived ambient tenant binding, and (4) `IsActive=false` is settled as renewal success rather than definite rejection. Formbricks create is unkeyed `POST /api/v1/webhooks`; response loss is ambiguous while HTTP 408 is classified retryable-before-handoff. Google renewal is addressable only with an existing provider webhook ID through `/watches/{id}:renew`. No production cadence changed. Portable evidence SHA-256 `f6a9dc40237dd553630466fc4c1563b74bbaf9618245ee0802359643c24f52dc`; raw SHA-256 `1b4580453178aefe79a181806e5468ea923012b7921844dae8715af671af5b84`.

- [x] **3.5 GREEN - Add the subscription-lifecycle one-pass job**
  - **Dependencies:** 3.4 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** scoped services, thin Quartz jobs, bounded telemetry.
  - **Files:** `src/Explore.API/Scheduling/RegistrationProviderSubscriptionLifecycleDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/RegistrationProviderSubscriptionLifecycleQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** stable interval job performs one `DrainOnceAsync`, preserves tenant/lease/generation behavior, propagates cancellation/failure, and is non-concurrent/payload-free.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RegistrationProviderSubscriptionLifecycleQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Evidence (2026-08-24):** independently confirmed by runtime reviewer `st_01a034c5` and architecture reviewer `st_01a034c6`. Durable authority persists only `lifecycle_retryable`, `renewal_in_doubt`, `renewal_rejected`, and `operator_dead_lettered`; provider codes are bounded metrics only. Ambiguous handoff preserves the exact lease/token/generation barrier. Formbricks create and Google unknown-ID create versus known-ID renewal outcome matrices cover 4xx, 408, 5xx, transport, and malformed success. Production claim-derived tenant scope restores the prior ambient tenant. Authorized event-scoped HAL recovery is exact-decision idempotent with transactional success/refusal audit; semantic analysis protects lifecycle sole authority. Final counts: boundary 5/5; SQLite 11/11; Application 12/12; Formbricks 18/18; Google Forms 48/48; recovery handlers/HAL/auth 44/44, 17/17, 1/1; foundation persistence 31/31; semantic authority 4/4; catalog/metrics 4/4 and 1/1; current hosted authority 1/1; Task 3.6 intended RED 0/4; Release build 0 warnings/errors. No Task 3.6 registration, owned-key, health, worker-removal, or cadence-transfer work occurred; the hosted worker remains sole 30-second authority. Portable evidence: [`evidence/3.5-green.md`](evidence/3.5-green.md), SHA-256 `57b3770868f2428220cd2b6e1929feed3eedf36a9194c0ff9a24917b7e08ce4b`; raw SHA-256 `d9980eb6bcc8f46d395f7b0c84f34923c90e76b8729c63dab44554af18b27d2f`.

- [x] **3.6 CUTOVER - Transfer subscription-lifecycle cadence atomically**
  - **Dependencies:** 3.5.
  - **Effort:** M.
  - **Guidance/skills:** scheduler registration, runbooks, ratchets.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/RegistrationProviderSubscriptionLifecycleWorker.cs` [DELETE]; `tests/Event.API.IntegrationTests/Features/RegistrationProviderSubscriptionLifecycleQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md` [MODIFY].
  - **Observable acceptance:** one 30-second trigger replaces the worker; disablement removes the key; docs/health/bounded metrics describe renewal/sweep backlog, leases and failures; no shim/dual authority.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/RegistrationProviderSubscriptionLifecycleQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`
  - **Evidence (2026-08-24):** independently confirmed by runtime reviewer `st_01a034fe` and architecture/privacy/operations reviewer `st_01a034ff`. Exact final counts: Task 3.6 5/5; combined Task 3.5/3.6/current authority 11/11; SQLite lifecycle safety/health 13/13; two-node PostgreSQL lifecycle recovery 2/2; lifecycle architecture/query shape 5/5; retained Release build 304 warnings/0 errors. One payload-free UTC 30-second DoNothing trigger has empty maps, no recovery request, and same-key non-concurrency; it is the sole cadence authority. The worker source/registration are deleted; exact owned/desired inventories include the key; disabled PostgreSQL cleanup preserves foreign variants; seven-scalar health/metrics use a dedicated six-COUNT/one-MIN query. Durable tenant/lease-token/generation state, the pre-provider `renewal_in_doubt` barrier, exact-decision recovery, and stale-owner refusal remain the sole provider-call safety authority. Portable evidence: [`evidence/3.6-cutover.md`](evidence/3.6-cutover.md), SHA-256 `cc737fe1f16b9a70d57e7042b103197404fa50feaa651705076705b23e5fca5a`; raw SHA-256 `b0b8a195b0456a0644d97b4101d3e2b881d5763eb0e0637e9a0aca1adad1ca36`.

### Phase 3 Verification

- [x] **Registration Provider Drains release gate: GREEN** from retained Task 3.3 and Task 3.6 constituent evidence; no additional unrecorded phase command is claimed.
- [x] **Retained Release build evidence:** 304 warnings, 0 errors.

---

## Phase 4 - IntegrationSync Safety, Job, and Cutover

### Lane: IntegrationSync safety + job + cutover

- [x] **4.1 RED - Expose stale Processing and unfenced settlement**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** L.
  - **Guidance/skills:** EF concurrency, lease/fence tests, Listmonk ambiguity; current code must fail this RED.
  - **Files:** `tests/Event.Persistence.IntegrationTests/Repositories/IntegrationSyncOutboxRepositoryTests.cs` [NEW]; `tests/Explore.Infrastructure.Tests/Infrastructure/Listmonk/IntegrationSyncDrainServiceTests.cs` [MODIFY]; `tests/Event.API.IntegrationTests/Features/IntegrationSyncDrainQuartzJobTests.cs` [NEW].
  - **Observable acceptance:** tests expose stranded stale `Processing` and id-only completion/failure; required behavior covers validated timeout, new token reclaim, exact tenant/id/token claim and settlement, wrong/missing tenant, explicit bypass, stale-token refusal, cancellation, duplicate trigger, empty payload, and deliberate classification of malformed/null processing evidence. A controlled Listmonk adapter must prove whether accepted-but-unsettled replay is safe: replay-safe evidence permits fenced lease recovery; unsafe or indeterminate evidence stops the slice and requires explicit Unknown/reconciliation reclassification before any cutover.
  - **Persistence RED command:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncOutboxRepositoryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Drain RED command:** `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncDrainServiceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Quartz RED command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncTask42JobBoundaryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Task 4.3 separation command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncTask43CutoverTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Current-authority control:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncTask41CurrentAuthorityControlTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Evidence (2026-08-24):** independently confirmed by `st_01a03554`. Exact final matrices: persistence 2/8; drain/provider 8/14; direct Listmonk adapter 4/5; Task 4.2 boundary 0/5; Task 4.3 cutover 0/4 with zero skips through the full production API host and required real PostgreSQL cleanup; current hosted authority 1/1. Six persistence stale/fence contracts and six drain/provider ambiguity/tenant contracts are intended product REDs. The production-repository SQLite control uses a barrier-released two-DbContext race; stale recovery requires configured timeout and a real initial claim/reclaim; settlement requires exact tenant/item/token/observed-started-at. Protocol facts are binding: subscriber creation is an unkeyed POST, identical replay sends two POSTs, and 408, 500, 503, and accepted-before-response-loss transport ambiguity are currently retryable. No production change occurred. Portable evidence: [`evidence/4.1-red.md`](evidence/4.1-red.md), SHA-256 `4558c9a7c87604a515db0f6b191e2089ce51159d758ca91b9f209ecff545ecd1`; raw SHA-256 `5ba2dd9a40084ddc56f223441480c81d17784daaa29a47ab2766a715e95997b0`.

- [x] **4.2 GREEN - Fence IntegrationSync with existing columns and add its job**
  - **Dependencies:** 4.1 observed RED.
  - **Effort:** L.
  - **Guidance/skills:** repository fencing, options validation, thin job; use existing `ProcessingStartedAt`/`ProcessingLeaseToken`, no migration.
  - **Files:** `src/Explore.Infrastructure/IntegrationSyncProcessorSettings.cs`, `src/Explore.Infrastructure/IntegrationSyncProcessorSettingsValidator.cs` [NEW], `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `src/Explore.Application/Contracts/Persistence/IIntegrationSyncOutboxRepository.cs`, `src/Explore.Persistence/Repositories/IntegrationSyncOutboxRepository.cs`, `src/Explore.Infrastructure/IntegrationSyncDrainService.cs`, `src/Explore.API/Scheduling/IntegrationSyncDrainJob.cs` [NEW], `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, tests from 4.1 [MODIFY].
  - **Observable acceptance:** when 4.1 proves replay safe, bounded validated lease timeout, stale `Processing` reclaim using existing columns, exact tenant/id/token completion/failure, wrong/stale zero-row settlement, cancellation without false settlement, and one payload-free job pass are GREEN with no model/migration delta. If 4.1 is unsafe or indeterminate, this task is blocked until the plan is reclassified to an explicit Unknown/reconciliation model and its generated provider migrations are approved.
  - **Persistence GREEN command:** `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncOutboxRepositoryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Drain GREEN command:** `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncDrainServiceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Quartz GREEN command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncDrainQuartzJobTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Handoff checkpoint (2026-08-24):** implementation exists but remains unreviewed at 16/39. Reported GREEN matrices: persistence 8/8, drain/provider 14/14, direct Listmonk 5/5, Task 4.2 boundary 5/5, recovery handlers 5/5 plus real SQLite recovery/HAL/auth/architecture proofs. The final Release build, LSP/diff/migration/single-authority gates, manual QA, cleanup evidence, raw evidence file, independent review, portable evidence, and record advancement are still required. Do not check this task until those gates are captured.
  - **Direct resume order:** inspect the current diff; rerun the three commands above plus focused `IntegrationSyncRecovery*Tests` and `IntegrationSyncTask42ArchitectureTests`; rerun `IntegrationSyncTask43CutoverTests` expecting 0/4 and `IntegrationSyncTask41CurrentAuthorityControlTests` expecting 1/1; run one Release build and diagnostics; manually verify pre/post-barrier outcomes and tenant restoration; create `.omo/start-work/evidence/4.2-green.txt`; review; then synchronize 17/39.

- [x] **4.3 CUTOVER - Transfer IntegrationSync ownership without the runner**
  - **Dependencies:** 4.2.
  - **Effort:** M.
  - **Guidance/skills:** atomic cutover, health/metrics, ratchets.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/IntegrationSyncProcessor.cs` [DELETE], `src/Explore.API/BackgroundServices/IntegrationSyncHostedDrainRunner.cs` [DELETE]; `tests/Event.API.IntegrationTests/Features/IntegrationSyncDrainQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/OUTBOX_PATTERN.md` [MODIFY].
  - **Observable acceptance:** configured cadence has one Quartz owner; old processor/runner disappear atomically; disabled has no key; docs/health/metrics expose backlog and stale leases with zero PII; ratchet rejects timer loops/unfenced signatures; no shim, dual authority, or migration.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncDrainQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Phase 4 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IntegrationSyncOutboxRepositoryTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

---

## Phase 5 - Local and Incoming Webhook Drains

### Lane: local webhook delivery

- [x] **5.1 RED - Specify Local delivery Quartz and claim safety**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** L.
  - **Guidance/skills:** delivery fencing, SSRF-safe tests, telemetry privacy.
  - **Files:** `tests/Event.API.IntegrationTests/Features/LocalWebhookDeliveryQuartzJobTests.cs` [NEW]; `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookDeliveryDrainServiceTests.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookDeliveryClaimIntegrationTests.cs`, `tests/Event.Persistence.IntegrationTests/TenantIsolation/WebhookRepositoryBypassTests.cs` [MODIFY].
  - **Observable acceptance:** RED requires stale recovery before one pass, wrong/missing tenant, explicit bypass, token+delivery-fence settlement, stale refusal, cancellation, duplicate/non-overlap, empty payload, exact ambiguity (`AlreadyClaimed` on lost fence, never overwrite), and zero destination/payload/tenant telemetry.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/LocalWebhookDeliveryQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **5.2 GREEN - Add the Local delivery one-pass job**
  - **Dependencies:** 5.1 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** Quartz delegation and bounded metrics.
  - **Files:** `src/Explore.API/Scheduling/LocalWebhookDeliveryDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/LocalWebhookDeliveryQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** stable job executes recovery then one batch, propagates cancellation/failure, is non-concurrent/payload-free, and preserves tenant/lease/fence semantics.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/LocalWebhookDeliveryQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **5.3 CUTOVER - Transfer Local delivery ownership atomically**
  - **Dependencies:** 5.2.
  - **Effort:** M.
  - **Guidance/skills:** registration, webhook health/runbook, ratchet.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/WebhookDeliveryProcessor.cs` [DELETE]; `src/Explore.Infrastructure/HealthChecks/LocalWebhookDeliveryHealthCheck.cs`, `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `tests/Event.API.IntegrationTests/Features/LocalWebhookDeliveryQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` [MODIFY].
  - **Observable acceptance:** one configured trigger replaces worker; health/metrics retain backlog/stale-lease meaning with zero PII; docs name pause/recovery and ambiguity; owned keys and ratchet prevent stale/dual authority.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/LocalWebhookDeliveryQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Lane: incoming webhook intake

- [x] **5.4 RED - Specify incoming intake Quartz and tenant execution**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** L.
  - **Guidance/skills:** tenant claims, deterministic lease renewal, machine principals.
  - **Files:** `tests/Event.API.IntegrationTests/Features/IncomingWebhookIntakeQuartzJobTests.cs` [NEW]; `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/IncomingWebhookDrainServiceTests.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/IncomingWebhookTenantExecutionTests.cs`, `tests/Event.Persistence.IntegrationTests/Repositories/IncomingWebhookSettlementRecoveryTests.cs`, `tests/Event.Persistence.IntegrationTests/TenantIsolation/WebhookRepositoryBypassTests.cs` [MODIFY].
  - **Observable acceptance:** RED covers one pass, signal-driven renewal, wrong/missing tenant, explicit bypass, fresh tenant/machine-principal scope, token+fence+generation, lease-loss and caller cancellation, duplicate/non-overlap, empty payload, and exact `Completed`/`LeaseLost`/`AuthorizationDenied`/`Failed` outcomes with zero PII.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookIntakeQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **5.5 GREEN - Add the incoming intake one-pass job**
  - **Dependencies:** 5.4 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** thin jobs and cancellation.
  - **Files:** `src/Explore.API/Scheduling/IncomingWebhookIntakeDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/IncomingWebhookIntakeQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** stable job invokes one drain pass, bubbles cancellation/failure, is payload-free/non-concurrent, and leaves tenant/lease/fence logic below API.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookIntakeQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **5.6 CUTOVER - Transfer incoming intake ownership atomically**
  - **Dependencies:** 5.5.
  - **Effort:** M.
  - **Guidance/skills:** composition, operations, ratchets.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/IncomingWebhookProcessor.cs` [DELETE]; `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `tests/Event.API.IntegrationTests/Features/IncomingWebhookIntakeQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` [MODIFY].
  - **Observable acceptance:** one trigger replaces worker; disabled has none; docs/health/metrics expose intake backlog, lease loss and authorization denial only; owned-key cleanup and ratchet prevent dual authority.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookIntakeQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Lane: incoming webhook effects

- [x] **5.7 RED - Specify incoming effects Quartz and receipt safety**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** L.
  - **Guidance/skills:** effect fencing, atomic receipts, deterministic cancellation.
  - **Files:** `tests/Event.API.IntegrationTests/Features/IncomingWebhookEffectQuartzJobTests.cs` [NEW]; `tests/Event.Application.UnitTests/Services/Webhooks/IncomingWebhookEffectProcessingServiceTests.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/IncomingWebhookTenantExecutionTests.cs`, `tests/Event.Persistence.IntegrationTests/Repositories/IncomingWebhookAtomicEffectTests.cs`, `tests/Event.Persistence.IntegrationTests/Repositories/IncomingWebhookSettlementRecoveryTests.cs` [MODIFY].
  - **Observable acceptance:** RED covers one pass, wrong/missing tenant, explicit bypass, fresh scopes, token+fence+generation, lease/caller cancellation, duplicate/non-overlap, empty payload, idempotent receipt, and exact `retry_scheduled`/`dead_lettered`/`recovered`/`AuthorizationDenied`/`LeaseLost` outcomes with zero PII.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookEffectQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **5.8 GREEN - Add the incoming effects one-pass job**
  - **Dependencies:** 5.7 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** Quartz delegation and receipt authority.
  - **Files:** `src/Explore.API/Scheduling/IncomingWebhookEffectDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/IncomingWebhookEffectQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** stable job performs one pass, propagates cancellation/failure, has empty payload/non-overlap, and keeps receipt/tenant/lease/fence/retry logic below API.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookEffectQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **5.9 CUTOVER - Transfer incoming effects ownership atomically**
  - **Dependencies:** 5.8.
  - **Effort:** M.
  - **Guidance/skills:** scheduler cutover, effect health, runbooks.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/IncomingWebhookEffectProcessor.cs` [DELETE]; `src/Explore.Infrastructure/HealthChecks/IncomingWebhookEffectHealthCheck.cs`, `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `tests/Event.API.IntegrationTests/Features/IncomingWebhookEffectQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` [MODIFY].
  - **Observable acceptance:** one trigger replaces worker; health/metrics preserve backlog/stale receipt and zero-PII dimensions; docs name effect-specific outcomes/recovery; owned-key cleanup and ratchet prevent reintroduction.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/IncomingWebhookEffectQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Phase 5 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/*Webhook*QuartzJobTests/*" --minimum-expected-tests 3 --no-progress`

---

## Phase 6 - Bulk Replay and Provider Publication

### Lane: webhook bulk replay

- [x] **6.1 RED - Specify bulk replay Quartz, fairness, and audit authority**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** M.
  - **Guidance/skills:** transactional audit, bounded fairness, cancellation.
  - **Files:** `tests/Event.API.IntegrationTests/Features/WebhookBulkReplayQuartzJobTests.cs` [NEW]; `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookBulkReplayServiceTests.cs`, `tests/Event.Persistence.IntegrationTests/Repositories/WebhookBulkReplayRepositoryTests.cs`, `tests/Event.Persistence.IntegrationTests/TenantIsolation/WebhookRepositoryBypassTests.cs` [MODIFY].
  - **Observable acceptance:** RED covers one bounded pass, wrong/missing tenant, explicit bypass, cancellation before next operation, duplicate/non-overlap, empty payload, operations-per-pass fairness, atomic audit, exact completed/failed/cancelled ambiguity, and zero selector/endpoint/payload/tenant telemetry.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookBulkReplayQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **6.2 GREEN - Add the bulk replay one-pass job**
  - **Dependencies:** 6.1 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** scoped Quartz jobs and bounded logs.
  - **Files:** `src/Explore.API/Scheduling/WebhookBulkReplayDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/WebhookBulkReplayQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** stable job calls one service pass, is payload-free/non-concurrent, propagates cancellation/failure, and reports bounded counts only.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookBulkReplayQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **6.3 CUTOVER - Transfer bulk replay ownership atomically**
  - **Dependencies:** 6.2.
  - **Effort:** M.
  - **Guidance/skills:** registration, audit operations, ratchet.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/WebhookBulkReplayProcessor.cs` [DELETE]; `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `tests/Event.API.IntegrationTests/Features/WebhookBulkReplayQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` [MODIFY].
  - **Observable acceptance:** one configured trigger replaces worker; disabled has no key; docs/health/metrics cover depth, fairness, cancellation/audit and zero PII; owned-key cleanup and ratchet prevent dual authority.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookBulkReplayQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Lane: webhook provider publication

- [x] **6.4 RED - Specify publication/reconciliation ordering and ambiguity**
  - **Dependencies:** Phase 2 verification.
  - **Effort:** L.
  - **Guidance/skills:** external ambiguity, fenced reconciliation, provider conformance.
  - **Files:** `tests/Event.API.IntegrationTests/Features/WebhookProviderPublicationQuartzJobTests.cs` [NEW]; `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookProviderUnknownReconciliationTests.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/Webhooks/WebhookProviderDispatchIdentityTests.cs`, `tests/Event.Persistence.IntegrationTests/Repositories/WebhookPublicationClaimRepositoryTests.cs` [MODIFY].
  - **Observable acceptance:** RED requires publication before reconciliation, wrong/missing tenant, explicit bypass, token/fence reclaim and stale refusal, cancellation, duplicate/non-overlap, empty payload, stable provider identity, and exact `PublicationUnknown` -> lookup/retry/deferred/manual outcomes, never blind republish; zero PII.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookProviderPublicationQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **6.5 GREEN - Add the provider publication one-pass job**
  - **Dependencies:** 6.4 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** Quartz orchestration and zero-PII telemetry.
  - **Files:** `src/Explore.API/Scheduling/WebhookProviderPublicationDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/WebhookProviderPublicationQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** stable job performs one publication then one reconciliation pass, propagates cancellation/failure, is payload-free/non-concurrent, and preserves unknown/manual outcomes below API.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookProviderPublicationQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`

- [x] **6.6 CUTOVER - Activate provider publication directly under Quartz**
  - **Dependencies:** 6.5.
  - **Effort:** M.
  - **Guidance/skills:** first-authority activation, provider health/runbook; never temporarily register the hosted processor.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/WebhookProviderPublicationProcessor.cs` [DELETE]; `src/Explore.Infrastructure/HealthChecks/SvixWebhookProviderHealthCheck.cs`, `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `tests/Event.API.IntegrationTests/Features/WebhookProviderPublicationQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/WEBHOOK_OPERATIONS_RUNBOOK.md` [MODIFY].
  - **Observable acceptance:** enabled gets exactly one Quartz trigger; disabled none; unregistered hosted class is deleted without becoming an authority; health/metrics/docs expose unknown age/manual reconciliation with zero PII; stale owned keys reconcile and ratchet rejects hosted registration.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookProviderPublicationQuartzJobTests/*" --minimum-expected-tests 1 --no-progress`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Phase 6 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/WebhookBulkReplayQuartzJobTests/*|/*/*/WebhookProviderPublicationQuartzJobTests/*" --minimum-expected-tests 2 --no-progress`

---

## Phase 7 - PDS Drain Extraction

### Lane: PDS scheduler-neutral extraction

- [x] **7.1 RED - Specify the extracted PDS drain boundary**
  - **Dependencies:** Phase 1 verification.
  - **Effort:** L.
  - **Guidance/skills:** Infrastructure extraction, deterministic parallel/cancellation tests, PDS fencing.
  - **Files:** `tests/Explore.Infrastructure.Tests/Infrastructure/Federation/PdsSyncDrainServiceTests.cs` [NEW]; `tests/Event.Application.UnitTests/Features/Federation/AtprotoPdsDeliveryProcessorTests.cs`, `tests/Event.Persistence.IntegrationTests/Federation/AtprotoFederationPersistenceTests.cs` [MODIFY].
  - **Observable acceptance:** RED specifies stable process owner, bounded batch/concurrency, per-claim scope, wrong/missing tenant, explicit bypass, expired lease/new token+fence, stale refusal, cancellation/no new claims, empty persisted payload, exact delivery/retry/failure/lease-lost outcomes, and no DID/repository/payload/tenant telemetry.
  - **RED test command:** `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PdsSyncDrainServiceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

- [x] **7.2 GREEN - Extract PDS orchestration into Infrastructure**
  - **Dependencies:** 7.1 observed RED.
  - **Effort:** L.
  - **Guidance/skills:** scheduler-neutral service, options validation, bounded parallelism; preserve `Atproto:PdsSync`.
  - **Files:** `src/Explore.Application/Contracts/Services/IPdsSyncDrainService.cs` [NEW]; `src/Explore.Infrastructure/Services/Federation/PdsSyncDrainService.cs`, `src/Explore.Infrastructure/Services/Federation/PdsSyncDrainSettings.cs`, `src/Explore.Infrastructure/Services/Federation/PdsSyncDrainSettingsValidator.cs` [NEW]; `src/Explore.Infrastructure/InfrastructureServicesRegistration.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/Federation/PdsSyncDrainServiceTests.cs` [MODIFY].
  - **Observable acceptance:** one pass owns stable lease owner, claim batch, validated 30-900 second lease, scoped bounded concurrency and cancellation; no API/Quartz dependency, PII telemetry, or migration.
  - **GREEN test command:** `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PdsSyncDrainServiceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

- [x] **7.3 CUTOVER - Register extraction and remove embedded orchestration**
  - **Dependencies:** 7.2.
  - **Effort:** M.
  - **Guidance/skills:** DI and architecture boundaries; hosted worker remains sole cadence authority until Phase 8.
  - **Files:** `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`, `tests/Explore.Infrastructure.Tests/Infrastructure/Federation/PdsSyncDrainServiceTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/CONFIGURATION.md`, `docs/OUTBOX_PATTERN.md`, `docs/ARCHITECTURE.md` [MODIFY]; `src/Explore.API/BackgroundServices/PdsSyncWorkerOptions.cs` [DELETE].
  - **Observable acceptance:** `PdsSyncWorker` retains cadence only and delegates one service pass; API-owned options and embedded `RunOnceAsync` orchestration disappear; exactly one hosted authority remains until Phase 8; docs/health/metrics/ratchet record Infrastructure ownership and unchanged keys/leases.
  - **GREEN test command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Phase 7 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PdsSyncDrainServiceTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

---

## Phase 8 - PDS Quartz Cutover

### Lane: PDS Quartz ownership

- [x] **8.1 RED - Specify PDS Quartz registration and clustered recovery**
  - **Dependencies:** Phase 7 verification and Phases 3-6 cutovers.
  - **Effort:** L.
  - **Guidance/skills:** clustered Quartz, PDS recovery, observable claim signals.
  - **Files:** `tests/Event.API.IntegrationTests/Features/PdsSyncDrainQuartzJobTests.cs` [NEW]; `tests/Event.Persistence.IntegrationTests/Federation/AtprotoFederationPersistenceTests.cs`, `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs` [MODIFY].
  - **Observable acceptance:** RED covers one pass, cancellation, duplicate two-node trigger/non-overlap, empty persisted payload, stable key/misfire, wrong/missing tenant, explicit bypass, active lease/fence evidence, node-loss reclaim/new token, stale refusal, and existing exact PDS ambiguity disposition rather than Quartz replay.
  - **RED test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PdsSyncDrainQuartzJobTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

- [x] **8.2 GREEN - Add the PDS one-pass job and catalog contract**
  - **Dependencies:** 8.1 observed RED.
  - **Effort:** M.
  - **Guidance/skills:** thin Quartz jobs, stable catalog, zero-PII telemetry.
  - **Files:** `src/Explore.API/Scheduling/PdsSyncDrainJob.cs` [NEW]; `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`, `src/Explore.Application/Services/ScheduledJobRegistry.cs`, `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`, `tests/Event.API.IntegrationTests/Features/PdsSyncDrainQuartzJobTests.cs`, `tests/Event.Application.UnitTests/Services/ScheduledJobRegistryTests.cs` [MODIFY].
  - **Observable acceptance:** `pds-sync-drain` executes one service pass, propagates cancellation/failure, is non-concurrent/payload-free, and logs bounded counts under its stable name.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PdsSyncDrainQuartzJobTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

- [x] **8.3 CUTOVER - Transfer PDS cadence and close timer ratchets**
  - **Dependencies:** 8.2.
  - **Effort:** L.
  - **Guidance/skills:** high-risk cutover, PDS operations, architecture ratchets.
  - **Files:** `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`, `src/Explore.API/BackgroundServices/PdsSyncWorker.cs` [DELETE]; `src/Explore.Application/Telemetry/BusinessMetrics.cs`, `tests/Event.API.IntegrationTests/Features/PdsSyncDrainQuartzJobTests.cs`, `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`, `docs/OPERATIONS.md`, `docs/CONFIGURATION.md`, `docs/OUTBOX_PATTERN.md`, `docs/ARCHITECTURE.md` [MODIFY].
  - **Observable acceptance:** one configured trigger replaces worker atomically; stale owned keys reconcile; health/docs/metrics expose backlog, active/stale claims and longest lease with zero DID/payload/tenant labels; ratchet detects `Task.Delay` and `PeriodicTimer`; no shim, dual authority, schema change, or migration.
  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PdsSyncDrainQuartzJobTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Architecture ratchet command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

### Phase 8 Verification

- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/PdsSyncDrainQuartzJobTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`

---

## Release-owner Runbook Acceptance - Outside Implementation Phases

- [ ] **R.1 Execute coordinated release/rollback rehearsal and attach evidence**
  - **Dependencies:** Phase 8 verification and every CUTOVER complete; this is a release-owner gate, not an implementation or manual-QA-only phase.
  - **Effort:** L (release owner/SRE).
  - **Guidance/skills:** coordinated deployment, standalone SQLite durability, clustered PostgreSQL/Quartz operations, backup/restore, and incident rollback; use observable state, never blind sleeps or rolling mixed versions.
  - **Files:** `docs/OPERATIONS.md` [only rehearsal-discovered corrections]; release evidence bundle [deployment-system owned].
  - **Observable acceptance:** record owners/timestamps for application and QRTZ backups plus tested restore. For standalone SQLite, prove durable scheduler restart, exact owned-key inventory, disabled behavior, application/Quartz backup and restore, prior-version rollback, and resumed backlog progress without claiming cluster semantics. For clustered PostgreSQL, prove old scale-to-zero, scheduler standby/pause, aggregate active-claim count reaching zero or aggregate expiry/recoverability after hard termination, new start/readiness, scheduler restoration RTO no greater than 30 minutes, per-lane first-fire/backlog-reduction and node-loss recovery, and rollback by new scale-to-zero/standby, aggregate active-claim observation, prior restore/start, and backlog recovery within a 45-minute rollback RTO. Never record lease-token values. Retrieve authorized and refused pause/resume audit records from the configured retained destination and record retention/access controls. Prove no authority overlap, duplicate committed effect, or tenant/payload/secret in QRTZ, telemetry, or audit evidence; record measured RTO/resource-budget pass/fail.

  - **GREEN test command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/QuartzSchedulerCompositionTests/*|/*/*/PdsSyncDrainQuartzJobTests/*" --minimum-expected-tests 2 --no-progress --maximum-parallel-tests 1`
  - **SQLite durability command:** `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/QuartzSqliteDurableSchedulingTests/*" --minimum-expected-tests 1 --no-progress --maximum-parallel-tests 1`
  - **Final architecture command:** `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet -- --treenode-filter "/*/*/ApiLiabilityRatchetTests/*" --minimum-expected-tests 1 --no-progress`

## Deferred / Separate Workstreams

- `dead-letter-summary`, `waitlist-promotion-scan`, and `tenant-maintenance-scan` implementation.
- `OutboxProcessor` migration/refactoring; it remains the durable hosted-service exception.
- Infrastructure consumers that are not API interval loops, and throughput tuning beyond existing controls.
- Any EF migration, Quartz DDL edit, compatibility shim, rolling mixed-version mode, or second authority; a proof requiring one forces re-planning.
