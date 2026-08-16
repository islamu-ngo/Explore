<!-- ABOUTME: API-wide plan for reducing accidental code, duplicated policy, and oversized responsibilities. -->
<!-- ABOUTME: Preserves every feature and trust boundary while making the smallest durable design simpler. -->

# API-Wide Code Liability Reduction — Implementation Plan

Last Updated: 2026-08-16 Europe/Brussels

## 0. Execution Status (2026-08-16)

Phases 0–6 and 8.2/8.3 are **delivered and verified**. Phase 7 (hotspot controller partition) and Phase 8.1
(composition-root extraction, which depends on it) remain. See `...-tasks.md` for the task-level ledger and
`...-context.md` for the current verification baseline.

Two planning questions this document left open are now closed:

- **§2.5 scheduling authority — resolved as option A.** TickerQ was removed from the repository upstream and
  Quartz.NET 3.19.1 with an ADO job store is the platform scheduler. Eight periodic sweeps migrated onto it;
  no bespoke lifecycle was introduced. Excluded workers are named with reasons in `tasks.md`.
- **§2.3 concurrent-workstream collision — no longer gating.** Execution is single-agent, so Phase 7 family
  order is chosen by coupling rather than by owner idleness. The matrix is retained as documentation of which
  workstream owns which surface.

The plan's central bet — that enforcement must precede migration — held. Every ratchet installed in Phase 1
caught its own liability class during the phases that followed, and because the baselines are exact sets
rather than ceilings, each successful migration forced its own baseline entry to be deleted.

## 1. Objective

Systematically reduce the lifetime cost of `Explore.API` and its immediate `Explore.Application` seams without weakening features, security, tenant isolation, HAL, OpenAPI, observability, or operational reliability. This is not a formatting campaign. It targets duplicated decisions, multiple ways to perform the same operation, presentation-owned orchestration, service location, repeated scheduling loops, registration boilerplate, monolithic capability surfaces, dead compatibility code, and untested infrastructure seams.

The desired end state has:

- one authoritative path for current-user identity, command-failure mapping, query validation, HAL registration, and periodic worker lifecycle;
- controllers that translate HTTP into CQRS requests and translate results back, without owning identity reconstruction or business workflows;
- explicit public contracts beside endpoints, even when this prevents maximum LOC reduction;
- cohesive feature files/modules instead of 1,000–2,500-line mixed-responsibility classes;
- compile-time-visible registrations and no reflection magic, new framework, source generator, or package;
- direct deletion of obsolete development-era code, with no aliases, adapters, dual paths, or compatibility shims;
- phase-local regression evidence before behavior-bearing consolidation;
- **an executable ratchet for every eliminated liability, installed before the migration that eliminates it.**

Success is measured by concepts and decisions removed, duplicated branches eliminated, dependency counts reduced, and feature contracts preserved. Net LOC should fall in consolidation phases, but no percentage target may justify hiding behavior.

### 1.1 Why the ratchet comes first

This program runs for months alongside 14 other active workstreams. Between the 2026-08-13 audit and the 2026-08-15 re-measurement — two days, with Phase 1 actively removing 174 lines — the API **grew**:

| Metric | 2026-08-13 audit | 2026-08-15 verified | Delta |
|---|---|---|---|
| `Explore.API` C# files / lines | — / 62,201 | 480 / 62,881 | +680 lines |
| Controller files / lines | 119 / 24,882 | 121 / 25,326 | +2 files, +444 lines |
| `HateoasAssemblerRegistration.cs` `AddScoped` calls | 278 | 293 | +15 |
| `RegistrationOrderController.cs` | 1,061 | 1,142 | +81 |
| `Explore.API/BackgroundServices` files / lines | 33 / 2,046 | 34 / 2,110 | +1 file, +64 lines |
| `ApiHostServiceCollectionExtensions.cs` | 509 | 518 | +9 |

A migrate-then-enforce program loses this race. Every liability class is therefore frozen by an architecture test with an explicit shrinking baseline **before** its migration phase runs. The allowlist is the debt ledger; it may only shrink.

## 2. Extensive Current-State Audit

All figures re-verified 2026-08-15 against working-tree HEAD.

### 2.1 Scale and hotspots

| Surface | Evidence (verified 2026-08-15) | Liability signal |
|---|---|---|
| Whole API | 480 files / 62,881 C# lines. | Large enough that one-shot modernization is unsafe. |
| Controllers | 121 files / 25,326 lines. | Mixed conventions plus repeated translation logic. |
| HAL | 170 files / 14,360 lines; `HateoasAssemblerRegistration.cs` is 456 lines with 293 `AddScoped` calls. | Valuable affordance logic; registration is repetitive, **but registration is only 3% of the subsystem** (see 2.6). |
| MCP | `EventManagementMcpTools.cs` is 2,516 lines; `EventManagementMcpDescriptors.cs` is 677. | One class mixes tools, gating, sanitization, mapping, pagination, and formatting. |
| Hosting/extensions | `ApiHostServiceCollectionExtensions.cs` is 518 lines. | Composition root mixes unrelated feature registration and worker topology. |
| Background services | 34 files / 2,110 lines; 17 files use `Task.Delay`. | Repeated enabled/initial-delay/loop/error/delay/cancellation mechanics. **TickerQ 10.4.0 is already a dependency** (see 2.5). |
| Problem mapping | `CommandResponseResultMapper.cs` is 643 lines; generic `MapCommandResponse` has 15 controller call sites (16 API-wide); 12 controllers retain private `Map*Failure`/`To*Problem` members. | A shared authority exists but adoption and taxonomy are incomplete. |
| Query models | `QueryValidationRules.cs` 443 lines; `PaginatedQueryRequests.cs` 401 lines. | Shared validation exists, but unrelated request families accumulate in one file. |
| Oversized controllers | `RegistrationOrderController` 1,142; `EventController` 1,033; `WebhooksController` 1,025; `InstanceSettingsController` 858; `ControlPlaneController` 672. | Multiple resource families and helper policies live in single classes. |
| Identity handling | 42 `FindFirst` and 44 `User.Find*` occurrences in controllers; `ExploreControllerBase` (147 lines) lazily resolves `IUserContext` from `HttpContext.RequestServices` and reconstructs provider identity in five protected members. | Three identity styles coexist: `IUserContext`, principal extensions, and manual claim parsing/service location. |
| Public API surface | 756 operations in `schemas/openapi_islamu-event.json`. | Any contract-affecting refactor has a very wide blast radius. |
| Toolchain baseline | **No project builds.** See 2.4. | The plan's own 16 verification gates cannot execute. |

### 2.2 Existing strengths to retain

- Clean Architecture direction is established: API → Application → Domain, with Persistence/Infrastructure implementing lower-layer contracts.
- Controllers do not use `DbContext`, repositories, or `SaveChanges` directly.
- Chained `IExceptionHandler` implementations already centralize RFC 7807 exception responses.
- MediatR authorization and performance behaviors already cover genuine cross-cutting concerns.
- `ApiProblemFactory`, typed problem descriptors, `CommandResponseResultMapper`, `QueryValidationRules`, `ExploreControllerBase`, and HAL registration are existing attempts at centralization. The plan completes or simplifies them rather than adding parallel systems.
- The architecture suite is substantial (14,879 lines) and already enforces controller shape, named routes, response contracts, tenant-filter boundaries, validator pairing, route uniqueness, endpoint classification, authorization surfaces, and PII inventory.
- **OperationId stability is structurally safe.** Every action declares `Name = RouteNames.*`; `OperationIdInvariantTransformer` rejects placeholder ids; `ContractInvariantsTests.OpenApiDocument_OperationIdsAreUnique` enforces uniqueness; NSwag uses `operationGenerationMode: SingleClientFromOperationId`. Controller class names therefore do **not** appear in generated client method names, and the Phase 7 controller partition cannot rename a single generated client method. This was verified, not assumed.

### 2.3 Concurrent-workstream collision map (previously missing)

`dev/active/` holds 15 workstreams and `dev/pause/` holds 9. The following actively contend for the exact files this plan edits. This workstream **must not** partition or restructure a file another workstream is currently modifying; it lands the shared authority and the ratchet, and the owning workstream adopts them.

| Concurrent workstream | Contended surface | Governing rule for this plan |
|---|---|---|
| `authorization-platform-redesign` | `IUserContext`, authorization providers, HAL link policies, `AuthorizationParityTests` | Phase 3 (identity) coordinates before touching authorization contracts; Phase 5 (HAL) is registration-only. |
| `webhook-delivery-redesign` | `WebhooksController`, `WebhookDeliveryProcessor`, `WebhookBulkReplayProcessor`, `WebhookRetentionCleanupProcessor`, `IncomingWebhookProcessor` | Webhooks family is **last** in Phase 7 ordering; workers excluded from Phase 6 until this workstream is idle. |
| `email-responsibility-architecture` | `EmailDispatchProcessor`, `EmailDispatchRetentionCleanupProcessor`, TickerQ dispatch mode | Phase 6 must not change email dispatch topology; TickerQ decision is coordinated, not unilateral. |
| `registration-data-collection` | `RegistrationOrderController`, `RegistrationFormsController`, registration workers | Registration family deferred in Phase 7 until idle. |
| `secrets-refactor-control-plane` | `ControlPlaneController`, instance settings | Control-plane family deferred in Phase 7 until idle. |
| `optional-retained-erasure-authority` | `PrivacyErasureController`, `PrivacyErasureCredentialCleanupProcessor` | Excluded from Phase 6 worker consolidation. |
| `event-location-privacy` | MCP location disclosure ceilings | Phase 6 (MCP) coordinates disclosure-ceiling changes; no unilateral sanitization edits. |
| `agent-architecture-modernization` | `.agents/`, `docs/`, canonical guidance | All documentation-convergence tasks coordinate to avoid contradictory canonical rewrites. |
| `multi-database-support` | EF provider seams reached by workers/outbox | Phase 6 must not assume a single provider's timing/transaction semantics. |

### 2.4 Verification-baseline reality (Blocker)

The `dotnet build`/`dotnet test` gates named in every phase **cannot currently run for any project**:

```
error MSB4242: SDK Resolver Failure ... "Microsoft.NET.SDK.WorkloadAutoImportPropsLocator"
System.InvalidOperationException: Workload set version 10.0.301.1 has missing manifests
likely removed by package management. Run "dotnet workload repair" to fix this.
```

Verified 2026-08-15: `src/Explore.API/Explore.API.csproj` and `tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj` both fail at SDK resolution in 0.1s, before compilation. `dotnet workload list` throws the same exception. `global.json` pins `sdk.version` `10.0.301`; the installed SDK is `10.0.302`.

Consequences that must be stated plainly:

- This is **not** the `Explore.Blazor.Client` WebAssembly task-host failure previously recorded. That diagnosis was wrong; the breakage is repository-wide and precedes compilation.
- The `0 errors, 758 warnings` baseline is **unverified**. The prior artifacts also contradict themselves: the evidence register recorded the same command as `exited 1 ... during restore, with 0 warnings and 0 errors`.
- Phase 1's two gates were never green. Phase 2 work started anyway. No further code change may land until Phase 0 closes.

### 2.5 TickerQ: the scheduling authority question (must be answered before Phase 6)

`Directory.Packages.props` pins **TickerQ 10.4.0**, plus `TickerQ.Dashboard`, `TickerQ.EntityFrameworkCore`, and `TickerQ.Instrumentation.OpenTelemetry`. All four are referenced by `src/Explore.API/Explore.API.csproj`. `ApiHostServiceCollectionExtensions.cs` registers `AddApiTickerQScheduler(...)`, `ApiHostStartupExtensions.cs` applies its migrations, and `ApiHostApplicationExtensions.cs` mounts it — currently gated to email dispatch via `EmailDispatchProcessorMode.TickerQ`.

The repository therefore already owns a durable, database-backed, operator-inspectable, OpenTelemetry-instrumented scheduler. Introducing a bespoke in-process periodic-worker base class beside it would create a **second permanent scheduling concept**, which is precisely what Design Rule 6 forbids. Phase 6 is blocked on an explicit, recorded decision (see 6.1) with three admissible outcomes:

- **A — TickerQ becomes the periodic scheduling authority.** Qualifying workers become TickerQ jobs; operators gain the dashboard and OTel spans; scheduling state survives restarts. Highest long-term value, largest migration and operational-documentation cost, and it changes the self-hoster's operational surface.
- **B — TickerQ stays scoped to email dispatch; one small in-process lifecycle owns the rest.** Cheapest, but two scheduling concepts persist permanently and must be documented as an intentional boundary with a named reason.
- **C — Defer Phase 6 entirely** and hand periodic-worker unification to a dedicated workstream that owns the operational contract.

No worker code changes before this decision is recorded in `context.md`.

### 2.6 Scope honesty on HAL

HAL is 170 files / 14,360 lines. Phase 5 addresses `HateoasAssemblerRegistration.cs` — 456 lines, 3.2% of the subsystem. The two largest HAL files, `RouteNames.cs` (1,052 lines) and `EventLinkPolicy.cs` (762 lines), plus the ~2,800 lines across the next nine link policies, are **out of scope** and this plan does not claim otherwise. Reason: link policies encode per-resource authorization decisions and are the UI's source of truth; consolidating them is an authorization change, not a boilerplate change, and it collides with `authorization-platform-redesign`. Recorded as deferred work, not as solved.

### 2.7 Contract debt register (new output, not a scope expansion)

Breaking changes are acceptable for this pre-v1 platform, but a behavior-preserving refactor cannot also be a contract change and still be verifiable by parity tests. Rather than silently preserving bad contracts, each phase **records** every duplicated route, misnamed operation, inconsistent DTO, and improvised status code it encounters into a contract debt register in the evidence file. That register is the input to a separate `openapi-contract-change` workstream. Preservation here is risk isolation, not compatibility sentiment.

### 2.8 Gemini report disposition

| Proposal | Decision |
|---|---|
| Primary constructors/expression bodies | Use where they delete assignment-only code; never mandate a mass syntax rewrite. |
| Positional records everywhere | Reject. Equality, construction, binding, and mutability semantics must be chosen per contract. |
| Generic base lookup controllers | Reject. They hide explicit route/OpenAPI/HAL/cache differences. |
| Generic validation behavior | Reject. It violates the manual-validator invariant. |
| Central exception handling | Already present; consolidate callers around it. |
| Blanket EF projection/AutoMapper `ProjectTo` | Reject. Repositories return entities; handlers map DTOs. Optimize only measured queries under repository intent. |
| Minimal APIs | Reject. A second endpoint model adds concepts and conflicts with controller fitness tests. |
| LOC percentage target | Reject. `EventApiClient.g.cs` alone is 152,132 generated lines; repository-wide LOC is not a maintainability signal. Contract metadata is valuable code and behavior must not be hidden to hit a number. |

## 3. Contract Classification

### Applicable intents

- `add-get-endpoint` and `add-write-endpoint`: controller, authorization, response, and route invariants apply to refactored actions.
- `add-cqrs-handler`: applies when controller-owned workflow moves into Application.
- `add-hal-link`: applies to HAL policy/registration work; no relation or authorization change is intended.
- `openapi-contract-change`: explicitly **not intended**. Contract drift blocks a phase and requires separate approval/reclassification. Findings route to the contract debt register (2.7).
- `ci-cd-change`, `add-ef-migration`, and `update-repository-query`: not intended.
- `ip-clean-room-governance`: applies because the user supplied an external report and requested external research.

### Authoritative files and skills

`AGENTS.md`, `docs/QUICK_REFERENCE.md`, `docs/GOVERNANCE.md`, `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `.agents/rules/api-controllers.md`, `application-layer.md`, `api-hateoas.md`, `tests.md`, `ip-clean-room.md`, and the clean-architecture, CQRS/MediatR, auth-patterns, error-tracking, outbox-pattern, and IP clean-room skills as each phase activates them.

## 4. Non-Negotiable Design Rules

1. Preserve routes, verbs, route names, operation IDs, response bodies/statuses, cache policies, media types, authentication, authorization, idempotency, HAL relations, and generated-client inputs unless a separately approved contract removal says otherwise.
2. Repositories continue returning entities; DTO mapping stays in handlers. No controller gains persistence access.
3. Validators remain manually instantiated. No validator DI or validation pipeline.
4. Current user/tenant/capability values come from trusted server context, never caller-supplied fields.
5. Do not "simplify" transactional outbox, retry, fencing, unknown-outcome handling, tenant filters, or security checks.
6. One abstraction must replace at least three materially identical implementations **and** must not duplicate a capability an existing dependency already provides. Otherwise keep direct code.
7. No reflection assembly scanning, runtime convention registration, new package, source generator, or framework migration.
8. No compatibility shims. Delete an obsolete internal path and update all callers in the same phase.
9. Splitting a file is allowed only when it creates a stable capability boundary; splitting alone is not claimed as LOC reduction.
10. Every phase starts with behavior characterization for its risky seam and ends with one Release build plus one fastest relevant non-browser test project. **That single test project must be the project that owns the phase's risk** (see §7), not a default.
11. Documentation is part of the architecture, not post-work cleanup. Every phase updates its canonical docs in the same slice, removes superseded guidance, and verifies that agents cannot learn the retired pattern from current documentation.
12. Do not create duplicate "refactor notes" as permanent documentation. Update the owning canonical page, keep historical rationale in Git/ADR history, and record only durable non-obvious findings in the journal.
13. **Ratchet before migration.** No liability class is migrated until an architecture test freezes it with an explicit baseline allowlist. The allowlist may only shrink; adding an entry requires a named reason in the test file.
14. **Do not restructure a file owned by an in-flight workstream** (§2.3). Land the shared authority and ratchet; the owning workstream adopts it.
15. **Every extracted CQRS request declares its authorization contract.** Moving orchestration out of a controller creates a new authorizable surface; it inherits nothing implicitly.

## 5. Target Architecture

The public control flow remains:

```text
HTTP/MCP input
  -> explicit presentation validation and trusted-context extraction
  -> MediatR command/query
  -> handler-owned validation, authorization enrichment, mapping, and use-case orchestration
  -> repository/entity or infrastructure contract
  -> typed result
  -> one API-owned result/problem/HAL mapper
```

Presentation primitives are few and explicit:

- `IUserContext`/principal extensions are the sole already-authenticated identity authority; provider account bootstrap is one named service/query, never base-controller claim reconstruction.
- `CommandResponseResultMapper` plus typed descriptors is the sole reusable `BaseCommandResponse` → RFC 7807 mapping authority.
- `QueryValidationRules` stays the shared rule set, while query request types are grouped by capability rather than one grab-bag file.
- HAL registrations remain explicit at compile time but use small generic registration helpers for the repeated detail/collection/assembler triples.
- Periodic scheduling has exactly one authority, chosen in 6.1 and documented as such.

## 6. Implementation Phases

Phase order is chosen so that **compounding, low-collision work lands first** and **high-collision restructuring lands last**, gated on the owning workstreams being idle.

### Phase 0 — Restore an executable verification baseline

**Blocker gate. No other phase may run while this is open.**

#### 0.1 Repair the toolchain and record a reproducible baseline
- **Files:** `global.json`, local SDK/workload state; no source changes.
- **Work:** resolve the workload-manifest failure (`dotnet workload repair`, or align `global.json` with the installed SDK, or install the pinned SDK). Then run the canonical Release build and record the **actual** error and warning counts, per project, in the evidence register. Reconcile against the contradictory `758 warnings` and `0 warnings` records and delete whichever is wrong.
- **Acceptance:** `dotnet build --configuration Release --verbosity quiet` completes with a recorded warning count; `dotnet test --project tests/Event.Architecture.Tests/...` runs to completion with a recorded pass/fail list; both numbers are in `...-evidence.md` with the date and SDK version.
- **Effort:** M — **Dependencies:** none

#### 0.2 Close or hand off the four unrelated architecture failures
- **Files:** none in this workstream; coordination only.
- **Work:** confirm whether the previously recorded failures (registration-form input naming, registration-form tenant-filter bypass, Blazor-owned registration-answer analytics DTOs, two missing privacy inventory properties) still fail once 0.1 lands. Record each as fixed-elsewhere, owned-by-workstream, or genuinely pre-existing.
- **Acceptance:** the architecture suite is either green or has a written, owner-attributed known-failure list that every later phase compares against. "Unrelated failures" is never again accepted as an unqualified reason to skip a gate.
- **Effort:** S — **Dependencies:** 0.1

#### 0.3 Record the concurrent-workstream collision matrix
- **Files:** `...-context.md`, `...-evidence.md`.
- **Work:** confirm §2.3 against the current `dev/active/` and `dev/pause/` contents; record, per contended file, which workstream currently owns it and whether it is idle.
- **Acceptance:** every Phase 5–7 target file has a named owner and an idle/active status; the matrix is re-checked at the start of each of those phases.
- **Effort:** S — **Dependencies:** none

#### Phase 0 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 1 — Ratchets first, contract pins, and dead-path removal

#### 1.1 Install shrinking-baseline ratchets for every liability class *(moved forward from old 8.2)*
- **Files:** `tests/Event.Architecture.Tests/` — one new focused test class per liability, following the existing baseline/exemption pattern already used by `HandlerValidatorPairingTests.cs` and `StorageUploadOpenApiContractTests.cs`.
- **Work:** freeze, with an explicit allowlist seeded from today's counts: (a) controller `HttpContext.RequestServices` use — baseline 1 (`ExploreControllerBase`); (b) controller claim parsing `FindFirst`/`User.Find*` — baseline 42/44 occurrences with a named file list; (c) private controller failure-mapping members — baseline 12; (d) `Task.Delay`-driven `BackgroundService` loops — baseline 17 files; (e) controller line ceiling — baseline the current five hotspots by name, no new controller above the agreed ceiling; (f) direct `AddScoped` in `HateoasAssemblerRegistration.cs` — baseline 293.
- **Acceptance:** each test fails when a **new** occurrence appears and passes when an existing one is removed; allowlists are file-scoped and comment-justified; no LOC-percentage or constructor-syntax assertion exists anywhere.
- **Effort:** L — **Dependencies:** Phase 0

#### 1.2 Pin externally observable API invariants
- **Files:** `tests/Event.Architecture.Tests/ApiConventionTests.cs`, `ApiContractArchitectureTests.cs`, `EndpointClassificationArchitectureTests.cs`, plus the per-family contract tests listed in the evidence register.
- **Work:** inventory all actions in the five hotspot controllers and the consolidation cohorts; record verb/template/name/auth/classification/cache/success/error/HAL semantics. Add only missing assertions required to detect planned regressions. Record every contract smell found into the contract debt register (§2.7).
- **Acceptance:** every later phase has a machine-checkable contract pin or an identified existing test; no LOC/style tests; the contract debt register exists and is non-empty or explicitly empty with a reason.
- **Effort:** L — **Dependencies:** Phase 0

#### 1.3 Delete confirmed compatibility/dead presentation paths — ✅ delivered, pending Phase 0 re-verification
- **Files:** recorded in the evidence register.
- **Work:** already executed — removed the unwired `Explore.API.Services.HeaderTenantResolver` and `Explore.API.Services.TenantContext`, the obsolete `PermissionAction` enum, its two `RequirePermission` overloads, `ResourceDescriptorRegistry.ToActionString`, and the bridge-only architecture test.
- **Acceptance:** unchanged; **re-verify under a working build in Phase 0** before treating as closed.
- **Effort:** M — **Dependencies:** 1.2

#### 1.4 Normalize truly mechanical controller adapters — ✅ delivered, pending Phase 0 re-verification
- **Files:** 28 mediator-only controllers, recorded in the evidence register.
- **Work:** already executed — primary constructors where they removed a field and an assignment-only constructor; net 174 lines removed.
- **Acceptance:** unchanged; **re-verify under a working build in Phase 0**.
- **Effort:** M — **Dependencies:** 1.2

#### 1.5 Converge API contract and controller documentation — ✅ delivered
- **Files:** `docs/API.md`, `docs/API_CONTRACT_INVENTORY.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/QUICK_REFERENCE.md`.
- **Work:** already executed — OpenAPI artifact path, tenant ownership, and authorization test guidance now match code.
- **Acceptance:** unchanged.
- **Effort:** M — **Dependencies:** 1.3, 1.4

#### Phase 1 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

*(Owns the risk: the ratchets and contract pins are architecture tests.)*

### Phase 2 — One identity authority and no controller service location

Highest compounding value, low collision. Coordinates with `authorization-platform-redesign` before touching authorization contracts.

#### 2.1 Characterize identity resolution paths — ✅ delivered
- **Files:** `ExploreControllerBase.cs`, `ApiAuthenticationPrincipalExtensions.cs`, `IUserContext` and implementation, `ResolveCurrentUserIdByIdentityRequest`/handler, all controller `FindFirst`/`ResolveCurrentUserIdAsync` call sites.
- **Work:** complete; the seven-row call-site matrix is in the evidence register and distinguishes authenticated user, provider bootstrap, machine/API-key, purpose-bound protocol, receipt, diagnostic, and service-location paths.
- **Acceptance:** met.
- **Effort:** L — **Dependencies:** Phase 1

#### 2.2 Replace controller service location and duplicate claim parsing
- **Files:** `ExploreControllerBase.cs` (147 lines; five protected identity members plus the `IUserContext` locator on line 17), `FooterController.TryGetCurrentUserId`, the 11 controller families calling `ResolveCurrentUserIdAsync`, the 17 using `CurrentUserId`/`RequiredUserId`/`UserContext`, Application identity contracts/handler, API authentication tests.
- **Work:** inject `IUserContext` explicitly; move `ResolveProviderSubject`/`ResolveAuthProvider`/`ResolveProviderId`/`ResolveEmailVerified`/`ResolveCurrentUserIdAsync` behind one named trusted provider-identity service or Application query — note that `ResolveCurrentUserIdAsync` currently takes `IMediator` as a **method parameter**, which is service location wearing a signature; that shape does not survive. Delete the superseded base helpers only after every caller migrates. Do not merge the purpose-bound API-key, setup-secret, managed-control-plane, ATProto, or privacy-receipt schemes into ordinary user context.
- **Acceptance:** zero `HttpContext.RequestServices` in `src/Explore.API/Controllers/`; ordinary controllers parse no identity claims; the `sub → nameidentifier → sid` fallback, `internal_user_id` short-circuit, ATProto DID selection, provider detection, verified-email defaults, and 401-on-no-identity remain tested; ratchet baselines (a) and (b) from 1.1 drop to 0.
- **Effort:** XL — **Dependencies:** 2.1

#### 2.3 Make the identity authority unambiguous in documentation
- **Files:** `docs/API.md` authentication sections, `docs/ARCHITECTURE.md` request flow, `docs/AUTHORIZATION.md`, `docs/AUTHORIZATION_PATTERNS.md`, `docs/QUICK_REFERENCE.md`, `docs/CODEBASE_STRUCTURE.md`.
- **Work:** document the exact authenticated-user, provider-bootstrap, machine-principal, and diagnostic boundaries; delete examples that parse claims or use controller service location; name the sole fallback order and trusted abstraction.
- **Acceptance:** an agent following any canonical auth example reaches the same authority; no current doc recommends `HttpContext.RequestServices`, ad hoc `FindFirst`, caller-supplied identity, or superseded base helpers.
- **Effort:** M — **Dependencies:** 2.2

#### Phase 2 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Explore.Infrastructure.Tests/Explore.Infrastructure.Tests.csproj --configuration Release --verbosity quiet`

*(Owns the risk: `Explore.Infrastructure.Tests/Identity/UserContextTests.cs` is the authoritative contract for the fallback order this phase consolidates. The previous plan never ran this project in any phase.)*

### Phase 2b — Identity behavior at the HTTP boundary

Split from Phase 2 so that a second risk-owning project can gate without adding a second command to a single phase.

#### 2b.1 Verify unchanged HTTP identity behavior across the migrated cohort
- **Files:** `tests/Event.API.IntegrationTests/Features/UserControllerTests.cs`, `InstanceOnboardingControllerTests.cs`, `UserExternalLoginIntegrationTests.cs`, `TenantStorageSettingsControllerTests.cs`, `ManagedControlPlaneAuthenticationRoutingTests.cs`, ATProto authentication/session tests, `EndpointAuthorizationMatrixTests.cs`.
- **Work:** extend only where the migration created a gap: first-claim UUIDv7 allocation, unauthenticated 401 shape, and provider-bootstrap 401 vs 403 distinction.
- **Acceptance:** no status, ProblemDetails, or claim-trust change across the cohort; authorization matrix unchanged.
- **Effort:** M — **Dependencies:** 2.2

#### Phase 2b verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

### Phase 3 — One command-result and ProblemDetails mapping authority

#### 3.1 Inventory failure taxonomies and characterize status mapping
- **Files:** `CommandResponseResultMapper.cs` (643 lines), `ApiProblemFactory.cs`, problem descriptors/codes, and the 12 controllers retaining private `Map*Failure`/`To*Problem` members.
- **Work:** group only semantically identical mappings. Pin status, problem type/code/title/detail, extensions, retry headers, resource identifiers, and security-safe detail rules. Record every improvised status code into the contract debt register.
- **Acceptance:** mapping matrix distinguishes reusable common cases from feature-specific cases; no "default BadRequest" collapse of distinct failures.
- **Effort:** L — **Dependencies:** Phase 1

#### 3.2 Generalize the existing mapper by typed policy, not controller inheritance
- **Files:** existing exception-handling mapper/factory/descriptors and focused architecture/API tests.
- **Work:** add the smallest typed mapping input needed for repeated not-found/validation/conflict/forbidden/gone/provider failures. Keep HTTP concerns in API and delete specialized mapper methods made redundant.
- **Acceptance:** the mapper's public surface is smaller than the removed private mappings; response parity tests pass; no Application HTTP dependency; `CommandResponseResultMapper.cs` line count decreases.
- **Effort:** L — **Dependencies:** 3.1

#### 3.3 Migrate high-duplication controller cohorts
- **Files:** `EventTicketingController.cs`, `EventParticipationController.cs`, `WebhookBulkReplaysController.cs`, `WebhookProviderPublicationsController.cs`, then other proven matches. **`WebhooksController.cs`, `RegistrationOrderController.cs`, and `ControlPlaneController.cs` are deferred** to their Phase 7 family slice — their owning workstreams are active (§2.3).
- **Work:** replace private response switches with the shared mapper and delete dead descriptors/helpers.
- **Acceptance:** one mapping decision per failure code; controller action bodies express request construction and success shape only; ratchet baseline (c) decreases and never increases; net LOC decreases without status/detail drift.
- **Effort:** L — **Dependencies:** 3.2

#### 3.4 Converge error-contract documentation and examples
- **Files:** `docs/API.md` response/error sections, `docs/API_COOKBOOK.md`, `docs/QUICK_REFERENCE.md`, `docs/TESTING.md` contract-test guidance. `docs/API_CONTRACT_INVENTORY.md` is generated — regenerate, never hand-edit.
- **Work:** document the single `BaseCommandResponse`/ProblemDetails mapping authority, typed exception boundaries, safe-detail rules, and permitted feature-specific policies. Delete private-switch examples and ambiguous "return BadRequest" guidance.
- **Acceptance:** documented status/problem mappings match tests and implementation; every example uses the current mapper/factory path; no duplicate error taxonomy is introduced.
- **Effort:** M — **Dependencies:** 3.3

#### Phase 3 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

*(Owns the risk: ProblemDetails shape is observable only over HTTP.)*

### Phase 4 — Compile-time HAL registration reduction

Independent of the controller work and low-collision; brought forward.

#### 4.1 Characterize the registration graph
- **Files:** `HateoasAssemblerRegistration.cs` (456 lines, 293 `AddScoped`), `HateoasServiceExtensions.cs`, all `ILinkPolicy`, `ICollectionLinkPolicy`, and `IResourceAssembler` contracts, HAL architecture tests.
- **Work:** classify the 293 registrations into detail+collection+assembler triples, detail-only, collection-only, shared-policy, and exceptional lifetime cases. Add a service-resolution test for every registered closed contract.
- **Acceptance:** every registration has exactly one category and expected lifetime; duplicate/missing registrations fail tests.
- **Effort:** M — **Dependencies:** Phase 1

#### 4.2 Introduce minimal generic registration helpers and migrate triples
- **Files:** registration extension files and tests only.
- **Work:** add compile-time generic helpers for patterns occurring at least three times; retain explicit type arguments at each call site; do not scan assemblies or infer by naming.
- **Acceptance:** the resolved service graph is provably identical; registrations remain grep-searchable by closed type; ratchet baseline (f) decreases materially; exceptional registrations stay direct.
- **Effort:** L — **Dependencies:** 4.1

#### 4.3 Update HAL authoring and registration guidance
- **Files:** `docs/API.md` HAL section, `docs/ARCHITECTURE.md` API representation, `docs/CODEBASE_STRUCTURE.md`, `docs/QUICK_REFERENCE.md`, and `.agents/rules/api-hateoas.md` only if its path-specific delta changes.
- **Work:** document the compile-time helper categories, exceptional direct registrations, lifetimes, service-resolution gate, and prohibition on reflection scanning. Record explicitly that link-policy consolidation (§2.6) is deferred and why.
- **Acceptance:** a new HAL resource can be registered from one canonical example without guessing; explicit link-policy ownership and fail-closed authorization remain prominent; the deferred scope is documented, not implied.
- **Effort:** S — **Dependencies:** 4.2

#### Phase 4 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

### Phase 5 — Periodic scheduling: one authority, decided before any code moves

#### 5.1 Decide and record the scheduling authority *(new — blocks 5.2)*
- **Files:** `...-context.md` decision log; `docs/ARCHITECTURE.md` and `docs/OPERATIONS.md` once decided.
- **Work:** evaluate options A/B/C from §2.5 against operator visibility, restart durability, OpenTelemetry coverage, multi-instance safety, `multi-database-support` provider neutrality, and the migration cost across 17 `Task.Delay` files. Coordinate with `email-responsibility-architecture`, which owns the existing TickerQ dispatch mode.
- **Acceptance:** one option is chosen with a written rationale and a named operator-impact statement; if B, the permanent two-scheduler boundary is documented as intentional with its reason; if C, Phase 5 closes here and the remaining tasks move to Deferred.
- **Effort:** M — **Dependencies:** Phase 0.3

#### 5.2 Pin scheduler, cancellation, logging, and health semantics
- **Files:** the qualifying retention, cleanup, delivery, dispatch, sync, replay, and reconciliation `BackgroundService` implementations plus their tests/health checks. **Excluded:** `EmailDispatchProcessor`, `EmailDispatchRetentionCleanupProcessor`, `PrivacyErasureCredentialCleanupProcessor`, `WebhookDeliveryProcessor`, `WebhookBulkReplayProcessor`, `WebhookRetentionCleanupProcessor`, `IncomingWebhookProcessor`, `IncomingWebhookEffectProcessor` (owned by active workstreams, §2.3); `OutboxProcessor` (243 lines, durable side-effect authority); `AiAssistantRunQueue`, `EmailDispatchHostedDrainRunner`, `IntegrationSyncHostedDrainRunner`, `PrivacyErasureStartupGate` (not periodic).
- **Work:** characterize enabled behavior, initial delay, interval unit, async scope creation, cancellation during delay, exception containment, stop logging, dry-run/batch settings, and health effects. **Additionally pin the operator-visible surface: structured log event names and fields, health-check names, and metric names.**
- **Acceptance:** tests distinguish intentional worker differences from copied loop mechanics; the operator-visible log/health/metric contract is recorded before any consolidation.
- **Effort:** L — **Dependencies:** 5.1

#### 5.3 Consolidate repeated timer-loop mechanics
- **Files:** per the 5.1 decision — TickerQ job registrations, or one API-host scheduling primitive; qualifying workers; worker tests.
- **Work:** one lifecycle owns enablement, initial delay, interval waiting, scope creation, cancellation, and exception containment; each worker supplies only enablement, schedule, and one iteration. Preserve worker-specific work and safe logging. Leave queue/event-driven workers alone.
- **Acceptance:** at least three loops deleted per abstraction; ratchet baseline (d) decreases; cancellation never logs an error; each iteration gets a fresh async scope where required; retry/fencing/outbox semantics remain below the scheduling wrapper; **every pinned log event name, health-check name, and metric name is either unchanged or listed in `docs/OPERATIONS.md` as a breaking operational change.**
- **Effort:** XL — **Dependencies:** 5.2

#### 5.4 Converge worker lifecycle and operations documentation
- **Files:** `docs/ARCHITECTURE.md` background-services section, `docs/OPERATIONS.md` lifecycle/shutdown/runbook sections, `docs/OUTBOX_PATTERN.md`, `docs/TESTING.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/CONFIGURATION.md` if any worker setting key changes.
- **Work:** document lifecycle ownership versus worker-specific processing, cancellation/error/scope rules, health behavior, and which workers intentionally remain event/queue driven. Document the self-hoster impact from 5.3. Delete copied per-worker loop guidance.
- **Acceptance:** operator behavior and implementation guidance agree; a self-hoster upgrading across this change can find every renamed log/health/metric/config key in one place; no doc suggests catching shutdown cancellation as an error, reusing scoped services, or moving outbox work into request paths.
- **Effort:** M — **Dependencies:** 5.3

#### Phase 5 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`

*(Owns the risk: worker iterations drive outbox, retention, and idempotency persistence semantics. The previous plan never ran this project in any phase.)*

### Phase 6 — MCP capability decomposition without protocol drift

#### 6.1 Characterize MCP tools, gates, disclosure ceilings, and descriptors
- **Files:** `EventManagementMcpTools.cs` (2,516 lines), `EventManagementMcpDescriptors.cs` (677 lines), readiness mapper, projected tool factory, MCP tests, `tests/Event.Architecture.Tests/AiContextDisclosureSchemaTests.cs`, AI context security docs.
- **Work:** map every tool to authorization, HAL gate, query, bound, truncation field, sanitization/disclosure ceiling, descriptor, and serialization context. Coordinate with `event-location-privacy`, which owns location disclosure ceilings.
- **Acceptance:** no tool moves until its complete security and truncation contract is pinned; location-disclosure ceilings are confirmed with their owning workstream, not re-derived.
- **Effort:** L — **Dependencies:** Phase 1, 0.3

#### 6.2 Partition the monolith and consolidate proven pure helpers
- **Files:** `EventManagementMcpTools*.cs`, descriptor/readiness files, tests.
- **Work:** partition by public discovery, management readiness, program, custom properties, registration/team, and templates. Centralize only identical bounds/trim/page/truncation helpers; keep location disclosure and AI sanitization explicit and fail-closed.
- **Acceptance:** tool names/descriptions/schema/output remain byte-identical; class responsibilities become navigable; no security gate or truncation indicator is lost; duplicated pure helpers are deleted.
- **Effort:** XL — **Dependencies:** 6.1

#### 6.3 Update MCP capability, security, and debugging documentation
- **Files:** `docs/ARCHITECTURE.md` MCP boundary, `docs/MCP_DEBUGGING.md`, `docs/API.md` where REST/HAL is the MCP authority, `docs/AI_CONTEXT_SECURITY.md`, `docs/CODEBASE_STRUCTURE.md`, and a new ADR only if an architectural decision changes (never rewrite accepted ADR history).
- **Work:** document the capability files, common gating path, bounds/truncation contract, disclosure ceilings, and serialization authority. Remove references to the monolith and duplicated tool-authoring patterns.
- **Acceptance:** MCP contributors can locate each capability and cannot bypass REST HAL authorization or AI disclosure sanitation by following docs; tool names/protocol examples remain current.
- **Effort:** M — **Dependencies:** 6.2

#### Phase 6 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

*(Owns the risk: `AiContextDisclosureSchemaTests` and the MCP/HAL authority guardrails live here.)*

### Phase 7 — Thin the five hotspot controller families

Moved last: these five files are contended by six concurrent workstreams (§2.3). Each family is a separate slice, started only when its owning workstream is idle, in whatever order idleness allows. **No two families run concurrently.**

#### 7.1 Move non-HTTP orchestration out of controllers (per family)
- **Files:** the family's controller plus its existing Application requests/handlers and tests.
- **Work:** identify controller helpers that normalize business inputs, coordinate several queries, compute readiness, or construct use-case results. Move those decisions into one existing/new CQRS request handler per use case. Keep HAL, headers, `ActionResult`, URL generation, and ProblemDetails in API.
- **Acceptance:** no controller-owned business workflow; repositories still return entities; handler tests cover moved logic; **every newly extracted request declares `IAuthorizedRequest`, `[AuthorizeResource]`, `ISecureRequest`, or a comment-justified "endpoint-authorized only" classification** (Design Rule 15); controller dependency count falls where orchestration services become unnecessary.
- **Effort:** XL per family — **Dependencies:** Phases 2, 2b, 3

#### 7.2 Partition by stable route capability without changing routes (per family)
- **Files:** the family's controller, `RouteNames`, HAL policies, architecture tests.
- **Work:** after shared logic is removed, split only along stable capabilities: Event discovery/calendar/management/moderation; registration guest/authenticated/management; webhook consumers/endpoints/messages/incoming operations; instance governance/storage/auth/operations; control-plane plans/settings/tenant lifecycle. Preserve exact class-level and action-level metadata and route templates. `Name = RouteNames.*` on every action is what keeps operationIds — and therefore generated client method names — stable (§2.2); never drop or rename one during a move.
- **Acceptance:** no resulting controller exceeds the agreed review ceiling unless a documented cohesive exception remains; no helper is duplicated between new controllers; `OpenApiParityTests` and `ContractInvariantsTests` pass unchanged; the checked-in `schemas/openapi_islamu-event.json` diff is empty except for intentional, reviewed entries.
- **Effort:** XL per family, one family at a time — **Dependencies:** 7.1 for that family

#### 7.3 Update capability ownership and endpoint maps (per family)
- **Files:** `docs/API.md`, `docs/CODEBASE_STRUCTURE.md`, and feature-specific canonical docs referenced by the moved endpoints. Regenerate `docs/API_CONTRACT_INVENTORY.md`; never hand-edit.
- **Work:** after each family lands, update controller/file ownership, endpoint group descriptions, CQRS responsibility, and navigation links. Remove old monolith ownership statements immediately; do not wait for all five families.
- **Acceptance:** repository paths, controller names, responsibility descriptions, and endpoint inventories match the completed family; no doc sends agents to the retired monolith for moved behavior.
- **Effort:** M per family — **Dependencies:** 7.2 for that family

#### Phase 7 verification (once per controller family, not per task)
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet`

*(Owns the risk: `EndpointAuthorizationMatrixTests`, `ContractInvariantsTests`, `OpenApiParityTests`, and `HateoasContractTests` all live here.)*

### Phase 8 — Composition-root cohesion, ratchet tightening, and final docs convergence

#### 8.1 Extract feature-cohesive registration methods without hiding topology
- **Files:** `ApiHostServiceCollectionExtensions.cs` (518 lines), existing API extension modules, hosting tests.
- **Work:** move coherent registration blocks for OpenAPI, background processing, health, and MCP into named methods/files. Keep concrete registrations and conditional topology visible — including the TickerQ enablement conditions, which are environment- and mode-dependent. No reflection, no module framework.
- **Acceptance:** host composition reads as an ordered list of capabilities; each extension owns one concern; registration order and environment/deployment gates are tested by `AppHostTopologyArchitectureTests`.
- **Effort:** L — **Dependencies:** Phases 4–7

#### 8.2 Tighten the Phase 1 ratchets to zero and add the residual gates
- **Files:** the architecture tests created in 1.1, plus canonical governance only for rules proven by completed phases.
- **Work:** drive every allowlist to its final value; convert each to a hard zero where the migration completed. Add the remaining forward-only gates: no controller service location, no controller identity parsing, no duplicate command-response switches where shared policy applies, HAL registration only through approved helpers, periodic workers only through the chosen scheduling authority. Do not enforce primary-constructor syntax, LOC limits, or file counts.
- **Acceptance:** every allowlist entry is either removed or carries a named, dated, owner-attributed reason; tests prevent reintroduction while allowing comment-justified feature-specific exceptions.
- **Effort:** M — **Dependencies:** 8.1 and all completed consolidations

#### 8.3 Canonical documentation convergence and stale-guidance audit
- **Files:** `docs/ARCHITECTURE.md`, `docs/API.md`, `docs/CODEBASE_STRUCTURE.md`, `docs/GOVERNANCE.md`, `docs/QUICK_REFERENCE.md`, `docs/OPERATIONS.md`, `docs/TESTING.md`, `docs/index.md`, matching `.agents/rules/*.md`, and skill guidance only where the implemented architecture changed its contract.
- **Work:** search the repository for retired class names, helpers, patterns, file paths, code examples, and conflicting definitions. Consolidate duplicated rules into the highest-authority canonical page and replace lower-level duplication with links. State the long-term rule: functionality is the asset; every additional path, abstraction, compatibility layer, and duplicated decision is a liability requiring evidence. Hand the contract debt register (§2.7) to a new `openapi-contract-change` workstream.
- **Acceptance:** zero references to retired production patterns outside historical archives/ADRs; canonical docs have one owner per rule; agent rules link to current authority; docs index is navigable; no aspirational claim contradicts repository reality; the contract debt register has a named successor workstream or is explicitly empty.
- **Effort:** L — **Dependencies:** 8.2

#### Phase 8 verification
`dotnet build --configuration Release --verbosity quiet`

`dotnet test --project tests/Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`

## 7. Testing Strategy

The repository rule is one Release build and at most one project-level `dotnet test` per phase. This plan satisfies that rule **and** closes the coverage hole by choosing the risk-owning project per phase and by splitting a phase when two projects genuinely own two risks — never by adding a second command to one phase.

| Phase | Risk being consolidated | Risk-owning test project | Why |
|---|---|---|---|
| 0 | Toolchain, baseline honesty | `Event.Architecture.Tests` | Fastest full-suite signal; establishes the known-failure list. |
| 1 | Ratchets and contract pins | `Event.Architecture.Tests` | The deliverable *is* architecture tests. |
| 2 | Identity fallback order and claim trust | `Explore.Infrastructure.Tests` | Owns `Identity/UserContextTests.cs`, the fallback-order authority. |
| 2b | Identity behavior over HTTP | `Event.API.IntegrationTests` | 401/403 shape and authorization matrix are HTTP-observable only. |
| 3 | ProblemDetails status/detail parity | `Event.API.IntegrationTests` | Problem shape is observable only over HTTP. |
| 4 | HAL service-graph resolution | `Event.Architecture.Tests` | Registration/lifetime correctness is static and reflective. |
| 5 | Worker iteration side effects | `Event.Persistence.IntegrationTests` | Outbox, retention, and idempotency semantics are persistence-level. |
| 6 | MCP disclosure and gating | `Event.Architecture.Tests` | Owns `AiContextDisclosureSchemaTests` and MCP/HAL authority guardrails. |
| 7 | Endpoint authorization and OpenAPI parity | `Event.API.IntegrationTests` | Owns `EndpointAuthorizationMatrixTests`, `ContractInvariantsTests`, `OpenApiParityTests`, `HateoasContractTests`. |
| 8 | Host topology and residual ratchets | `Event.Architecture.Tests` | Owns `AppHostTopologyArchitectureTests`. |

Characterization tests are written into the project that will gate the phase, never into a project no phase runs. Obsolete compatibility tests are deleted when their behavior is intentionally removed; no test is skipped or commented out.

Forbidden in this workstream: Playwright, `Explore.Blazor.Client.E2ETests`, Chrome DevTools MCP, app startup, Aspire/Docker startup, live-service smoke, manual runtime walkthroughs, and `Event.Benchmarks` (no measured performance question exists).

## 8. Cross-Cutting Quality Classification

| Concern | Plan treatment |
|---|---|
| Security/auth | Phase 2 centralizes trusted identity; Phase 7 requires an explicit authorization contract on every extracted request (Design Rule 15); all fail-closed behavior pinned. |
| Authorization/HAL | Server policies remain authoritative; Phase 4 changes registration only; link-policy consolidation is explicitly deferred (§2.6). |
| Multi-tenancy | No query-filter bypass or caller-supplied tenant authority. Phase 7 moves orchestration into handlers, which relocates where tenant context is resolved — every extracted request must resolve tenant from the same trusted source as the endpoint it replaces, and the existing tenant-filter guardrails gate the family. |
| Privacy/AI | MCP disclosure ceilings, sanitization, and safe diagnostics are immutable contracts; location ceilings coordinate with `event-location-privacy`. |
| Reliability | Phase 5 preserves retry/outbox/fencing and improves cancellation consistency; `OutboxProcessor` is explicitly excluded from consolidation. |
| Observability | Problem types/codes and structured logs are pinned. Phase 5 additionally pins worker log event names, health-check names, and metric names, because self-hosters alert on them. |
| Self-hosting/operations | Phases 0–4 and 6–8 have zero operator-visible impact by construction. Phase 5 is the only phase that can change operator surface; it carries a mandatory `docs/OPERATIONS.md` upgrade note and, under option A, a documented change to the deployment's scheduling model. |
| OpenAPI/BFF | No operation or schema change. `Name = RouteNames.*` plus `SingleClientFromOperationId` keeps generated client method names stable across controller splits (verified, §2.2). Contract smells are recorded, not fixed here (§2.7). |
| Persistence | No migration/model work and no repository DTO leakage. Phase 5 gates on `Event.Persistence.IntegrationTests`. |
| Dependencies/IP | No dependency change. TickerQ is already present; Phase 5.1 decides its scope rather than adding anything. External report supplied hypotheses only. |
| Compatibility | No shims. Intentional internal removals update all callers atomically; public contract change requires separate approval and a successor workstream. |
| Concurrency with other work | §2.3 collision map is binding; Design Rule 14 forbids restructuring files owned by in-flight workstreams. |

## 9. Risk Register

| # | Risk | Severity | Mitigation |
|---|---|---|---|
| R1 | No project builds; all 20 phase gates unrunnable. | Blocker | Phase 0.1 before any further code change. |
| R2 | Liabilities regrow faster than they are removed across a months-long program. | Blocker | Ratchets moved to Phase 1 with shrinking baselines (Design Rule 13). |
| R3 | Six concurrent workstreams own the five hotspot controllers and eight background workers. | Critical | §2.3 collision map; Design Rule 14; Phase 7 moved last and gated on idleness. |
| R4 | A bespoke worker lifecycle becomes a second permanent scheduling concept beside TickerQ. | Critical | Phase 5.1 decision gate blocks 5.2. |
| R5 | Worker consolidation silently renames log events/health checks/metrics and breaks operator alerting. | Critical | 5.2 pins the operator-visible surface; 5.3 requires it unchanged or documented. |
| R6 | Extracted CQRS requests create unauthorized surfaces. | Critical | Design Rule 15; Phase 7 gates on `EndpointAuthorizationMatrixTests`. |
| R7 | Identity bootstrap paths differ intentionally from resolved `IUserContext`; a merge weakens trust. | Critical | 2.1 matrix (complete); purpose-bound schemes explicitly excluded; Phase 2 gates on `UserContextTests`. |
| R8 | Failure codes with similar names require different public status/detail safety. | Major | 3.1 mapping matrix; no default-BadRequest collapse. |
| R9 | MCP helper reuse leaks location or hidden event data. | Major | 6.1 pins ceilings before any move; coordinate with `event-location-privacy`. |
| R10 | Controller partition adds attribute repetition without cohesion gain. | Major | 7.1 must complete before 7.2 for each family. |
| R11 | The unverified warning baseline makes "warning-neutral" unprovable. | Major | Phase 0.1 records the real per-project count. |
| R12 | Documentation convergence collides with `agent-architecture-modernization`. | Moderate | Coordinate canonical-page ownership before each docs task. |

## 10. Success Metrics And Definition Of Done

Baselines are the 2026-08-15 verified figures. Targets are ratchet floors, not LOC quotas.

| Metric | Baseline (2026-08-15) | Target | Gate |
|---|---|---|---|
| Controller `HttpContext.RequestServices` occurrences | 1 | 0 | 1.1(a) → 8.2 |
| Controller `FindFirst` / `User.Find*` occurrences | 42 / 44 | 0 in ordinary controllers; diagnostics allowlisted by name | 1.1(b) → 8.2 |
| Controllers with private failure-mapping members | 12 | 0 where shared policy applies; remainder comment-justified | 1.1(c) → 8.2 |
| `CommandResponseResultMapper.cs` lines | 643 | Decreased, with the mapper's public surface smaller than the removed private mappings | 3.2 |
| `Task.Delay`-driven `BackgroundService` files | 17 | Only the excluded/event-driven set remains | 1.1(d) → 5.3 |
| `HateoasAssemblerRegistration.cs` `AddScoped` calls | 293 | Materially reduced; every remaining direct call is an intentional exception | 1.1(f) → 4.2 |
| Controllers over the review ceiling | 5 | 0, or a documented cohesive exception each | 1.1(e) → 7.2 |
| `EventManagementMcpTools.cs` lines | 2,516 | Partitioned into navigable capability files with identical protocol output | 6.2 |
| `ApiHostServiceCollectionExtensions.cs` lines | 518 | Ordered capability list with visible topology | 8.1 |
| Public API operations | 756 | 756 (unchanged) | `OpenApiParityTests` every phase |
| Release build warnings | **unknown — Phase 0.1 must establish** | Neutral vs. the Phase 0.1 baseline | 0.1 |

**Definition of done for the program:** every ratchet allowlist is at its final value; every metric above meets its target or carries a dated, owner-attributed exception; the contract debt register has a named successor workstream; and no canonical doc describes a retired pattern.

## 11. Explicitly Rejected "Optimizations"

- Generic controller inheritance, CRUD controller generators, repository-returned DTOs, injected validators, service locator helpers, reflection registration, convention-only routes, blanket records, Minimal APIs, blanket AutoMapper projection, catch-all failure mapping, removal of HAL metadata, removing cancellation tokens, consolidating distinct security paths, and replacing durable workers/outboxes with in-request work.
- Splitting every large file without first removing cross-capability logic.
- Warning suppression, generated-code exclusion, or nullable weakening to claim a clean build.
- Benchmarks/LOC dashboards that become permanent maintenance surfaces without a measured performance question.
- **A bespoke scheduling abstraction adopted without first deciding whether TickerQ is the scheduling authority.**
- **Deferring enforcement to the end of the program.**

## 12. Implementation-Agent Contract

1. Read context/tasks first, then only the current phase and referenced rules/skills.
2. Complete one risk-coherent phase/family at a time; do not run a repository-wide mechanical rewrite.
3. **Do not edit any file listed in §2.3 as owned by an active workstream without first re-checking its idle status.**
4. Write characterization evidence before consolidating identity, failures, workers, HAL registrations, or MCP security logic.
5. **Install the ratchet before the migration it protects.** A migration task whose ratchet does not exist yet is not ready.
6. Mark substantial tasks immediately in `tasks.md`; reconcile small tasks by phase end.
7. Update context for a phase, decision, blocker, failure, material discovery, or handoff; update this plan only for strategy/scope/acceptance changes.
8. Run one Release build and the single risk-owning test project named in §7 at phase end. Do not start Aspire, Docker, browsers, or live services under this planning workflow.
9. **A phase is not complete until its gate is green.** A blocked gate blocks the phase; it does not license starting the next phase. Record blocked gates as blockers, never as completions.
10. Any contract drift, weakened error detail safety, tenant ambiguity, missing HAL link, unauthorized extracted request, or worker semantic mismatch blocks the phase.
11. Report `Implemented`, `Verified`, `Remaining`, `Next`, and `Docs updated` after each slice.
12. A phase is not complete until its documentation task is complete. "Code works but docs later" is prohibited technical debt.
13. Documentation updates must delete or correct stale guidance, not merely append a new section that leaves contradictory examples searchable.
14. Record contract smells into the contract debt register rather than fixing them here.

## 13. Research And Provenance

- User-supplied Gemini report: hypothesis source only; its examples, organization, percentages, and claims are not implementation authority.
- Tavily was invoked for official/industry research and returned usage-limit status 432 for every request; no Tavily content influenced this plan.
- Context7 was not exposed in the active MCP inventory; no Context7 result is claimed.
- All architecture and implementation choices were independently derived from repository code, tests, rules, and existing native patterns.
- No third-party source, snippet, AST, SQL, migration, test, comment, asset, or copied documentation prose entered the plan.

## 14. Potential Risks And Unknowns

The program's hardest unresolved area is no longer a code seam — it is the absence of an executable verification baseline (R1) combined with the absence of enforcement (R2) while fourteen other workstreams edit the same files (R3). Those three compound: unverifiable changes to contended files with no ratchet is how a liability-reduction program becomes a liability. Phase 0 and Phase 1 exist to remove exactly that compounding before any further consolidation.

Among the code seams, identity centralization, failure mapping, worker lifecycle, and MCP decomposition remain highest-risk because superficially similar code contains security or reliability differences; characterization tasks are mandatory and exclusions are valid outcomes. The scheduling-authority question (§2.5) is the single decision with the longest half-life: choosing a bespoke loop base class over TickerQ, or the reverse, sets this platform's background-processing model for years and changes what self-hosters can observe. It must be decided deliberately, not discovered during a refactor.
