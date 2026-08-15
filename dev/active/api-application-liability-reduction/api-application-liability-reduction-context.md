<!-- ABOUTME: Resume context for the API-wide code-liability reduction program. -->
<!-- ABOUTME: Records audited hotspots, architecture decisions, blockers, and the next executable slice. -->

# API-Wide Code Liability Reduction — Context

Last Updated: 2026-08-15 Europe/Brussels

## SESSION PROGRESS (2026-08-15 Europe/Brussels)

### ✅ COMPLETED
- Audited all major `Explore.API` directories, largest files, controller actions/dependencies/helpers, identity patterns, problem mapping, HAL registration, background loops, MCP tools, hosting composition, architecture tests, and build state.
- Delivered old Phase 1 implementation work: deleted three confirmed compatibility paths (API-local `HeaderTenantResolver`/`TenantContext`, obsolete `PermissionAction` enum bridge), normalized 28 mediator-only controllers for a net 174 lines removed, and converged the affected API/tenant/authorization documentation.
- Completed identity characterization (now task 2.1) across ordinary users, provider bootstrap, machine/API-key, purpose-bound ATProto/setup/control-plane, receipt, and diagnostic principals.
- Constructor-injected auth/authz configuration services into `InstanceOnboardingController` and `InstanceSettingsController`; the base `IUserContext` service locator is the only remaining controller service location.
- **Senior CTO review 2026-08-15 re-baselined the plan.** See "Plan re-baseline" below.

### 🟡 IN PROGRESS
- Nothing. All implementation is stopped behind Phase 0.

### ⏭️ NEXT
1. **Phase 0.1 — repair the toolchain.** `dotnet workload repair` (or align `global.json` with the installed SDK), then record the real per-project Release warning/error counts in the evidence register.
2. **Phase 0.2** — re-run the architecture suite and produce an owner-attributed known-failure list.
3. **Phase 0.3** — confirm the concurrent-workstream collision matrix against current `dev/active/` and `dev/pause/`.
4. **Phase 1.1** — install the shrinking-baseline ratchets before any further migration.

### ⚠️ BLOCKERS
- **B1 (Blocker) — no project in the repository builds.** SDK resolution fails before compilation for every project, including `Explore.API` and `Event.Architecture.Tests`: `MSB4242 ... Workload set version 10.0.301.1 has missing manifests likely removed by package management. Run "dotnet workload repair" to fix this.` `dotnet workload list` throws the same exception. `global.json` pins SDK `10.0.301`; the installed SDK is `10.0.302`. This supersedes the earlier diagnosis of a `Explore.Blazor.Client` WebAssembly task-host failure — that diagnosis was wrong, and the failure is repository-wide and precedes compilation.
- **B2 (Critical) — the validation baseline is unverified and self-contradictory.** The plan and this file previously recorded `0 errors, 758 warnings`; the evidence register recorded the same command as `exited 1 ... during restore, with 0 warnings and 0 errors`. Neither is currently reproducible. "Warning-neutral" is unprovable until Phase 0.1 lands.
- **B3 (Critical) — Phase 1 was never verified.** Both old Phase 1 gates are unchecked, yet Phase 2.2 work started. No further code change lands until Phase 0 closes and the old Phase 1 deliverables are re-verified under a working build.
- **B4 (Critical) — six concurrent workstreams own this plan's Phase 7 targets.** See the collision matrix below.
- **B5 (Critical) — the scheduling-authority question is open.** TickerQ 10.4.0 is already a dependency; Phase 5.1 must decide its scope before any worker code moves.
- Tavily research returned usage-limit status 432; Context7 MCP is unavailable in this session. External-doc retrieval is unavailable but not blocking.

## Plan re-baseline (2026-08-15 Senior CTO review)

Material changes to the workstream:

1. **Phase 0 added** — restore an executable verification baseline. Blocker gate; nothing else runs while open.
2. **Ratchets moved from last to first** (old 8.2 → new 1.1). Verified drift over two days: API +680 lines, controllers +2 files/+444 lines, HAL `AddScoped` 278 → 293, `RegistrationOrderController` 1,061 → 1,142. A migrate-then-enforce program loses that race.
3. **Phase order inverted by collision risk.** Controller-family partitioning moved from Phase 4 to Phase 7 and is gated on the owning workstream being idle; HAL registration moved forward to Phase 4.
4. **Phase 5.1 decision gate added** — TickerQ is already in the composition root; a bespoke worker lifecycle beside it would violate Design Rule 6. No worker code moves before the decision is recorded here.
5. **Verification bar rebound to risk ownership** (plan §7). `Explore.Infrastructure.Tests` and `Event.Persistence.IntegrationTests` now gate the phases whose contracts they own; neither was run by any phase in the previous plan. Phase 2 was split into 2/2b so each phase still runs exactly one test project.
6. **Design Rules 13–15 added** — ratchet before migration; do not restructure files owned by in-flight workstreams; every extracted CQRS request declares its authorization contract.
7. **Success metrics table added** (plan §10) with verified baselines and ratchet floors.
8. **Contract debt register added** (plan §2.7) so preserved-but-bad contracts route to a successor `openapi-contract-change` workstream instead of being silently kept.
9. **Operator-visible worker surface pinned** (plan 5.2) — log event names, health-check names, metric names. Self-hosters alert on these.
10. **HAL scope stated honestly** (plan §2.6) — Phase 4 touches 456 of 14,360 HAL lines; link-policy consolidation is deferred with a reason, not implied as solved.

Confirmed as **not** a risk, verified rather than assumed: controller partitioning cannot rename generated client methods. Every action declares `Name = RouteNames.*`, `OperationIdInvariantTransformer` rejects placeholder ids, `ContractInvariantsTests.OpenApiDocument_OperationIdsAreUnique` enforces uniqueness, and NSwag uses `operationGenerationMode: SingleClientFromOperationId`, so operationIds — and therefore `EventApiClient` method names — are independent of controller class names.

## Concurrent-workstream collision matrix

Binding under Design Rule 14. Re-check idle status at the start of Phases 5, 6, and 7.

| Workstream | Contended surface | Rule |
|---|---|---|
| `authorization-platform-redesign` | `IUserContext`, authorization providers, HAL link policies | Coordinate before Phase 2 touches authorization contracts; Phase 4 is registration-only. |
| `webhook-delivery-redesign` | `WebhooksController`, 4 webhook workers | Webhooks family last in Phase 7; those workers excluded from Phase 5. |
| `email-responsibility-architecture` | Email dispatch processors, TickerQ dispatch mode | Owns the existing TickerQ usage; co-owns the Phase 5.1 decision. |
| `registration-data-collection` | `RegistrationOrderController`, registration workers | Registration family deferred until idle. |
| `secrets-refactor-control-plane` | `ControlPlaneController`, instance settings | Control-plane family deferred until idle. |
| `optional-retained-erasure-authority` | `PrivacyErasureController` + credential cleanup worker | Excluded from Phase 5. |
| `event-location-privacy` | MCP location disclosure ceilings | Co-owns Phase 6.1 ceilings; no unilateral sanitization edits. |
| `agent-architecture-modernization` | `.agents/`, canonical `docs/` | Coordinate every documentation-convergence task. |
| `multi-database-support` | EF provider seams reached by workers/outbox | Phase 5 must stay provider-neutral. |

## Audited Hotspots (verified 2026-08-15)

| Area | Current signal | Planned response |
|---|---|---|
| Controllers | 121 files / 25,326 LOC; five hotspots at 672–1,142 lines | Remove orchestration, then partition by stable capability — Phase 7, collision-gated. |
| Identity | 42 `FindFirst` + 44 `User.Find*` in controllers; `ExploreControllerBase` locates `IUserContext` and reconstructs provider identity in five members | One explicit trusted identity authority — Phase 2. |
| Command failures | 643-line shared mapper; 15 `MapCommandResponse` call sites; 12 controllers with private switches | Characterize, then converge on typed API mapping policy — Phase 3. |
| HAL | 170 files / 14,360 lines; registration file 456 lines / 293 `AddScoped` | Compile-time generic helpers for registration only — Phase 4. Link policies deferred. |
| Background workers | 34 files / 2,110 lines; 17 use `Task.Delay`; TickerQ 10.4.0 already present | Decide the scheduling authority first — Phase 5.1. |
| MCP | 2,516-line event tool class + 677-line descriptors | Pin security/disclosure, partition capabilities — Phase 6. |
| Host composition | 518-line mixed registration method | Named capability registration methods, visible topology — Phase 8. |
| Query models | 844 lines across two mixed files | Group by capability only when touched; retain shared validation rules. |
| Public surface | 756 OpenAPI operations | `OpenApiParityTests` must stay green in every phase. |

## Key Decisions
1. Code liability includes duplicated decisions and mixed responsibilities, not only LOC. `EventApiClient.g.cs` is 152,132 generated lines — repository-wide LOC is not a maintainability signal.
2. Explicit HTTP/HAL/OpenAPI/security metadata is valuable code and remains visible.
3. Consolidate only patterns proven semantically identical by tests.
4. Use existing native abstractions before adding one; any new abstraction must replace at least three implementations **and** must not duplicate a capability an existing dependency already provides.
5. No Minimal APIs, generic controllers, validation pipeline, reflection registration, blanket records/projections, or new packages.
6. No compatibility shims; intentional removals update all internal callers atomically.
7. Public contract drift is not part of this refactor — not for compatibility sentiment, but because a behavior-preserving refactor cannot also be a contract change and stay verifiable by parity tests. Contract smells go to the debt register and a successor workstream.
8. Documentation is executable agent context: stale examples are technical debt and must be removed in the same phase as the code they describe.
9. Update canonical owners instead of appending parallel guidance; preserve historical decisions in ADR/Git history.
10. **Enforcement precedes migration.** Every liability class is frozen by an architecture test with a shrinking allowlist before its migration phase runs.
11. **Do not restructure files owned by in-flight workstreams.** Land the shared authority and the ratchet; the owning workstream adopts them.
12. **A blocked gate blocks the phase.** It does not license starting the next phase.

## Key Files

| Path | Responsibility |
|---|---|
| `global.json` | Pins SDK `10.0.301`; installed SDK is `10.0.302` — implicated in B1. |
| `src/Explore.API/Controllers/ExploreControllerBase.cs` | 147 lines. `IUserContext` service locator (line 17) plus five provider-identity members; `ResolveCurrentUserIdAsync` takes `IMediator` as a method parameter. Phase 2 target. |
| `src/Explore.API/ExceptionHandling/CommandResponseResultMapper.cs` | 643 lines. Existing shared command/problem mapping authority. Phase 3 target. |
| `src/Explore.API/Extensions/HateoasAssemblerRegistration.cs` | 456 lines / 293 `AddScoped`. Phase 4 target. |
| `src/Explore.API/Hosting/ApiHostServiceCollectionExtensions.cs` | 518 lines. API composition root; registers `AddApiTickerQScheduler`. Phase 8 target. |
| `src/Explore.API/Extensions/TickerQSchedulerExtensions.cs` | Existing durable scheduler wiring. Central to the Phase 5.1 decision. |
| `src/Explore.API/BackgroundServices/` | 34 files / 2,110 lines; `OutboxProcessor.cs` (243) is excluded from consolidation. |
| `src/Explore.API/Mcp/EventManagementMcpTools.cs` | 2,516 lines. MCP tools, gates, mapping, bounds, sanitization. Phase 6 target. |
| `src/Explore.API/Hateoas/RouteNames.cs` | 1,052 lines. Source of operationId stability — never rename during a controller move. |
| `src/Explore.Blazor.Client/nswag.json` | `SingleClientFromOperationId` — the reason controller splits are client-safe. |
| `tests/Event.Architecture.Tests/` | 14,879 lines of static/reflection gates; host for the Phase 1.1 ratchets. |
| `tests/Explore.Infrastructure.Tests/Identity/UserContextTests.cs` | Fallback-order authority; gates Phase 2. |
| `tests/Event.Persistence.IntegrationTests/` | Outbox/retention/idempotency semantics; gates Phase 5. |
| `tests/Event.API.IntegrationTests/` | `EndpointAuthorizationMatrixTests`, `ContractInvariantsTests`, `OpenApiParityTests`, `HateoasContractTests`. Gates Phases 2b, 3, 7. |

## Validation Baseline
- **Unestablished.** See B1/B2. `dotnet build --configuration Release --verbosity quiet` fails at SDK resolution in ~0.1s for every project. Phase 0.1 must record the real per-project error/warning counts, with the SDK version and date, in the evidence register.
- Planning check: `git diff --check -- dev/active/api-application-liability-reduction`.
- Each implementation phase: one Release build plus the single risk-owning test project named in plan §7.

## Current Risks / Unknowns
- The scheduling-authority decision (plan §2.5) has the longest half-life of any choice here; it sets the platform's background-processing model and what self-hosters can observe.
- Identity bootstrap paths may intentionally differ from resolved `IUserContext`; purpose-bound API-key, setup-secret, managed-control-plane, ATProto, and receipt schemes stay separate.
- Failure codes with similar names may require different public status/detail safety.
- Periodic workers differ in interval units, scope behavior, retry/fencing, and health semantics — and in the log/health/metric names operators alert on.
- MCP helpers combine disclosure ceilings with mapping; careless reuse could leak location or hidden event data.
- Phase 7 orchestration moves relocate where tenant context and authorization are resolved; every extracted request needs its own authorization contract.
- Large-controller partitioning may add attribute repetition; orchestration deletion must happen first so cohesion improves rather than merely moving lines.

## Handoff — 2026-08-15 Europe/Brussels
- **Current state:** planning artifacts re-baselined by Senior CTO review. All implementation is stopped behind Phase 0. The old Phase 1 code deliverables (three dead-path deletions, 28 controller normalizations, docs convergence) are landed but **unverified** — they must be re-verified under a working build before Phase 1 is closed.
- **Next action:** Phase 0.1 — `dotnet workload repair` or align `global.json` with the installed SDK, then record the real Release build baseline in the evidence register.
- **Blockers:** B1 (repository-wide build failure), B2 (unverified/contradictory warning baseline), B3 (Phase 1 unverified while Phase 2 started), B4 (six-workstream collision on Phase 7 targets), B5 (open scheduling-authority decision).
- **Validation:** none currently reproducible. Prior claims of "API/Application/architecture projects compile serially" cannot be reproduced in this environment and must be re-established, not carried forward.
- **Documentation:** OpenAPI artifact paths, tenant authority paths, and authorization parity guidance were converged with the deleted code and remain valid.
- **Notes:** do not reintroduce the API-local tenant implementations or the enum permission bridge; current authorities are the infrastructure tenant context, API middleware, and `AuthorizationActions` strings. Do not touch any file listed in the collision matrix without re-checking its owner's idle status.
