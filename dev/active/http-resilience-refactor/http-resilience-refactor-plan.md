# HTTP Resilience & Runtime Reliability Refactor — Implementation Plan

> Last Updated: 2026-03-13 Europe/Brussels
> Plan Status: Architecture and execution plan finalized; implementation partially started.
> Implementation Status: Only the setup-secret/BFF hardening slice is complete. All remaining phases are still open unless explicitly marked in `http-resilience-refactor-tasks.md`.

## Executive Summary

During investigation of the setup secret validation bug, systemic runtime reliability issues were discovered across the HTTP service layer, middleware pipeline, API error contracts, and BFF endpoints. This plan addresses **runtime correctness, resilience, and security** — not structural/convention issues (covered by `enterprise-cleanup`).

**Core Problem:** No unified HTTP error handling strategy. Services silently swallow errors, the middleware pipeline has ordering bugs, the API mixes ProblemDetails with anonymous JSON, controllers bypass the global exception handler, cancellation tokens aren't propagated, and there are zero resilience policies on any HttpClient.

**Scope:** Organized into four execution streams:
- **Stream A** — Runtime Stabilization (tests, error contracts, HTTP services, middleware)
- **Stream B** — Server-Side Reliability (handler decomposition, resilience policies, trace propagation)
- **Stream C** — Security Hardening (antiforgery, cookie hardening, rate limiting)
- **Stream D** — BFF Structural (route-group decomposition)

**Out of Scope (moved to `enterprise-cleanup`):**
- `CongfigurePersistenceServices` typo fix
- `S3ConfigResolver` missing `.Trim()`
- Domain collection mutability (`EventSeries`, `Tenant`, `Group`) — separate EF Core regression risk
- Namespace cleanup, file-scoped conversions, CQRS restructuring

## Artifact Responsibilities

This track intentionally uses three separate documents. They serve different audiences and must not be allowed to drift.

- `http-resilience-refactor-plan.md`
  - Architecture diagnosis, target state, execution waves, governance rules, phase gates, and completion criteria.
- `http-resilience-refactor-context.md`
  - Current implementation state, key file anchors, recent decisions, blockers, and restart instructions.
- `http-resilience-refactor-tasks.md`
  - Delivery tracker only. This is the authoritative checklist for what is completed, in progress, and still open.

If these files disagree, `http-resilience-refactor-tasks.md` is the source of truth for implementation status.

## Authoritative Status Story

### Plan Status
- The architecture and delivery model are mature enough to execute.
- The plan has been revised based on external review and now defines the hardening program.

### Implementation Status
- Completed slice:
  - setup-secret persistence/restoration moved behind the BFF trust boundary
- Not started at program level:
  - Phase 0 integration safety net
  - canonical client error contract rollout
  - API/filter ProblemDetails normalization
  - middleware reorder program
  - handler decomposition and resilience profiles
  - antiforgery/rate limiting hardening
  - full BFF route-group split

### Non-Negotiable Standards
- No contradictory status reporting across plan/context/tasks.
- No silent HTTP failure flattening in critical client services.
- No browser ownership of security-significant secrets.
- No broad BFF decomposition or page-level follow-on work before tests and failure contracts stabilize.

---

## Current State Analysis

### HTTP Service Layer (Explore.Blazor.Client/Services/)

**5 different error handling patterns** — no consistency:

| Pattern | Services | Risk |
|---------|----------|------|
| Silent swallow: `catch { return null; }` | EventService (lines 351, 364) | **CRITICAL** |
| Return empty collections on failure | CategoryService, LocationService, AdminService | **HIGH** |
| Return typed error model | TenantNavigationService | OK but unique |
| Throw exceptions | OrganizationService, GroupService | OK but inconsistent |
| No handling at all | InstanceOnboardingService (pre-fix) | **CRITICAL** — was the setup-secret bug |

**Systemic traps:**
- `PostAsJsonAsync` + `ReadFromJsonAsync` does NOT check HTTP status — silently parses error bodies
- `GetFromJsonAsync` DOES throw on non-2xx — behavioral mismatch
- 7+ instances of `HttpResponseMessage` not disposed (missing `using`)
- `CancellationToken` not passed through service methods or to outbound HTTP calls
- No unified exception type for API errors — services can't consistently parse ProblemDetails failures

### Middleware Pipeline (Explore.API/Program.cs & Explore.Blazor/Program.cs)

**Ordering bugs** (API Program.cs lines 618-644):
1. `UseExceptionHandler()` runs BEFORE `UseForwardedHeaders()` — ForwardedHeaders must be first
2. Forwarded-header trust not configured (`KnownProxies`/`KnownNetworks`) — security risk for self-hosted deployments
3. Tenant resolution runs AFTER request logging — logs get null tenant context
4. No cancellation integration with `UseRequestTimeouts()`

**Middleware ordering rules** (not a rigid sequence — order depends on app semantics):
- `UseForwardedHeaders()` MUST be first, with trusted proxies configured
- `UseExceptionHandler()` MUST be early (catches everything downstream)
- `UseRequestTimeouts()` after `UseRouting()` when explicit routing is used
- Tenant resolution BEFORE anything that needs tenant context (logging, auth if tenant-sensitive)
- Whether tenant resolution goes before/after authentication depends on whether auth scheme/policy selection is tenant-sensitive — **must be evaluated for this project**

### API Error Response Contracts

**Three incompatible response formats coexist:**

| Format | Where |
|--------|-------|
| RFC 7807 ProblemDetails | GlobalExceptionHandler, ApiTenantResolutionMiddleware | ✅ |
| `new { error = "..." }` anonymous | Filters (SetupSecretRequired, BlockInSingleTenant), BFF endpoints | ❌ |
| `new { success, message }` anonymous | InstanceOnboardingController (4 instances) | ❌ |

**Controller exception anti-patterns (7 controllers):**
- Generic `catch (Exception ex)` blocks that bypass `GlobalExceptionHandler`
- Return `StatusCode(500, new { error, stackTrace })` — leaks stack traces, wrong format
- Files: EventController (2), CategoryController, LocationController, EventSessionController, EventSessionAgendaItemController, TagController
- **Rule:** No controller-local generic catch blocks for error formatting. Targeted catches with explicit business translation (e.g., concurrency → 409, downstream failure → 502) are acceptable.

**Missing ValidatorBehavior:**
- 47 FluentValidation validators registered via `AddValidatorsFromAssembly`
- No `ValidationBehavior<TRequest, TResponse>` pipeline behavior — validation is manual per-handler
- Potential interaction with 4 validation layers: model binding, DataAnnotations, FluentValidation, MediatR pipeline
- **Boundary model needed:** HTTP edge validates request shape/transport; application layer validates business rules

### BFF Endpoint Handlers (Explore.Blazor/Extensions/BffEndpointExtensions.cs)

- **1000+ line monolith** — all BFF proxy endpoints in one file
- Mix of `IResult` and anonymous JSON error responses
- `async Task<IResult>(HttpContext ctx)` signature trap (caused the setup-secret bug — `Task<IResult>` coerced to `Task`, IResult.ExecuteAsync never runs)
- Missing CSRF/antiforgery on state-changing endpoints
- No rate limiting on sensitive endpoints
- Should be decomposed by **bounded context/route group**, not arbitrary line counts

### DelegatingHandler (CircuitAccessTokenService.cs lines 225-378)

`AccessTokenForwardingHandler` does **5 unrelated things** in 150 lines:
1. Resolves token from HttpContext cookie
2. Resolves token from shared token store
3. Adds Authorization header
4. Forwards tenant slug + X-Forwarded-Host headers
5. Forwards X-Setup-Secret header

**Blazor circuit risk:** Tokens read from `HttpContext` are captured at circuit start and not updated if the user re-authenticates. The decomposition must document the token lifecycle: refresh, circuit reconnection, null HttpContext scenarios, render mode transitions.

### Missing Resilience

- Zero `Microsoft.Extensions.Http.Resilience` policies on any HttpClient
- `AddStandardResilienceHandler()` applies 5 strategies by default: rate limiter, total timeout (30s), retry (3x exponential+jitter), circuit breaker, attempt timeout (10s)
- Default retries **all HTTP methods** unless `DisableForUnsafeHttpMethods()` is called
- Need distinct **resilience profiles** by client type (interactive UI, admin/setup, background)
- Resilience belongs on **server-side** only (Explore.Blazor), not browser-side (.Client)
- `IHttpClientFactory` cookie pitfall: pooled handlers share `CookieContainer` — confirm server-side outbound clients set `UseCookies = false`

### Missing Cancellation Propagation

Request timeout middleware signals `HttpContext.RequestAborted`, but the app continues unless code observes the token:
- Service methods don't accept `CancellationToken`
- BFF endpoints don't pass `HttpContext.RequestAborted`
- Outbound `HttpClient` calls don't use cancellation tokens
- Long-running DB queries don't observe cancellation

---

## Proposed Future State

### Canonical Error Contract (`ApiProblemException`)
A typed exception carrying:
- HTTP status code
- `ProblemDetails` payload (parsed from API response)
- Validation error dictionary (if present)
- Downstream service name
- Trace/correlation context

Services throw `ApiProblemException` on non-2xx responses (after reading the ProblemDetails body first — NOT using `EnsureSuccessStatusCode()` which discards the body). Services remain domain-oriented — no CRUD-shaped base wrappers.

### Middleware Ordering (Rules-Based)
Not a rigid sequence — ordering depends on app semantics:
1. `ForwardedHeaders` at top, with `KnownProxies`/`KnownNetworks` configured
2. `ExceptionHandler` early (catches downstream)
3. `RequestTimeouts` after `UseRouting()` 
4. Tenant resolution before anything that needs tenant context
5. Request logging after tenant resolution (for tenant as logging dimension)
6. Tenant/auth order evaluated per project: does auth scheme depend on tenant?

### Consistent Error Responses
- ALL errors return RFC 7807 ProblemDetails (API, filters, BFF)
- No generic catch blocks in controllers — only targeted business-translation catches
- `ValidatorBehavior` auto-validates commands, then manual validation removed from handlers
- Formal DTOs replace all anonymous types

### Resilient HTTP Communication (Server-Side Only)
- Resilience profiles per client type: interactive (short timeouts), admin (medium), background (long + retry)
- `DisableForUnsafeHttpMethods()` by default; idempotent POSTs opted-in explicitly
- W3C `traceparent` via OpenTelemetry for trace propagation (not custom correlation headers)
- `UseCookies = false` on all server-side outbound clients

### Full Cancellation Chain
Every request path: `HttpContext.RequestAborted` → BFF handler → service method → HttpClient call → (API → MediatR handler → DB query)

### Responsibility Boundaries

#### `.Client` responsibilities
- UI orchestration and presentation-friendly service contracts
- business-meaningful error rendering and retry affordances
- no ownership of security-significant secrets or trust-boundary state
- no raw transport ambiguity leaking directly into pages

#### BFF responsibilities
- trust boundary and cookie/auth mediation
- antiforgery enforcement and sensitive state mediation
- downstream API orchestration and normalization where appropriate
- cancellation, trace propagation, and secure forwarding behavior

#### API responsibilities
- canonical ProblemDetails and typed validation behavior
- domain/application rule enforcement
- tenant isolation and authorization enforcement
- stable downstream contracts for BFF and client consumption

### UI Consumer Contract

The HTTP resilience work is not complete when errors are merely typed. The UI must have a predictable translation model:

- validation failures → inline form errors or property-level summaries
- forbidden/unauthorized → explicit permission/auth messaging
- not found → route-level empty/not-found state
- transient transport failures → retryable banner/snackbar state
- unexpected faults → safe fallback + telemetry

### Lightweight Target Architecture For Dependent Blazor Refactors

This plan does not implement the event-page decomposition itself, but it must define the error/transport contracts those pages will consume:

- page coordinators own route/query binding and top-level orchestration
- workflow components own focused UI slices (registration, detail drawers, media, recurrence, session editing)
- shared workflow services own normalization, helper logic, and upload coordination
- BFF-facing service layer owns transport and typed failure outcomes

This keeps downstream UI refactors from creating smaller files with the same boundary confusion.

---

## Execution Gates

### Gate A — Safety Net Before Broad Service Refactors
Do not start broad client-service refactors until all of the following are true:
- setup-secret integration coverage exists
- ProblemDetails contract tests are green
- middleware regression tests exist for the critical routing/exception paths

### Gate B — Contract Discipline Before Broad Blazor Workflow Decomposition
Do not start major downstream workflow decomposition until all of the following are true:
- high-risk services no longer swallow failures
- API, BFF, and filters are aligned on canonical ProblemDetails behavior for targeted flows
- UI can distinguish validation, forbidden, not-found, transient-failure, and unexpected-fault outcomes

### Gate C — Shell/Platform Work After Dominant Regression Hotspots Are Reduced
Do not move into shell/platform hardening until all of the following are true:
- event and setup flows are no longer the dominant regression hotspot
- layout/navigation behavior has direct tests where touched
- ownership is clear for shell state, theme state, and navigation state

### Gate D — Evidence Before Tuning
Do not tune resilience profiles, middleware details, or adjacent non-functional concerns beyond the defined scope until:
- structural contract cleanup is complete
- telemetry/logging for targeted flows exists
- route-level or flow-level evidence justifies the tuning

---

## Delivery Waves

The plan still executes through streams/phases below, but leadership tracking should group execution into five delivery waves:

1. **Wave 1 — Safety and Truth**
   - integration tests, canonical ProblemDetails path, service failure contracts, setup-secret trust-boundary hardening
2. **Wave 2 — Runtime Contract Cleanup**
   - remove silent swallowing, standardize client services, normalize API/filter error behavior, validation boundary rollout
3. **Wave 3 — Boundary and Pipeline Discipline**
   - middleware ordering, cancellation propagation, delegating handler split, BFF handler conventions
4. **Wave 4 — Security and Server Reliability**
   - resilience profiles, antiforgery, cookie policy evaluation, setup-secret rate limiting, BFF route-group decomposition
5. **Wave 5 — Evidence-Based Follow-Through**
   - targeted observability expansion, final verification, cross-flow validation of the new runtime model

---

## Implementation Phases

### Stream A — Runtime Stabilization

#### Phase 0: Safety Net (Integration Tests)
**Goal:** Tests for the flows we're about to change, so we catch regressions immediately.

##### Task 0.1: Setup-Secret Flow Integration Test
- **File:** `Event.API.IntegrationTests/` (new test class)
- Test: POST validate-secret with correct secret → 200 + `{ valid: true }`
- Test: POST validate-secret with wrong secret → 200 + `{ valid: false }`
- Test: POST validate-secret without tenant header → succeeds (exempt path)
- **Effort:** M

##### Task 0.2: BFF-to-API Error Propagation Tests
- **File:** `Event.API.IntegrationTests/` (new test class)
- Test: API returns ProblemDetails 400 → BFF surfaces structured error
- Test: API returns 404 → BFF surfaces not-found
- Test: API returns 500 → BFF surfaces server error (no stack trace leak)
- **Effort:** M

##### Task 0.3: ProblemDetails Contract Tests
- **File:** `Event.API.IntegrationTests/` (new test class)
- Test: GlobalExceptionHandler returns valid ProblemDetails with `traceId`, `timestamp`
- Test: ValidationExceptionHandler returns validation errors keyed by property
- Test: Content-negotiation — `Accept: application/json` gets ProblemDetails
- **Effort:** M

##### Task 0.4: Middleware-Order Regression Tests
- Test: ForwardedHeaders respected for `X-Forwarded-Proto` → `Request.Scheme` is correct
- Test: Tenant resolution skips exempt paths
- Test: Exception handler catches controller exceptions and returns ProblemDetails
- **Effort:** S

#### Phase 1: Canonical Error Contract + Service Layer
**Goal:** Unified outbound HTTP error handling with `ApiProblemException`.

##### Task 1.1: Create `ApiProblemException`
- **File:** `Explore.Blazor.Client/Exceptions/ApiProblemException.cs` (new)
- Properties: `StatusCode`, `ProblemDetails`, `ValidationErrors` (dictionary), `ServiceName`
- Read ProblemDetails from response body BEFORE throwing (don't use `EnsureSuccessStatusCode()`)
- Static factory: `ApiProblemException.FromResponse(HttpResponseMessage, string serviceName)`
- **Effort:** S

##### Task 1.2: Create HTTP Response Extension Methods
- **File:** `Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs` (new)
- `EnsureSuccessOrThrowProblem(this HttpResponseMessage, string serviceName)` — reads body, parses ProblemDetails, throws `ApiProblemException`
- `ReadProblemDetailsAsync(this HttpResponseMessage)` — parses ProblemDetails from error body
- These replace scattered status-checking logic; services call them, stay domain-oriented
- **Effort:** S

##### Task 1.3: Add CancellationToken to All Service Methods
- **All files** in `Explore.Blazor.Client/Services/`
- Every public method accepts `CancellationToken cancellationToken = default`
- Pass token to all `HttpClient` calls (`GetAsync`, `PostAsJsonAsync`, etc.)
- **Effort:** M

##### Task 1.4: Refactor EventService
- **File:** `Explore.Blazor.Client/Services/EventService.cs`
- Remove silent swallowing (lines 351, 364)
- Use `response.EnsureSuccessOrThrowProblem("EventAPI")` instead of silently deserializing
- Add `using` on all HttpResponseMessage
- Pass `CancellationToken` to all outbound calls
- **Effort:** M

##### Task 1.5: Refactor CategoryService, LocationService, AdminService
- **Files:** `CategoryService.cs`, `LocationService.cs`, `AdminService.cs`
- Remove empty-collection-on-failure pattern
- Use `EnsureSuccessOrThrowProblem` consistently
- Add `using` + `CancellationToken`
- **Effort:** M

##### Task 1.6: Refactor TenantNavigationService, GroupService, OrganizationService
- **Files:** `TenantNavigationService.cs`, `GroupService.cs`, `OrganizationService.cs`
- Standardize to `EnsureSuccessOrThrowProblem` pattern
- Add missing `using` + `CancellationToken`
- **Effort:** M

##### Task 1.7: Audit All HttpResponseMessage Disposal
- **All service files** in `Explore.Blazor.Client/Services/`
- Every `SendAsync`, `PostAsJsonAsync`, `PutAsJsonAsync`, `DeleteAsync` result wrapped in `using`
- **Effort:** S

#### Phase 2: API Error Contract Consistency
**Goal:** Canonical ProblemDetails from every error path.

##### Task 2.1: Replace Anonymous Types with Formal DTOs
- **File:** `Explore.Application/DTOs/InstanceOnboarding/` (new DTOs)
- **File:** `Explore.API/Controllers/InstanceOnboardingController.cs`
- Create `StorageConnectionTestResultDto`, `SecretValidationResultDto`, etc.
- Replace all `Ok(new { ... })` with typed DTOs
- Update `[ProducesResponseType]` to concrete types
- **Effort:** S

##### Task 2.2: Remove Generic Catch Blocks from Controllers
- **Files:** `EventController.cs` (2 blocks), `CategoryController.cs`, `LocationController.cs`, `EventSessionController.cs`, `EventSessionAgendaItemController.cs`, `TagController.cs`
- Delete generic `catch (Exception ex) { return StatusCode(500, new { error, stackTrace }) }` blocks
- Keep any targeted catches that translate domain exceptions to specific HTTP status codes (409/502/422)
- Let `GlobalExceptionHandler` handle everything else
- **Effort:** S

##### Task 2.3: Standardize Filter Error Responses to ProblemDetails
- **Files:** `SetupSecretRequiredAttribute.cs`, `BlockInSingleTenantAttribute.cs`
- Replace `new { error = "..." }` with `TypedResults.Problem(...)` 
- **Effort:** S

##### Task 2.4: Fix POST Create Endpoints to Return 201
- **Files:** `ExternalApiKeyController.cs`, `InstanceOnboardingController.cs`
- `Ok(response)` → `Created(...)` or `CreatedAtRoute(...)` for create operations
- Update `[ProducesResponseType]` from 200 to 201
- **Effort:** S

##### Task 2.5: ValidationBehavior — Define Boundary Model + Implement
- **Phase 1 — Define boundaries:**
  - HTTP edge (model binding/DataAnnotations) → transport/shape validation
  - Application layer (FluentValidation via MediatR pipeline) → business rule validation
  - Document which layer owns what
- **Phase 2 — Create behavior:**
  - **File:** `Explore.Application/Behaviors/ValidationBehavior.cs` (new)
  - Auto-invokes registered FluentValidation validators for all commands
  - Throws `ValidationException` on failure (caught by `ValidationExceptionHandler`)
- **Phase 3 — Remove manual validation:**
- Remove manual `_validator.ValidateAsync()` calls from all command handlers
- Validate no duplicate error messages
- **Effort:** L
- **Related Skills:** `cqrs-mediatr-guidelines`
- **Governance rule:** no partial rollout without explicit scope control. Either migrate a tightly bounded handler set with complete contract verification, or migrate the whole targeted command surface in one disciplined pass.

#### Phase 3: Middleware Pipeline Fixes
**Goal:** Correct middleware ordering per ASP.NET Core guidance.

##### Task 3.1: Fix Middleware Ordering in API Program.cs
- **File:** `Explore.API/Program.cs`
- `UseForwardedHeaders()` at top
- Configure `ForwardedHeadersOptions.KnownProxies` / `KnownNetworks` for self-hosted deployments
- `UseExceptionHandler()` early
- Evaluate tenant/auth ordering: does this project's auth scheme selection depend on tenant?
- Move request logging after tenant resolution for tenant as log dimension
- **Effort:** M

##### Task 3.2: Fix Middleware Ordering in Blazor Program.cs
- **File:** `Explore.Blazor/Program.cs`
- Apply same ordering rules
- **Effort:** S

##### Task 3.3: Make Tenant-Exempt Paths Configurable
- **File:** `Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`
- Move hardcoded exempt paths to a static `TenantExemptPaths` collection or config
- **Effort:** S

##### Task 3.4: BFF Endpoints Pass CancellationToken
- **File:** `Explore.Blazor/Extensions/BffEndpointExtensions.cs`
- All BFF endpoint handlers pass `HttpContext.RequestAborted` to downstream service calls
- **Effort:** S

---

### Stream B — Server-Side Reliability

#### Phase 4: DelegatingHandler Decomposition
**Goal:** Split AccessTokenForwardingHandler into focused, composable handlers.

##### Task 4.1: Extract TenantHeaderForwardingHandler
- **File:** `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs` (new)
- Move tenant slug + X-Forwarded-Host forwarding logic
- **Effort:** M

##### Task 4.2: Extract SetupSecretForwardingHandler
- **File:** `Explore.Blazor/Services/SetupSecretForwardingHandler.cs` (new)
- Move X-Setup-Secret header forwarding
- **Effort:** S

##### Task 4.3: Simplify AccessTokenForwardingHandler
- **File:** `Explore.Blazor/Services/CircuitAccessTokenService.cs`
- After extraction: only resolves access token + adds Authorization header
- **Document token lifecycle:** refresh, circuit reconnection, null HttpContext, render mode transitions
- **Effort:** M

#### Phase 5: HTTP Resilience Policies (Server-Side Only)
**Goal:** Resilience profiles on server-side `Explore.Blazor` HttpClient registrations.

##### Task 5.1: Add Microsoft.Extensions.Http.Resilience Package
- **File:** `Explore.Blazor/Explore.Blazor.csproj` (server project ONLY, not .Client)
- **Effort:** S

##### Task 5.2: Define Client Resilience Profiles
- **Interactive UI calls:** Short total timeout (15s), attempt timeout (5s), retry only safe methods, circuit breaker
- **Admin/setup calls:** Medium timeout (30s), attempt timeout (10s), retry safe methods
- **Background/integration calls:** Long timeout (60s), retry safe + idempotent, circuit breaker
- Use `AddStandardResilienceHandler()` with `DisableForUnsafeHttpMethods()` as baseline
- Idempotent POSTs opted-in explicitly per profile, not by verb
- **Effort:** M

##### Task 5.3: Verify HttpClient Cookie Behavior
- Confirm all server-side outbound clients set `UseCookies = false`
- `IHttpClientFactory` pooled handlers share `CookieContainer` — unintended cookie leakage risk
- **Effort:** S

##### Task 5.4: Trace Propagation via OpenTelemetry
- Use W3C `traceparent` header via OpenTelemetry (not custom correlation headers)
- Verify .NET's built-in `Activity`/`DiagnosticSource` propagates trace context to outbound HTTP calls
- If additional business-level correlation needed, add ONE configurable header
- **Effort:** S

---

### Stream C — Security Hardening

#### Phase 6: Antiforgery, Cookies, Rate Limiting
**Goal:** Close security gaps on BFF state-changing endpoints.

##### Task 6.1: Antiforgery on BFF State-Changing Endpoints
- Verify Data Protection is configured (key-ring persistence — even in dev, ensure it works across restarts)
- Enable antiforgery on POST/PUT/DELETE BFF endpoints
- Test matrix: SSR, InteractiveServer, InteractiveAuto, WASM render modes
- Antiforgery middleware must run after authentication/authorization
- **Effort:** L
- **Related Skills:** `blazor-bff-patterns`, `auth-patterns`

##### Task 6.2: Setup Secret Cookie — Evaluate SameSite
- Evaluate whether setup-secret cookie participates in any cross-site flow (OIDC redirects, email links into onboarding)
- If no cross-site flows: use `SameSite=Strict`
- If OIDC redirect flows exist: use `SameSite=Lax` and document why
- **Effort:** S

##### Task 6.3: Rate Limiting on Setup Secret Endpoint
- Named rate-limit policy for setup-secret validation
- Key by trusted client identity or session, not raw IP (raw IP unreliable behind proxies — depends on ForwardedHeaders trust being correct from Phase 3)
- Make limits configurable
- Log throttle decisions with setup context
- **Effort:** M

---

### Stream D — BFF Structural

#### Phase 7: BFF Route-Group Decomposition
**Goal:** Split BffEndpointExtensions monolith by bounded context.

##### Task 7.1: Decompose BFF Endpoints by Route Group
- Split by bounded context / security policy / dependency profile:
  - `BffEventEndpoints.cs` — event CRUD proxy
  - `BffOrganizationEndpoints.cs` — org management proxy
  - `BffAdminEndpoints.cs` — admin operations proxy
  - `BffSetupEndpoints.cs` — onboarding/setup proxy
  - Additional groups as the domain warrants
- Extract shared endpoint conventions and ProblemDetails helpers into a `BffEndpointConventions.cs`
- Apply consistent error handling, auth metadata, and antiforgery per route group
- **WARNING:** Ensure all new endpoint handlers use `async Task(HttpContext ctx)` with direct `ctx.Response.WriteAsJsonAsync()` — NOT `async Task<IResult>` which silently loses response body
- **Effort:** L

##### Task 7.2: Standardize BFF Error Response Format
- All BFF endpoints: replace `new { error = "..." }` with `Results.Problem(...)` or direct ProblemDetails writes
- **Effort:** M

##### Task 7.3: Add Missing ProducesResponseType Attributes (API)
- 15+ controllers missing error status documentation
- Add `[ProducesResponseType]` for 400, 401, 404 as appropriate
- Lower priority — documentation hardening, not runtime stabilization
- **Effort:** M

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| ValidationBehavior interacts with 4 validation layers (model binding, DataAnnotations, FluentValidation, MediatR) producing duplicates or inconsistent shapes | High | Medium | Define boundary model first; remove manual validation in same pass |
| `async Task<IResult>` trap resurfaces in new BFF endpoint files | High | Medium | Enforce `async Task(HttpContext)` pattern in all new files; add test |
| Middleware reorder changes auth/tenant interaction | Medium | Medium | Evaluate tenant/auth dependency before reordering; test auth flows |
| Resilience retry on non-idempotent operations | High | Low | `DisableForUnsafeHttpMethods()` by default; explicit opt-in |
| Token lifecycle in Blazor circuits (token captured at circuit start, not refreshed) | Medium | Medium | Document lifecycle; test re-auth after circuit reconnection |
| Antiforgery breaks SSR ↔ Interactive transitions | High | Medium | Test matrix across all render modes before enabling broadly |
| Rate limiting inaccurate behind proxies without ForwardedHeaders trust | Medium | Medium | Phase 3 (ForwardedHeaders config) must complete before Phase 6.3 |
| BFF route-group split changes auth/antiforgery/cancellation behavior by accident | High | Medium | Preserve metadata and conventions through shared route-group helpers and regression tests |
| Typed service contracts improve transport correctness but UI still renders failures inconsistently | High | Medium | Enforce the UI consumer contract and verify top-risk flows end-to-end |

---

## Definition Of Done

This program is not "done" because the plan reads well. It is done when the following measurable conditions are true for the targeted flows:

- zero silent HTTP failure flattening remains in targeted client services
- API, BFF, and filters all return RFC 7807 ProblemDetails on targeted error paths
- setup-secret and comparable sensitive state remain server-controlled
- targeted service methods observe cancellation consistently
- route-group decomposition preserves auth metadata, cancellation propagation, ProblemDetails behavior, and trace context
- top-risk flows have direct tests proving error-shape and trust-boundary behavior
- trace correlation exists across BFF → API for the targeted flows

## Success Metrics

1. **Zero silent exception swallowing** — every HTTP failure throws `ApiProblemException` or returns structured error
2. **100% ProblemDetails** — all API, filter, and BFF error responses conform to RFC 7807
3. **No generic catch blocks in controllers** — only targeted business-translation catches
4. **CancellationToken propagated end-to-end** — `RequestAborted` → service → HttpClient → API
5. **Resilience profiles active** — server-side HttpClient registrations have appropriate timeout/retry/breaker per client type
6. **W3C trace context propagated** — traces flow from BFF through API via OpenTelemetry
7. **BFF decomposed by bounded context** — coherent route groups, shared conventions
8. **Integration tests cover all changed flows** — Phase 0 tests pass throughout
9. **UI error rendering is contract-aligned** — targeted UI flows distinguish validation / forbidden / not-found / transient / unexpected failure states

---

## Execution Order

```
Phase 0 (Tests) ──────────────────────────────────────────►
                  ┌─ Phase 1 (Error Contract + Services) ─►
Stream A:         ├─ Phase 2 (API Error Consistency) ─────►  
                  ├─ Phase 3 (Middleware) ─────────────────►
                  └───────────────────────────────────────── 
                                                             
Stream B:            Phase 4 (Handler Decomposition) ──► Phase 5 (Resilience) ──►
                                                             
Stream D:                Phase 7 (BFF Decomposition) ──────────────────────────►
                                                             
Stream C:                              Phase 6 (Security) ────────────────────►
```

- **Phase 0** first (safety net)
- **Phases 1-3** in parallel after Phase 0 (Stream A core)
- **Phase 4** after Phase 1 (needs new error contract)
- **Phase 5** after Phase 4 (needs clean handler pipeline)
- **Phase 7** after Phase 2 (so BFF files get correct error patterns)
- **Phase 6** after Phase 3 + Phase 7 (needs ForwardedHeaders trust + split BFF files)

---

## Potential Risks & Unknowns

The **highest-risk area is the ValidationBehavior (Task 2.5)**. Four validation layers can interact: model binding, DataAnnotations, FluentValidation via pipeline, and manual FluentValidation in handlers. This can produce duplicate messages, inconsistent property naming, conflicting timing, and different error shapes depending on entry point. The mitigation is to define the boundary model (what validates where) FIRST, then implement the behavior AND remove manual validation in the same pass — not as separate steps.

The **most architecturally important unknown is the tenant/auth middleware ordering (Task 3.1)**. If the project's authentication scheme selection, issuer configuration, or claims transformation depends on tenant context, then tenant resolution MUST run before authentication. If not, auth can stay earlier. This decision cannot be made from the plan — it must be evaluated by inspecting how `AddAuthentication()` and claims transformations interact with tenant state in this specific codebase.

The **BFF decomposition (Task 7.1)** carries the `async Task<IResult>` trap that caused the original setup-secret bug. When ASP.NET Core minimal API handlers have signature `async Task<IResult>(HttpContext ctx)`, `Task<IResult>` is coerced to `Task` (RequestDelegate), so `IResult.ExecuteAsync` never runs and the response body is empty. Every new BFF endpoint file must use `async Task(HttpContext ctx)` with direct response writes. This should be enforced by a convention test.
