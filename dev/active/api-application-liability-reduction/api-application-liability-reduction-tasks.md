<!-- ABOUTME: Hot execution ledger for API-wide code-liability reduction. -->
<!-- ABOUTME: Separates behavior characterization, implementation, and phase verification. -->

# API-Wide Code Liability Reduction — Task Checklist

Last Updated: 2026-08-15 Europe/Brussels

## Status Summary
- **Overall status:** 🔴 **Halted at Phase 0.** No project in the repository builds; every phase gate is unrunnable.
- **Completed:** 4/30 implementation tasks (1.3, 1.4, 1.5, 2.1) — **all landed but unverified**, pending Phase 0.
- **Current priority:** Phase 0.1 — repair the toolchain and record a real Release build baseline.
- **Next recommended slice:** Phase 0.1 → 0.2 → 0.3 → 1.1 (install ratchets before any further migration).
- **Do not start:** Phase 2.2 or anything downstream. Phase 2.2 was started prematurely while Phase 1's gates were unchecked; it is reset to unstarted.

## Maintenance Rules
- Read all three artifacts once initially; on resume read context/tasks and only the current plan phase.
- Mark substantial tasks immediately and reconcile small tasks by phase end.
- Characterization and implementation are separate tasks; never consolidate an unpinned security/reliability seam.
- **Install the ratchet before the migration it protects** (plan Design Rule 13). A migration task whose ratchet does not exist is not ready.
- **Re-check the collision matrix in `context.md` before editing any Phase 5/6/7 target file** (plan Design Rule 14).
- Update context for phase/decision/blocker/failure/discovery/handoff; update plan only for strategy changes.
- Run verification once at phase end using the single risk-owning test project from plan §7. Never start browser, Aspire, Docker, or live services for this workstream.
- **A blocked gate blocks the phase.** Record it as a blocker; do not proceed to the next phase.

## Phase 0: Executable verification baseline 🔴 BLOCKER
- [ ] **0.1 Repair the toolchain and record a reproducible baseline** — resolve `MSB4242` / missing workload manifests for set `10.0.301.1`; reconcile `global.json` (`10.0.301`) with the installed SDK (`10.0.302`); record real per-project Release error/warning counts, SDK version, and date in the evidence register; delete whichever of the contradictory `758 warnings` / `0 warnings` records is wrong; **Effort M**.
- [ ] **0.2 Close or hand off the four unrelated architecture failures** — registration-form input naming, registration-form tenant-filter bypass, Blazor-owned registration-answer analytics DTOs, two missing privacy inventory properties; produce an owner-attributed known-failure list; **Effort S**; depends 0.1.
- [ ] **0.3 Record the concurrent-workstream collision matrix** — confirm the `context.md` matrix against current `dev/active/` and `dev/pause/`; mark each contended file idle or active; **Effort S**.

### Phase 0 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 1: Ratchets, contract pins, dead paths ⏳
- [ ] **1.1 Install shrinking-baseline ratchets for every liability class** — six frozen baselines: (a) controller `HttpContext.RequestServices` = 1, (b) controller `FindFirst`/`User.Find*` = 42/44, (c) private controller failure-mapping members = 12, (d) `Task.Delay` `BackgroundService` files = 17, (e) the five named oversized controllers, (f) `HateoasAssemblerRegistration.cs` `AddScoped` = 293; each allowlist file-scoped and comment-justified; no LOC-percentage or syntax assertions; **Effort L**; depends Phase 0.
- [ ] **1.2 Pin externally observable API invariants** — hotspot route/auth/cache/status/ProblemDetails/HAL authorities recorded without duplicate tests; contract debt register created; **Effort L**; depends Phase 0.
- [x] **1.3 Delete confirmed compatibility/dead presentation paths** — removed zero-caller API tenant services and the permission enum bridge with reference evidence; ⚠️ **re-verify under a working build in Phase 0**; **Effort M**.
- [x] **1.4 Normalize truly mechanical controller adapters** — 28 controllers, net 174 lines removed, independent review approved; ⚠️ **re-verify under a working build in Phase 0**; **Effort M**.
- [x] **1.5 Converge API contract and controller documentation** — generated OpenAPI path, tenant ownership, and authorization test guidance now match code; **Effort M**; depends 1.3–1.4.

### Phase 1 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2: Identity authority ⏳
- [x] **2.1 Characterize internal, provider-bootstrap, API-key, and diagnostic identity paths** — seven-row call-site matrix recorded in the evidence register; **Effort L**.
- [ ] **2.2 Remove controller service location and manual claim parsing** — inject `IUserContext`; move the five `ExploreControllerBase` provider-identity members behind one trusted service/query; eliminate the `IMediator`-as-parameter shape on `ResolveCurrentUserIdAsync`; delete `FooterController.TryGetCurrentUserId` after all six callers migrate; keep purpose-bound API-key/setup-secret/managed-control-plane/ATProto/receipt schemes separate; ratchets (a) and (b) reach 0; **Effort XL**; depends 1.1, 2.1. *(Previous partial work on `InstanceOnboardingController`/`InstanceSettingsController` configuration services is retained but does not close this task.)*
- [ ] **2.3 Make the identity authority unambiguous in canonical documentation** — delete claim-parsing/service-location guidance; name the sole fallback order; **Effort M**; depends 2.2.

### Phase 2 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

## Phase 2b: Identity behavior at the HTTP boundary ⏳
- [ ] **2b.1 Verify unchanged HTTP identity behavior across the migrated cohort** — first-claim UUIDv7 allocation, unauthenticated 401 shape, provider-bootstrap 401 vs 403; authorization matrix unchanged; **Effort M**; depends 2.2.

### Phase 2b Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 3: Command result and ProblemDetails authority ⏳
- [ ] **3.1 Inventory and pin failure taxonomies and public mappings** — status/detail/extensions/retry parity matrix across the 12 controllers with private switches; improvised status codes recorded to the contract debt register; **Effort L**; depends 1.1.
- [ ] **3.2 Generalize the existing mapper with the smallest typed policy** — API-only; mapper public surface smaller than the removed private mappings; `CommandResponseResultMapper.cs` shrinks from 643 lines; **Effort L**; depends 3.1.
- [ ] **3.3 Migrate proven controller cohorts and delete private switches** — `EventTicketingController`, `EventParticipationController`, `WebhookBulkReplaysController`, `WebhookProviderPublicationsController`, then other proven matches; **`WebhooksController`, `RegistrationOrderController`, `ControlPlaneController` deferred to their Phase 7 family slice** (active owners); ratchet (c) decreases; **Effort L**; depends 3.2.
- [ ] **3.4 Converge error-contract documentation and examples** — one mapping authority and taxonomy; regenerate `API_CONTRACT_INVENTORY.md`, never hand-edit; **Effort M**; depends 3.3.

### Phase 3 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 4: HAL registration ⏳
- [ ] **4.1 Characterize and service-resolve the complete HAL registration graph** — 293 registrations classified into triples/detail-only/collection-only/shared/exceptional with expected lifetimes; **Effort M**; depends 1.1.
- [ ] **4.2 Replace repeated triples with compile-time generic helpers** — no scanning/reflection; explicit type arguments at each call site; resolved graph provably identical; ratchet (f) decreases; **Effort L**; depends 4.1.
- [ ] **4.3 Update HAL authoring and registration guidance** — one current helper example plus explicit exceptions; **record that link-policy consolidation (14,360-line subsystem, `RouteNames.cs` 1,052, `EventLinkPolicy.cs` 762) is deferred and why**; **Effort S**; depends 4.2.

### Phase 4 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 5: Periodic scheduling authority ⏳
- [ ] **5.1 Decide and record the scheduling authority** — evaluate TickerQ-as-authority (A) vs. scoped TickerQ plus one in-process lifecycle (B) vs. defer (C) against operator visibility, restart durability, OTel coverage, multi-instance safety, provider neutrality, and migration cost across 17 files; coordinate with `email-responsibility-architecture`; record the rationale and operator-impact statement in `context.md`; **blocks 5.2**; **Effort M**; depends 0.3.
- [ ] **5.2 Pin enablement, delays, intervals, scopes, cancellation, errors, health — and the operator-visible log/health/metric names** — intentional differences recorded; excluded set honored (email, privacy-erasure, 4 webhook workers, `OutboxProcessor`, queue/drain/gate services); **Effort L**; depends 5.1.
- [ ] **5.3 Consolidate qualifying timer loops behind the chosen authority** — at least three loops replaced; ratchet (d) decreases; no outbox/retry/fencing weakening; cancellation never logs an error; **every pinned log/health/metric name unchanged or listed in `docs/OPERATIONS.md` as a breaking operational change**; **Effort XL**; depends 5.2.
- [ ] **5.4 Converge worker lifecycle and operations documentation** — implementation and runbooks agree; self-hoster upgrade note complete; `docs/CONFIGURATION.md` updated if any setting key changed; **Effort M**; depends 5.3.

### Phase 5 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 6: MCP decomposition ⏳
- [ ] **6.1 Pin every tool's authorization, HAL gate, bounds, truncation, disclosure, descriptor, and serialization contract** — complete matrix; location ceilings confirmed with `event-location-privacy`, not re-derived; **Effort L**; depends 1.1, 0.3.
- [ ] **6.2 Partition event MCP capabilities and consolidate only pure identical helpers** — tool names/descriptions/schema/output byte-identical; no gate or truncation indicator lost; **Effort XL**; depends 6.1.
- [ ] **6.3 Update MCP capability, security, and debugging documentation** — no stale monolith or bypass guidance; **Effort M**; depends 6.2.

### Phase 6 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Phase 7: Hotspot controller families ⏳ (collision-gated, one family at a time)
Family order is decided by owner idleness, not by a fixed list. Re-check the collision matrix before starting each family. **No two families run concurrently.**

- [ ] **7.1 Move non-HTTP orchestration into CQRS handlers** (per family) — HAL/headers/ProblemDetails stay API-owned; **every extracted request declares `IAuthorizedRequest`, `[AuthorizeResource]`, `ISecureRequest`, or a comment-justified endpoint-authorized-only classification**; tenant context resolved from the same trusted source as the endpoint replaced; **Effort XL/family**; depends Phases 2, 2b, 3.
- [ ] **7.2 Partition by stable capability** (per family) — exact route templates and `Name = RouteNames.*` preserved on every action; `OpenApiParityTests` and `ContractInvariantsTests` unchanged; `schemas/openapi_islamu-event.json` diff empty; **Effort XL/family**; depends 7.1 for that family.
- [ ] **7.3 Update capability ownership and endpoint maps** (per family) — no stale monolith paths; regenerate the contract inventory; **Effort M/family**; depends 7.2 for that family.

Family tracker:
- [ ] Event — owner idle? ☐
- [ ] Instance Settings — owner idle? ☐ (`secrets-refactor-control-plane`)
- [ ] Control Plane — owner idle? ☐ (`secrets-refactor-control-plane`)
- [ ] Registration Order — owner idle? ☐ (`registration-data-collection`)
- [ ] Webhooks — owner idle? ☐ (`webhook-delivery-redesign`) — last

### Phase 7 Verification — once per family
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

## Phase 8: Composition, ratchet tightening, docs convergence ⏳
- [ ] **8.1 Extract feature-cohesive host registration methods with visible concrete topology** — no module framework; TickerQ enablement conditions stay visible; gated by `AppHostTopologyArchitectureTests`; **Effort L**; depends Phases 4–7.
- [ ] **8.2 Drive the Phase 1.1 ratchets to their final values and add residual gates** — every allowlist entry removed or carrying a named, dated, owner-attributed reason; no LOC/style tests; **Effort M**; depends 8.1 and all completed consolidations.
- [ ] **8.3 Canonical documentation convergence and stale-guidance audit** — one authority per rule, zero retired patterns in current docs; hand the contract debt register to a successor `openapi-contract-change` workstream; **Effort L**; depends 8.2.

### Phase 8 Verification
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## Deferred / Separate Workstreams
- **HAL link-policy consolidation** — 170 files / 14,360 lines, of which Phase 4 touches 456. `RouteNames.cs` (1,052) and `EventLinkPolicy.cs` (762) are untouched. Deferred because link policies encode per-resource authorization and collide with `authorization-platform-redesign`.
- **Contract debt register → `openapi-contract-change` workstream** — duplicated routes, misnamed operations, inconsistent DTOs, improvised status codes found during Phases 1–7. Breaking changes are acceptable; they are simply not verifiable inside a behavior-preserving refactor.
- **Build-warning elimination** — by warning family and owning project; never suppress. Baseline must first be established in Phase 0.1.
- Measured repository-query optimization and EF projection changes under `update-repository-query`.
- UI/BFF simplification, persistence/migrations, and infrastructure-wide refactors outside the API seam.
