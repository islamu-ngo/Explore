<!-- ABOUTME: Resume-critical context for migrating API-hosted interval queue drains to Quartz.NET under hard RED-first safety gates. -->
<!-- ABOUTME: Records verified runtime gaps, binding cutover decisions, evidence boundaries, and the exact implementation handoff. -->

# Periodic Queue-Drain Migration to Quartz.NET - Context

Last Updated: 2026-08-25 Europe/Brussels

## SESSION PROGRESS

- **Implementation status: COMPLETE.** All 39 implementation tasks are complete; R.1 is an external release-owner gate.
- Re-read the worker implementations, scheduler composition, persistence contracts, existing tests, package pins, and operator documentation against the Senior CTO audit rather than carrying forward the prior artifacts' assumptions.
- The Senior CTO audit **initially rejected the prior artifact set**. It did not prove each lane's crash/settlement semantics, omitted persisted Quartz-key reconciliation and lane observability, understated the `IntegrationSync` safety defect, and used premature "ready for Phase 1" language.
- P.1 I-VSD traceability was accepted on 2026-08-24 through the Senior CTO feedback workflow after the report, plan, context, and ledger were synchronized to the current Q1-Q12 task mapping.
- **Current verdict:** approve for implementation only with a hard, per-slice RED gate. Approval means an agent may create the named failing proof for one slice; it does not authorize production edits before that RED is observed and recorded.
- Synchronized source-of-truth links:
  - [Implementation plan](./queue-driven-worker-migration-plan.md) (`dev/active/queue-driven-worker-migration/queue-driven-worker-migration-plan.md`)
  - [Task ledger](./queue-driven-worker-migration-tasks.md) (`dev/active/queue-driven-worker-migration/queue-driven-worker-migration-tasks.md`)
  - [I-VSD queue-driven worker migration report](../../../islamic-value-sensitive-design/i-vsd-queue-driven-worker-migration.md) (`islamic-value-sensitive-design/i-vsd-queue-driven-worker-migration.md`)
- The Phase 2 Release build completed with **0 errors and 305 warnings**. This is a compile result, not a warning-quality or deployment certification.
- code-review-graph and Context7 were used for repository impact review and official Quartz documentation. AnySearch MCP was not exposed, so no AnySearch result is claimed.

## IMPLEMENTED

- IntegrationSync uses tenant/id/token/observed-start exact settlement fences. Stale pre-handoff work is reclaimable; stale or malformed post-handoff evidence parks as ambiguous without replaying the unkeyed Listmonk POST. Claim-derived tenant context is restored after every pass.
- Registration-provider, IntegrationSync, local webhook delivery, incoming intake/effects, bulk replay, provider publication, and PDS each have one stable payload-free Quartz job and no retained timer-loop authority.
- Final checks: Release build 0 errors/585 warnings; IntegrationSync drain 8/8; Listmonk adapter 5/5; IntegrationSync SQLite 3/3; PDS drain 1/1; scheduler composition 12/12; timer ratchet 7/7; Clean Architecture 15/15; `git diff --check` and migration/model-snapshot scan clean.
- Task 3.5 durable design remains unchanged. Task 3.6 transfers cadence only: one payload-free UTC 30-second DoNothing trigger is the sole authority; worker source/DI are removed; exact owned/desired inventories and global-disabled cleanup include the key; a dedicated Application/Persistence health query emits seven bounded zero-PII aggregates with six COUNT commands and one MIN command. Durable tenant/lease-token/generation state, the pre-provider `renewal_in_doubt` barrier, exact-decision recovery, and stale-owner refusal remain the sole provider-call safety authority; Quartz recovery never decides settlement.
- Phase 2 evidence: RequestLoggingPrivacyTests 2/2, focused scheduler audit/privacy 1/1, QuartzSchedulerCompositionTests 7/7, SchedulerHealthCheckTests 6/6, and ApiLiabilityRatchetTests 15/15; real concurrent PostgreSQL reconciliation preserved foreign state and was idempotent; real HTTP proved administrator success, authenticated non-administrator and anonymous denial, and unrelated-denial isolation; logs, scopes, and metrics were canary-free. Two independent reviews confirmed the result and cleanup left zero relevant processes, running containers, or reports.
- `OutboxProcessor` remains the sole hosted general-outbox authority. Semantic Roslyn ratchets reject scheduler aliases, shims, replacement jobs/services, aliased bases, indirect DI authority paths, and service resolution while preserving PDS and managed-control-plane exceptions.
- Task 3.3 is confirmed: Quartz is the sole payload-free 10-second cadence authority; the exact tenant/token/fence claim is durably parked before provider I/O; recovery is exposed through authorized event-scoped HAL with three exact decisions, evidence references, idempotence, and durable success/refusal audit; mutation methods return entities and delivery/recovery/health use split queries; health is exactly four COUNT commands plus one MIN command; semantic ratchets cover indirect interfaces, repositories, DI/factories, and aliases; and global disablement performs one-shot exact owned-key cleanup through a private never-started scheduler without a runtime operator surface.
- Task 3.3 portable evidence: [`evidence/3.3-cutover.md`](evidence/3.3-cutover.md), SHA-256 `b5ffbf9eee9dd60a34e4e87a037e44948af4f7e03f02008e4b3b9faa5d42453d`. Local raw evidence: `.omo/start-work/evidence/3.3-cutover.txt`, SHA-256 `9074ce7587338438eb75a03232e77e6c29598e011e9b487e8e07c066d219be5b`; `.omo` is local and is not claimed portable. Runtime reviewer `st_01a033f6` confirmed all behavior subject only to final status sync; architecture/privacy/operations reviewer `st_01a033f7` confirmed substantive closure and identified portability as the sole remaining blocker, resolved by the tracked snapshot.
- Task 3.4 portable evidence: [`evidence/3.4-red.md`](evidence/3.4-red.md), SHA-256 `f6a9dc40237dd553630466fc4c1563b74bbaf9618245ee0802359643c24f52dc`. Local raw evidence: `.omo/start-work/evidence/3.4-red.txt`, SHA-256 `1b4580453178aefe79a181806e5468ea923012b7921844dae8715af671af5b84`; `.omo` is local and is not claimed portable. Independent reviewer `st_01a03495` confirmed Task 3.4 RED and the Task 3.5 handoff.
- Task 3.5 portable evidence: [`evidence/3.5-green.md`](evidence/3.5-green.md), SHA-256 `57b3770868f2428220cd2b6e1929feed3eedf36a9194c0ff9a24917b7e08ce4b`. Local raw evidence: `.omo/start-work/evidence/3.5-green.txt`, SHA-256 `d9980eb6bcc8f46d395f7b0c84f34923c90e76b8729c63dab44554af18b27d2f`; `.omo` is local and is not claimed portable. Runtime reviewer `st_01a034c5` and architecture reviewer `st_01a034c6` independently confirmed GREEN.
- Task 3.6 portable evidence: [`evidence/3.6-cutover.md`](evidence/3.6-cutover.md), SHA-256 `cc737fe1f16b9a70d57e7042b103197404fa50feaa651705076705b23e5fca5a`. Local raw evidence: `.omo/start-work/evidence/3.6-cutover.txt`, SHA-256 `b0b8a195b0456a0644d97b4101d3e2b881d5763eb0e0637e9a0aca1adad1ca36`; `.omo` is local and is not claimed portable. Runtime reviewer `st_01a034fe` and architecture/privacy/operations reviewer `st_01a034ff` independently confirmed CUTOVER. AnySearch MCP and Context7 MCP were unavailable; no result from either tool is claimed.
- Task 4.1 portable evidence: [`evidence/4.1-red.md`](evidence/4.1-red.md), SHA-256 `4558c9a7c87604a515db0f6b191e2089ce51159d758ca91b9f209ecff545ecd1`. Local raw evidence: `.omo/start-work/evidence/4.1-red.txt`, SHA-256 `5ba2dd9a40084ddc56f223441480c81d17784daaa29a47ab2766a715e95997b0`; `.omo` is local and is not claimed portable. Independent reviewer `st_01a03554` confirmed the corrected RED, exact matrices, unkeyed protocol facts, no-production-change boundary, required no-skip PostgreSQL/full-host proof, and cleanup. AnySearch MCP and Context7 MCP were unavailable; no result from either tool is claimed.
- The cumulative Task 3.2 scheduler-admin architecture repair is GREEN: `SchedulerAdminAuthorizationPolicy` is the sole instance-admin policy authority; `QuartzSchedulerSettings.InstanceAdminPolicyName` and `SchedulerAdminController` Quartz coupling are removed.
- Implementation is complete under the preserved RED-first decisions; this does not claim a deployment-specific WORM backend or release-owner rehearsal.

## NEXT

1. Release owner executes R.1 coordinated deployment/rollback rehearsal; no rolling mixed-version window is supported.
2. Install or tool-manifest `dotnet-stryker` before claiming the mutation threshold gate.

## BLOCKERS

- No implementation blocker remains. Mutation scoring and release rehearsal are unexecuted environment/operator gates.
- Deployment-specific immutable/tamper-evident WORM-or-equivalent retention backend, access review, and certification remain operator-owned.

## Binding Decisions

1. **Quartz owns cadence only.** Each job performs one bounded pass and delegates claim, lease, fence, retry, tenant, reconciliation, and transport behavior to scheduler-neutral services. Jobs do not loop, sleep, drain-until-empty, or catch unexpected infrastructure failures as success.
2. **`OutboxProcessor` stays a `BackgroundService`.** It is the durable general-outbox exception required by `docs/QUICK_REFERENCE.md` rule 27 and `.agents/rules/api-scheduling.md`; no `GeneralOutboxDrainJob` is introduced.
3. **Global scheduler authority is `Scheduler:Quartz:Enabled`.** EmailDispatch mode controls only EmailDispatch job registration. OpenAPI generation and the `Testing` environment continue to suppress runtime scheduling.
4. **One authority per lane.** The hosted loop is removed in the same version that registers its Quartz replacement. There is no dual-mode compatibility shim, alias registration, or feature flag that permits both authorities.
5. **No mixed versions.** Upgrade and rollback are coordinated stop/wait/start operations. All old replicas stop, active leases expire or are safely settled, persisted Quartz keys are reconciled, and only then may the new version start. Rolling old/new coexistence is unsupported.
6. **No compatibility shim.** This pre-v1 migration breaks misleading composition state and obsolete scheduler identities directly. Do not preserve `UseQuartzEmailDispatch`, old worker-owner aliases, duplicate job names, or old/new configuration aliases.
7. **Stable identity is an operator contract.** Every recurring drain gets one catalog name, one `JobKey`, one derived trigger key, `[DisallowConcurrentExecution]`, and an empty `JobDataMap`. The attribute protects executions of the same job definition/`JobKey`; it is not a class-wide lock and does not replace durable claims.
8. **Durable state remains authoritative.** Quartz fire/recovery state never decides whether an external side effect was committed. Existing lane-specific token/fence/reconciliation state does, and any uncovered defect is repaired behind a RED proof before that lane is cut over.
9. **Configuration sections and bounded controls remain stable where semantics are sound.** Existing enabled, cadence, batch, concurrency, retry, and lease controls stay authoritative. PDS orchestration moves below API while retaining `Atproto:PdsSync`; no speculative replacement section is added.
10. **Unexpected failures bubble.** Per-item expected outcomes remain durable lane results; unexpected database/service failures fail the Quartz execution so scheduler telemetry and operator surfaces remain truthful.
11. **No hand-edited Quartz DDL or EF migration for scheduler rows.** Existing idempotent embedded ADO schema initialization remains the schema path. Desired job/trigger reconciliation is application/operator behavior, not schema migration.
12. **Performance budgets are gates, not claims.** The proposed budgets below are currently unmeasured and must not be described as met until each slice records comparable before/after evidence.

## Persisted `QRTZ_` Key Reconciliation

- Current persistent configuration uses `UseProperties=true`, System.Text.Json serialization, startup schema validation, stable scheduler name/instance ID settings, optional clustering, and the configured table prefix (default `QRTZ_`).
- Quartz.NET 3.19.1 Microsoft DI scheduling documents `Scheduling.IgnoreDuplicates=false` and `Scheduling.OverWriteExistingData=true` defaults. Same-key definitions can therefore be overwritten at startup; **obsolete or renamed keys are not thereby deleted**.
- Every cutover manifest must list exact desired job and trigger keys plus exact retired keys. After all old replicas stop, reconciliation must:
  1. inspect the configured scheduler namespace/table prefix;
  2. add/update desired definitions under stable keys;
  3. unschedule and delete only the workstream's known retired triggers/jobs;
  4. prove disabled lanes have neither a live trigger nor an orphaned durable job unless deliberately retained;
  5. leave unrelated scheduler entries untouched; and
  6. record before/after bounded key/state evidence without `JobDataMap` payload or tenant data.
- A key rename is a breaking operator event, not an alias opportunity. Rollback uses the reverse manifest after the stop/lease-wait boundary. Never reconcile keys while mixed application versions are live.

## Verified Current Quartz 3.19.1 Behavior

- `Directory.Packages.props` pins `Quartz` and `Quartz.Extensions.Hosting` to **3.19.1**; API package references consume the central pin.
- `[DisallowConcurrentExecution]` applies to a job definition instance identified by its job key, not globally to every instance of the job class. Database claims/fences remain required across different keys and non-Quartz callers.
- Jobs requesting recovery can be re-executed after hard shutdown, but the queue drains do not use Quartz recovery as side-effect truth. Durable lane policy decides retry, `Unknown`, reconciliation, or dead-letter behavior.
- Current cron registrations use UTC plus `WithMisfireHandlingInstructionDoNothing`; simple maintenance intervals use `WithMisfireHandlingInstructionNextWithRemainingCount`. Missed cadence collapses to a later bounded pass rather than replaying every missed occurrence.
- Current scheduler composition registers recurring EmailDispatch jobs whenever Quartz is composed, while `AddApiQuartzScheduler(... enabled: useQuartzEmailDispatch && !isOpenApiGeneration)` prevents Quartz composition unless EmailDispatch selected Quartz. `ApiHostApplicationExtensions` and `ApiHostStartupExtensions` repeat the same EmailDispatch gate for middleware/endpoints and schema application. This is the verified lifecycle coupling to remove.
- Official fallback sources consulted because MCP research tools were unavailable:
  - [Quartz.NET - More About Jobs](https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/more-about-jobs.html)
  - [Quartz.NET - Microsoft DI Integration](https://www.quartz-scheduler.net/documentation/quartz-3.x/packages/microsoft-di-integration.html)
  - [Quartz.NET - Configuration Reference](https://www.quartz-scheduler.net/documentation/quartz-3.x/configuration/reference.html)

## Verified Worker Roster

### Remain Hosted Services

- `OutboxProcessor` - explicit durable general-outbox exception.
- `ManagedControlPlaneRegistrationWorker` - retry-until-success bootstrap, not an interval queue drain.
- Startup synchronization and event/stream consumers without a periodic queue-drain loop remain out of scope.

### Migration Candidates

| Slice | Current worker | Current cadence/boundary | Required disposition |
|---|---|---|---|
| Registration | `RegistrationProviderSubmissionWriteDrainJob` | Sole payload-free Quartz cadence every 10 seconds; durable pre-provider barrier; `DrainRegistrationProviderSubmissionWriteEffectsCommand` | Task 3.3 confirmed; hosted worker removed |
| Registration | `RegistrationProviderSubscriptionLifecycleDrainJob` | Sole payload-free Quartz cadence every 30 seconds; fenced `RegistrationProviderSubscriptionLifecycleService.DrainOnceAsync` | Task 3.6 confirmed; hosted worker removed |
| Integration | `IntegrationSyncProcessor` | `Task.Delay`; `IIntegrationSyncDrainService` through `IntegrationSyncHostedDrainRunner` | Repair stale/settlement safety first, then replace |
| Webhook | `WebhookDeliveryProcessor` | initial delay + `PeriodicTimer`; stale recovery then `IWebhookDeliveryDrainService.ProcessBatchAsync` | Preserve recovery-before-drain order |
| Webhook | `IncomingWebhookProcessor` | `PeriodicTimer`; `IIncomingWebhookDrainService` | Preserve renewable fenced tenant execution |
| Webhook | `IncomingWebhookEffectProcessor` | `PeriodicTimer`; `IIncomingWebhookEffectDrainService` | Preserve renewable fenced tenant execution |
| Webhook | `WebhookBulkReplayProcessor` | initial delay + `PeriodicTimer`; `IWebhookBulkReplayService.ProcessQueuedAsync` | Preserve bounded audit behavior |
| Webhook | `WebhookProviderPublicationProcessor` | `PeriodicTimer`; publication then reconciliation, but **not registered by the API host** | Register directly as Quartz; never add a temporary hosted registration |
| PDS | `PdsSyncWorker` | `Task.Delay`; claim, stable owner, bounded parallelism, and per-claim scopes embedded in worker | Extract scheduler-neutral drain first, then replace |

## Lane Safety and Observability Baseline

| Lane | Verified safety already present | Hard gap that the slice must expose RED before production edits |
|---|---|---|
| EmailDispatch | Atomic claim/admission, provider-handoff fence, stale recovery to safe retry or `Unknown`, operator reconciliation, health and bounded metrics | No real two-node Quartz drain proof for transport accepted before local settlement; generic cluster and delegation tests are insufficient |
| Registration submission write | Task 3.3 confirmed: sole Quartz cadence, durable pre-provider handoff barrier, exact tenant/token/fence settlement, authorized evidence-based HAL recovery with durable audit/idempotence, split query contracts, five-command bounded health, semantic ratchets, and disabled exact-key cleanup | CUTOVER complete; preserve Quartz as sole cadence authority and the explicit recovery boundary for parked ambiguity |
| Registration subscription lifecycle | Task 3.6 confirmed: sole payload-free Quartz cadence, PostgreSQL advisory lock, exact lease-token/generation settlement, `renewal_in_doubt` barrier and recovery, two-node interruption/restart safety, seven-scalar health, semantic ratchets, and exact disabled cleanup | CUTOVER complete; preserve durable authority and no hosted fallback |
| IntegrationSync | Atomic pending/retry-to-`Processing` claim token, active-claim reload, privacy-erasure check, retry/dead-letter service tests, tenant-scoped settings/secrets selected from the persisted tenant ID | `GetPendingBatch` never selects stale `Processing`; cancellation/crash after claim strands rows indefinitely. `MarkAsCompleted` updates by ID only, and `MarkAsFailed` loads by ID only: neither predicates settlement on active status/token/fence. No durable ambiguous-result reconciliation and no queue health/metrics. This must be fixed before job cutover |
| Local webhook delivery | Per-tenant claim limits, lease token/delivery fence, stale recovery before drain, fenced settlement, claim-lag/outcome metrics, `LocalWebhookDeliveryHealthCheck` | Preserve ordering and prove one-pass job behavior; health wording/job identity must converge |
| Incoming webhook intake | Expired-claim recovery with new token/fence/generation, active-claim checks, lease renewal, isolated tenant/machine-principal execution, persistence and tenant-execution tests | No dedicated intake lane readiness surface; prove scheduler cancellation/lease-loss behavior without weakening scopes |
| Incoming webhook effects | Renewable token/fence/generation claims, receipt-backed execution, `IncomingWebhookEffectHealthCheck`, processing metrics | Prove scheduler cancellation/lease-loss and preserve replay/retention invariants |
| Webhook bulk replay | Bounded audited replay service and repository/Application/API tests | No registered Quartz boundary and no dedicated queue health/oldest-due metric |
| Provider publication | Token/fence claims, `PublicationUnknown`, automatic/manual reconciliation, bounded webhook metrics, provider health check | Processor is configured/documented but not host-registered; prove publication-before-reconciliation order and distinguish provider connectivity health from queue-lane health |
| PDS | Expired claim reclaim, token/fence active checks, lease renewal, fenced `TrySettleAsync`/`TryFailAsync`; persistence tests reject stale settlement | Orchestration still lives in API worker; no Quartz boundary, lane backlog/stale-lease health, or scheduler interruption proof. Preserve process-level owner, batch, parallelism, per-claim scope, gate rechecks, and payload secrecy during extraction |

Generic `explore.scheduler.job_executions` and `explore.scheduler.job_duration` are useful scheduler signals but cannot answer whether a lane is disabled, backlogged, stale, repeatedly lease-lost, or making durable progress. Each slice must either preserve an existing lane health/metric surface or add a bounded, tenant-free equivalent before cutover.

## Tenant, PII, and Value-Sensitive Boundaries

- Quartz recurring drains carry an empty `JobDataMap`. Do not persist tenant IDs, user IDs, recipient addresses, webhook URLs/bodies, provider payloads, DIDs, record keys, secrets, or raw exceptions in `QRTZ_` rows.
- Cross-tenant queue discovery may use only the existing named `TenantFilterBypassReasons`; item execution must re-enter the lane's persisted tenant context and, where currently required, a fresh machine-principal scope. A Quartz job itself receives no ambient tenant authority.
- IntegrationSync must continue resolving Listmonk URL and credentials with the persisted `outbox.TenantId`; the stale-claim repair must not broaden cross-tenant reads or expose subscriber email/name/payload in scheduler data, metrics, or wrapper logs.
- Metrics use closed, bounded lane/job/outcome vocabularies. Tenant/user/provider object IDs and payload-derived labels are forbidden. Operator status may expose aggregate counts/timestamps only.
- Pausing or disabling a job stops new claims; it does not mutate, discard, silently settle, or expose durable queue data. Backlog consequence and recovery duty must remain visible to self-hosters.
- The linked I-VSD report governs Amanah/truthfulness (do not claim completion), non-harm (no blind duplicate external effects), privacy/avoiding spying (data minimization), justice (tenant isolation and fair bounded draining), and operator accountability (observable pause, cutover, and recovery).

## Hard Per-Slice RED Gates

1. **Phase 1 - contract and EmailDispatch safety:** first observe deterministic RED in Tasks 1.1 and 1.4 for the real clustered crash window and forbidden general-outbox promise. No production correction in the next action.
2. **Scheduler authority and persisted keys:** RED composition matrix for scheduler enabled/disabled x EmailDispatch hosted/Quartz/disabled, plus RED proof that known obsolete durable keys are reconciled without touching unrelated keys.
3. **Registration submission, registration subscription, and IntegrationSync:** each lane gets its own failing delegation/registration/interruption/health proof. IntegrationSync additionally requires RED persistence tests for stale reclaim and stale-token/fence settlement refusal before its repository/service changes.
4. **Webhook lanes:** each job gets RED delegation, enabled/disabled registration, cancellation/lease-loss, ordering, tenant-scope, and lane-health proof applicable to that lane; do not treat one generic webhook test as coverage for all five jobs.
5. **PDS:** RED extraction parity, concurrent claim, stale settlement, cancellation, and lane-health proof before moving orchestration or deleting the worker.
6. **Release rehearsal:** only after all preceding slices are GREEN: RED architecture ratchet for remaining timer loops, then single-node SQLite and two-node PostgreSQL stop/start, pause/resume, crash, backlog recovery, persisted-key, and rollback evidence.

A slice closes only when its named RED was observed, the smallest production change turns it GREEN, affected project tests and build pass, operator docs match, and the context/task ledger are reconciled. A downstream slice cannot borrow a predecessor's GREEN result.

## Existing Evidence to Preserve

- `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs` and `Fixtures/QuartzPostgreSqlSchedulerFixture.cs` - generic two-node PostgreSQL trigger acquisition and distinct cluster members.
- `tests/Event.API.IntegrationTests/Features/QuartzSqliteDurableSchedulingTests.cs`, `QuartzPostgreSqlSchemaTests.cs`, `QuartzSchemaInitializerTests.cs`, and `SchedulerHealthCheckTests.cs` - durable store/schema/health behavior.
- `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzJobsTests.cs` and `tests/Explore.Infrastructure.Tests/Infrastructure/EmailDispatchDrainServiceTests.cs` - wrapper delegation and EmailDispatch durable service behavior; not the missing clustered crash proof.
- `tests/Explore.Infrastructure.Tests/Infrastructure/Listmonk/IntegrationSyncDrainServiceTests.cs` - current claim/retry/dead-letter behavior; mocks do not prove stale-row recovery or fenced settlement.
- Registration tests: `RegistrationProviderSubmissionWriteEffectServiceTests.cs`, `RegistrationProviderSubscriptionLifecycleServiceTests.cs`, and their domain lifecycle tests.
- Webhook tests: `WebhookDeliveryClaimIntegrationTests.cs`, `WebhookDeliveryDrainServiceTests.cs`, `IncomingWebhookDrainServiceTests.cs`, `IncomingWebhookTenantExecutionTests.cs`, incoming settlement/recovery persistence tests, bulk replay tests, provider dispatch identity tests, and provider unknown reconciliation tests.
- PDS tests: `AtprotoPdsDeliveryProcessorTests.cs` and `Event.Persistence.IntegrationTests/Federation/AtprotoFederationPersistenceTests.cs`, including expired reclaim and stale settlement refusal.
- `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs` - current timer-loop ratchet; it currently needs later expansion to cover `PeriodicTimer` after each target wrapper is removed.

## Proposed Performance Budgets - UNMEASURED

| Budget | Proposed gate | Current evidence |
|---|---|---|
| Cadence | Preserve each lane's configured/fixed interval exactly; no catch-up loop | Source values verified; Quartz migration unmeasured |
| Pass boundedness | One configured batch per fire; no drain-until-empty | Target architecture only; unmeasured |
| Scheduling lag | p95 trigger-to-job-start no greater than one configured lane interval under the supported test load | Proposed; no histogram baseline captured |
| Drain duration | p95 one-pass duration below its configured interval and below the lane's lease-renewal/expiry safety bound | Proposed; no per-lane baseline captured |
| Throughput | At equal batch/concurrency settings, no more than 10% sustained throughput regression versus the hosted worker | Proposed; no comparable benchmark captured |
| Scheduler overhead | No unbounded `QRTZ_` job/trigger growth; key count converges to the desired manifest after restart/cutover | Task 3.3 exact-key cleanup is confirmed; later lane cutovers remain unmeasured |
| Resource use | No more than 10% steady-state API CPU or managed-memory regression at equal queue load | Proposed; no load evidence captured |

Do not convert these proposals into pass claims. Capture the hosted-worker baseline before deleting each wrapper, then measure the Quartz slice with identical queue fixtures, batch, concurrency, provider fake, and database topology.

## Validation Baseline

| Check | Recorded result |
|---|---|
| Release build | 0 errors; 305 warnings |
| P.1 I-VSD traceability | Complete 2026-08-24; Q1-Q12 mapping synchronized |
| Migration implementation | **IN PROGRESS** |
| Implementation tasks complete | **16/39** |
| Phase 1 | GREEN |
| Phase 2 | GREEN: RequestLoggingPrivacyTests 2/2; focused scheduler audit/privacy 1/1; QuartzSchedulerCompositionTests 7/7; SchedulerHealthCheckTests 6/6; ApiLiabilityRatchetTests 15/15 |
| Phase 2 runtime proof | Real concurrent PostgreSQL reconciliation/foreign-key/idempotency; real HTTP administrator/non-administrator/anonymous and unrelated-denial isolation; canary-free logs/scopes/metrics; two independent reviews confirmed |
| Phase 2 cleanup | Zero relevant processes, running containers, or reports |
| Task 3.1 RED | Confirmed: 9 total, 8 intended failures, 1 pass; `.omo/start-work/evidence/3.1-red.txt` |
| Task 3.2 GREEN | Confirmed: boundary 5/5; combined phase split 9 total, 6 passed, 3 intended Task 3.3 failures; `.omo/start-work/evidence/3.2-green.txt` |
| Task 3.3 | CONFIRMED CUTOVER: combined 10/10; production PostgreSQL/Quartz 3/3; portable evidence SHA-256 `b5ffbf9eee9dd60a34e4e87a037e44948af4f7e03f02008e4b3b9faa5d42453d`; reviewers `st_01a033f6` and `st_01a033f7` |
| Task 3.4 RED | CONFIRMED: exact phase filters 0/5, 0/4, and current authority 1/1; real SQLite repository/service 2/5; Application 9/10; provider 2/2; architecture 2/2; metrics 1/1; portable [`evidence/3.4-red.md`](evidence/3.4-red.md) SHA-256 `f6a9dc40237dd553630466fc4c1563b74bbaf9618245ee0802359643c24f52dc`; raw SHA-256 `1b4580453178aefe79a181806e5468ea923012b7921844dae8715af671af5b84`; reviewer `st_01a03495` |
| Task 3.5 GREEN | CONFIRMED: boundary 5/5; real SQLite safety/recovery 11/11; Application 12/12; Formbricks 18/18; Google Forms 48/48; recovery handlers/HAL/auth 44/44, 17/17, 1/1; foundation persistence 31/31; semantic authority 4/4; catalog/metrics 4/4 and 1/1; portable [`evidence/3.5-green.md`](evidence/3.5-green.md) SHA-256 `57b3770868f2428220cd2b6e1929feed3eedf36a9194c0ff9a24917b7e08ce4b`; reviewers `st_01a034c5` and `st_01a034c6` |
| Task 3.6 CUTOVER | CONFIRMED: exact 5/5; combined 11/11; lifecycle architecture/query shape 5/5; SQLite 13/13; PostgreSQL 2/2; retained Release build 304 warnings/0 errors; portable [`evidence/3.6-cutover.md`](evidence/3.6-cutover.md) SHA-256 `cc737fe1f16b9a70d57e7042b103197404fa50feaa651705076705b23e5fca5a`; raw SHA-256 `b0b8a195b0456a0644d97b4101d3e2b881d5763eb0e0637e9a0aca1adad1ca36`; reviewers `st_01a034fe` and `st_01a034ff` |
| Registration Provider Drains phase | **GREEN** from retained Task 3.3 and Task 3.6 constituent evidence; no additional unrecorded phase command is claimed |
| Task 4.1 RED | **CONFIRMED at 16/39:** persistence 2/8; drain/provider 8/14; direct Listmonk adapter 4/5; Task 4.2 boundary 0/5; Task 4.3 cutover 0/4 with zero skips through full production host/required PostgreSQL; current hosted authority 1/1. Six stale/fence and six ambiguity/tenant product REDs are observed; unkeyed Listmonk protocol facts block ordinary replay. Portable [`evidence/4.1-red.md`](evidence/4.1-red.md) SHA-256 `4558c9a7c87604a515db0f6b191e2089ce51159d758ca91b9f209ecff545ecd1`; raw SHA-256 `5ba2dd9a40084ddc56f223441480c81d17784daaa29a47ab2766a715e95997b0`; reviewer `st_01a03554`. No production change. |
| Single-node SQLite rehearsal | Not run |
| Two-node PostgreSQL lane rehearsal | Phase 2 reconciliation proof complete; final release rehearsal remains R.1 |
| Mixed-version rehearsal | Forbidden, not a supported validation mode |
| Performance budgets | Proposed and unmeasured |
| External research | Graph/AnySearch/Context7 MCP unavailable; official Quartz docs fallback recorded |
| Retained audit backend | WORM-or-equivalent requirement documented; deployment backend is operator-owned and not claimed |

## Resume-Critical Files

### Workstream and Governance

- `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-plan.md`
- `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-tasks.md`
- `dev/active/queue-driven-worker-migration/queue-driven-worker-migration-context.md`
- `dev/active/queue-driven-worker-migration/evidence/3.3-cutover.md`
- `dev/active/queue-driven-worker-migration/evidence/3.4-red.md`
- `dev/active/queue-driven-worker-migration/evidence/3.5-green.md`
- `dev/active/queue-driven-worker-migration/evidence/3.6-cutover.md`
- `dev/active/queue-driven-worker-migration/evidence/4.1-red.md`
- `islamic-value-sensitive-design/i-vsd-queue-driven-worker-migration.md`
- `.agents/rules/api-scheduling.md`
- `docs/QUICK_REFERENCE.md`

### Scheduler Composition, Identity, and Persistence

- `Directory.Packages.props`
- `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs`
- `src/Explore.API/Hosting/ApiHostApplicationExtensions.cs`
- `src/Explore.API/Hosting/ApiHostStartupExtensions.cs`
- `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`
- `src/Explore.API/Scheduling/QuartzSchedulerKeys.cs`
- `src/Explore.API/Configuration/QuartzSchedulerSettings.cs`
- `src/Explore.API/Scheduling/QuartzSchemaInitializer.cs`
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobNames.cs`
- `src/Explore.Application/Contracts/Scheduling/ScheduledJobDescriptor.cs`
- `src/Explore.Application/Services/ScheduledJobRegistry.cs`

### Highest-Risk Lane Sources

- `src/Explore.Infrastructure/IntegrationSyncDrainService.cs`
- `src/Explore.Persistence/Repositories/IntegrationSyncOutboxRepository.cs`
- `src/Explore.Application/Contracts/Persistence/IIntegrationSyncOutboxRepository.cs`
- `src/Explore.API/BackgroundServices/IntegrationSyncProcessor.cs`
- `src/Explore.API/BackgroundServices/IntegrationSyncHostedDrainRunner.cs`
- `src/Explore.API/BackgroundServices/PdsSyncWorker.cs`
- `src/Explore.Application/Features/Federation/Atproto/Services/AtprotoPdsDeliveryProcessor.cs`
- `src/Explore.Persistence/Repositories/PdsSyncOutboxRepository.cs`
- `src/Explore.Infrastructure/Webhooks/WebhookDeliveryDrainService.cs`
- `src/Explore.Infrastructure/Webhooks/IncomingWebhookDrainService.cs`
- `src/Explore.Infrastructure/Webhooks/IncomingWebhookEffectDrainService.cs`
- `src/Explore.Infrastructure/Webhooks/WebhookProviderPublicationDrainService.cs`

### Tests and Operator Contract

- `tests/Event.API.IntegrationTests/Features/QuartzClusteringTests.cs`
- `tests/Event.API.IntegrationTests/Fixtures/QuartzPostgreSqlSchedulerFixture.cs`
- `tests/Event.API.IntegrationTests/Features/EmailDispatchQuartzJobsTests.cs`
- `tests/Explore.Infrastructure.Tests/Infrastructure/Listmonk/IntegrationSyncDrainServiceTests.cs`
- `tests/Event.Persistence.IntegrationTests/Federation/AtprotoFederationPersistenceTests.cs`
- `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`
- `docs/OPERATIONS.md`
- `docs/CONFIGURATION.md`
- `docs/ARCHITECTURE.md`
- `docs/OUTBOX_PATTERN.md`
- `docs/WEBHOOK_OPERATIONS_RUNBOOK.md`

## Handoff Notes

- Begin by re-reading the three linked artifacts and the I-VSD report; do not infer status from the old 2026-08-20 wording or from existing generic Quartz tests.
- The exact next action is **verify and finish the existing Task 4.2 GREEN implementation only**. Task 4.1 RED is independently confirmed at 16/39. Do not discard the current Task 4.2 code, do not add ordinary stale replay for the unkeyed Listmonk POST, and do not start Task 4.3.
- Resume-critical Task 4.2 production surfaces include `IIntegrationSyncOutboxRepository`, `IIntegrationSyncOutboxDeliveryQuery`, `IIntegrationSyncRecoveryQuery`, `IntegrationSyncOutboxRepository`, persistence query implementations, `IntegrationSyncDrainService`, `ListmonkSyncService`, `IntegrationSyncProcessorSettingsValidator`, `IntegrationSyncDrainJob`, scheduling names/registry/keys, Listmonk settings controller, recovery CQRS requests/handlers/DTOs, HAL link policy/routes, DI registration, and generated OpenAPI/client artifacts.
- Resume-critical Task 4.2 tests include `IntegrationSyncOutboxRepositoryTests`, `IntegrationSyncDrainServiceTests`, `ListmonkSyncServiceTests`, `IntegrationSyncDrainQuartzJobTests`, `IntegrationSyncRecoveryPersistenceTests`, `IntegrationSyncRecoveryHandlerTests`, `IntegrationSyncRecoveryContractTests`, and `IntegrationSyncTask42ArchitectureTests`.
- For async RED tests, subscribe to the exact scheduler execution or durable state transition before firing the trigger and await it with a bounded timeout. Do not copy the existing fixed-delay duplicate-observation pattern into new tests.
- Treat IntegrationSync as a real durability repair, not a thin wrapper migration. The stale `Processing` and unfenced settlement defects must remain visible in task scope until RED-to-GREEN evidence exists.
- Preserve all unrelated worktree changes. Do not edit outside the files assigned by the active task, do not create a compatibility shim, and do not commit unless explicitly requested.
- After every slice, reconcile plan dependency/gate text, task checkboxes/evidence, and the top status sections here in the same session. `IN PROGRESS`, `NEXT`, `BLOCKERS`, implementation status, and validation rows must never disagree. Preserve the AnySearch/Context7 unavailable disclosure.
