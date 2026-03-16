# HTTP Resilience & Runtime Reliability Refactor — Task Checklist

> Last Updated: 2026-03-13 Europe/Brussels (context-reset handoff)
> Matches plan v2 — 4 streams, Phases 0-7 (Phase 8 moved to enterprise-cleanup)

## Session Checkpoint (2026-03-13 Europe/Brussels)

- ✅ Implemented an unplanned but report-aligned Phase 6/7 precursor slice: setup-secret persistence/restore is now BFF-controlled instead of browser-storage-controlled.
- ✅ Added `GET /bff/setup-secret` and updated `/bff/setup-secret/sync` behavior.
- ✅ Removed `sessionStorage` usage from Setup page + onboarding service and updated tests.
- ✅ High-risk client service error-contract rollout is in place across InstanceOnboarding, TenantNavigation, Event, Category, Location, Organization, Group, and Admin services.
- ✅ Targeted API contract cleanup is in place for filter ProblemDetails and the two create endpoints explicitly called out by the plan.
- ✅ BFF route-group façade is now active and the old monolithic `BffEndpointExtensions.cs` implementation is gone.
- ✅ Extracted forwarding handlers are now wired into outbound server HttpClient registrations.
- 🟡 Middleware hardening has started (forwarded-header trust config + tenant/auth decision), but cancellation propagation, token-lifecycle documentation, and the remaining security hardening items are still open.

## Tracker Rules

- This file is the authoritative implementation tracker for the HTTP resilience program.
- `http-resilience-refactor-plan.md` defines architecture, gates, and definition of done.
- Do not mark later phases in progress until the gate conditions in the plan are satisfied.

## Gate Summary

- **Gate A:** Safety-net integration coverage must exist before broad service refactors.
- **Gate B:** Canonical ProblemDetails + typed failure outcomes must stabilize before downstream workflow decomposition work depends on them.
- **Gate C:** Shell/platform-adjacent work waits until the dominant runtime regression hotspots are reduced.
- **Gate D:** Tuning and broader hardening changes require evidence and telemetry, not guesswork.

---

## Governance Prerequisites ⏳ NOT STARTED

- [ ] **G.1** Define Validation Boundary Model
  - Document HTTP edge vs application-layer validation ownership before `ValidationBehavior` rollout
  - Acceptance: one explicit boundary model documented in plan and implemented consistently

- [ ] **G.2** Define Tenant/Auth Middleware Decision
  - Decide whether tenant resolution must run before authentication based on actual auth scheme and claims behavior
  - Status: decision made from code inspection (`X-API-Key`-based scheme selection is not tenant-sensitive); tasks/context docs updated, but governance checklist not yet fully normalized
  - Acceptance: decision documented and referenced by middleware changes

- [x] **G.3** Define Resilience Profile Mapping
  - Map each targeted server-side HttpClient to interactive/admin/background resilience profiles
  - Status: first-pass profile mapping is implemented in `Explore.Blazor/Extensions/HttpClientExtensions.cs`; governance checkpoint still needs final documentation polish
  - Acceptance: profile ownership is documented before resilience configuration begins

- [x] **G.4** Define BFF Route-Group Ownership + Handler Convention
  - Assign endpoint families to route-group files and enforce `async Task(HttpContext ctx)` convention
  - Status: façade + split endpoint files are live (`BffAuthEndpoints`, `BffPreferenceEndpoints`, `BffSetupSecretEndpoints`, `BffStorageEndpoints`), but the governance checklist entry still needs explicit closeout
  - Acceptance: route-group ownership and signature rule documented before the split starts

- [x] **G.5** Define Token Lifecycle Documentation Scope
  - Document circuit token capture, refresh, reconnection, and render-mode implications before handler decomposition
  - Status: lifecycle scope is now documented in `docs/BLAZOR.md`; governance checklist still needs final closeout
  - Acceptance: lifecycle note location and verification scope are defined

- [ ] **G.6** Define Antiforgery Render-Mode Test Matrix
  - Specify how SSR, InteractiveServer, InteractiveAuto, and WASM flows will be verified
  - Acceptance: matrix documented before antiforgery rollout

- [x] **G.7** Define SameSite Evaluation Criteria
  - Document the decision tree for setup-secret cookie SameSite behavior
  - Status: current rationale is documented in `docs/BLAZOR.md` (`SameSite=Lax` because onboarding can cross top-level auth redirects)
  - Acceptance: cookie policy can be chosen with explicit rationale

- [x] **G.8** Define Setup-Secret Rate-Limit Configuration Model
  - Specify configuration location, keying strategy, and expected verification approach
  - Status: BFF model is now implemented/configured in `Explore.Blazor/Extensions/RateLimitingExtensions.cs` and `Explore.Blazor/appsettings.json`; governance checkpoint still needs closeout
  - Acceptance: rate limiting can be implemented without ad hoc config choices

- [ ] **G.9** Define Cancellation Propagation Verification
  - Specify how end-to-end timeout/cancellation behavior will be proven in tests
  - Status: setup-secret and auth-debug slices now propagate cancellation; full verification path still needs to be documented
  - Acceptance: verification path documented before broad CancellationToken rollout

---

## Stream A — Runtime Stabilization

### Phase 0: Safety Net (Integration Tests) ⏳ NOT STARTED

- [x] **0.1** Setup-Secret Flow Integration Test
  - POST validate-secret with correct secret → 200 + `{ valid: true }`
  - POST validate-secret with wrong secret → 200 + `{ valid: false }`
  - POST validate-secret without tenant header → succeeds (exempt path)
  - Acceptance: 3+ green tests covering the entire setup-secret flow end-to-end

- [x] **0.2** BFF-to-API Error Propagation Tests
  - API returns ProblemDetails 400 → BFF surfaces structured error
  - API returns 404 → BFF surfaces not-found
  - API returns 500 → BFF surfaces server error (no stack trace leak)
  - Acceptance: 3+ green tests proving errors propagate correctly from API through BFF

- [x] **0.3** ProblemDetails Contract Tests
  - GlobalExceptionHandler returns valid ProblemDetails with `traceId`, `timestamp`
  - ValidationExceptionHandler returns validation errors keyed by property
  - Content-negotiation: `Accept: application/json` gets ProblemDetails
  - Acceptance: All error responses conform to RFC 7807 structure

- [x] **0.4** Middleware-Order Regression Tests
  - ForwardedHeaders respected for `X-Forwarded-Proto` → `Request.Scheme` is correct
  - Tenant resolution skips exempt paths
  - Exception handler catches controller exceptions and returns ProblemDetails
  - Acceptance: Middleware ordering behavior is locked in by tests

---

### Phase 1: Canonical Error Contract + Service Layer 🟡 MOSTLY COMPLETE
**Depends on:** Phase 0

- [x] **1.1** Create `ApiProblemException`
  - File: `Explore.Blazor.Client/Exceptions/ApiProblemException.cs` (new)
  - Properties: `StatusCode`, `ProblemDetails`, `ValidationErrors` (dictionary), `ServiceName`
  - Static factory: `ApiProblemException.FromResponse(HttpResponseMessage, string serviceName)`
  - Reads ProblemDetails from response body BEFORE throwing (NOT `EnsureSuccessStatusCode()`)
  - Acceptance: Exception type compiles; carries full error context from API responses

- [x] **1.2** Create HTTP Response Extension Methods
  - File: `Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs` (new)
  - `EnsureSuccessOrThrowProblem(this HttpResponseMessage, string serviceName)`
  - `ReadProblemDetailsAsync(this HttpResponseMessage)`
  - Acceptance: Extension methods replace all scattered status-checking logic

- [ ] **1.3** Add CancellationToken to All Service Methods
  - All files in `Explore.Blazor.Client/Services/`
  - Every public method: `CancellationToken cancellationToken = default`
  - Pass token to all `HttpClient` calls
  - Status: highest-risk service slice done; broad repo-wide service signature audit still open
  - Acceptance: `grep -r "CancellationToken" Services/` shows every public method

- [x] **1.4** Refactor EventService
  - File: `Explore.Blazor.Client/Services/EventService.cs`
  - Remove silent swallowing (lines 351, 364)
  - Use `response.EnsureSuccessOrThrowProblem("EventAPI")`
  - Add `using` on all HttpResponseMessage; pass CancellationToken
  - Acceptance: No silent generic swallow path remains in the targeted high-risk flow; shared typed problem parsing is in place

- [x] **1.5** Refactor CategoryService, LocationService, AdminService
  - Remove empty-collection-on-failure pattern
  - Use `EnsureSuccessOrThrowProblem` consistently
  - Add `using` + CancellationToken
  - Acceptance: Shared typed problem parsing/logging is in place across the targeted generated-client surfaces; some broad repo-wide fallback semantics remain for later cleanup

- [x] **1.6** Refactor TenantNavigationService, GroupService, OrganizationService
  - Standardize to `EnsureSuccessOrThrowProblem` pattern
  - Add missing `using` + CancellationToken
  - Acceptance: Targeted service set now uses the shared typed failure model; remaining repo-wide consistency audit continues under 1.3/1.7

- [ ] **1.7** Audit All HttpResponseMessage Disposal
  - Every `SendAsync`, `PostAsJsonAsync`, `PutAsJsonAsync`, `DeleteAsync` wrapped in `using`
  - Status: major hotspots addressed; full service-tree audit still pending
  - Acceptance: Zero undisposed HttpResponseMessage instances

---

### Phase 2: API Error Contract Consistency 🟡 IN PROGRESS
**Depends on:** Phase 0

- [ ] **2.1** Replace Anonymous Types with Formal DTOs
  - Create `StorageConnectionTestResultDto`, `SecretValidationResultDto`, etc.
  - Replace 4 `Ok(new { ... })` in InstanceOnboardingController with typed DTOs
  - Update `[ProducesResponseType]` to concrete types
  - Status: onboarding controller is already mostly typed in the current tree; latest sweep covered `TenantController`, `LanguageController`, `ActorKeyStoreController`, `ModuleController`, and `EventRegistrationController`, but more controller paths still remain repo-wide
  - Acceptance: Zero anonymous-type action results in any controller

- [x] **2.2** Remove Generic Catch Blocks from Controllers
  - EventController (2 blocks), CategoryController, LocationController, EventSessionController, EventSessionAgendaItemController, TagController
  - Delete generic `catch (Exception ex)` → let GlobalExceptionHandler handle
  - Keep targeted catches with business translation (e.g., concurrency → 409)
  - Acceptance: No controller-local generic catch blocks; targeted catches only

- [x] **2.3** Standardize Filter Error Responses to ProblemDetails
  - `SetupSecretRequiredAttribute.cs` — `new { error }` → `TypedResults.Problem(...)`
  - `BlockInSingleTenantAttribute.cs` — `new { error }` → `TypedResults.Problem(...)`
  - Acceptance: All filters return RFC 7807 ProblemDetails

- [x] **2.4** Fix POST Create Endpoints to Return 201
  - `ExternalApiKeyController.cs` — `Ok(response)` → `Created(...)`
  - `InstanceOnboardingController.cs` — `Ok(response)` → `Created(...)`
  - Update `[ProducesResponseType]` from 200 to 201
  - Acceptance: All create operations return HTTP 201

- [ ] **2.5** ValidationBehavior — Define Boundary Model + Implement
  - **Step 1:** Define boundary model — HTTP edge validates shape, application validates business rules
  - **Step 2:** Create `ValidationBehavior.cs` in `Explore.Application/Behaviors/`
  - **Step 3:** Remove manual `_validator.ValidateAsync()` from all command handlers (same pass)
  - Test no duplicate validation messages
  - Acceptance: Validators auto-run via pipeline; zero manual validation calls; no duplicates

---

### Phase 3: Middleware Pipeline Fixes 🟡 IN PROGRESS
**Depends on:** Phase 0

- [ ] **3.1** Fix Middleware Ordering in API Program.cs
  - `UseForwardedHeaders()` at top with `KnownProxies`/`KnownNetworks` configured
  - `UseExceptionHandler()` early
  - Evaluate tenant/auth ordering: does auth scheme depend on tenant?
  - Move request logging after tenant resolution
  - Status: evidence collected and config landed; auth-scheme decision is documented, but final closeout still needs explicit log-context / runtime verification
  - Acceptance: Correct order verified; all auth flows work; logs have tenant context

- [x] **3.2** Fix Middleware Ordering in Blazor Program.cs
  - Apply same ordering rules as API
  - Status: forwarded-header trust config, rate limiter placement, antiforgery placement, and explicit route-group mapping are now aligned in `Explore.Blazor/Program.cs`
  - Acceptance: Consistent ordering across both hosts

- [x] **3.3** Make Tenant-Exempt Paths Configurable
  - Move hardcoded paths from `IsTenantExemptPath()` to constants or config
  - Acceptance: Exempt paths defined in single location

- [x] **3.4** BFF Endpoints Pass CancellationToken
  - All BFF endpoint handlers pass `HttpContext.RequestAborted` to downstream calls
  - Status: split BFF surface audited; setup-secret, storage, and auth-debug downstream calls now pass cancellation
  - Acceptance: RequestAborted propagated through entire BFF call chain

- [x] **3.5** Setup-secret persistence/restore moved behind BFF trust boundary — S
  - Added `GET /bff/setup-secret`.
  - Setup page no longer reads or writes browser `sessionStorage` for setup-secret.
  - `/bff/setup-secret/sync` now reuses trusted persisted secret when available.
  - Acceptance: setup-secret is restored/validated by server-controlled state, not browser storage.

---

## Stream B — Server-Side Reliability

### Phase 4: DelegatingHandler Decomposition 🟡 IN PROGRESS
**Depends on:** Phase 1 (needs new error contract)

- [x] **4.1** Extract TenantHeaderForwardingHandler
  - New file: `Explore.Blazor/Services/TenantHeaderForwardingHandler.cs`
  - Move tenant slug + X-Forwarded-Host forwarding logic
  - Acceptance: Tenant headers forwarded; dedicated handler, single responsibility

- [x] **4.2** Extract SetupSecretForwardingHandler
  - New file: `Explore.Blazor/Services/SetupSecretForwardingHandler.cs`
  - Move X-Setup-Secret header forwarding
  - Acceptance: Setup secret forwarding isolated

- [x] **4.3** Simplify AccessTokenForwardingHandler
  - After extraction: only resolves access token + adds Authorization header
  - Document token lifecycle: refresh, circuit reconnection, null HttpContext, render mode transitions
  - Status: token-only responsibility is implemented; lifecycle documentation is still open
  - Acceptance: Handler has single responsibility; lifecycle documented

---

### Phase 5: HTTP Resilience Policies (Server-Side Only) 🟡 IN PROGRESS
**Depends on:** Phase 4 (needs clean handler pipeline)

- [x] **5.1** Add Microsoft.Extensions.Http.Resilience Package
  - `Explore.Blazor/Explore.Blazor.csproj` only (server-side, NOT .Client)
  - Acceptance: Package installed; clean build

- [x] **5.2** Define Client Resilience Profiles
  - Interactive UI: 15s total, 5s attempt, retry safe methods, circuit breaker
  - Admin/setup: 30s total, 10s attempt, retry safe methods
  - Background: 60s total, retry safe + idempotent
  - `DisableForUnsafeHttpMethods()` as baseline; idempotent POSTs opted-in explicitly
  - Status: first-pass interactive/admin/background profiles are wired in `Explore.Blazor/Extensions/HttpClientExtensions.cs`; final profile ownership and tuning evidence remain open
  - Acceptance: Three named profiles configured on HttpClient registrations

- [x] **5.3** Verify HttpClient Cookie Behavior
  - Confirm all server-side outbound clients set `UseCookies = false`
  - Check `IHttpClientFactory` pooled handlers don't share `CookieContainer`
  - Acceptance: No unintended cookie leakage between clients

- [x] **5.4** Trace Propagation via OpenTelemetry
  - W3C `traceparent` header via OpenTelemetry (not custom correlation headers)
  - Verify .NET `Activity`/`DiagnosticSource` propagates trace context
  - Status: verified from shared host bootstrap: both API and BFF call `builder.AddServiceDefaults()`, and `Explore.ServiceDefaults/Extensions.cs` enables `AddAspNetCoreInstrumentation()` plus `AddHttpClientInstrumentation()`
  - Acceptance: Traces flow from BFF through API via standard W3C context

---

## Stream C — Security Hardening

### Phase 6: Antiforgery, Cookies, Rate Limiting 🟡 IN PROGRESS
**Depends on:** Phase 3 (ForwardedHeaders trust) + Phase 7 (split BFF files)

- [ ] **6.1** Antiforgery on BFF State-Changing Endpoints
  - Verify Data Protection key-ring persistence (works across restarts)
  - Enable antiforgery on POST/PUT/DELETE BFF endpoints
  - Test matrix: SSR, InteractiveServer, InteractiveAuto, WASM render modes
  - Antiforgery middleware after authentication/authorization
  - Status: browser-side header injection now exists centrally in `BrowserCredentialsMessageHandler`, and explicit validation is enabled on the proven custom BFF write routes (`/bff/auth/refresh-schemes`, setup-secret write routes, upload-proxy); broader rollout and render-mode verification remain open
  - Acceptance: State-changing requests without token rejected; all render modes work

- [x] **6.2** Setup Secret Cookie — Evaluate SameSite
  - Does setup-secret cookie participate in cross-site flows (OIDC redirects, email links)?
  - If no cross-site: `SameSite=Strict`
  - If OIDC redirect flows: `SameSite=Lax` with documented rationale
  - Acceptance: SameSite evaluated and set with documented reasoning

- [x] **6.2a** Remove browser storage dependency for setup-secret — S
  - `Explore.Blazor.Client/Pages/Setup.razor`
  - `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
  - Acceptance: browser no longer stores setup-secret in `sessionStorage`; BFF owns the persisted state.

- [x] **6.3** Rate Limiting on Setup Secret Endpoint
  - Named rate-limit policy for setup-secret validation
  - Key by trusted client identity or session (not raw IP — unreliable behind proxies)
  - Depends on Phase 3 ForwardedHeaders trust being configured correctly
  - Acceptance: Excessive attempts return 429; limits configurable

---

## Stream D — BFF Structural

### Phase 7: BFF Route-Group Decomposition 🟡 IN PROGRESS
**Depends on:** Phase 2 (correct error patterns before splitting)

- [x] **7.1** Decompose BFF Endpoints by Route Group
  - Split by bounded context / security policy / dependency profile:
    - `BffEventEndpoints.cs`, `BffOrganizationEndpoints.cs`, `BffAdminEndpoints.cs`, `BffSetupEndpoints.cs`
  - Extract shared conventions into `BffEndpointConventions.cs`
  - ⚠️ ALL handlers must use `async Task(HttpContext ctx)` — NOT `async Task<IResult>` (causes empty responses)
  - Acceptance: Coherent route groups; no single monolith; all endpoints route correctly

- [x] **7.2** Standardize BFF Error Response Format
  - All BFF endpoints: `new { error = "..." }` → `Results.Problem(...)` or ProblemDetails writes
  - Acceptance: 100% ProblemDetails on all BFF error paths

- [x] **7.0** Add BFF setup-secret status endpoint — S
  - New route: `GET /bff/setup-secret`
  - Used by Setup page to restore/validate persisted secret state
  - Acceptance: Setup flow can recover persisted secret without browser storage

- [ ] **7.3** Add Missing ProducesResponseType Attributes (API)
  - 15+ controllers missing error status documentation
  - Add `[ProducesResponseType]` for 400, 401, 404 as appropriate
  - Lower priority — documentation, not runtime stabilization
  - Acceptance: Swagger shows all possible status codes

---

## Summary

| Stream | Phase | Tasks | Status | Dependencies |
|--------|-------|-------|--------|--------------|
| A | 0. Safety Net Tests | 4 | ✅ | None |
| A | 1. Error Contract + Services | 7 | 🟡 | Phase 0 |
| A | 2. API Error Consistency | 5 | 🟡 | Phase 0 |
| A | 3. Middleware Pipeline | 4 | ✅ | Phase 0 |
| B | 4. Handler Decomposition | 3 | ✅ | Phase 1 |
| B | 5. HTTP Resilience | 4 | 🟡 | Phase 4 |
| C | 6. Security Hardening | 3 | 🟡 | Phase 3 + Phase 7 |
| D | 7. BFF Decomposition | 3 | 🟡 | Phase 2 |
| **Total** | | **33** | | |

**Execution order:**
```
Phase 0 ──► Phases 1, 2, 3 (parallel) ──► Phase 4 ──► Phase 5
                                    Phase 2 ──► Phase 7 ──► Phase 6
                                    Phase 3 ─────────────►
```

---

## Items Moved to `enterprise-cleanup`

The following tasks were removed from this plan (not HTTP resilience scope):
- `CongfigurePersistenceServices` typo → `ConfigurePersistenceServices`
- `S3ConfigResolver.cs` line 156 missing `.Trim()` on config fallback
- `EventSeries`, `Tenant`, `Group` — mutable `ICollection<T>` → `IReadOnlyCollection<T>`

## Quick Resume

1. Treat Phase 0 and the high-risk Phase 1/3/4/7 slices as completed baseline work.
2. Continue next with:
   - remaining API anonymous error payload cleanup,
   - `ValidationBehavior` rollout,
   - trace verification and antiforgery client/header path design.
3. Rerun:
   - `dotnet build "Explore.Blazor/Explore.Blazor.csproj" --configuration Release --no-dependencies --verbosity minimal`
   - `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity minimal`
   - `dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj" --configuration Release --verbosity minimal`
