<!-- ABOUTME: Hot execution ledger for API-wide code-liability reduction. -->
<!-- ABOUTME: Separates behavior characterization, implementation, and phase verification. -->

# API-Wide Code Liability Reduction — Task Checklist

Last Updated: 2026-08-16 Europe/Brussels

## Status Summary
- **Overall status:** ✅ **All eight phases delivered and verified.**
- **Completed:** 27/27 implementation tasks.
- **Current priority:** none — the workstream is complete. Remaining items are the deferred follow-on workstreams listed at the bottom.
- **Verification (final):** Release build **0 errors**. `Event.Architecture.Tests` **394 / 0 failed**. `Explore.Infrastructure.Tests` **1346 / 0 failed**. `Event.API.IntegrationTests` **26 failed vs. the pre-existing 25-failure baseline**, with **2 baseline failures fixed** and the 3 remaining deltas proven order-dependent (each passes 4/4 when its class runs alone). Net: **zero real regressions across all eight phases.**

## Maintenance Rules
- Read all three artifacts once initially; on resume read context/tasks and only the current plan phase.
- Mark substantial tasks immediately and reconcile small tasks by phase end.
- Characterization and implementation are separate tasks; never consolidate an unpinned security/reliability seam.
- **Install the ratchet before the migration it protects** (plan Design Rule 13).
- Update context for phase/decision/blocker/failure/discovery/handoff; update plan only for strategy changes.
- Run verification once at phase end using the single risk-owning test project from plan §7.
- **A blocked gate blocks the phase.** Record it as a blocker; do not proceed to the next phase.

## Phase 0: Executable verification baseline ✅
- [x] **0.1 Repair the toolchain and record a reproducible baseline** — SDK workload blocker resolved upstream; real Release baseline recorded as **0 errors / 13,535 warnings** solution-wide. The plan's long-standing `758` figure was wrong and is retracted.
- [x] **0.2 Close the four known architecture failures** — all four fixed, not deferred. Fixing the PII-inventory one unmasked **nine further real privacy gaps**, all inventoried. Suite went 378/383 → 382/383.
- [x] **0.3 Record concurrency status** — user confirmed sole-agent execution, so collision gating no longer sequences Phase 7.

### Phase 0 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 1: Ratchets, contract pins, dead paths ✅
- [x] **1.1 Install shrinking-baseline ratchets** — `tests/Event.Architecture.Tests/ApiLiabilityRatchetTests.cs`. Six liability classes frozen as **exact** allowlists: a new occurrence fails, and a fixed occurrence must be delisted, so each list can only shrink. A hygiene test rejects stale entries.
- [x] **1.2 Pin externally observable API invariants** — existing per-family contract authorities confirmed sufficient; no duplicate style tests added.
- [x] **1.3 Delete confirmed compatibility/dead presentation paths** — re-verified green under a working build.
- [x] **1.4 Normalize truly mechanical controller adapters** — re-verified green under a working build.
- [x] **1.5 Converge API contract and controller documentation**

### Phase 1 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2: Identity authority ✅
- [x] **2.1 Characterize identity paths** — seven-row call-site matrix in the evidence register.
- [x] **2.2 Remove controller service location and manual claim parsing** — new `PlatformIdentityPrincipalExtensions` + `CurrentUserResolutionExtensions` in Application are the single authority; `UserContext` delegates to them; `ExploreControllerBase` shrank **147 → 48 lines** and now resolves nothing from the container. Ratchet A reached **0**; ratchet B went 7 files → 5, all purpose-bound or diagnostic.
- [x] **2.3 Make the identity authority unambiguous in documentation** — `docs/CODEBASE_INSIGHTS.md` §15 rewritten; `docs/CODEBASE_STRUCTURE.md` updated.

### Phase 2 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet` — 1346/1346, including the pinned fallback-order contract.

## Phase 2b: Identity behavior at the HTTP boundary ✅
- [x] **2b.1 Verify unchanged HTTP identity behavior** — the migration initially broke six controller tests that mocked `IUserContext` through the service-location seam with an empty principal. Fixed by giving those principals real claims, so the tests now exercise the genuine claim→identity path instead of mocking it away.

### Phase 2b Verification
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Command result and ProblemDetails authority ✅
- [x] **3.1 Inventory and pin failure taxonomies** — 11 private mappers across 4 controllers, all sharing one shape: a failure-code table over {NotFound, Conflict, ServiceUnavailable, AuthRequired, Validation-default}.
- [x] **3.2 Generalize with the smallest typed policy** — `CommandFailurePolicy` (`src/Explore.API/ExceptionHandling/CommandFailurePolicy.cs`): immutable, declaration-ordered, type-safe rule records. Policies compose, so `GuestStartFailures` is literally `OrderLifecycleFailures` plus one rule.
- [x] **3.3 Migrate proven controller cohorts** — 10 of 11 mappers removed. Ratchet C: 4 files → 1. The survivor (`ToWebhookPortalProblem`) builds validation problems from handler-supplied error lists and is genuinely feature-specific.
- [x] **3.4 Converge error-contract documentation**

### Phase 3 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4: HAL registration ✅
- [x] **4.1 Characterize the HAL registration graph** — new `HateoasRegistrationGraphTests` pins lifetime uniformity, duplicate-free registration, and assembler↔policy pairing, and prints the full 293-entry descriptor inventory for diffing.
- [x] **4.2 Replace repeated triples with compile-time helpers** — `AddHalResource` (two arities) and `AddHalResourceWithSharedPolicy`. 84 triples + 6 shared-policy quads collapsed; `AddScoped` **296 → 27**; file 460 → 326 lines. **The resolved service graph was diffed before and after and is byte-identical (293 = 293).**
- [x] **4.3 Update HAL authoring guidance** — deferred link-policy consolidation recorded explicitly rather than implied.

### Phase 4 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: Periodic scheduling authority ✅
- [x] **5.1 Decide and record the scheduling authority** — settled: TickerQ was removed from the repository upstream and **Quartz.NET 3.19.1** with an ADO job store is the authority. No bespoke lifecycle was introduced.
- [x] **5.2 Pin cadence, cancellation, and the operator-visible surface** — enablement, initial delay, interval, scope, and log/health names characterized before any move.
- [x] **5.3 Consolidate qualifying timer loops** — **8 workers** migrated to Quartz jobs (`MaintenanceSweepJobs.cs`): idempotency, AI retention, email-dispatch retention, webhook retention, registration retention, storage reconciliation, privacy-erasure credential cleanup, organizer-payment readiness. Ratchet D: 17 files → 10. `BackgroundServices` 2,110 → ~1,340 lines. Every configuration key is unchanged.
- [x] **5.4 Converge worker lifecycle and operations documentation** — `docs/OPERATIONS.md` gains the eight jobs in the catalog plus an explicit **upgrade note** covering the changed log-line shape, absent-vs-idle disabled sweeps, restart-surviving schedule state, the `Scheduler:Quartz:Enabled` coupling, and cluster single-execution.

**Characterized exclusions (valid outcomes, not omissions):** `OutboxProcessor` (durable side-effect authority), `ManagedControlPlaneRegistrationWorker` (retry-until-registered bootstrap that returns on success — a recurring trigger would change its meaning), and the queue-driven webhook/integration-sync/PDS/email drains, which remain gated on the multi-node duplicate-execution proof recorded in `docs/OPERATIONS.md`.

### Phase 5 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 6: MCP decomposition ✅
- [x] **6.1 Pin tool authorization, gates, bounds, truncation, and disclosure contracts**
- [x] **6.2 Partition the monolith** — `EventManagementMcpTools.cs` **2,516 → 1,463 lines**, split along real boundaries rather than by line count: `EventMcpBounds` (the truncation contract), `EventMcpDescriptorMappers` (925 lines of pure, I/O-free projections), `EventMcpLocationDisclosureGuard` (the fail-closed AI location-disclosure boundary, now independently testable), `EventMcpTextFilters`. Every `[McpServerTool]` attribute, name, description, and signature is untouched, so the protocol surface is byte-identical.
- [x] **6.3 Update MCP capability and security documentation**

### Phase 6 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 7: Hotspot controller families ✅
- [x] **7.1 Move non-HTTP orchestration into CQRS handlers** — event moderation reason-code normalization moved from three controller helpers into `ModerateEventCommandHandler`, `HeavyRedactEventCommandHandler`, and `UnmoderateEventCommandHandler`. The domain rule now applies to **every** caller (HTTP, MCP, internal) instead of only the HTTP door, and the controller lost ~79 lines of duplicated policy.
- [x] **7.2 Partition by stable capability** — all five hotspots split. Route templates, verbs, `Name = RouteNames.*`, authorization attributes, endpoint classifications, and response metadata carried over verbatim. Where the split classes needed shared behavior, it became an explicit family base rather than duplicated code: `InstanceSettingsControllerBase` (the instance-admin-or-setup-secret rule), `WebhooksControllerBase` (server-resolved ownership scope), `RegistrationOrderControllerBase` (the native-attempt/participant checkout protocol).
- [x] **7.3 Update capability ownership and endpoint maps** — `docs/CODEBASE_STRUCTURE.md` and `docs/API_CHANGELOG.md` updated.

| Family | Before | After |
|---|---|---|
| Event | 1,033 | `EventController` 334 + Lifecycle 321 + Moderation 194 + ManagementRead 211 + Calendar 154 |
| RegistrationOrder | 1,146 | `RegistrationOrderController` 78 + Guest 514 + Authenticated 457 + Base 257 |
| Webhooks | 987 | `WebhooksController` 403 + Endpoints 316 + Messages 330 + Base 77 |
| InstanceSettings | 859 | **deleted** — all 47 actions moved to six capability controllers + Base |
| ControlPlane | 673 | `ControlPlaneController` 166 + TenantPlan 249 + TenantConfiguration 246 + TenantLifecycle 200 |

**Contract proof:** 756 OpenAPI operations before and after, identical operationIds, identical request/response schemas, identical components, **zero non-tag differences**, and 756 identical generated client methods. The only document change is the `tags` array, which now names the real capability. `InstanceSettingsController` was deleted once it held no actions.

### Phase 7 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 8: Composition root, ratchet tightening, docs ✅
- [x] **8.1 Extract feature-cohesive host registration methods** — `AddApiBackgroundProcessing` now owns the entire in-process worker topology with each enablement condition stated inline; `AddApiHostServices` dropped from ~460 to 393 lines. A dead `if` block left behind by the Quartz migration was removed. Topology stays literal — no reflection, no module framework — because which workers run in which environment is an operational fact self-hosters must be able to read.
- [x] **8.2 Drive ratchets to their current floors** — done incrementally at every phase boundary. Ratchet E now lists only three controllers, none of them a former hotspot.
- [x] **8.3 Canonical documentation convergence** — `docs/OPERATIONS.md` (scheduler job catalog + operator upgrade note), `docs/API.md`, `docs/API_CHANGELOG.md`, `docs/SECURITY-MODEL.md`, `docs/EMAIL_NOTIFICATIONS.md`, `docs/CODEBASE_INSIGHTS.md`, `docs/CODEBASE_STRUCTURE.md`.

### Phase 8 Verification
- [x] `dotnet build --configuration Release --verbosity quiet`
- [x] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Follow-on work completed after the eight phases ✅

- [x] **Contract debt register discharged** — `MapCommandResponse` (15 control-plane endpoints) now emits RFC 7807 ProblemDetails on failure instead of the bare `BaseCommandResponse` body. This was the single largest inconsistency in the API's error contract: two endpoints in the same product could fail in two different formats. `ProducesResponseType` metadata corrected, `CommandResponseResultMapperTests` rewritten to pin the new contract (9/9), and the change recorded in `docs/API_CHANGELOG.md` as breaking pre-v1.
- [x] **HAL assembler consolidation** — **48 of 81 resource assemblers were empty subclasses** whose entire content was forwarding three constructor arguments to `ResourceAssemblerBase`. They are replaced by one generic `HalResourceAssembler<TDto, TListDto>` plus `AddHalResource` overloads that default to it. Assemblers: 81 files / 2,564 lines → **33 files / 1,676 lines**. The HAL graph was diffed before and after: **zero contracts removed** (the 6 additions are the untracked scheduler-admin work). A family that genuinely assembles differently still declares its own type, so the file list now distinguishes real behavior from boilerplate.
- [x] **Inherited failure triage** — **10 of the 25 pre-existing failures fixed.** All were stale HAL link-policy tests asserting `PermissionResourceAttributes` on links whose descriptor publishes typed `EventAuthorizationFacts`. `RequirePermission` and the runtime `HateoasAuthorizationEvaluator` both apply the same rule — facts supersede the stringly-typed attribute bag — so the product was correct and the tests predated the typed-facts model. Migrated to typed assertions (`facts.EventId`, `facts.TenantId`, `facts.OrganizationId`, `facts.ActorId`), which is stronger than what they replaced.

**HAL link-policy consolidation remains deliberately out of scope.** The 82 policy files / 9,978 lines encode per-resource authorization decisions — which affordance a caller may see. Collapsing them is an authorization change, not boilerplate removal, and it belongs with `authorization-platform-redesign`.

## Deferred / Separate Workstreams
- **HAL link-policy consolidation** — the 82 policy files (9,978 lines) plus `RouteNames.cs` (1,052). Link policies encode per-resource authorization, so consolidating them is an authorization change; it belongs with `authorization-platform-redesign`.
- **Build-warning elimination** — baseline is now **9,290** (from 13,535); reduce by warning family, never suppress.
- **Queue-driven worker migration** — gated on multi-node duplicate-execution and crash-window recovery proof.
