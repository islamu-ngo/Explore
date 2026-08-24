<!-- ABOUTME: Repository-grounded Senior CTO plan for migrating bounded API queue-drain cadence to Quartz.NET. -->
<!-- ABOUTME: Defines RED-first lane safety, persisted-key reconciliation, coordinated cutover, and operator recovery. -->

# Queue-Driven Worker Migration to Quartz.NET - Senior CTO Plan

Last Updated: 2026-08-25 Europe/Brussels

## 1. Decision, Status, and Exact Handoff

- **Implementation status: COMPLETE.** All queue-drain safety repairs, one-pass jobs, and atomic cadence cutovers are implemented.
- **Implementation tasks complete:** **39/39**.
- **Final verification:** Release build 0 errors/585 warnings; focused IntegrationSync, Listmonk, PDS, scheduler composition, timer-ratchet, and Clean Architecture matrices are GREEN. R.1 remains an external release-owner rehearsal.
- **Intent classification:** `schedule-background-work`, **Tier 3**. This changes multi-replica cadence ownership for tenant-bearing durable side effects and requires clustered persistence, fencing evidence, coordinated release control, and rollback proof.
- **Senior CTO verdict:** approve for implementation only under the hard per-slice RED-first gates in this plan. Approval authorizes the named failing proof, not production edits before RED is observed and recorded.
- **Confirmed Task 3.6:** exact final counts are Task 3.6 5/5; combined boundary/cutover/current authority 11/11; lifecycle architecture/query shape 5/5; SQLite lifecycle safety/health 13/13; two-node PostgreSQL lifecycle recovery 2/2; retained Release build 304 warnings/0 errors.
- **Current next:** release owner executes R.1 coordinated stop/wait/start and rollback rehearsal; mixed-version operation remains unsupported.
- **Hard stop:** Quartz is the sole payload-free 30-second subscription-lifecycle cadence authority; the hosted source/registration are deleted with no mode, shim, fallback, dual authority, or mixed-version support. Durable tenant/lease-token/generation state, the pre-provider `renewal_in_doubt` barrier, exact-decision recovery, and stale-owner refusal remain the sole provider-call safety authority; Quartz recovery never decides settlement.
- **Task 3.3 architecture and behavior:** Quartz is the sole 10-second submission-write cadence authority through one payload-free UTC `*/10 * * * * ?` trigger with DoNothing misfire handling, `RequestsRecovery=false`, and per-key non-concurrency. The exact tenant/lease-token/processing-fence claim is durably parked before provider I/O. Authorized event-scoped queue HAL supports only `confirm-accepted`, `retry-definitely-not-accepted`, or `dead-letter` with bounded evidence references, exact-fence idempotence, and durable success/refusal audit. Mutation methods return domain entities while delivery, recovery, and health use split query contracts. Health emits exactly five tenant-free aggregates using four COUNT commands and one MIN command. Semantic ratchets cover indirect interface, repository, DI/factory, and alias paths. Globally disabled persistent composition performs exact one-shot owned-key cleanup through a private never-started Quartz scheduler, preserves foreign variants, and exposes no runtime scheduler/operator surface.
- **Cumulative architecture fix:** Task 3.2 also repaired the scheduler-admin policy boundary: `SchedulerAdminAuthorizationPolicy` is the sole authority for the instance-admin policy name; `QuartzSchedulerSettings.InstanceAdminPolicyName` and Quartz coupling in `SchedulerAdminController` are removed.
- **durable safety/no-dual-cadence boundary:** Task 3.3 transferred cadence atomically; Quartz owns cadence while durable tenant/token/fence state and evidence-based operator recovery remain provider-call safety authorities.
- **Task 3.3 evidence and review:** portable [`evidence/3.3-cutover.md`](evidence/3.3-cutover.md), SHA-256 `b5ffbf9eee9dd60a34e4e87a037e44948af4f7e03f02008e4b3b9faa5d42453d`; local raw `.omo/start-work/evidence/3.3-cutover.txt`, SHA-256 `9074ce7587338438eb75a03232e77e6c29598e011e9b487e8e07c066d219be5b`. Independent runtime reviewer `st_01a033f6` confirmed all behavioral gates subject only to this final status synchronization; independent architecture/privacy/operations reviewer `st_01a033f7` confirmed all substantive defects closed and identified portability as the sole remaining blocker, now resolved by the tracked snapshot.
- **Task 3.4 evidence and review:** portable [`evidence/3.4-red.md`](evidence/3.4-red.md), SHA-256 `f6a9dc40237dd553630466fc4c1563b74bbaf9618245ee0802359643c24f52dc`; local raw `.omo/start-work/evidence/3.4-red.txt`, SHA-256 `1b4580453178aefe79a181806e5468ea923012b7921844dae8715af671af5b84`. Independent reviewer `st_01a03495` confirmed the RED and Task 3.5 handoff; AnySearch MCP and Context7 MCP remained unavailable and no result from either is claimed.
- **Task 3.5 evidence and review:** portable [`evidence/3.5-green.md`](evidence/3.5-green.md), SHA-256 `57b3770868f2428220cd2b6e1929feed3eedf36a9194c0ff9a24917b7e08ce4b`; local raw `.omo/start-work/evidence/3.5-green.txt`, SHA-256 `d9980eb6bcc8f46d395f7b0c84f34923c90e76b8729c63dab44554af18b27d2f`. Runtime reviewer `st_01a034c5` and architecture reviewer `st_01a034c6` independently confirmed GREEN. AnySearch MCP and Context7 MCP remained unavailable; no result from either is claimed.
- **Task 3.6 evidence and review:** portable [`evidence/3.6-cutover.md`](evidence/3.6-cutover.md), SHA-256 `cc737fe1f16b9a70d57e7042b103197404fa50feaa651705076705b23e5fca5a`; local raw `.omo/start-work/evidence/3.6-cutover.txt`, SHA-256 `b0b8a195b0456a0644d97b4101d3e2b881d5763eb0e0637e9a0aca1adad1ca36`. Runtime reviewer `st_01a034fe` and architecture/privacy/operations reviewer `st_01a034ff` independently confirmed CUTOVER. AnySearch MCP and Context7 MCP remained unavailable; no result from either is claimed.
- **Compatibility posture:** pre-v1 and forward-only. There is no compatibility shim, alias registration, dual old/new authority, or supported mixed-version window.
- **Phase 2 Release build:** `dotnet build --configuration Release --verbosity quiet` completed with **0 errors and 305 warnings**. This is a compile result, not a warning-quality or deployment certification.

### Authoritative workstream records

- [Context](queue-driven-worker-migration-context.md) - exact status, next action, blockers, and handoff.
- [Task ledger](queue-driven-worker-migration-tasks.md) - executable P.1 and RED/GREEN/CUTOVER tasks.
- [I-VSD queue-driven worker migration report](../../../islamic-value-sensitive-design/i-vsd-queue-driven-worker-migration.md) - stakeholder harms, privacy, autonomy, recovery, and support duties.
- [Quartz implementation report](../../report/quartznet-background-jobs-implementation-report.md) and [Quartz selection report](../../report/quartznet-background-jobs-selection-report.md) - current capability and selection evidence.
- `.agents/rules/api-scheduling.md` and `docs/QUICK_REFERENCE.md` rule 27 - repository scheduling rules.

The I-VSD report exists and P.1 was accepted on 2026-08-24 through the Senior CTO feedback workflow after the report, plan, context, and ledger were synchronized to the current Q1-Q12 mapping. This planning approval is not represented as implementation, stakeholder validation, legal review, or religious-legal approval.

### Current blockers

1. No implementation blocker remains.
2. `dotnet-stryker` is not installed or tool-manifested, so the mutation gate is unexecuted rather than passed.
3. Coordinated release rehearsal and deployment-specific WORM-or-equivalent audit-backend certification remain operator-owned.

### Research provenance

Repository evidence was reviewed with code-review-graph and official Quartz behavior was checked through Context7. AnySearch MCP was not exposed in this environment, so no AnySearch result is claimed. Primary references remain official Quartz sources: [More About Jobs](https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/more-about-jobs.html), [Clustering](https://www.quartz-scheduler.net/documentation/quartz-3.x/tutorial/advanced-enterprise-features.html), [Microsoft DI Integration](https://www.quartz-scheduler.net/documentation/quartz-3.x/packages/microsoft-di-integration.html), [ASP.NET Core Integration](https://www.quartz-scheduler.net/documentation/quartz-3.x/packages/aspnet-core-integration.html), and [Configuration Reference](https://www.quartz-scheduler.net/documentation/quartz-3.x/configuration/reference.html). The official NuGet listing records `Quartz.OpenTelemetry.Instrumentation` as [deprecated](https://www.nuget.org/packages/Quartz.OpenTelemetry.Instrumentation/3.6.3).

## 2. Historical Pre-Migration State

### 2.1 Scheduler composition today

- `Directory.Packages.props` pins Quartz packages to **3.19.1**.
- `ApiHostServiceCollectionExtensions` derives `UseQuartzEmailDispatch` from `EmailDispatchProcessor:Enabled` plus `Mode=Quartz`, then uses it to enable the entire scheduler.
- `ApiHostStartupExtensions` repeats that email gate for scheduler schema startup. `ApiHostApplicationExtensions` repeats it for scheduler middleware and endpoint mapping.
- `QuartzSchedulerExtensions.RegisterRecurringJobs` registers EmailDispatch jobs whenever Quartz is composed, while maintenance/deadline/registration/payment jobs already depend on the same scheduler.
- Existing canonical scheduler contracts are `Scheduler:Quartz:*`, stable scheduler name, `platform-recurring` / `platform-on-demand`, existing implemented job keys, `/admin/scheduler`, `/api/admin/scheduler`, standalone `/quartz`, scheduler health, and `explore.scheduler.*` metrics.
- `SchedulerTelemetryJobListener` is the repository's bounded observer and remains. `.AddSource("Quartz")` remains the current ActivitySource subscription; this plan does not introduce the deprecated Quartz OpenTelemetry integration package.

### 2.2 Historical worker roster and ownership

| Component | Current cadence/registration | Current durable or one-pass boundary | Decision |
|---|---|---|---|
| `OutboxProcessor` | Registered hosted `Task.Delay` loop | General outbox claim, dispatch, retry, reconciliation coupled to worker | **Remain hosted**; explicit exception |
| `ManagedControlPlaneRegistrationWorker` | Registered retry loop; exits after acknowledgement | Durable management registration command | **Remain hosted**; retry-until-success bootstrap |
| `RegistrationProviderSubmissionWriteDrainJob` | Sole Quartz cadence, payload-free UTC cron every 10 seconds; hosted worker removed | `DrainRegistrationProviderSubmissionWriteEffectsCommand` below a durable pre-provider barrier | Registration submission lane; Task 3.3 confirmed |
| `RegistrationProviderSubscriptionLifecycleDrainJob` | Sole Quartz cadence, payload-free UTC cron every 30 seconds; hosted worker deleted | `RegistrationProviderSubscriptionLifecycleService.DrainOnceAsync` with lease/generation/checkpoint authority | Task 3.6 confirmed |
| `IntegrationSyncProcessor` | Enabled registration; delay loop | `IIntegrationSyncDrainService` through `IntegrationSyncHostedDrainRunner` | Repair safety, then migrate |
| `WebhookDeliveryProcessor` | Enabled initial-delay/`PeriodicTimer` | stale recovery, then `IWebhookDeliveryDrainService.ProcessBatchAsync` | Local delivery lane |
| `IncomingWebhookProcessor` | Registered with incoming processing | `IIncomingWebhookDrainService.ProcessBatchAsync` | Incoming intake lane |
| `IncomingWebhookEffectProcessor` | Same enablement | `IIncomingWebhookEffectDrainService.ProcessBatchAsync` | Incoming effect lane |
| `WebhookBulkReplayProcessor` | Registered initial-delay/`PeriodicTimer` | `IWebhookBulkReplayService.ProcessQueuedAsync` | Bulk replay lane |
| `WebhookProviderPublicationProcessor` | Class/settings exist but API host does **not** register it | publication pass, then reconciliation pass | Direct-to-Quartz lane; never temporarily host |
| `PdsSyncWorker` | Registered outside Testing/OpenAPI; delay loop | claim, stable owner, bounded parallelism, per-claim scopes embedded in `RunOnceAsync` | Phase 7 extracts orchestration but retains a thin hosted cadence wrapper; Phase 8.3 alone deletes it during Quartz cutover |

`NotificationFanoutProcessor`, `WebPushDispatchProcessor`, startup gates, broker consumers, and stream/event subscribers are outside this plan. `AtprotoJetstreamSubscriber` remains hosted.

### 2.3 Existing evidence and material gaps

- `QuartzClusteringTests` proves generic two-node PostgreSQL trigger acquisition and distinct scheduler instances. It does not prove lane-specific external-effect safety.
- `QuartzSqliteDurableSchedulingTests` proves a local SQLite scheduler survives restart. **SQLite restart is not clustering evidence.**
- Email job tests prove delegation; durable Email tests prove service states. The real clustered accepted-before-settlement path remains missing.
- Registration submission now has sole Quartz cadence, token/fence settlement, durable pre-provider parking, explicit authorized recovery for accepted/definitely-not-accepted/dead-letter decisions, split delivery/recovery/health queries, and five bounded health aggregates. Subscription lifecycle retains token/generation settlement and durable checkpoint behavior but has not moved cadence.
- Webhook and PDS lanes already use durable tenant-bearing claims, lease tokens/fences, stale recovery/reclaim, and fenced settlement below API wrappers.
- Provider publication has `PublicationUnknown`, bounded reconciliation, and manual reconciliation.
- Generic `explore.scheduler.job_executions` / `job_duration` cannot show lane backlog, oldest due age, stale leases, ambiguity, or durable progress.

### 2.4 IntegrationSync is not a wrapper-only migration

`IntegrationSyncOutbox` already has `ProcessingStartedAt` and `ProcessingLeaseToken`. `TryMarkAsProcessing` creates a token, and `GetActiveClaimAsync` checks tenant + ID + token before Listmonk I/O. However:

1. `GetPendingBatch` selects only `Pending`/`RetryScheduled`; stale `Processing` rows are never recovered.
2. `MarkAsCompleted` updates by ID only.
3. `MarkAsFailed` loads and mutates by ID only.
4. No persistence integration tests prove stale recovery, wrong-tenant rejection, token replacement, or stale settlement refusal.
5. No dedicated IntegrationSync queue-health signal exists.

This is a production safety defect and a pre-cutover blocker.

## 3. Future Architecture and Binding Semantics

```text
Quartz 3.19.1 recurring trigger (cadence only; empty JobDataMap)
    -> stable API IJob (one bounded pass; no business retry or tenant authority)
        -> scheduler-neutral drain/service/command
            -> durable queue claim + persisted tenant + lease/fence authority
                -> bounded external or local work
                    -> fenced durable settlement/retry/dead-letter/ambiguity policy
```

### 3.1 Quartz owns cadence only

Quartz 3.19.1 is the cadence, persistence, misfire, and operator-control mechanism. Durable lane rows remain the correctness authority for eligibility, claim ownership, tenant, retry, ambiguity, idempotency, and settlement. A missing trigger may delay durable work; it cannot erase it. A repeated trigger may attempt another claim; it cannot authorize stale settlement.

Official semantics are binding:

- `[DisallowConcurrentExecution]` applies per job definition / **`JobKey`**, not class-wide. Every recurring lane gets exactly one stable key; aliases would create another concurrency domain and are forbidden.
- Quartz clustering supplies shared-store load balancing/failover. It is **not external exactly-once** and cannot fence SMTP, Listmonk, HTTP, Svix, registration providers, or PDS side effects.
- recovery can re-execute interrupted jobs. Queue-drain jobs therefore do not set `RequestsRecovery`; an exception requires RED proof that the complete pass is replay-safe.
- `WaitForJobsToComplete=true` is cooperative. It waits for jobs that honor cancellation and finish within host shutdown bounds; it does not make a hard kill safe or prove provider settlement.
- SQLite durable restart evidence applies to Tier 1 single-node operation. Tier 3 clustering evidence requires two distinct PostgreSQL-backed scheduler nodes.

### 3.2 Recurring job contract

Each migrated recurring job must:

1. have one stable catalog name, `JobKey`, and derived trigger key in `platform-recurring`;
2. use `[DisallowConcurrentExecution]` with its per-key meaning understood;
3. persist an **empty job and trigger `JobDataMap`**;
4. execute one bounded pass, never loop, sleep, recurse, or drain until empty;
5. preserve the existing enabled, cadence, initial-delay, batch, concurrency, retry, and lease controls where semantics remain sound;
6. select an explicit backlog-collapsing misfire instruction, never replay every missed interval;
7. leave `RequestsRecovery=false` unless a separate replay-safe RED gate authorizes it;
8. bubble unexpected pass failures to Quartz while leaving expected item outcomes to the drain;
9. place no claim, lease, fence, retry, tenant, provider, or transport logic in the API job;
10. emit only stable job/lane/outcome, bounded counts/ages, duration, and exception type.

### 3.3 One authority and canonical contracts

- `Scheduler:Quartz:Enabled` becomes the global scheduler authority. EmailDispatch mode controls only EmailDispatch job registration.
- Replace internal `UseQuartzEmailDispatch` with scheduler-wide composition state. No alias, duplicate property, or old/new configuration shim remains.
- The old wrapper registration is removed in the same release that registers a lane's Quartz job. No configuration can compose both.
- Stable current operator contracts are retained **because they are canonical**, not because the migration promises generic compatibility: current scheduler settings, implemented keys/groups, endpoint paths, health names, metric names, and sound lane settings remain stable.
- Remove `GeneralOutboxDrain` from machine-consumed scheduled names, bounded labels, registry, tests, and docs. Do not replace the promise. Preserve unrelated control-plane `general-outbox` status terminology. `OutboxProcessor` remains registered and documented.

## 4. Persisted Owned-Key Inventory and Reconciliation

Quartz Microsoft DI defaults can overwrite same-key definitions; they do not delete removed or renamed durable keys. Every release therefore carries a versioned, checked-in owned-key inventory with exact desired and retired `JobKey`/`TriggerKey` pairs.

After all old replicas stop, one cluster-safe reconciliation operation must:

1. inspect the configured scheduler namespace and current keys through Quartz APIs;
2. add/update exact desired platform-owned definitions with stable identity and explicit schedule;
3. unschedule/delete only exact retired or disabled keys listed in the owned inventory;
4. prove disabled lanes have no live trigger and no accidental orphan job;
5. record bounded before/after desired, retired, drifted, and foreign counts;
6. preserve all unknown jobs, triggers, and calendars.

Never delete by table prefix, group wildcard, raw SQL, or "everything not currently registered." Never delete unrelated `QRTZ_*` rows. RED seeds unrelated scheduler state and proves it survives. Rollback uses the reverse exact manifest after stop/lease wait; a key rename is a breaking operator event, never an alias opportunity.

## 5. Lane Durable-State, Tenant, Ambiguity, and Health Matrix

| Lane / proposed key | Durable correctness state | Tenant and fence authority | Ambiguity/recovery authority | Required lane signal before cutover |
|---|---|---|---|---|
| Email safety gate (existing keys) | `EmailDispatchOutbox`, attempts, receipts | tenant + ID + token/fence | accepted-but-unsettled -> `Unknown`; controlled reconciliation | existing email health plus Unknown/stale/age evidence |
| `registration-provider-submission-write-drain` | submission write effect claim/attempt | persisted tenant; exact token + processing fence; tenant context is set/cleared | durable pre-provider barrier; authorized exact-fence `confirm-accepted`, `retry-definitely-not-accepted`, or `dead-letter`; transactional success/refusal audit | confirmed five-command backlog/dead-letter/oldest-due/parked/stale-processing health |
| `registration-provider-subscription-lifecycle-drain` | renewal/sweep state and checkpoint | persisted tenant; lease token + processing generation | existing overlap/checkpoint and bounded failures | due renewal/sweep, oldest due, stale lease, terminal failure |
| `integration-sync-drain` | `IntegrationSyncOutbox`, attempts, existing processing fields | cross-tenant claim only; tenant + ID + token active and settlement predicates | RED must prove replay-safe stale requeue; otherwise explicit Unknown/reconciliation reclassification | backlog, oldest due, stale Processing, retry/dead-letter, progress |
| `webhook-delivery-drain` | local target, message, attempt evidence, endpoint state | tenant + lease token + delivery fence; fresh tenant scope | existing stale recovery and timeout/network policy | preserve local delivery health and add scheduler job identity |
| `incoming-webhook-drain` | verified incoming message claim/state | tenant + token + processing fence + generation; renewable lease | existing claim executor outcomes | intake backlog/age/stale lease/progress |
| `incoming-webhook-effect-drain` | incoming effect outbox | tenant-bearing claim + token/fence/generation in fresh scope | existing retry/dead-letter/recovered categories | preserve effect health, add exact job/progress identity |
| `webhook-bulk-replay-drain` | queued replay operation, targets, transactional audit | persisted operation tenant; transaction owns transition | transaction rollback/failure settlement; no wrapper provider handoff | queued/executing/oldest operation and failure count |
| `webhook-provider-publication-drain` | publication, unknown, reconciliation state | tenant + lease token/expiry in dispatcher/reconciler | `PublicationUnknown`, automatic then manual reconciliation | queue health distinct from provider connectivity health |
| `pds-sync-drain` | `PdsSyncOutbox`, retry/dead-letter/supersession, canonical ATProto record | tenant + user + token + monotonic fence + expiry | existing processor/repository retry, compensation, stale refusal | backlog/age/stale lease/retry/dead-letter/progress |

Quartz recurring state contains no tenant ID or lane payload. Bounded cross-tenant discovery uses only existing named bypass reasons. Each item re-enters its persisted tenant and required machine-principal scope before tenant-owned work; context is disposed/cleared before the next item.

## 6. Mandatory IntegrationSync Repair Before Cutover

Use the existing `ProcessingStartedAt` and `ProcessingLeaseToken`. **No schema change is planned unless RED evidence forces reclassification.**

### RED

Before production edits, add real persistence and drain tests that fail because current behavior cannot satisfy:

- bounded recovery of stale `Processing` older than a validated lease timeout while fresh rows remain untouched;
- wrong tenant, wrong token, replaced token, and already-settled completion/failure affect zero rows;
- the old owner cannot settle after stale recovery and a new claim;
- cancellation leaves work recoverable and creates no false settlement;
- malformed/null processing evidence receives a deliberate safe classification rather than a guessed retry;
- a controlled Listmonk adapter proves whether repeating the same subscriber identity update after accepted-but-unsettled execution is safe.

### GREEN

- Add a validated `IntegrationSyncProcessor:ProcessingLeaseTimeoutSeconds` only because no current stale-age control exists; proposed default `120`, bounds `30..3600`.
- Recover one bounded stale batch before normal due selection. Match `Status=Processing`, observed token, and `ProcessingStartedAt <= cutoff` atomically; clear processing fields and return only replay-safe work to the existing retry path.
- Change completion/failure contracts to require `tenantId` + `leaseToken`, predicate tenant + ID + `Processing` + token, and report whether exactly one row settled.
- Treat zero-row settlement as lease lost/already claimed, not completed, retried, or dead-lettered.
- Preserve the privacy-erasure fence and tenant-scoped Listmonk settings/secrets before provider I/O.

If RED disproves replay safety after provider acceptance, stop. Reclassify Lane IntegrationSync to an explicit Unknown/reconciliation state with generated provider migrations and approved rollback. That RED is the only path to a schema change; do not auto-retry ambiguity merely to keep this plan schema-free.

### CUTOVER

The safety repair may ship before scheduler ownership. `integration-sync-drain` cannot register and `IntegrationSyncProcessor`/runner cannot be removed until all RED cases are GREEN and the no-schema hypothesis is confirmed or an approved reclassification is complete.

## 7. Eight-Phase Delivery Plan - 39 Implementation Tasks

The completed ledger is authoritative: **8 implementation phases, 39 implementation tasks**, with P.1 as a planning prerequisite and R.1 as a release-owner gate outside implementation phases. Tests are observed RED before production implementation. A downstream lane cannot borrow another lane's GREEN.

### P.1 - I-VSD traceability prerequisite (not an implementation task)

**Status: COMPLETE (2026-08-24).** Reviewer: Senior CTO feedback workflow. The report, plan, context, and ledger use the same authoritative Q1-Q12 mapping and preserve all missing-evidence and escalation boundaries.

**Accepted scope:** implementation may begin only with Tasks 1.1 and 1.4 RED. Stakeholder interviews, legal/privacy review, production support ownership, audit-retention proof, capacity evidence, and any religious-legal review remain explicitly missing or escalated rather than silently approved.

**Gate:** Tasks 1.1 and 1.4 may now run RED; record both failures and stop before production edits.

### Phase 1 - Email Crash Window and GeneralOutbox Contract (Tasks 1.1-1.6)

#### EmailDispatch crash-window lane (1.1 RED -> 1.2 GREEN -> 1.3 CUTOVER)

**RED 1.1:** subscribe to transport acceptance before triggering a real two-node PostgreSQL Quartz drain. Force local settlement loss through a controlled barrier. Assert one provider call, exact durable `Unknown`, no automatic resend, wrong/missing tenant and stale-fence refusal, cancellation recovery, empty Quartz payload, and zero-PII telemetry. No fixed sleep or duplicate-observation delay.

**GREEN 1.2:** preserve current behavior if RED fails only because proof was absent; otherwise make the smallest Email drain/repository correction shown by RED. Accepted-but-unsettled is `Unknown`; definitely-not-accepted retains its lane retry policy.

**CUTOVER 1.3:** publish evidence, docs, telemetry, and ratchet updates only. Email authority does not change.

#### GeneralOutbox contract lane (1.4 RED -> 1.5 GREEN -> 1.6 CUTOVER)

**RED 1.4:** Application catalog tests fail while `general-outbox-drain` remains a scheduled name, bounded label, registry descriptor, or docs promise; architecture proof requires hosted `OutboxProcessor` and forbids a replacement job.

**GREEN 1.5:** remove the scheduled constant/descriptor/label and tests. Preserve unrelated control-plane `general-outbox` status terminology.

**CUTOVER 1.6:** ratchet and document `OutboxProcessor` as the hosted exception. Introduce no GeneralOutbox job/service.

### Phase 2 - Global Scheduler Composition (Tasks 2.1-2.3)

**RED 2.1:** composition matrix covers scheduler enabled/disabled x EmailDispatch Quartz/hosted/disabled, Testing/OpenAPI suppression, maintenance under hosted Email, lane-disabled absence, stable keys, empty maps, explicit misfires, no recovery request, and foreign-QRTZ preservation during owned-key retirement.

**GREEN 2.2:** make `Scheduler:Quartz:Enabled` global, make Email registration mode-conditional, and replace `UseQuartzEmailDispatch` directly. No shim. Add exact owned-key behavior and preserve the custom listener.

**CUTOVER 2.3:** complete. Finite owned-key reconciliation, scheduler/schema/health/operator contracts, bounded audit, and zero-PII telemetry are GREEN. Real concurrent PostgreSQL reconciliation preserved foreign state and was idempotent; real HTTP proved administrator/non-administrator/anonymous outcomes and unrelated-denial isolation. This phase shipped without queue-worker ownership changes; retained immutable/tamper-evident WORM-or-equivalent audit storage remains an operator-owned documented deployment responsibility.

### Phase 3 - Registration Provider Drains (Tasks 3.1-3.6)

#### Submission-write lane (3.1 RED -> 3.2 GREEN -> 3.3 CUTOVER)

**RED 3.1 - CONFIRMED:** 9 total tests: 8 intended failures and 1 current-hosted-authority pass. It proves the required one-command, cancellation/exception, 10-second cadence, same-key non-overlap, empty-map, tenant cleanup, token/fence, bypass, ambiguity, and lane-health contracts are not all implemented. Evidence: `.omo/start-work/evidence/3.1-red.txt`.

**GREEN 3.2 - CONFIRMED:** the thin `registration-provider-submission-write-drain` job and stable scheduler-neutral catalog/key boundary pass 5/5. It preserves command-owned claim/provider/settlement behavior and deliberately does not register a trigger, health check, or second authority. The combined phase filter is 9 total: 6 passed, 3 intended Task 3.3 failures. Evidence: `.omo/start-work/evidence/3.2-green.txt`.

**CUTOVER 3.3 - CONFIRMED:** one payload-free 10-second Quartz trigger replaced the hosted worker atomically. Durable pre-provider parking, exact-fence HAL recovery and audit, entity/query separation, five-command health, semantic authority/health ratchets, and disabled one-shot exact-key cleanup are confirmed. Evidence: [`evidence/3.3-cutover.md`](evidence/3.3-cutover.md), SHA-256 `b5ffbf9eee9dd60a34e4e87a037e44948af4f7e03f02008e4b3b9faa5d42453d`; local raw SHA-256 `9074ce7587338438eb75a03232e77e6c29598e011e9b487e8e07c066d219be5b`. Independent reviewers: runtime `st_01a033f6`; architecture/privacy/operations `st_01a033f7`.

#### Subscription-lifecycle lane (3.4 RED -> 3.5 GREEN -> 3.6 CUTOVER)

**RED 3.4 - CONFIRMED:** exact phase filters are `RegistrationProviderSubscriptionLifecycleTask35JobBoundaryTests` 0/5, `RegistrationProviderSubscriptionLifecycleTask36CutoverTests` 0/4, and `RegistrationProviderSubscriptionLifecycleTask34CurrentAuthorityControlTests` 1/1. Supporting counts are real SQLite repository/service 2/5, Application control flow 9/10, provider protocol 2/2, exact named bypass architecture 2/2, and bounded zero-PII metrics 1/1; the Release build had 0 warnings and 0 errors. Four product-safety REDs are real: (1) accepted-then-cancelled lease reclaim repeats an unkeyed provider create, (2) accepted-before-settlement process loss repeats that create after reclaim/restart, (3) two-tenant claims never bind claim-derived ambient tenant context, and (4) `IsActive=false` is settled as renewal success instead of definite rejection. Provider facts are binding: Formbricks create is unkeyed `POST /api/v1/webhooks`, response loss is ambiguous, HTTP 408 is currently classified retryable-before-handoff despite the unkeyed POST, and Google renewal is addressable only when an existing provider webhook ID selects `/watches/{id}:renew`. Cadence is unchanged. Evidence: [`evidence/3.4-red.md`](evidence/3.4-red.md), SHA-256 `f6a9dc40237dd553630466fc4c1563b74bbaf9618245ee0802359643c24f52dc`; raw SHA-256 `1b4580453178aefe79a181806e5468ea923012b7921844dae8715af671af5b84`; reviewer `st_01a03495`.

**GREEN 3.5 - CONFIRMED:** `registration-provider-subscription-lifecycle-drain` is an unregistered, non-concurrent, payload-free one-pass boundary. Durable authority persists only `lifecycle_retryable`, `renewal_in_doubt`, `renewal_rejected`, and `operator_dead_lettered`; provider codes are bounded metrics only. Ambiguous handoff preserves the exact lease/token/generation barrier. Formbricks create and Google unknown-ID create versus known-ID renewal outcome matrices cover 4xx, 408, 5xx, transport, and malformed success. Production claim-derived tenant scope restores the prior ambient tenant. Authorized event-scoped HAL recovery is exact-decision idempotent with transactional success/refusal audit, and semantic analysis protects lifecycle sole authority. Exact final counts: boundary 5/5; SQLite 11/11; Application 12/12; Formbricks 18/18; Google Forms 48/48; recovery handlers/HAL/auth 44/44, 17/17, 1/1; foundation persistence 31/31; semantic authority 4/4; catalog/metrics 4/4 and 1/1; current hosted authority 1/1; Release build 0 warnings/errors. Task 3.6 remains intended RED 0/4; the hosted worker alone retains the 30-second cadence and no Task 3.6 work occurred. Evidence: [`evidence/3.5-green.md`](evidence/3.5-green.md), SHA-256 `57b3770868f2428220cd2b6e1929feed3eedf36a9194c0ff9a24917b7e08ce4b`; raw SHA-256 `d9980eb6bcc8f46d395f7b0c84f34923c90e76b8729c63dab44554af18b27d2f`. Reviewers: runtime `st_01a034c5`; architecture `st_01a034c6`.

**CUTOVER 3.6 - CONFIRMED:** one payload-free UTC 30-second Quartz trigger atomically replaces the deleted hosted worker; exact inventories and disabled cleanup own the job/trigger; seven bounded health aggregates use a dedicated fixed seven-command query; sole-authority/query ratchets and operator docs are updated. Durable tenant/lease-token/generation state, the pre-provider `renewal_in_doubt` barrier, exact-decision recovery, and stale-owner refusal remain authoritative. Final counts: Task 3.6 5/5; combined 11/11; SQLite 13/13; PostgreSQL 2/2; lifecycle architecture/query shape 5/5; retained Release build 304 warnings/0 errors. Evidence: [`evidence/3.6-cutover.md`](evidence/3.6-cutover.md), SHA-256 `cc737fe1f16b9a70d57e7042b103197404fa50feaa651705076705b23e5fca5a`; raw SHA-256 `b0b8a195b0456a0644d97b4101d3e2b881d5763eb0e0637e9a0aca1adad1ca36`. Reviewers: runtime `st_01a034fe`; architecture/privacy/operations `st_01a034ff`.

**Phase 3 - Registration Provider Drains: GREEN.** The retained Task 3.3 and Task 3.6 evidence closes both atomic cadence transfers; no additional unrecorded phase command is claimed.

### Phase 4 - IntegrationSync Safety, Job, and Cutover (Tasks 4.1-4.3)

**RED 4.1 - CONFIRMED:** exact final matrices are persistence 2/8, drain/provider 8/14, direct Listmonk adapter 4/5, Task 4.2 boundary 0/5, Task 4.3 cutover 0/4 with zero skips through the full production API host and required PostgreSQL, and current hosted authority 1/1. Six persistence stale/fence contracts and six drain/provider ambiguity/tenant contracts are intended product REDs. The SQLite GREEN control is a barrier-released two-repository race; stale recovery requires configured timeout plus real initial claim/reclaim, and settlement requires exact tenant/item/token/observed-started-at. Protocol facts are binding: Listmonk subscriber creation is an unkeyed POST, identical replay sends two POSTs, and HTTP 408, 500, 503, and accepted-before-response-loss transport ambiguity are currently retryable. No production change occurred. Evidence: [`evidence/4.1-red.md`](evidence/4.1-red.md), SHA-256 `4558c9a7c87604a515db0f6b191e2089ce51159d758ca91b9f209ecff545ecd1`; local raw SHA-256 `5ba2dd9a40084ddc56f223441480c81d17784daaa29a47ab2766a715e95997b0`; reviewer `st_01a03554`. Implementation is 16/39; Task 4.2 GREEN is next and must first add an approved explicit non-replay ambiguity/reconciliation classification.

**GREEN 4.2:** repair stale recovery and tenant+token settlement fencing, then add the thin `integration-sync-drain` job. No schema unless RED forced approved ambiguity reclassification.

**CUTOVER 4.3:** atomically register the job and delete `IntegrationSyncProcessor`, `IntegrationSyncHostedDrainRunner`, and old registrations; reconcile keys, ratchet, health/metrics, and docs. Block if reclassification is unresolved.

### Phase 5 - Local and Incoming Webhook Drains (Tasks 5.1-5.9)

#### Local delivery lane (5.1 RED -> 5.2 GREEN -> 5.3 CUTOVER)

**RED 5.1:** prove stale recovery precedes claims, one bounded pass, current initial delay/interval, tenant fairness, token/fence loss, cancellation, timeout/network durable outcomes, empty maps, non-overlap, and health wording.

**GREEN 5.2:** one job calls recovery then one process batch. No scheduler retry.

**CUTOVER 5.3:** atomically register the job and delete the worker; preserve `LocalWebhookDeliveryHealthCheck` identity and add exact scheduler/backlog context.

#### Incoming intake lane (5.4 RED -> 5.5 GREEN -> 5.6 CUTOVER)

**RED 5.4:** with controllable `TimeProvider` and barriers, prove one batch, lease-loss cancellation, tenant/fence/generation refusal, interleaved-tenant cleanup, empty maps, non-overlap, and dedicated lane health.

**GREEN 5.5:** add a thin incoming job. Claim renewal remains in the bounded drain because it protects active work, not cadence.

**CUTOVER 5.6:** atomically register the job and delete `IncomingWebhookProcessor`; reconcile keys and docs/signals.

#### Incoming effect lane (5.7 RED -> 5.8 GREEN -> 5.9 CUTOVER)

**RED 5.7:** separately prove effect token/fence/generation, receipt-backed execution, lease renewal/loss, replay/retention, tenant cleanup, one pass, empty maps, and effect health.

**GREEN 5.8:** add a thin effect job while preserving existing outcomes and health.

**CUTOVER 5.9:** atomically register the job and delete the effect worker. Intake evidence cannot substitute for effect evidence.

### Phase 6 - Bulk Replay and Provider Publication (Tasks 6.1-6.6)

#### Bulk replay lane (6.1 RED -> 6.2 GREEN -> 6.3 CUTOVER)

**RED 6.1:** prove operation/item bounds, transaction/audit atomicity, tenant isolation, cancellation, one service call, initial delay/interval, empty maps, non-overlap, and queue health.

**GREEN 6.2:** add one job delegating once to `IWebhookBulkReplayService`.

**CUTOVER 6.3:** atomically register the job and delete the bulk worker; preserve audited state and add progress signals.

#### Provider publication lane (6.4 RED -> 6.5 GREEN -> 6.6 CUTOVER)

**RED 6.4:** prove disabled absence, publication-before-reconciliation order, bounded paired passes, token/fence loss, `PublicationUnknown` automatic/manual reconciliation, cancellation, empty maps, non-overlap, and queue health distinct from provider connectivity.

**GREEN 6.5:** add one job invoking the two existing passes in order.

**CUTOVER 6.6:** activate directly through Quartz when configured. The current processor is not host-registered; never add a temporary hosted registration.

### Phase 7 - PDS Drain Extraction (Tasks 7.1-7.3)

**RED 7.1:** characterize embedded `RunOnceAsync`: stable process lease owner, claim batch, bounded parallelism, per-claim scope, tenant/user/token/fence/expiry, expired reclaim, stale refusal, supersession, cancellation, payload secrecy, and lane health.

**GREEN 7.2:** extract orchestration into `IPdsSyncDrainService` and an Infrastructure implementation/settings validator while preserving `Atproto:PdsSync`. `AtprotoPdsDeliveryProcessor` and `IPdsSyncOutboxRepository` remain correctness authorities.

**CUTOVER 7.3:** **modify**, do not delete, `PdsSyncWorker` into a thin hosted cadence wrapper that delegates exactly one pass to `IPdsSyncDrainService`. Keep its hosted registration as the sole PDS cadence authority. Delete only `PdsSyncWorkerOptions`; Infrastructure-owned settings retain the existing configuration section. Remove embedded orchestration, update boundaries/tests/docs, and do not register the Quartz PDS job yet.

### Phase 8 - PDS Quartz Cutover (Tasks 8.1-8.3)

**RED 8.1:** prove the extracted service under Quartz with one pass, cancellation, two-node non-overlap, empty persisted maps, stable key/misfire, wrong/missing tenant, active lease/fence evidence, node-loss reclaim/new token, stale refusal, exact ambiguity policy, and lane health.

**GREEN 8.2:** add `pds-sync-drain` as a thin one-pass job plus stable catalog/key contract. The hosted worker remains the only active cadence authority until CUTOVER.

**CUTOVER 8.3:** atomically register the Quartz trigger, remove the hosted registration, and **delete `PdsSyncWorker`**. Reconcile exact keys, close `Task.Delay`/`PeriodicTimer` ratchets, and ship PDS health/metrics/docs. This is the only task that removes the worker; no release composes both authorities.

### R.1 - Release-owner runbook acceptance (outside implementation phases)

After all 39 tasks and Phase 8 verification are GREEN, capture single-node SQLite durability and two-node PostgreSQL clustering, coordinated stop/start, node termination after claim, pause/resume with retained audit, ambiguity, backlog recovery, exact key reconciliation, reverse rollback, resource evidence, scheduler RTO <=30 minutes, and rollback RTO <=45 minutes. R.1 is not a ninth implementation phase and is not a manual-QA-only substitute for CI.

## 8. Performance, Capacity, and Deterministic Query Budgets

The repository has no accepted lane measurements. These are **proposed and unmeasured**, never current claims. RED captures hosted-worker baseline before deletion; GREEN repeats identical fixtures, batch, concurrency, provider fake, and database topology.

### Work bounds

| Lane | Maximum admitted work per trigger |
|---|---|
| Registration submission | command batch default <=100 unless current validated request changes |
| Registration subscription | <=10 renewals plus <=10 sweeps |
| IntegrationSync | <= `BatchSize` |
| Local delivery | candidates <= `CandidateBatchSize`; claims <= `BatchSize` |
| Incoming intake/effects | claims <= `BatchSize`; concurrency <= `MaxConcurrentItems` |
| Bulk replay | operations <= `OperationsPerPass`; items <= `MaximumItemsPerOperation` per operation |
| Provider publication | publication and reconciliation batches each <= configured `BatchSize` |
| PDS | claims <= `BatchSize`; concurrency <= `MaxConcurrency` |

No trigger drains until empty. Backlog changes pass count and oldest age, not one-pass bounds.

### Proposed query budgets

Instrument EF commands for `N=0`, `N=1`, and `N=batch`. The wrapper adds **zero application-database commands** beyond the delegated pass. A lane must satisfy `Q(N) <= Q(0) + c*N`, with proposed unmeasured coefficients: submission `8`, subscription `12`, IntegrationSync `6`, local delivery `12`, incoming `8`, effects `10`, bulk replay `8` per operation, provider publication/reconciliation `12`, and PDS `12`.

A coefficient may be evidence-adjusted once during RED before production edits; record SQL shape and rationale. Superlinear growth is a blocker. CI interceptors deterministically assert bounded `Take/LIMIT`, status/due predicates, tenant predicates where required, and token/fence settlement predicates. CI does not assert wall-clock duration.

### Proposed production-shaped benchmark matrix

These thresholds are release evidence, not normal CI wall-clock assertions. Each fixture uses 50 tenants, `10 x` the lane's admitted batch as due work, and at least `100 x` the admitted batch as historical settled/terminal rows unless the row states are not applicable.

| Lane | Current/proposed bound | Proposed p95 claim/select budget | Proposed p95 one-pass budget excluding provider latency | Backlog recovery target after dependency recovery |
|---|---|---:|---:|---|
| EmailDispatch safety | batch 50; concurrency 8/global, 2/tenant; stale timeout 900s | 250ms | 5s before provider time | stale recovery visible within two one-minute recovery scans |
| Registration submission | batch 100; lease 60s | 100ms | 30s | `10 x` batch clears within 12 normal fires |
| Registration subscription | 10 renewals + 10 sweeps; lease 120s | 100ms per claim query | 10s | `10 x` each batch clears within 12 normal fires; checkpoint lag <=12h |
| IntegrationSync | batch 25; sequential current pass; proposed lease 120s | 100ms | 15s | stale reclaim begins within two 5s fires; `10 x` batch clears within 12 fires |
| Local webhook delivery | batch 100; concurrency 16/global, 4/tenant, 1/endpoint; lease 120s | 150ms | 2s before endpoint time | stale recovery visible within two 5s fires; `10 x` batch clears within 12 fires |
| Incoming intake | batch 50; concurrency 8; lease 120s | 100ms | 1s for local handlers | lease-loss detection within 40s; `10 x` batch clears within 12 fires |
| Incoming effects | batch 50; concurrency 8; lease 120s | 100ms | 2s | warning at 30s oldest due, operator page at 5m |
| Bulk replay | 10 operations/pass; 100 items/operation; 500 reserved items/tenant | 200ms operation selection | 30s per pass | 100 queued operations clear within 12 fires |
| Provider publication | batch 100; lease 120s | 150ms | 3s before provider time | `PublicationUnknown` warning at 2m, page at 15m, manual-review page at 1h |
| PDS | batch 20; concurrency 10; lease 90s | 100ms | 2s before provider time | trigger-to-claim <=10s; stale lease ratio <0.1% of claims/hour |

If a lane cannot meet its proposed threshold with the verified current indexes and settings, RED records the query plan and the slice adds only the required query/index correction. A schema change still triggers the intent and approval rules for generated provider migrations.

### Proposed performance budgets

- preserve each lane's exact configured/fixed cadence;
- p95 trigger-to-start <= one configured interval under supported test load;
- p95 one-pass duration below its interval and lease safety bound;
- no more than 10% sustained throughput, steady-state API CPU, or managed-memory regression at equal load;
- desired QRTZ key count converges after restart/cutover with no unbounded job/trigger growth;
- scheduler recovery RTO after one clustered node loss <=30 minutes.

Release benchmarks use fixed data/environment metadata. Deterministic CI subscribes to the exact event/state before triggering and awaits it with a bounded timeout. Fixed sleeps, polling delays, wait-for-time assertions, duplicate-observation windows, and test retries are forbidden.

## 9. Verification Contract

Each RED is observed with the exact focused command in the [task ledger](queue-driven-worker-migration-tasks.md). Each phase ends with exactly one Release build and at most one selected test-project command as specified there. Do not substitute solution-level tests or rerun unchanged inputs until they pass.

| Scenario | Required deterministic evidence |
|---|---|
| Global scheduler disabled | no Quartz drains; scheduler degraded; lane backlog unchanged/visible |
| Lane disabled | no job or trigger, not dormant/paused |
| Email hosted/disabled | no Email Quartz keys; non-email scheduler jobs still present |
| Stable identity | exact one key/trigger; empty maps; explicit misfire; `RequestsRecovery=false` |
| Long pass | same-key overlap blocked; different keys not falsely class-locked |
| PostgreSQL cluster | distinct nodes, one trigger acquisition, durable lane claim/fence remains authority |
| SQLite restart | persisted local state survives; labelled non-cluster evidence |
| Hard interruption | stale/expired claim observable; old tenant/token/fence cannot settle |
| Tenant concurrency | claim-derived fresh scope; interleaved tenants; context cleanup; wrong tenant affects zero rows |
| Ambiguous provider result | exact lane `Unknown`/`PublicationUnknown`/`Parked`/reconciliation policy; no Quartz retry |
| Key reconciliation | desired keys converge; exact retired keys removed; unrelated QRTZ rows survive |
| Shutdown | cooperative cancellation observed; hard kill recovers through durable state |
| Telemetry | closed bounded dimensions and aggregate ages/counts; zero tenant/PII/payload/token data |
| Query shape | deterministic command counts/shapes within frozen budget; no timing assertion |

Architecture ratchets must cover both `Task.Delay` and `PeriodicTimer`, require hosted `OutboxProcessor` and `ManagedControlPlaneRegistrationWorker`, reject GeneralOutbox jobs and dual authorities, and delist each deleted wrapper in its CUTOVER.

## 10. Coordinated Stop/Start and Rollback Runbook

Rolling old/new coexistence is forbidden.

### Preflight

1. Record versions, scheduler name, exact desired/retired key manifest, enabled lanes, trigger states, executing jobs, aggregate backlog/oldest age, and lane lease settings.
2. Confirm Tier 3 PostgreSQL persistent store, clustering, `InstanceId=AUTO`, synchronized clocks, schema validation, and no unrelated reconciliation candidate.
3. Record observable lease evidence: aggregate active/stale count, oldest processing age, nearest/latest expiry, lease-lost/fence-replacement count. Never record token/tenant values.
4. Name cutover owner, rollback owner, support escalation, and stop conditions.

### Cutover - scheduler RTO <=30 minutes

1. Start the clock when the first old replica begins stopping.
2. Stop **all** old replicas; verify no old process or cadence authority remains.
3. Observe cooperative completion. Require active claims to reach zero; after hard termination, require every aggregate lease to be expired/recoverable. Elapsed time alone is not evidence.
4. Start new replicas only after the boundary. Reconcile exact owned keys before readiness.
5. Verify expected keys/triggers, empty maps, no error state, no executing retired key, lane health, and one event-signalled bounded pass for every changed enabled lane.
6. End the clock only when durable progress is visible and backlog is stable/decreasing. Target <=30 minutes.

### Rollback - RTO <=45 minutes

1. Declare rollback on authority, key, tenant, fence, ambiguity, health, or capacity breach; start clock.
2. Pause exact migrated keys through the authorized scheduler surface; prove no new claim begins.
3. Allow cooperative completion, stop **all** new replicas, and observe active lease zero/expiry/recoverability.
4. Apply the reverse exact owned-key manifest through Quartz APIs; preserve unrelated QRTZ and all application queue rows.
5. Deploy the prior binary/configuration to all replicas; verify prior hosted workers are sole lane authorities and canonical existing scheduler jobs remain healthy.
6. Prove stale completion refusal, lane-specific recovery, and decreasing backlog. Target <=45 minutes.

No schema rollback is expected. If IntegrationSync RED forced schema-bearing reclassification, approve a separate migration and rollback procedure before that lane CUTOVER.

## 11. Operations, Zero-PII Telemetry, and Support

A healthy scheduler is not a healthy queue. Preserve `SchedulerTelemetryJobListener`, scheduler health/status/admin contracts, and bounded scheduler metrics. Before each CUTOVER, preserve or add lane backlog count, oldest due age, active/stale lease count, durable progress, ambiguity/manual-review count, and terminal count where applicable.

Allowed dimensions are stable job/group/lane, closed outcome/failure category, exception type, duration, aggregate counts and age buckets. Never emit tenant/user IDs, email, payload, registration answers, webhook URL/body, provider response/identifier, secret, DID, record key, lease token, raw exception text, or support-ticket content. Observable lease evidence is aggregate count/age/fence-loss, not token values.

Pause/resume/run-now/reset/interrupt remain opt-in, authenticated, instance-scoped, HAL-gated, read-only by default, and audited for success and refusal. Runbooks never instruct direct SQL edits. Audit retention/access/tamper evidence is a release gate because console-only logging is not durable accountability.

Support ownership follows the I-VSD report: upstream owns safe software defaults/docs/tests; each operator owns deployment capacity, monitoring, providers, audit destination, backup/restore, recovery, and local user remedy. Runbooks name an owner/escalation for unavailable scheduler, prolonged pause, backlog, stale lease, repeated failure, `Unknown`, `PublicationUnknown`, `Parked`, dead-letter growth, suspected tenant breach, telemetry leakage, and failed rollback.

## 12. I-VSD Acceptance

This is provider-responsibility engineering governance, not a fatwa, Sharia certification, legal opinion, security certification, or guarantee of no harm.

| Duty | Engineering consequence |
|---|---|
| Amanah / truthfulness | state remains NOT STARTED; do not call clustering exactly-once or healthy scheduler healthy queue |
| Non-harm | preserve ambiguity states; prohibit blind replay and mixed versions |
| Justice / tenant rights | bounded fair discovery; exact claim-derived tenant and fence; no context leak |
| Privacy / avoiding spying | empty recurring maps and zero-PII scheduler/telemetry/support evidence |
| Ease / autonomy | in-process SQLite path remains; no mandatory broker, scheduler SaaS, dashboard, or remote telemetry |
| Accountability | authorized retained operator audit; named cutover/rollback/support owner |
| Excellence | deterministic RED-first tests, query/resource budgets, observable recovery, rehearsed rollback |

The task ledger and consultation report use the same authoritative mapping below.

| I-VSD obligation | Authoritative current task IDs |
|---|---|
| `I-VSD-Q1` - accepted-but-unsettled Email becomes `Unknown` once | 1.1-1.3 |
| `I-VSD-Q2` - no Quartz general-outbox correctness promise | 1.4-1.6 |
| `I-VSD-Q3` - truthful disablement and lane backlog consequence | 2.1-2.3; CUTOVERs 3.3, 3.6, 4.3, 5.3, 5.6, 5.9, 6.3, 6.6, 8.3 |
| `I-VSD-Q4` - submission ambiguity and IntegrationSync retry/dead-letter stay lane-owned | 3.1-3.3; 4.1-4.3 |
| `I-VSD-Q5` - claim-derived tenant execution and cleanup | 3.1-3.6; 4.1-4.3; 5.1-5.9; 6.1-6.6; 7.1-8.3 |
| `I-VSD-Q6` - `PublicationUnknown`, reconciliation, replay exclusion | 6.1-6.6 |
| `I-VSD-Q7` - PDS retry/dead-letter/lease/stale-fence safety | 7.1-7.3; 8.1-8.3 |
| `I-VSD-Q8` - empty maps and payload/PII-free scheduler/telemetry | 1.1-1.3; 2.1-2.3; 3.1-8.3 |
| `I-VSD-Q9` - authorized control audit with retained evidence | 2.1-2.3; R.1 |
| `I-VSD-Q10` - upgrade, node failure, recovery, pause/resume, rollback | R.1 |
| `I-VSD-Q11` - bounded catch-up and resource use | 3.1-8.3; R.1 |
| `I-VSD-Q12` - support responsibility and escalation | 2.3; CUTOVERs 3.3, 3.6, 4.3, 5.3, 5.6, 5.9, 6.3, 6.6, 7.3, 8.3; R.1 |

P.1 records reviewer/date plus accepted/escalated controls and synchronizes the report's reciprocal text. Missing stakeholder interviews, legal/privacy review, production support rota, audit retention, capacity evidence, and religious-legal review remain labelled missing; do not invent approval.

## 13. Risk Register

| Severity | Risk | Mandatory gate |
|---|---|---|
| Blocker | Email accepted-before-settlement duplicates | Phase 1 deterministic real-drain RED/GREEN |
| Blocker | lifecycle remains email-gated | Phase 2 composition matrix |
| Blocker | IntegrationSync strands/stale-settles | Section 6 persistence RED/GREEN before cutover |
| Critical | mixed versions create two cadence authorities | coordinated stop/wait/start only |
| Critical | clustering mistaken for external exactly-once | durable claim/fence/ambiguity tests |
| Critical | reconciliation deletes unrelated scheduler rows | exact owned inventory and foreign-row survival proof |
| Critical | privileged worker crosses tenant boundary | interleaved tenant/fresh scope/wrong-tenant zero-row proof |
| High | recovery re-executes provider work | no `RequestsRecovery`; lane durable recovery only |
| High | scheduler healthy while queue stalls | per-lane backlog/age/stale/progress health |
| High | PDS orchestration leaks into API job | extract scheduler-neutral service first |
| High | publication starts without reconciliation | one ordered two-pass job and Unknown proof |
| High | audit cannot support incident review | retained/access-controlled audit evidence |
| Moderate | misfire or resume floods resources | collapse misfires, one bounded pass, stop thresholds |
| Moderate | query count grows with backlog/tenants | frozen deterministic query-shape budget |
| Moderate | telemetry leaks identity/payload | zero-PII assertions and closed dimensions |
| Moderate | custom listener replaced by deprecated integration | retain repository listener and official provenance |

## 14. Pull Request and Dependency Strategy

| PR | Ledger scope | Entry/exit |
|---|---|---|
| P | P.1 traceability and reviewer/date evidence | **complete 2026-08-24**; authorizes 1.1 and 1.4 RED only |
| 1 | Phase 1, Tasks 1.1-1.6 | first PR may contain only 1.1/1.4 RED and stop; GREEN/CUTOVER follow observed failures |
| 2 | Phase 2, Tasks 2.1-2.3 | Phase 1 GREEN; global authority and owned-key convergence; no queue cutover |
| 3 | Phase 3, Tasks 3.1-3.6 | separate registration lane RED/GREEN/CUTOVER evidence |
| 4 | Phase 4, Tasks 4.1-4.3 | IntegrationSync safety repair and atomic scheduler cutover |
| 5 | Phase 5, Tasks 5.1-5.9 | separate local/intake/effect RED/GREEN/CUTOVER evidence |
| 6 | Phase 6, Tasks 6.1-6.6 | bulk replay and publication evidence/cutovers |
| 7 | Phase 7, Tasks 7.1-7.3 | extract PDS; keep `PdsSyncWorker` as thin hosted wrapper; delete only options type |
| 8 | Phase 8, Tasks 8.1-8.3 | add PDS Quartz job; 8.3 alone deletes hosted worker during atomic cutover |
| R | R.1 outside implementation phases | automated gates already GREEN; release-owner rehearsal and RTO evidence |

PRs may be smaller. Never combine work in a way that hides RED, crosses an unresolved dependency, or temporarily registers two authorities. Operator-visible names/settings/health/metrics/docs ship with their CUTOVER, not later.

## 15. Definition of Done

- P.1 is approved, the report reciprocally references the authoritative Q1-Q12 mapping, and all **8 phases / 39 implementation tasks** retain exact dependencies and evidence.
- The [context](queue-driven-worker-migration-context.md), [tasks](queue-driven-worker-migration-tasks.md), and this plan agree on status, next, blockers, and evidence after every slice.
- Phase 1 real Email crash-window proof and GeneralOutbox absence contract are GREEN before Phase 2.
- Scheduler lifecycle is global; Email mode controls only Email jobs; one host graph has one authority per lane.
- `OutboxProcessor` and `ManagedControlPlaneRegistrationWorker` remain hosted exceptions.
- No GeneralOutbox scheduled promise/job/service exists.
- IntegrationSync stale recovery and tenant+lease-token settlement fencing are proven using existing fields unless RED forced approved schema reclassification.
- Every recurring lane has exact stable key/trigger, empty maps, explicit collapse misfire, no recovery request, one bounded pass, and correct per-key non-concurrency semantics.
- Durable claims, retries, tenant scopes, fences, ambiguity, and settlement remain below Quartz.
- Exact owned-key reconciliation converges without deleting unrelated QRTZ state.
- Every lane has bounded tenant-free backlog/age/stale/progress/terminal signals and zero-PII telemetry.
- Proposed query/performance/resource budgets have measured before/after evidence and deterministic CI query-shape enforcement.
- SQLite evidence is labelled local durability; PostgreSQL evidence proves two-node scheduler mechanics without exactly-once claims.
- Phase 7.3 leaves `PdsSyncWorker` as a thin hosted cadence wrapper and deletes only `PdsSyncWorkerOptions`; Phase 8.3 alone deletes the worker while atomically registering Quartz.
- Architecture detects `Task.Delay` and `PeriodicTimer` after Phase 8.3 and retains only justified hosted exceptions.
- Coordinated cutover and rollback have observable lease/key evidence, scheduler RTO <=30m, and rollback RTO <=45m.
- Required focused test and Release build commands pass once; no suppression, fixed sleeps, polling waits, skipped failures, retries, or manual-only acceptance remains.

## 16. Implementation-Agent Contract

1. Read this plan, [context](queue-driven-worker-migration-context.md), [task ledger](queue-driven-worker-migration-tasks.md), [I-VSD report](../../../islamic-value-sensitive-design/i-vsd-queue-driven-worker-migration.md), both linked Quartz reports, `.agents/rules/api-scheduling.md`, and `docs/QUICK_REFERENCE.md` before a slice.
2. Follow the current exact handoff: Task 4.2 GREEN only. First implement the approved explicit non-replay ambiguity/reconciliation classification required by confirmed Task 4.1 RED; do not add ordinary stale replay for the unkeyed Listmonk POST.
3. Re-read current source/tests before editing. Repository behavior outranks a stale file list.
4. Observe the named RED before production code. Do not add post-hoc tests or broaden beyond the failed contract.
5. Make the smallest GREEN change. Do not refactor adjacent workers, migrate unlisted lanes, add another scheduler, or create a generic retry/dead-letter abstraction.
6. Never add compatibility aliases, dual registrations, mixed authorities, broad QRTZ deletion, scheduler business retry, tenant/payload maps, or direct SQL operator controls.
7. Keep `OutboxProcessor` and `ManagedControlPlaneRegistrationWorker` hosted. Never create a GeneralOutbox Quartz replacement.
8. Stop/reclassify IntegrationSync if accepted-before-settlement replay safety fails; generate migrations rather than hand-editing only after explicit approval.
9. Subscribe to exact async signals before action; use controlled barriers, `TimeProvider`, and bounded timeouts. No sleeps, polling delays, duplicate-observation windows, or retries.
10. Preserve stable current contracts only where identified as canonical. Update docs, tasks, context, I-VSD traceability, and evidence in the same slice.
11. Run exactly the scoped diagnostics/build/test gate once and report actual output. Never suppress type errors, warnings, test failures, or skip failing tests.
12. Use the coordinated runbook and retain zero-PII aggregate lease/key/RTO evidence for CUTOVER/rollback.
13. Stop when any RED, dependency, tenant, ambiguity, key, health, audit, budget, I-VSD, RTO, or single-authority gate is unresolved. Schedule pressure is not an override.
