<!-- ABOUTME: Implementation report on Quartz.NET background jobs — capability surface, current usage, and adoption priorities. -->
<!-- ABOUTME: Grounds every claim in repository code and the shipped Quartz 3.19.1 assembly rather than vendor marketing. -->

# Quartz.NET Background Jobs — Implementation & Adoption Report

> **Status:** Implementation report and prioritized roadmap
> **Last Updated:** 2026-08-16 Europe/Brussels
> **Quartz.NET version in use:** `3.19.1` (Apache-2.0)
> **Applies to:** `src/Explore.API/Scheduling/`, `src/Explore.API/Extensions/QuartzSchedulerExtensions.cs`, `src/Explore.API/BackgroundServices/`
> **Companion document:** [`quartznet-background-jobs-selection-report.md`](quartznet-background-jobs-selection-report.md) — *why* Quartz.NET was chosen
> **Delivered by:** [`dev/active/tickerq-to-quartznet-migration/`](../active/tickerq-to-quartznet-migration/)

---

## 1. Executive Summary

Quartz.NET is now the platform's durable scheduler, replacing TickerQ. It is live, but **deliberately narrow**: it drives three jobs, while **35 `BackgroundService` classes** continue to run their own private `PeriodicTimer` / `Task.Delay` loops elsewhere in the codebase.

That gap is the central finding of this report. The migration bought a capability the platform has barely started spending.

```
┌──────────────────────────────────────────────────────────────────────────┐
│  BACKGROUND WORK TODAY                                                   │
├──────────────────────────────────────────────────────────────────────────┤
│  Quartz-scheduled ..............   3 jobs   ▓                            │
│  Hand-rolled BackgroundService ..  35 classes ▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓▓  │
│                                                                          │
│  Of those 35, roughly 13 are periodic maintenance or deadline work that  │
│  Quartz already models better. The rest are queue drains, startup gates, │
│  or long-lived stream consumers that should NOT move.                    │
└──────────────────────────────────────────────────────────────────────────┘
```

**The single highest-value next step** is not migrating drains — it is `InventoryHoldExpiryWorker`, which polls every 60 seconds to discover expiries that are already known at write time. That is precisely the shape Quartz one-off triggers exist for, it is on the ticketing revenue path, and the pattern it establishes unlocks the rest.

---

## 2. What We Use Quartz.NET For Today

Three jobs, all owned by the email-dispatch domain. Everything is confined to `Explore.API`; Application-layer contracts stay scheduler-neutral.

| Job | Type | Schedule | Payload | Delegates to |
|---|---|---|---|---|
| `email-dispatch-drain` | `EmailDispatchDrainJob` | Cron `*/10 * * * * ?` | none | `IEmailDispatchDrainService.ProcessBatchAsync` |
| `email-dispatch-recovery-scan` | `EmailDispatchRecoveryScanJob` | Cron `0 */1 * * * ?` | none | `IEmailDispatchDrainService.RecoverStaleProcessingAsync` |
| `event-reminder-dispatch` | `EventReminderDispatchJob` | One-off `SimpleTrigger`, `StartAt(dueAt)` | pointer-only JSON | `IEmailDispatchDrainService.ProcessSingleAsync` |

### 2.1 Architectural shape

- **Durable ADO job store** co-located in the primary database under the `QRTZ_` prefix, on PostgreSQL, SQLite, SQL Server, MariaDB, and MySQL. No EF Core `DbContext`, no second migration chain.
- **Schema by embedded idempotent DDL** (`QuartzSchemaInitializer`), applied at API startup, gated by `Scheduler:Quartz:ApplySchemaOnStartup`. The scripts contain no `DROP`/`TRUNCATE`.
- **`[DisallowConcurrentExecution]`** on all three jobs — the scheduler, not the job body, enforces single-flight.
- **Pointer-only payloads.** `JobDataMap` carries durable identifiers as JSON with `UseProperties = true`. Message content never enters scheduler state; enforced by `DurableSideEffectBoundaryTests`.
- **The drain is the single retry authority.** One-off reminder triggers deliberately have *no* scheduler retry — a failed wake-up leaves the outbox row due and the next drain pass collects it. This avoids two competing retry policies over one state machine.
- **Graceful shutdown** via `WaitForJobsToComplete = true`.

### 2.2 Operator surface

A read-only JSON status endpoint (`QuartzSchedulerStatusEndpoint`) at `Scheduler:Quartz:StatusEndpointPath`, disabled by default, behind the `quartz_instance_admin` policy. It reports scheduler identity, job/trigger state, and next/previous fire times — never payloads.

---

## 3. Quartz.NET Capability Surface — Used vs. Available

Verified against the shipped `Quartz 3.19.1` assembly and its documentation, not vendor claims.

| Capability | Status | Notes |
|---|---|---|
| `CronTrigger` (6/7-field, seconds precision) | ✅ **In use** | Requires `?` in day-of-month **or** day-of-week; both `*` is rejected |
| `SimpleTrigger` + `StartAt` | ✅ **In use** | One-off delayed execution |
| `AdoJobStore` durable persistence | ✅ **In use** | All five providers |
| `[DisallowConcurrentExecution]` | ✅ **In use** | |
| `JobDataMap` + System.Text.Json | ✅ **In use** | `UseProperties = true` |
| Misfire instructions | ✅ **In use** | `DoNothing` on cron, `FireNow` on one-off |
| DI-scoped job factory | ✅ **In use** | Scope per execution |
| Graceful shutdown | ✅ **In use** | |
| **Clustering** (DB load-balance + failover) | ⚙️ **Implemented, unexercised** | `ClusteringEnabled`; validated to require `InstanceId=AUTO`. No multi-node test |
| **`DailyTimeIntervalTrigger`** | ❌ Unused | "Every 15 min between 09:00–17:00" without cron gymnastics |
| **`CalendarIntervalTrigger`** | ❌ Unused | True calendar months/years; DST-correct |
| **`RecurrenceTrigger`** (RFC 5545 RRULE) | ❌ Unused | **Directly relevant** — event recurrence is an RRULE domain |
| **Calendars** (`HolidayCalendar`, exclusions) | ❌ Unused | Blackout windows; suppress sends on excluded dates |
| **Job/Trigger/Scheduler listeners** | ❌ Unused | Centralized failure metrics, audit, chaining |
| **Trigger priority** | ❌ Unused | Ordering under contention |
| **`RequestRecovery`** | ❌ Unused | Re-fire jobs interrupted by a hard crash |
| **`PersistJobDataAfterExecution`** | ❌ Unused | Cursor/checkpoint state across executions |
| **Execution limits / execution groups** | ❌ Unused | ⚠️ Needs extra schema columns — see §6 |
| **Node affinity** (`PREFERRED_NODE`) | ❌ Unused | ⚠️ Needs extra schema columns — see §6 |
| **`TimeProvider`** injection | ❌ Unused | Deterministic schedule testing without sleeps |
| **`Quartz.Dashboard`** (first-party UI) | ✅ Adopted (standalone only) | Blazor Server + SignalR; referenced only by `Event.Standalone` — see §7 |
| Native OpenTelemetry source | ✅ Wired | `.AddSource("Quartz")` in ServiceDefaults |

---

## 4. The Wider Background-Work Landscape

35 `BackgroundService` subclasses plus 3 direct `IHostedService` implementations. Classified **by shape**, which determines whether Quartz helps:

### 4.1 Periodic maintenance — strong Quartz candidates

Each currently owns an `InitialDelaySeconds` + `PeriodicTimer(PollingIntervalMinutes)` loop. None survives a restart with schedule fidelity; all re-drift on every deploy.

`AiRetentionCleanupProcessor` · `EmailDispatchRetentionCleanupProcessor` · `IdempotencyCleanupProcessor` · `RegistrationRetentionCleanupProcessor` · `WebhookRetentionCleanupProcessor` · `PrivacyErasureCredentialCleanupProcessor` · `StorageReconciliationProcessor` · `OrganizerPaymentReadinessReconciliationWorker` · `SvixWebhookEventTypeSyncWorker` · `WebhookEventTypeCatalogSyncWorker` · `ManagedControlPlaneRegistrationWorker`

**What Quartz adds:** real cron windows (run retention at 03:00, not "3600s after whenever the pod started"), durable next-fire-time, `[DisallowConcurrentExecution]` across replicas once clustered, and one uniform operator view instead of 11 bespoke settings blocks.

### 4.2 Deadline / expiry work — best fit, highest value

`InventoryHoldExpiryWorker` (polls every 60s) · `RegistrationFinalizationWorker`

**What Quartz adds:** the expiry instant is known when the hold is created. Polling converts a known deadline into an average 30-second delay and a permanent table scan. A one-off trigger fires *at* the deadline. This is the same shape already proven by `event-reminder-dispatch`.

### 4.3 Continuous queue drains — migrate the *timer*, keep the claim logic

`OutboxProcessor` · `IntegrationSyncProcessor` · `WebhookDeliveryProcessor` · `WebhookBulkReplayProcessor` · `WebhookProviderPublicationProcessor` · `IncomingWebhookProcessor` · `IncomingWebhookEffectProcessor` · `NotificationFanoutProcessor` · `WebPushDispatchProcessor` · `PdsSyncWorker` · `RegistrationProviderSubmissionWriteWorker` · `RegistrationProviderSubscriptionLifecycleWorker`

**Caution:** these already implement atomic claim, lease, fairness, and backpressure against the database. Quartz replaces only the *timer*. Value is real but modest, and the regression risk is the highest of any category. **Do not treat these as early wins.**

### 4.4 Do **not** migrate

- **Startup gates:** `AiProviderSettingsBootstrapWorker`, `CerbosPolicyBootSyncWorker`, `PrivacyErasureStartupGate`, `JwtAuthorityWarmupHostedService`, `LookupDataCacheInitializer`. These must run once, before traffic. A scheduler adds latency and failure modes for no benefit.
- **Long-lived stream consumers:** `AtprotoJetstreamSubscriber`, the three `EmailDispatchRabbitMq*` services. These hold persistent connections; they are not scheduled work.
- **In-memory queue workers:** `AiAssistantRunWorker` drains a `Channel<T>` for interactive latency. Durable scheduling would make it slower and worse.

---

## 5. Priorities

Ordered by *value ÷ risk*, not by convenience.

### P0 — Correctness and proof (do before any migration)

| # | Item | Why now |
|---|---|---|
| P0.1 | **Execute the non-SQLite DDL against real engines** | Only SQLite is proven end-to-end. PostgreSQL is the Tier 2/3 default and is asserted only structurally. A Testcontainers PostgreSQL round-trip closes the largest correctness unknown in the migration |
| P0.2 | **Enable `PerformSchemaValidation`** | Quartz can verify the schema matches its expectations at startup. This class of defect is otherwise silent — see §6 |
| P0.3 | **Prove clustering before Tier 3** | `ClusteringEnabled` is implemented and validated but never run multi-node. Two replicas double-firing `email-dispatch-drain` is a live risk the moment anyone scales out |

### P1 — Highest-value adoption

| # | Item | Why |
|---|---|---|
| P1.1 | **`InventoryHoldExpiryWorker` → one-off triggers** | Revenue path. Removes a 60s poll and an average 30s release delay; frees held inventory precisely at expiry. Reuses the proven `event-reminder-dispatch` pattern |
| P1.2 | **Retention/cleanup family (6 processors) → cron jobs** | Highest count, lowest risk, near-identical shape. Lets operators put heavy deletes in real off-peak windows. Convert as one batch to establish the template |
| P1.3 | **Job listener for uniform failure telemetry** | A single `IJobListener` gives every job consistent failure counters, durations, and OTel spans — instead of 35 hand-written log lines |

### P2 — Capability expansion

| # | Item | Why |
|---|---|---|
| P2.1 | **`RecurrenceTrigger` (RRULE) for recurring events** | The product domain already speaks RFC 5545. Worth a spike before hand-rolling recurrence expansion |
| P2.2 | **Calendars for quiet hours / blackout windows** | Natural fit for notification policy: suppress non-critical sends on excluded dates without touching job logic |
| P2.3 | **Reconciliation & catalog syncs → cron** | `StorageReconciliation`, `OrganizerPaymentReadiness`, Svix/webhook catalog syncs |
| P2.4 | **`TimeProvider` for deterministic schedule tests** | Removes sleep-based timing from scheduler tests |
| P2.5 | **Queue drains** | Only after P1 proves the pattern. Timer-only replacement; preserve all claim/lease logic |

### Explicitly deferred

- Migrating startup gates or stream consumers (§4.4) — wrong tool.
- `Quartz.Dashboard` — see §7.
- Execution limits / node affinity — needs schema work (§6) and only matters with clustering.

---

## 6. Schema Feature-Gating ⚠️

Quartz's newer capabilities require **additional optional columns**. The store degrades *silently* when they are absent — it logs and continues rather than failing, which makes this an easy class of defect to ship unnoticed.

| Column | Table(s) | Gates | Present in our DDL? |
|---|---|---|---|
| `MISFIRE_ORIG_FIRE_TIME` | `QRTZ_TRIGGERS` | Correct `ScheduledFireTimeUtc` on misfired triggers | ✅ **Yes — added 2026-08-16** |
| `EXECUTION_GROUP` | `QRTZ_TRIGGERS`, `QRTZ_FIRED_TRIGGERS` | Execution limits / per-node thread caps | ❌ No |
| `PREFERRED_NODE`, `PREFERRED_NODE_AUTO` | `QRTZ_TRIGGERS` | Node affinity (must be added together) | ❌ No |

> **Found while researching this report.** `MISFIRE_ORIG_FIRE_TIME` was missing from all four scripts. Quartz probes for it and logs *"Column MISFIRE_ORIG_FIRE_TIME not found in triggers table. ScheduledFireTimeUtc will not be corrected for misfired triggers with AdoJobStore."* — it does not throw. Because the platform configures misfire handling explicitly, this was a genuine correctness loss that tests could not catch, since no misfire occurred during them. Fixed across all four provider scripts with a per-provider regression test.

`EXECUTION_GROUP` and node-affinity columns are deliberately **not** added: they gate features we do not use, and unused columns are liabilities. Add them together with the feature, never speculatively.

---

## 7. Resolved Decision: `Quartz.Dashboard` (2026-08-16)

`Quartz.Dashboard` 3.19.1 is **first-party and Apache-2.0**. This decision is now **resolved with a topology-aware
split**, not a single yes/no.

**Correction to the earlier assessment.** That assessment credited the package with "an HTTP API". It has none.
Verified against the shipped assembly's exported types and the official package documentation, its entire public
surface is `AddQuartzDashboard(Action<QuartzDashboardOptions>)`, `MapQuartzDashboard()`, the
`MapQuartzDashboard(RazorComponentsEndpointConventionBuilder)` coexistence overload, `QuartzDashboardOptions`, and
`IDashboardAuthorizationFilter`. There is no `MapQuartzHttpApi()` and no API-only hosting mode. `QuartzDashboardOptions.ApiPath`
exists but is the dashboard's own internal transport for its Blazor components (`IQuartzApiClient` / `InProcessQuartzApiClient`),
undocumented, and covered by the package's own warning that its API surface may change between releases. Any plan that
assumed a Blazor-free REST surface from this package was unimplementable.

**What shipped instead:**

1. **A first-party scheduler administration API** at `/api/admin/scheduler` — a normal versioned HAL controller over an
   Application-owned `ISchedulerOperations` seam implemented by a Quartz adapter in the API layer. It works in **both**
   topologies, adds no Blazor or SignalR dependency to the API host, and gates its controls through the platform's own
   HAL affordance pipeline — which a third-party endpoint could never do, since it emits no HAL links. Enabled with
   `Scheduler:Quartz:AdminApiEnabled`, read-only by default.
2. **A first-party admin UI** — the `InstanceSchedulerSection` under Instance Settings, consuming that API through the
   BFF proxy and gating every control on server-emitted `_links`.
3. **The upstream dashboard in `Event.Standalone` only**, at `Scheduler:Quartz:DashboardPath` (default `/quartz`),
   enabled with `Scheduler:Quartz:DashboardEnabled`. The package reference lives solely in `Event.Standalone`.

**Why the standalone embed uses the self-contained mapping.** The documented "existing Blazor app" overload expects the
host's `Router` to resolve the dashboard's attribute-routed pages via `AdditionalAssemblies`. This application routes
through **Blazouter**'s explicit route table, which resolves only components listed in it and has no attribute-route
fallback — so that overload would render the app's not-found page at every dashboard path. `MapQuartzDashboard()`
brings the dashboard's own root component and route table, leaving it independent of the host router. This also keeps
every dashboard component out of `Explore.Blazor.Client`, which is compiled into the WebAssembly bundle.

The read-only JSON status endpoint at `/admin/scheduler` is retained unchanged as the lightweight scripted-check path.

---

## 8. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Non-SQLite DDL has a provider-specific defect | Medium | High | P0.1 Testcontainers round-trip; P0.2 schema validation |
| Two replicas double-fire cron jobs when scaled out | Medium | High | P0.3 — clustering is implemented but unproven; treat scale-out as gated on it |
| Silent capability loss from a missing optional column | Medium | Medium | §6 table; enable `PerformSchemaValidation`; regression tests per provider |
| Migrating queue drains regresses claim/lease semantics | Medium | High | Keep drains at P2.5; migrate the timer only, never the claim logic |
| Scheduler tables contend when instances share a database | Medium | Medium | Distinct `SchedulerName` per instance, or deliberate clustering |
| Cron expressions ported from another scheduler are invalid | Low | Medium | Quartz needs `?`; validate every expression on migration |

---

## 9. Definition of Done for the Next Slice

A background job is considered "migrated to Quartz" only when all hold:

1. It is an `IJob` in `Explore.API/Scheduling/` with `[DisallowConcurrentExecution]` where single-flight matters.
2. It delegates to an Application-layer contract and owns no business state.
3. Its trigger carries pointer-only data; no payload, PII, or secrets enter scheduler tables.
4. Its cron expression is Quartz-valid (`?` rule) and its misfire instruction is chosen deliberately.
5. The replaced `BackgroundService` is **deleted**, not left dormant behind a flag.
6. `DurableSideEffectBoundaryTests` still passes — Quartz stays out of Application and Domain.
7. Its settings moved under `Scheduler:Quartz` or its own validated options class, and `docs/CONFIGURATION.md` documents them.

---

## 10. Verification Commands

```bash
dotnet build --configuration Release --verbosity quiet

dotnet test tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj \
  --configuration Release --verbosity quiet -- --treenode-filter "/*/*/Quartz*/*"

dotnet test tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj \
  --configuration Release --verbosity quiet

dotnet run --file .ci/scripts/validate-dependency-license-policy.cs
```

---

## 11. Appendix — Key Files

| Path | Responsibility |
|---|---|
| `Explore.API/Extensions/QuartzSchedulerExtensions.cs` | DI registration, provider switch, job/trigger wiring, status route |
| `Explore.API/Configuration/QuartzSchedulerSettings.cs` | Validated settings (`Scheduler:Quartz`) |
| `Explore.API/Scheduling/QuartzSchedulerKeys.cs` | Stable job/trigger keys and cron constants |
| `Explore.API/Scheduling/QuartzSchemaInitializer.cs` | Embedded idempotent DDL application |
| `Explore.API/Resources/Quartz/QuartzSchema.*.sql` | Per-provider schema |
| `Explore.API/Scheduling/QuartzSchedulerStatusEndpoint.cs` | Read-only operator surface |
| `Explore.Application/Contracts/Scheduling/*` | Scheduler-neutral job catalog |
| `Event.API.IntegrationTests/Features/QuartzSqliteDurableSchedulingTests.cs` | End-to-end durability proof |
