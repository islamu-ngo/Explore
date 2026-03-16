# HTTP Resilience & Runtime Reliability Refactor — Context

> Last Updated: 2026-03-13 Europe/Brussels (context-reset handoff)
> Status: Development stage — no backward-compatibility constraints.

## SESSION PROGRESS (2026-03-13 Europe/Brussels)

### ✅ COMPLETED
- Full codebase audit across 4 dimensions (HTTP services, middleware, API contracts, domain/persistence)
- Root cause analysis of setup-secret validation bug (fixed in prior session)
- Enterprise pattern research (Polly, ProblemDetails RFC 7807, typed clients, DelegatingHandler best practices)
- Plan v1 created with 8 phases
- External architectural review incorporated (20 feedback points)
- **Plan v2 rewritten** — restructured into 4 streams, 8 phases, incorporating all feedback
- Implemented the first live hardening slice for setup-secret trust boundaries instead of staying plan-only.
- Added `GET /bff/setup-secret` in `Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs` to validate/restore persisted setup-secret state server-side.
- Updated `/bff/setup-secret/sync` to reuse the trusted persisted secret when the client does not resend it.
- Removed `sessionStorage` dependency from `Explore.Blazor.Client/Pages/Setup.razor` and from `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`.
- Added/updated tests in:
  - `Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs`
  - `Explore.Blazor.Client.Tests/Pages/SetupTests.cs`
- Added/verified Phase 0 safety-net coverage for the setup/BFF slice:
  - `Event.API.IntegrationTests/Features/SetupSecretFlowTests.cs`
  - `Event.API.IntegrationTests/Features/ProblemDetailsContractTests.cs`
  - `Event.API.IntegrationTests/Features/Middleware/MiddlewareOrderTests.cs`
  - new BFF persistence/error-path assertions in `Explore.Blazor.Client.Tests/Pages/SetupTests.cs`
  - new API status-propagation assertions in `Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs`
- Implemented the shared Phase 1 core contract in:
  - `Explore.Blazor.Client/Exceptions/ApiProblemException.cs`
  - `Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs`
- Moved `Explore.Blazor.Client/Services/InstanceOnboardingService.cs` onto the shared error contract primitives (`EnsureSuccessOrThrowProblem`, `ReadJsonOrThrowAsync`, `ApiProblemException.FromResponseAsync`).
- Extended the shared client error contract into generated-client services:
  - `Explore.Blazor.Client/Services/CategoryService.cs`
  - `Explore.Blazor.Client/Services/LocationService.cs`
  - `Explore.Blazor.Client/Services/TenantNavigationService.cs`
  - `Explore.Blazor.Client/Services/EventService.cs`
  - `Explore.Blazor.Client/Services/OrganizationService.cs`
  - `Explore.Blazor.Client/Services/GroupService.cs`
  - `Explore.Blazor.Client/Services/AdminService.cs`
- Normalized targeted generated-client failure responses to `Request failed.` plus parsed error details from API/NSwag `ApiException.Response`.
- Completed Phase 2 filter normalization for targeted paths:
  - `Explore.API/Filters/BlockInSingleTenantAttribute.cs` now returns 404 ProblemDetails instead of a bare `NotFoundResult`
  - `Explore.API/Filters/SetupSecretRequiredAttribute.cs` remains aligned on ProblemDetails
- Added unit coverage in `Event.Application.UnitTests/Infrastructure/BlockInSingleTenantAttributeTests.cs`.
- Completed the high-risk client-service normalization pass for:
  - `Explore.Blazor.Client/Services/OrganizationService.cs`
  - `Explore.Blazor.Client/Services/GroupService.cs`
  - `Explore.Blazor.Client/Services/AdminService.cs`
- Completed the specific create-status API cleanup required by the plan:
  - `Explore.API/Controllers/ExternalApiKeyController.cs` now returns `201 Created`
  - `Explore.API/Controllers/InstanceOnboardingController.cs` `Complete(...)` now returns `201 Created`
- Replaced several remaining anonymous API error payloads with ProblemDetails:
  - `Explore.API/Controllers/TenantController.cs`
  - `Explore.API/Controllers/LanguageController.cs`
  - `Explore.API/Controllers/ActorKeyStoreController.cs`
  - `Explore.API/Controllers/ModuleController.cs`
  - `Explore.API/Controllers/EventRegistrationController.cs`
- Proved by direct code inspection that API auth scheme selection is not tenant-sensitive in this repo: `Explore.API/Program.cs` switches on `X-API-Key` presence, not tenant context.
- Added explicit forwarded-header trust configuration to both hosts:
  - `Explore.API/Program.cs`
  - `Explore.API/appsettings.json`
  - `Explore.Blazor/Extensions/MiddlewareExtensions.cs`
  - `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`
  - `Explore.Blazor/appsettings.json`
- Switched forwarded-header network handling to `.KnownIPNetworks` / `System.Net.IPNetwork` to avoid .NET 10 deprecation warnings.
- Replaced the old `Explore.Blazor/Extensions/BffEndpointExtensions.cs` monolith with a thin route-group façade that delegates to:
  - `Explore.Blazor/Extensions/BffAuthEndpoints.cs`
  - `Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`
  - `Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`
  - `Explore.Blazor/Extensions/BffStorageEndpoints.cs`
- Wired the extracted forwarding handlers into the outbound API client pipeline in `Explore.Blazor/Extensions/HttpClientExtensions.cs`:
  - `AccessTokenForwardingHandler`
  - `TenantHeaderForwardingHandler`
  - `SetupSecretForwardingHandler`
- Simplified `Explore.Blazor/Services/CircuitAccessTokenService.cs` so `AccessTokenForwardingHandler` now owns only bearer-token forwarding.
- Added server-side resilience scaffolding in `Explore.Blazor/Extensions/HttpClientExtensions.cs` with interactive/admin/background profiles and `UseCookies = false` on outbound handlers.
- Added `Microsoft.Extensions.Http.Resilience` to `Explore.Blazor/Explore.Blazor.csproj`.
- Added cancellation-token propagation to the auth debug metadata fetch path in `Explore.Blazor/Extensions/BffAuthEndpoints.cs`.
- Standardized the split BFF surface so the remaining preference endpoint failures also return ProblemDetails instead of bare `400`/`401` results.
- Added BFF setup-secret rate limiting in:
  - `Explore.Blazor/Extensions/RateLimitingExtensions.cs`
  - `Explore.Blazor/Program.cs`
  - `Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`
  - `Explore.Blazor/appsettings.json`
- Implemented the browser-side antiforgery header path and enabled validation on the proven custom BFF write endpoints:
  - `Explore.Blazor.Client/Services/Http/BrowserCredentialsMessageHandler.cs`
  - `Explore.Blazor.Client/Services/Http/BffClient.cs`
  - `Explore.Blazor.Client/Services/InstanceOnboardingService.cs`
  - `Explore.Blazor/Extensions/AntiforgeryEndpointExtensions.cs`
  - `Explore.Blazor/Extensions/BffAuthEndpoints.cs`
  - `Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`
  - `Explore.Blazor/Extensions/BffStorageEndpoints.cs`
- Documented the BFF route-group model, setup-secret SameSite rationale, token lifecycle scope, and rate-limit keying strategy in `docs/BLAZOR.md`.
- Verified the Phase 5 trace-propagation assumption from the shared bootstrap: both hosts call `builder.AddServiceDefaults()`, and `Explore.ServiceDefaults/Extensions.cs` enables ASP.NET Core + HttpClient OpenTelemetry instrumentation.

### 🟡 IN PROGRESS
- Phase 0 is now effectively in place for the setup-secret/BFF risk area and has green client-side verification.
- Phase 1 targeted rollout is effectively complete for the highest-risk service slice, but the broad `CancellationToken` audit and full `HttpResponseMessage` disposal audit across all remaining services are still open.
- Phase 2 has advanced beyond filters: create-status fixes are in place and some anonymous controller error payloads were replaced, but the controller-wide anonymous payload sweep and ValidationBehavior rollout are still open.
- Phase 3 has started: forwarded-header trust config and tenant/auth ordering evidence are in place, but BFF cancellation propagation and final middleware verification are still open.
- Phase 4-7 have now started for real: handler decomposition is active in the client pipeline, the BFF monolith is collapsed into the façade, and server-side resilience/cookie-hardening scaffolding is in place.
- The split BFF surface now has no remaining bare `BadRequest` / `Unauthorized` / `new { error = ... }` responses in `Explore.Blazor/Extensions/Bff*.cs`.
- Phase 6 now has live implementation progress: the BFF setup-secret edge is rate limited and the current SameSite decision is explicitly documented.
- Explicit antiforgery enforcement is no longer fully deferred: the browser-side header path now exists centrally in `BrowserCredentialsMessageHandler`, and endpoint validation is enabled on the proven custom BFF write routes (`/bff/auth/refresh-schemes`, `/bff/setup-secret`, `/bff/setup-secret/sync`, `/bff/setup-secret` DELETE, `/bff/storage/upload-proxy`).
- Broad antiforgery rollout is still intentionally limited: theme/language and reverse-proxied `/api/*` writes are not yet covered by endpoint-level validation because the current work focused on the proven custom BFF write surface.

### ⚠️ BLOCKERS
- Full TUnit filtering via standard `dotnet test --filter ...` still does not work in this repo. Use full project test runs or runner-compatible partitioning.
- `dotnet test --project "Event.API.IntegrationTests/Event.API.IntegrationTests.csproj" --configuration Release --verbosity quiet` still has one unrelated pre-existing failure: `ApiEndpointSmokeTests.Public_Get_Endpoints_ReturnOk` fails because `/api/eventseries` returns `400 BadRequest`.
- The worktree remains globally dirty far beyond this track; avoid broad staging commands and treat unrelated modified files as intentional baseline.
- The earlier reflection-based `SetToken(...)` workaround is no longer present in `Explore.Blazor/Extensions/MiddlewareExtensions.cs`; the current compile/build blockers in this slice were resolved by explicit route-group calls in `Explore.Blazor/Program.cs` and concrete `SetupSecretSessionService` registration.
- Full solution-level verification remains blocked by unrelated baseline compile errors outside this track (`TemporalView`, `PermissionAction`, EventSeries DTO/client work, etc.). Narrow Blazor verification is available instead.

---

## THIS SESSION — IMPLEMENTATION CHECKPOINT

### Artifact Alignment

- `http-resilience-refactor-plan.md` now owns diagnosis, execution waves, phase gates, boundary definitions, and definition-of-done criteria.
- `http-resilience-refactor-tasks.md` is the authoritative implementation tracker.
- This context file owns restart state, file anchors, and recent implementation decisions.

If status ever appears inconsistent, trust the task checklist over prose elsewhere.

### Files Modified and Why

**`Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`**
- Added `GET /bff/setup-secret`.
- Server now owns setup-secret restoration/validation instead of the browser.

**`Explore.Blazor.Client/Pages/Setup.razor`**
- Stopped reading/writing `sessionStorage`.
- Restores persisted setup-secret state from BFF endpoint.

**`Explore.Blazor.Client/Services/InstanceOnboardingService.cs`**
- Removed manual `X-Setup-Secret` injection from browser state.

**`Explore.Blazor.Client.Tests/Services/InstanceOnboardingServiceTests.cs`**
- Added coverage proving the service no longer injects `X-Setup-Secret` from browser storage.
- Added status-propagation assertions for `410 Gone`, `400 BadRequest`, and `500 InternalServerError` during setup-secret validation.

**`Explore.Blazor.Client.Tests/Pages/SetupTests.cs`**
- Updated tests to verify BFF-backed persisted-secret restore behavior.
- Added safety-net assertions proving BFF persistence failures surface a stable UI message without leaking raw server detail.

**`Explore.Blazor.Client/Exceptions/ApiProblemException.cs`**
- Upgraded the shared client error contract to carry parsed problem payload, validation errors, service name, and trace context.

**`Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs`**
- Added `EnsureSuccessOrThrowProblem(...)` and `ReadProblemDetailsAsync(...)` on top of the typed exception contract.

**`Explore.Blazor.Client/Services/InstanceOnboardingService.cs`**
- Replaced ad hoc response checks in shared helpers with the typed error contract primitives while keeping current external behavior stable.

**`Explore.Blazor.Client/Services/CategoryService.cs`**
- Generated-client (`ApiException`) failures now flow through the shared typed problem contract before producing normalized failure responses.

**`Explore.Blazor.Client/Services/LocationService.cs`**
- Generated-client (`ApiException`) failures now flow through the shared typed problem contract before producing normalized failure responses.

**`Explore.Blazor.Client/Services/TenantNavigationService.cs`**
- Raw `HttpClient` command/read paths now use `EnsureSuccessOrThrowProblem(...)` with normalized command-failure responses.

**`Explore.Blazor.Client/Services/EventService.cs`**
- High-volume generated-client catch paths now parse `ApiException` into the shared problem contract for consistent logging and later strict-rollout work.

**`Explore.Blazor.Client/Services/OrganizationService.cs`**
- Write operations no longer rethrow transport-layer `ApiException`; they now return normalized failure responses while preserving existing read fallbacks.

**`Explore.Blazor.Client/Services/GroupService.cs`**
- Raw `HttpClient` reads now use `EnsureSuccessOrThrowProblem(...)`, and generated-client writes return normalized failure responses instead of rethrowing.

**`Explore.Blazor.Client/Services/AdminService.cs`**
- Broad ApiException catch surface now logs parsed problem details consistently across organization, lookup, category, tag, and location operations while preserving bool/null/list semantics.

**`Explore.API/Controllers/ExternalApiKeyController.cs`**
- Create endpoint now returns `201 Created` via `CreatedAtAction(...)` instead of `200 OK`.

**`Explore.API/Controllers/InstanceOnboardingController.cs`**
- `Complete(...)` now returns `201 Created`.
- Existing onboarding responses were already mostly typed in this tree; no additional anonymous onboarding payload conversion was needed in this session.

**`Explore.API/Controllers/TenantController.cs`**
- Replaced anonymous error payloads for tenant/navigation mismatch and missing-tenant paths with ProblemDetails.

**`Explore.API/Controllers/LanguageController.cs`**
- Replaced anonymous not-found payload with ProblemDetails.

**`Explore.API/Controllers/ActorKeyStoreController.cs`**
- Replaced anonymous bad-request/not-found payloads with ProblemDetails.

**`Explore.API/Controllers/ModuleController.cs`**
- Replaced anonymous bad-request/not-found payloads with ProblemDetails for schema/enable/disable paths.

**`Explore.API/Controllers/EventRegistrationController.cs`**
- Replaced anonymous bad-request/not-found payloads with ProblemDetails.

**`Explore.API/Filters/BlockInSingleTenantAttribute.cs`**
- Now emits 404 ProblemDetails instead of an empty `NotFoundResult`.

**`Event.Application.UnitTests/Infrastructure/BlockInSingleTenantAttributeTests.cs`**
- Added unit coverage for 404/403 ProblemDetails gating behavior.

**`Explore.API/Program.cs`**
- Added explicit forwarded-header trust configuration using `ForwardedHeaders:KnownProxies` / `KnownNetworks`.
- Confirmed current API pipeline already satisfies the main ordering goals: forwarded headers first, exception handling early, request logging after tenant resolution.

**`Explore.Blazor/Extensions/MiddlewareExtensions.cs`**
- Added explicit forwarded-header trust configuration with development-only trust-all fallback when no trusted proxies/networks are configured.
- Contains the current reflection workaround around `CircuitAccessTokenService.SetToken(...)`.

**`Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs`**
- Registered `CircuitAccessTokenService` concretely and exposed `ICircuitAccessTokenService` from the same scoped instance to support middleware changes.

**`Explore.API/appsettings.json`** and **`Explore.Blazor/appsettings.json`**
- Added empty `ForwardedHeaders:KnownProxies` and `ForwardedHeaders:KnownNetworks` sections so trust configuration is explicit and deployment-owned.

**`Explore.Blazor/Extensions/BffEndpointExtensions.cs`**
- Replaced the previous monolith with the intended route-group façade.

**`Explore.Blazor/Extensions/BffAuthEndpoints.cs`**
- Auth debug endpoint now accepts `CancellationToken` and passes it through the metadata-fetch HTTP call.

**`Explore.Blazor/Extensions/BffPreferenceEndpoints.cs`**
- Invalid theme/language requests and unauthenticated `/bff/me` now return ProblemDetails instead of bare status results.

**`Explore.Blazor/Extensions/RateLimitingExtensions.cs`**
- Adds the named `BffSetupSecret` rate-limit policy keyed by authenticated user first, then antiforgery/session cookie, and IP only as the last fallback.

**`Explore.Blazor/Extensions/BffSetupSecretEndpoints.cs`**
- `POST /bff/setup-secret` and `POST /bff/setup-secret/sync` now require the BFF setup-secret limiter.
- Setup-secret POST/sync/delete routes now also require explicit antiforgery validation.

**`Explore.Blazor/Extensions/BffStorageEndpoints.cs`**
- Upload-proxy now requires explicit antiforgery validation.

**`Explore.Blazor/Extensions/BffAuthEndpoints.cs`**
- `/bff/auth/refresh-schemes` now requires explicit antiforgery validation.

**`Explore.Blazor/Extensions/HttpClientExtensions.cs`**
- Added resilience profiles, registered the extracted forwarding handlers, and enforced `UseCookies = false` for outbound server clients.

**`Explore.Blazor/Extensions/AntiforgeryEndpointExtensions.cs`**
- Adds the reusable minimal-API antiforgery validation filter used by the custom BFF write endpoints.

**`Explore.Blazor/Services/CircuitAccessTokenService.cs`**
- Simplified `AccessTokenForwardingHandler` to token-only responsibility after handler extraction became real.

**`Explore.Blazor/Services/SetupSecretForwardingHandler.cs`**
- Uses the concrete `SetupSecretSessionService` registration to avoid the previous ambiguous `GetForUser(...)` call-site issue.

**`Explore.Blazor/Program.cs`**
- Now calls the split auth/BFF route groups explicitly (`BffAuthEndpoints.MapAuthEndpoints(app)` and `BffEndpointExtensions.MapBffEndpoints(app)`) to avoid extension-method ambiguity.
- Registers BFF rate limiting and activates `app.UseRateLimiter()` in the server pipeline.

**`Explore.Blazor.Client/Services/Http/BrowserCredentialsMessageHandler.cs`**
- Now injects `X-CSRF-TOKEN` automatically on mutating same-origin browser requests by reading the existing `XSRF-TOKEN` cookie via `bff.js`.

**`Explore.Blazor.Client/Services/Http/BffClient.cs`**
- Simplified to rely on the shared WASM handler pipeline for antiforgery instead of manually reading cookies itself.

**`Explore.Blazor.Client/Services/InstanceOnboardingService.cs`**
- `RefreshAuthSchemesAsync()` now uses the antiforgery-capable browser path when `BffClient` is available.

**`Explore.Blazor.Client.Tests/Services/Http/BrowserCredentialsMessageHandlerTests.cs`**
- Adds coverage proving the shared WASM handler injects `X-CSRF-TOKEN` on mutating requests and skips GETs.

**`docs/BLAZOR.md`**
- Documents the split BFF route-group model, setup-secret cookie rationale (`SameSite=Lax`), rate-limit keying strategy, token lifecycle constraints, and handler-chain responsibilities.

**`Explore.ServiceDefaults/Extensions.cs`**
- Shared proof point that trace propagation uses standard OpenTelemetry instrumentation (`AddAspNetCoreInstrumentation()` + `AddHttpClientInstrumentation()`) for both API and BFF hosts.

### Exact State of Unfinished Work

- The broader HTTP resilience plan is still only partially implemented beyond the setup-secret/BFF slice, the shared service error contract, and the first middleware hardening steps.
- Next immediate implementation candidates remain:
  1. Finish the controller-wide anonymous error payload sweep (`ActorKeyStoreController`, `ModuleController`, `EventRegistrationController`, plus any remaining `BadRequest(new { error = ... })` / `NotFound(new { error = ... })` controller paths).
  2. Complete Phase 2.5 `ValidationBehavior` with an explicit validation-boundary model and same-pass removal of manual validator calls.
  3. Complete the remaining Phase 3 cancellation propagation audit across the full BFF/auth surface and confirm no downstream calls still ignore `RequestAborted`.
  4. Decide whether to expand antiforgery validation beyond the custom BFF write routes (theme/language and/or reverse-proxied `/api/*` writes) now that the client-side header path exists.
  5. Start the remaining Phase 5/6 items only after the remaining Phase 2 contract work is stabilized.
- Phase gates added to the plan mean broad service rollout should not proceed without the safety-net tests described in Phase 0.

### Quick Resume

1. Re-read this file and `http-resilience-refactor-tasks.md`.
2. Use the setup-secret slice as the reference pattern for trust-boundary hardening already completed.
3. Resume at API contract cleanup and middleware/cancellation work, not at setup-secret or the initial client-service rollout.
4. Exact current handoff anchors:
   - `Explore.API/Controllers/ActorKeyStoreController.cs:92`
   - `Explore.API/Controllers/ModuleController.cs:107`
   - `Explore.API/Controllers/EventRegistrationController.cs:115`
   - `Explore.Blazor/Extensions/MiddlewareExtensions.cs:193`
5. Validation commands:
   - `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity minimal`
   - `dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj" --configuration Release --verbosity minimal`
   - `dotnet build "Explore.Blazor/Explore.Blazor.csproj" --configuration Release --verbosity minimal`
   - `dotnet build "Explore.Blazor/Explore.Blazor.csproj" --configuration Release --no-dependencies --verbosity minimal`
   - `dotnet build "Explore.API/Explore.API.csproj" --configuration Release --verbosity minimal`
   - `dotnet test --project "Event.API.IntegrationTests/Event.API.IntegrationTests.csproj" --configuration Release --verbosity minimal` (currently 1 unrelated pre-existing `ApiEndpointSmokeTests` failure on `/api/eventseries`)

### Verification Evidence From This Session

- `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity quiet` → passed (`578/578`)
- `dotnet test --project "Event.Application.UnitTests/Event.Application.UnitTests.csproj" --configuration Release --verbosity quiet` → passed (`491/491`)
- `dotnet build "Explore.Blazor/Explore.Blazor.csproj" --configuration Release --verbosity quiet` → passed
- `dotnet build "Explore.Blazor/Explore.Blazor.csproj" --configuration Release --no-dependencies --verbosity quiet` → passed after the BFF façade/handler/resilience changes
- `grep` over `Explore.Blazor/Extensions/Bff*.cs` shows no remaining bare `Results.BadRequest()`, `Results.Unauthorized()`, or `new { error = ... }` error responses after the BFF preference/auth cleanup
- `dotnet build "Explore.Blazor/Explore.Blazor.csproj" --configuration Release --no-dependencies --verbosity quiet` → still passes after adding the BFF setup-secret rate limiter
- `dotnet build "Explore.API/Explore.API.csproj" --configuration Release --verbosity quiet` → passed
- `lsp_diagnostics` clean on `Explore.API/Controllers/ActorKeyStoreController.cs`, `Explore.API/Controllers/ModuleController.cs`, and `Explore.API/Controllers/EventRegistrationController.cs` after the latest ProblemDetails cleanup
- `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --no-build --verbosity quiet` → passed (`571/571`) after the antiforgery header-path changes

## Key Changes from External Review (v1 → v2)

1. **Moved Phase 8 out** — typo fix, `.Trim()`, domain collections → `enterprise-cleanup` (not HTTP resilience scope)
2. **Added Phase 0** — integration tests as safety net before behavior changes
3. **Replaced `HttpServiceBase` with `ApiProblemException`** — typed exception carrying ProblemDetails, not CRUD-shaped base wrappers. Services stay domain-oriented.
4. **Added CancellationToken propagation** — every service method, BFF endpoint, HttpClient call must observe cancellation
5. **Middleware ordering as rules, not sequence** — tenant/auth order depends on whether auth is tenant-sensitive
6. **"No generic catch" instead of "zero try-catch"** — targeted business-translation catches (409/502/422) are valid
7. **ValidatorBehavior is now a dedicated mini-program** — define boundary model first, implement behavior, remove manual validation in same pass
8. **Resilience by client profile** — interactive (short timeout), admin (medium), background (long + retry). Server-side only.
9. **W3C traceparent via OpenTelemetry** — not custom correlation headers
10. **BFF decomposition by bounded context** — not arbitrary line-count metrics
11. **Rate limiting with proxy-aware keying** — not just raw IP
12. **SameSite evaluated, not blindly set** — depends on OIDC redirect flows
13. **ForwardedHeaders trust configuration** — KnownProxies/KnownNetworks as explicit tasks
14. **HttpClient cookie audit** — confirm `UseCookies = false` on outbound clients
15. **Token lifecycle in Blazor circuits** — documented as architectural risk (captured at circuit start, not refreshed)

**Dropped from plan (dev stage, not relevant):**
- Rollback controls / feature flags
- `[Obsolete]` forwarding methods
- Operational dashboards
- Formal ADRs
- Load testing

---

## Key Files

### New Files to Create (Phase 0-1)

**`Event.API.IntegrationTests/` — New test classes**
- Setup-secret flow tests
- BFF-to-API error propagation tests
- ProblemDetails contract tests
- Middleware-order regression tests

**`Explore.Blazor.Client/Exceptions/ApiProblemException.cs`** (new)
- Typed exception carrying: StatusCode, ProblemDetails, ValidationErrors dict, ServiceName
- Static factory: `ApiProblemException.FromResponse(HttpResponseMessage, string serviceName)`
- Design center: read ProblemDetails from response body BEFORE throwing (don't use `EnsureSuccessStatusCode()` which discards the body)

**`Explore.Blazor.Client/Extensions/HttpResponseExtensions.cs`** (new)
- `EnsureSuccessOrThrowProblem(this HttpResponseMessage, string serviceName)` — replaces scattered status-checking

### HTTP Service Layer (Phase 1)

**`Explore.Blazor.Client/Services/EventService.cs`**
- Silent exception swallowing at lines 351, 364
- Missing `using` on HttpResponseMessage
- Missing CancellationToken on methods

**`Explore.Blazor.Client/Services/CategoryService.cs`**, **`LocationService.cs`**, **`AdminService.cs`**
- Empty collection returns on failure — masks broken API calls

**`Explore.Blazor.Client/Services/TenantNavigationService.cs`**, **`GroupService.cs`**, **`OrganizationService.cs`**
- Missing `using` on HttpResponseMessage
- Inconsistent error patterns

### API Error Contracts (Phase 2)

**`Explore.API/ExceptionHandling/GlobalExceptionHandler.cs`** — THE STANDARD
- ✅ Maps typed exceptions to ProblemDetails with traceId + timestamp

**`Explore.API/Filters/SetupSecretRequiredAttribute.cs`**, **`BlockInSingleTenantAttribute.cs`**
- ❌ Use `new { error }` instead of ProblemDetails

**Controllers with generic catch blocks (to remove):**
- EventController.cs (2 blocks: lines 178-215, 343-366)
- CategoryController.cs (lines 164-181)
- LocationController.cs (lines 205-220)
- EventSessionController.cs, EventSessionAgendaItemController.cs, TagController.cs

**InstanceOnboardingController — 4 anonymous type instances:**
- Line 319, 401, 418, + one more — all need formal DTOs

### Middleware (Phase 3)

**`Explore.API/Program.cs` (lines 618-644)** — middleware ordering
**`Explore.Blazor/Program.cs`** — same issues
**`Explore.API/Middleware/ApiTenantResolutionMiddleware.cs`** — hardcoded exempt paths

### Validation (Phase 2, Task 2.5)

**`Explore.Application/ApplicationServicesRegistration.cs` (lines 18-24)**
- Has `PerformanceBehavior`, `AuthorizationBehavior`, `AddValidatorsFromAssembly`
- ❌ Missing: `ValidationBehavior`

**Boundary model to define:**
- HTTP edge: model binding + DataAnnotations → transport/shape validation
- Application layer: FluentValidation via MediatR pipeline → business rule validation

### DelegatingHandler (Phase 4)

**`Explore.Blazor/Services/CircuitAccessTokenService.cs` (lines 225-378)**
- Split into: TenantHeaderForwardingHandler, SetupSecretForwardingHandler, simplified AccessTokenForwardingHandler
- **Token lifecycle risk:** tokens captured at circuit start, not refreshed on re-auth

### Resilience (Phase 5)

**`Explore.Blazor/Explore.Blazor.csproj`** — add `Microsoft.Extensions.Http.Resilience` (server-side ONLY)
- Three profiles: interactive (15s), admin (30s), background (60s)
- `DisableForUnsafeHttpMethods()` by default
- Verify `UseCookies = false` on outbound clients

### BFF (Phase 7)

**`Explore.Blazor/Extensions/BffEndpointExtensions.cs`** — 1000+ line monolith
- Split by bounded context into route-group files
- **CRITICAL:** Use `async Task(HttpContext ctx)` pattern, NOT `async Task<IResult>` (causes silent empty responses)

---

## Important Decisions

1. **`ApiProblemException` over base class wrappers** — services stay domain-oriented; exception carries full ProblemDetails context including validation dict
2. **Boundary model for validation** — HTTP edge owns shape, application layer owns business rules
3. **Resilience server-side only** — browser `.Client` calls don't get resilience policies
4. **W3C traceparent** — OpenTelemetry propagation, not custom correlation headers
5. **Middleware order as rules** — tenant/auth order depends on whether auth is tenant-sensitive (evaluate, don't hardcode)
6. **No backward compat needed** — dev stage, rename directly, change APIs freely

---

## Items Moved to `enterprise-cleanup` Plan

- `CongfigurePersistenceServices` typo → `ConfigurePersistenceServices`
- `S3ConfigResolver.cs` line 156 missing `.Trim()` on config fallback
- `EventSeries`, `Tenant`, `Group` — mutable `ICollection<T>` → `IReadOnlyCollection<T>` (EF Core regression risk, separate concern)
- `[ProducesResponseType]` for error cases on 15+ controllers (moved to Phase 7.3 as low-priority documentation task)

---

## Quick Resume

To continue:
1. Read this file for current state and key decisions
2. Read `http-resilience-refactor-tasks.md` for what's next
3. Read `http-resilience-refactor-plan.md` for detailed task specifications
4. Start with **Phase 0** (integration tests) — safety net before behavior changes
5. Then Phases 1-3 can execute in parallel (Stream A core)
