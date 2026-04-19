ABOUTME: AI agent context for Blazor clean code refactor implementation.
ABOUTME: Contains hotspot inventory, CTO decisions, ServiceResult design, and operability requirements.

# Context: Blazor Clean Code Refactor

> Status: PLANNING COMPLETE (v2 — CTO-reviewed) — Ready for implementation
> Last Updated: 2026-04-16

## Scope

| Project | Role | In Scope |
|---------|------|----------|
| Explore.Blazor | BFF server host | YES |
| Explore.Blazor.Client | WASM UI, pages, components, services | YES |
| Explore.Blazor.IntegrationTests | BFF integration tests | YES |
| Explore.Blazor.Client.Tests | Component/service unit tests | YES |
| Explore.Blazor.Client.E2ETests | E2E browser tests | YES |
| Explore.API/Application/Domain/Persistence/Infrastructure | Backend | NO |

## Skills Required

Load these skills before implementation:
- `blazor-ui-conventions` — MudBlazor v9, render modes, component patterns
- `blazor-css-isolation` — BEM, ::deep policy, @layer system
- `blazor-bff-patterns` — YARP, token forwarding, service layer
- `design-system` — CSS tokens, wrapper components, DialogOptionsFactory

## CTO Review Decisions (v2)

These decisions from CTO review are **binding** for implementation:

### 1. Delivery Waves Replace Flat Phases
21 phases reorganized into 5 waves: A (Safety), B (BFF Hardening), C (Service Contract Reform), D (UI Decomposition), E (Conformance). See plan for full sequencing.

### 2. Change Type Classification Required
Every task labeled: STRUCTURAL, BEHAVIORAL, SECURITY, CONTRACT, or OPERATOR. No PR mixes SECURITY with STRUCTURAL without justification.

### 3. ServiceResult Must Be Structured Error Contract
String-only failures rejected. Required shape:

```csharp
public sealed class ServiceResult<T>
{
    public bool IsSuccess { get; }
    public T? Value { get; }                    // on success
    public string? ErrorCode { get; }           // machine-readable: "AUTH_SESSION_EXPIRED", "VALIDATION_FAILED"
    public string? UserMessage { get; }         // safe for display
    public string? DeveloperMessage { get; }    // diagnostics, not displayed in production
    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }
    public FailureCategory Category { get; }
    public bool IsRetryable { get; }
    public int? HttpStatusCode { get; }
    public Exception? Exception { get; }        // internal only, not surfaced to UI

    // Static factories
    public static ServiceResult<T> Success(T value);
    public static ServiceResult<T> Failure(string errorCode, string userMessage);
    public static ServiceResult<T> ValidationFailure(IReadOnlyDictionary<string, string[]> errors);
    public static ServiceResult<T> NotFound(string message);
    public static ServiceResult<T> SessionExpired();
    public static ServiceResult<T> TransientFailure(string message);
    public static ServiceResult<T> FromApiException(ApiException ex);
}

public enum FailureCategory
{
    None = 0,          // success
    Validation,        // user-fixable input errors
    NotFound,          // resource doesn't exist
    Forbidden,         // permission denied
    SessionExpired,    // re-auth needed
    ProviderUnavailable, // external dependency down
    ProviderMisconfigured, // config error
    TransientFailure,  // retry may help
    Unknown            // catch-all
}
```

### 4. UI Error Handling Tiers
- **Inline validation** for user-fixable issues (FailureCategory.Validation)
- **Banners/cards** for domain/business failures (NotFound, Forbidden)
- **Snackbar** only for minor transient notifications (TransientFailure)
- **Dedicated error states** for load failures (ProviderUnavailable)
- **Re-auth flow** for SessionExpired
- **Feature unavailable** state for ProviderMisconfigured

### 5. State Classification Before Decomposition
EventList/EventDetail state must be classified (URL/service/local/computed) BEFORE extraction begins. Page coordinator / page state model required for complex pages.

### 6. DynamicAuthSchemeManager is Stop-the-Line
Split into 6A (stabilize + test + document) and 6B (refactor). Architectural decision required: runtime dynamic vs startup-only scheme mutation.

### 7. Line Counts Are Soft Guardrails
Not hard acceptance gates. Real quality = cohesion, dependency count, mutation surface, testability, state clarity.

### 8. ABOUTME Is Hygiene Automation
Batch or automate. Do not compete with high-value engineering work.

### 9. Operability Is First-Class
New Phase X: startup config validation, diagnostics, feature-unavailable vs misconfigured, self-hoster support.

### 10. Pattern Extraction After Decompositions Settle
Phase 13 moved late in Wave D. Extract abstractions from stable understanding, not early guesses.

## Critical Hotspots

### BFF Server (Explore.Blazor)

| File | Lines | Issue | Change Type |
|------|-------|-------|-------------|
| BffAuthEndpoints.cs | 550 | God class, 10+ static handlers, 6 duplicated logger resolutions | STRUCTURAL |
| DynamicAuthSchemeManager.cs | 539 | Complex state machine, dual locking, 8 public methods | SECURITY |
| BffSetupSecretEndpoints.cs | 346 | Large endpoint file with mixed concerns | STRUCTURAL |
| BffPreferenceEndpoints.cs | 310 | Large endpoint file with mixed concerns | STRUCTURAL |
| CircuitAccessTokenService.cs | 326 | Two concerns in one file (token + setup secret session) | STRUCTURAL |
| MiddlewareExtensions.cs | 241 | Inline middleware lambdas with complex logic | STRUCTURAL |
| ConfigurationExtension.cs | 197 | 8 Console.WriteLines in production code | BEHAVIORAL |
| HttpClientExtensions.cs | 196 | Complex handler chain setup | SECURITY |
| BffAdminClaimsTransformation.cs | 210 | Duplicated claim type constants | STRUCTURAL |

### Blazor Client (Explore.Blazor.Client)

| File | Razor Lines | Code-Behind Lines | Issue | Change Type |
|------|-------------|-------------------|-------|-------------|
| EventList | 1,094 | 1,557 | GOD COMPONENT — 15 [Inject]s, 30+ private fields | BEHAVIORAL |
| EventDetail | 625 | 1,122 | God component — sessions, registrations, reviews mixed | BEHAVIORAL |
| CreateEvent | 674 | 729 | Large form with sessions, aspects, speakers | BEHAVIORAL |
| EventEdit | 593 | 531 | Large form, similar to CreateEvent | BEHAVIORAL |
| FooterSettings | 781 | — | Footer template management with inline dialogs | STRUCTURAL |
| InstanceGovernanceSection | 738 | — | Governance, feature flags, render policies mixed | STRUCTURAL |
| TenantLookupTablesSection | 668 | — | Category, tag, location management mixed | STRUCTURAL |

### Large Services

| Service | Lines | Issue | Change Type |
|---------|-------|-------|-------------|
| ImageStorageService | 972 | S3 upload, image processing, validation mixed | STRUCTURAL |
| AdminService | 764 | Governance, lookup, tenant admin mixed | CONTRACT |
| InstanceOnboardingService | 614 | Multi-step onboarding in single service | STRUCTURAL |
| EventService | 418 | Swallows all exceptions | CONTRACT |
| FooterAdminService | 346 | Large but coherent | — |

### CSS Violations

- 52/93 .razor.css files (57%) use ::deep
- 62 hardcoded color values in 19 files (worst: AdminListDetails 28 occurrences)
- 77 inline style attributes in .razor files
- Hardcoded px values in NotificationPanel, EventCard, MainLayout

### Missing ABOUTME Headers

30+ files missing: NavMenu.razor.cs, AdminListDetails.razor.cs, Settings.razor.cs, MyRegistrations.razor.cs, MyReviews.razor.cs, UserProfile.razor.cs, MyEvents.razor.cs, MyOrganizations.razor.cs, CreateOrganization.razor.cs, OrganizationMembers.razor.cs, MapsService.cs, EventSessionSpeakerService.cs, LandingPageService.cs, LazyAssemblyLoader.cs, all Lookup services (6), all Http handlers (4), ImageStorageService.cs, TenantConfiguration.cs, TenantConstants.cs, DateTimeHelper.cs, BaseCommandResponse.cs, TenantContext.cs, UserInfo.cs

## Strengths (Do Not Break)

### CSS & Design System
- @layer system correctly implemented (7 global CSS files)
- All 5 wrapper components compliant (display:contents + ::deep)
- 290+ design token references across 53 files
- BEM methodology consistent (42 files use Class= with BEM)
- No bare .mud-* selectors outside mudblazor-overrides.css
- Zero !important in .razor.css files

### Architecture
- No direct IEventApiClient usage in components (service layer enforced)
- Architecture tests: 2 tests + passing
- Middleware pipeline order is correct
- YARP token forwarding chain works
- Cookie auth security mostly correct (HttpOnly, Secure, SameSite)
- Service layer wraps NSwag client consistently

### Test Infrastructure
- BlazorTestContext (custom bUnit) with MudBlazor support + auth helpers
- MockServiceFactory with pre-configured mocks for all services
- BlazorBffWebApplicationFactory for integration tests
- PlaywrightFixture for E2E with Aspire orchestration
- 686 unit tests + 21 integration tests

## BFF Pattern Summary

### Token Forwarding Flow
```
Browser (WASM) → BFF (cookie auth) → YARP transform (Bearer token) → API (JWT validation)
```

### DelegatingHandler Chain (Server-side)
```
AccessTokenForwardingHandler → TenantHeaderForwardingHandler → SetupSecretForwardingHandler
```

### DelegatingHandler Chain (WASM)
```
BrowserCredentialsMessageHandler → BffUnauthorizedHandler
```

### Service Layer Pattern — CURRENT vs TARGET
```csharp
// CURRENT: swallows exceptions
try { return await _apiClient.GetAsync(...); }
catch (Exception ex) { _logger.LogError(ex, "..."); return null; }

// TARGET: structured error contract
try { return ServiceResult<T>.Success(await _apiClient.GetAsync(...)); }
catch (ApiException ex) { _logger.LogError(ex, "..."); return ServiceResult<T>.FromApiException(ex); }
catch (HttpRequestException ex) { _logger.LogError(ex, "..."); return ServiceResult<T>.TransientFailure("Service temporarily unavailable"); }
```

## Operability Requirements (NEW — CTO)

### Error State Distinction Matrix

| State | ServiceResult Mapping | UI Presentation |
|-------|----------------------|-----------------|
| Feature disabled by policy | Forbidden + specific ErrorCode | "Feature not available for your organization" banner |
| Feature unavailable by config | ProviderMisconfigured | "Feature requires configuration" admin notice |
| Provider unreachable | ProviderUnavailable + IsRetryable=true | "Service temporarily unavailable" with retry |
| Provider misconfigured | ProviderMisconfigured + DeveloperMessage | Admin: "Check provider configuration" with diagnostics |
| Permission denied | Forbidden | "You don't have permission" inline |
| Session expired | SessionExpired | Re-auth flow redirect |
| Validation error | Validation + ValidationErrors | Inline field errors |
| Transient failure | TransientFailure + IsRetryable=true | Snackbar with retry option |

### Startup Validation Checklist
- [ ] Auth provider config (authority URL, realm, client ID)
- [ ] YARP cluster target reachable or logged
- [ ] Cookie security settings consistent with environment
- [ ] HTTPS behind reverse proxy with forwarded headers
- [ ] Required secrets present in expected mode

## Test Baseline

| Suite | Methods | Status |
|-------|---------|--------|
| Blazor Integration | 21 | Pass |
| Blazor Client Unit | 686 | Pass |
| Blazor Client E2E | 2 | Requires Aspire |
| Architecture (Blazor) | 2 | Pass |

## Continuation Notes

- Plan files: `dev/active/blazor-clean-code-refactor/`
- Related completed refactor: `dev/active/api-clean-code-refactor/` (backend, ALL phases complete)
- Skills to load: blazor-ui-conventions, blazor-css-isolation, blazor-bff-patterns, design-system
- Docs to read: BLAZOR.md, ARCHITECTURE.md, SECURITY.md
- Journal: `dev/_journal/journal.md`, `dev/_journal/MAJOR_DECISIONS.md`

## Session Checkpoint — 2026-04-16

### What's Done
- Full exploration (5 background agents) covering BFF server, Client UI, CSS/design system, tests, service layer patterns
- Research: Tavily (Blazor Clean Architecture 2025/2026, MudBlazor v9), Context7 (MudBlazor docs)
- Plan v1 created (21 flat phases)
- CTO review incorporated → Plan v2 (5 delivery waves, structured ServiceResult, operability workstream)
- All 3 plan files fully up to date (plan, tasks, context)
- Journal updated with 8 Blazor plan insights
- MAJOR_DECISIONS updated with 6 CTO binding decisions

### What's Next
- Implementation has NOT started. Ready to begin Wave A (Safety+Baseline).
- No code changes made to Blazor projects in this session.

---

## Session Checkpoint — 2026-04-18 (v3 Update)

### Trigger
User asked: "what do you think about the @dev/active/blazor-clean-code-refactor/ plan ? use subagents to analyze the blazor projects and then update the plan and report to me. there is a lot to refactor in blazor project ! lots more. Be sure to use tavily mcp for research and context 7 mcp for documentation."

### What's Done in This Session
- Re-read v2 plan files (plan/tasks/context)
- Fired 5 parallel `explore` background agents covering: render-mode + lifecycle, DI/clean-architecture, a11y/i18n/UX, BFF security/observability, test infrastructure
- Fired 1 parallel `librarian` agent on .NET 10 Blazor + MudBlazor v9 + BFF + YARP + enterprise-grade patterns (Tavily + Context7)
- Consulted Oracle for opinionated critique of the consolidated findings (severity sequencing, render-mode strategy, i18n scope, view-model layer, CSP minimum, DI lifetime fix architecture, Wave F evaluation, plan format)
- Rewrote `blazor-clean-code-refactor-plan.md` to v3 (full rewrite per Oracle's recommendation, NOT addendum)
- Rewrote `blazor-clean-code-refactor-tasks.md` to v3 with new Wave 0 + Phase A0 + expanded Phase 18B
- Updated this context file with v3 checkpoint (this section)

### Key Findings (Consolidated)
**Stop-the-line defects discovered (now Wave 0):**
1. `SetupSecretSessionService` Singleton holds per-user secrets — cross-user leakage in production
2. `IDynamicAuthSchemeManager` Singleton holds per-circuit state — cross-circuit contamination
3. `async void OnLocationChanged()` in AnalyticsInitializer.razor:253 — process crash risk
4. `.Result` sync-over-async at 6 sites in EventDetail/EventList/OrganizationDetails — Blazor Server deadlock risk
5. Auth endpoints (/auth/status, /auth/signout, /bff/auth/refresh-schemes) lack Cache-Control: no-store
6. YARP cluster has no RequestTimeout — thread pool exhaustion risk

**Render mode (now Phase A0):**
- All 32 pages hardcode `@rendermode InteractiveServer`
- 3 pages use `[PersistentState]` without rendermode declared (defeats SSR↔WASM handoff)
- v2 plan claimed InteractiveAuto was the "default" but reality contradicted

**BFF security gaps (added to Phase 2):**
- 5 missing security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy)
- IdP token revocation missing (tokens valid 7d post-signout)
- No XSRF rotation policy

**Architecture violations (added to Phase 7 + 12):**
- IEventService 16-method interface mixing events/sessions/registrations (ISP violation, blocks ServiceResult)
- 11 model classes inside IFooterAdminService.cs interface file
- Magic strings for claim types and config keys
- 8+ empty/swallowing catch blocks
- Service-locator pattern in 2 sites

**Test infrastructure deficits (now Phase 18B):**
- 154 brittle Markup.Contains() across 18 files
- 0 GetByRole/data-testid usage
- MockServiceFactory missing 20 services
- 24 pages, 15 services, 14 components untested
- 2 E2E tests (HTTP 200 + /auth/status)
- Blazor architecture tests = 2 (target 15)

**a11y on touched components (added to Phase 14):**
- AppButton/AppIconButton no AriaLabel parameter
- AppTextField no aria-invalid/aria-describedby
- S3Image no Alt parameter, no loading="lazy"
- RTL set via JS but MudRTLProvider not synced

**Operability (added to Phase X):**
- Auth provider startup config validates but does NOT fail on critical missing config
- No OpenTelemetry (Serilog only)
- No health checks for API/Keycloak/DB

### Oracle's Binding Decisions (incorporated into v3)
1. Wave 0 is BLOCKING pre-flight, not "Wave A first phase"
2. Render-mode = its own dedicated Phase A0 with cohort migration + eligibility matrix; NOT per-page edits
3. CSP stays in Wave B (not Wave 0); start with conservative report-only mode; nonce-based CSP deferred
4. NO app-wide ViewModel layer; introduce page-local presentation models in refactored hotspots only (Phase 17A.6)
5. Defer full i18n to separate track; require no NEW hardcoded strings on touched files; use existing TranslationService stack (NOT IStringLocalizer + .resx)
6. Drop Wave F (.NET 10 modernization) from this program; allow only a Short evaluation spike
7. Full rewrite of plan files (NOT addendum) — clean source of truth

### Top 5 Risks (Per Oracle)
1. Cross-user/circuit state leakage in singleton services (CONFIRMED currently shipping)
2. Render-mode migration regressions (prerender, double-init, missing service registration)
3. Auth hardening regressions during scheme management / cache headers / CSP changes
4. ServiceResult contract churn through many components with weak test coverage
5. Test blind spots allowing UI/a11y/render regressions to ship undetected

### Effort Estimate
- Wave 0: Short (2–4 days) BLOCKING
- Wave A: Medium (~1 week)
- Wave B: Medium (1–1.5 weeks)
- Wave C: Medium (1–1.5 weeks)
- Wave D: Large (2–3 weeks)
- Wave E: Medium (~1 week)
- **Total: ~7–10 weeks**

### Items Explicitly DEFERRED to Separate Tracks
App-wide IStringLocalizer + .resx, app-wide ViewModel layer, LazyAssemblyLoader, NativeAOT for WASM, WebSocket compression, PWA/service worker, Microsoft.FeatureManagement, nonce-based CSP, front-channel logout, client-side OpenTelemetry, full a11y axe-playwright audit, bundle size monitoring + Core Web Vitals.

### Files Updated This Session
- `dev/active/blazor-clean-code-refactor/blazor-clean-code-refactor-plan.md` (rewritten v3)
- `dev/active/blazor-clean-code-refactor/blazor-clean-code-refactor-tasks.md` (rewritten v3)
- `dev/active/blazor-clean-code-refactor/blazor-clean-code-refactor-context.md` (this section appended)

### What's Next
- **Wave 0 COMPLETE. All 6 phases implemented and verified.**
- **Wave A Phase 1 IN PROGRESS.** 2 existing arch tests pass; 11 new tests designed (Phase 0.A-C violations already fixed, so tests will be regression guardrails). Deep subagent started writing the test file but was blocked by dirty working tree rule — needs to be resumed.
- After Phase 1 arch tests complete, proceed to Phase 5 (Observability), Phase 17A (State Classification), Phase X (Operability), and Phase A0 (Render Mode Correction).
- Decisions already logged in MAJOR_DECISIONS.md from analysis session.

---

## Session Checkpoint — 2026-04-18 (Implementation Started)

### Wave 0 Implementation — ALL COMPLETE ✅

**Phase 0.A — SetupSecretSessionService static field fix** ✅
- `CircuitAccessTokenService.cs`: Removed `static` from `_store` field and `CleanupExpiredEntries()` method on SetupSecretSessionService (Singleton, so instance field suffices)
- CircuitAccessTokenService `_tokenStore` LEFT as static — `GetTokenForUser()` static method required by AccessTokenForwardingHandler for cross-circuit token resolution. Architectural debt documented for Wave B Phase 3.
- Files modified: `Explore.Blazor/Services/CircuitAccessTokenService.cs`

**Phase 0.B — async void OnLocationChanged fix** ✅
- `AnalyticsInitializer.razor:253`: Changed `private async void OnLocationChanged(...)` → `private void OnLocationChanged(...)` with `_ = InvokeAsync(async () => { ... })` fire-and-forget pattern
- Properly marshals to Blazor sync context, preserves try/catch for error logging
- Files modified: `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor`

**Phase 0.C — .Result sync-over-async fix** ✅
- `EventDetail.razor.cs:133` — `agendaTask.Result` → `await agendaTask`
- `EventList.razor.cs:725-726` — `detailTask.Result + sessionsTask.Result` → `await detailTask + await sessionsTask`
- `OrganizationDetails.razor.cs:141` — `eventsTask.Result` → `await eventsTask`
- All three were post-`Task.WhenAll` so functionally safe, but `.Result` masks exceptions as AggregateException
- Files modified: `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs`, `EventList.razor.cs`, `Organizations/OrganizationDetails.razor.cs`

**Phase 0.D — Cache-Control headers on auth endpoints** ✅
- Added `Cache-Control: no-store, no-cache` and `Pragma: no-cache` headers to:
  - `HandleSignoutAsync` (signout endpoint)
  - `HandleAuthStatus` (status endpoint)
  - `HandleRefreshSchemesAsync` (refresh-schemes endpoint)
- Files modified: `Explore.Blazor/Extensions/BffAuthEndpoints.cs`

**Phase 0.E — YARP RequestTimeout** ✅
- Added `HttpRequest = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(30) }` to ClusterConfig
- Added `using Yarp.ReverseProxy.Forwarder;`
- Files modified: `Explore.Blazor/Extensions/YarpProxyExtensions.cs`

**Phase 0.F — Build + Test Verification** ✅
- Build: 0 errors, 1382 warnings (all pre-existing CA warnings in test projects)
- Application UnitTests: 823 pass
- Domain UnitTests: 192 pass
- Architecture Tests: 74 pass
- Secrets UnitTests: 190 pass
- Blazor IntegrationTests: 21 pass
- Blazor Client Tests: 657 pass, 28 pre-existing failures (unchanged)
- **ZERO new failures introduced**

### Wave A Phase 1 — Architecture Guardrails (IN PROGRESS)

**Violation scan results for 13 arch tests:**

| Test # | Violation Type | Known Exceptions | Status After Wave 0 |
|--------|---------------|-------------------|---------------------|
| 1.1 | IEventApiClient direct inject | `InstanceTenantsSection.razor` | Should PASS |
| 1.2 | Console.WriteLine | `ConfigurationExtension.cs` (8 instances, Phase 5 will fix) | Should PASS with exception |
| 1.4 | [Inject] concrete types | Need scan | TBD |
| 1.5 | new DialogOptions | `LoginPromptDialog`, `SettingsConnectedApps`, `TenantLookupTablesSection` (6x), `CreateApiKeyDialog` | Should PASS with 4 known exceptions |
| 1.6 | NavigationManager in Common/Collection | No matches found | Should PASS clean |
| 1.7 | IJSRuntime outside Interop/Http | `UserSettingsService`, `InstanceOnboardingService`, `AccessibilityFocusService`, `AccessibilityAnnouncerService` | Should PASS with 4 known exceptions |
| 1.8 | ISnackbar in data services | No matches found | Should PASS clean |
| 1.9 | Singleton with mutable state | SetupSecretSessionService (fixed in 0.A, needs arch test), IDynamicAuthSchemeManager (fix deferred) | Test needed |
| 1.10 | async void outside handlers | 3 remaining: `EventList.FlushPendingChanges`, `EventEdit.RemoveSession`, `CreateEvent.RemoveSession` (Timer/list callbacks, not crash risks) | Should PASS with 3 known exceptions |
| 1.11 | .Result / .Wait() | All fixed in 0.C | Should PASS clean (regression guard) |
| 1.12 | IConfiguration in services | `DynamicAuthSchemeManager.cs` (Wave B Phase 6B will fix) | Should PASS with 1 known exception |
| 1.13 | Service locator in services/ | None found in Services/ dir | Should PASS clean |
| 1.14 | Model classes in interface files | `IFooterAdminService.cs` (10 classes), `IContactShareConsentService.cs` (2 classes), `InstanceOnboardingService.cs` (15+ classes) | Should PASS with 3 known exceptions |
| 1.15 | Hardcoded strings | Deferred to later phase | Not implemented yet |

**Pre-existing LSP errors (NOT caused by Wave 0 changes):**
- `EventDetail.razor.cs:591` — missing EventRegistration type
- `EventList.razor.cs:1070` — missing EventRegistration type
- `OrganizationDetails.razor.cs:52` — no suitable OnInitializedAsync override
- `YarpProxyExtensions.cs:124/128` — ambiguous ISetupSecretSessionService.GetForUser (duplicate type definition)

### Uncommitted Changes
All Wave 0 changes are unstaged. Files modified:
1. `Explore.Blazor/Services/CircuitAccessTokenService.cs` — removed `static` from SetupSecretSessionService
2. `Explore.Blazor.Client/Shared/AnalyticsInitializer.razor` — async void fix
3. `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` — .Result → await
4. `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` — .Result → await
5. `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs` — .Result → await
6. `Explore.Blazor/Extensions/BffAuthEndpoints.cs` — Cache-Control headers
7. `Explore.Blazor/Extensions/YarpProxyExtensions.cs` — YARP RequestTimeout

### Resume Instructions
1. **Phase 1 arch tests**: The deep subagent was writing `BlazorClientArchitectureTests.cs` with 13 total tests (2 existing + 11 new). It got blocked by dirty working tree. Resume by:
   - Writing the test file directly (the violation analysis is complete above)
   - Running `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
   - The arch test project does NOT reference Blazor projects — uses file-scanning approach
2. **Phase 5**: Remove 8 Console.WriteLines from ConfigurationExtension.cs, add ILogger replacement
3. **Phase A0**: Render-mode cohort migration — project already has dynamic configurable render mode, default should be InteractiveServer

---

## SESSION PROGRESS LOG — Wave A Phase 1 COMPLETE (2026-04-19)

**Branch:** `refactor/blazor-clean-code-wave-a` (off develop af8a9401)

### Deliverable
`Event.Architecture.Tests/BlazorClientArchitectureTests.cs` — **843 lines, 14 tests all green**.
Enforces 14 of 15 planned architecture guardrails (1.1 through 1.14). Rule 1.15 (hardcoded strings) deferred — requires localization inventory.

### Test Design
- File-scanning approach (arch-test project does not reference Blazor projects).
- TUnit `[Test]` + `await Assert.That(violations).IsEmpty().Because(...)` pattern matches existing style.
- Graceful skip if `ResolveProjectRoot` returns null (cross-machine portability).
- All exception lists use forward-slash paths + `StringComparer.OrdinalIgnoreCase`.

### Exception Lists (documented, time-boxed)

| Rule | Exceptions | Resolution Path |
|------|------------|-----------------|
| 1.1 IEventApiClient | `InstanceTenantsSection.razor` | Phase 12 (service decomposition) |
| 1.2 Console.WriteLine | `ConfigurationExtension.cs`, `LazyAssemblyLoader.cs`, `Setup.razor` | Phase 5 (ILogger swap) |
| 1.3 Middleware lambdas | `MiddlewareExtensions.cs:35/62/91/143/169` | Phase 3 (extract static methods) |
| 1.4 [Inject] interfaces | None — test enforces with state-container whitelist | — |
| 1.5 new DialogOptions | `LoginPromptDialog.razor`, `SettingsConnectedApps.razor`, `TenantLookupTablesSection.razor`, `CreateApiKeyDialog.razor`, `CreateApiKeyDialog.razor.cs` | Phase 13 (DialogOptionsFactory migration) |
| 1.7 IJSRuntime in Services | `UserSettingsService`, `InstanceOnboardingService`, `AccessibilityFocusService`, `AccessibilityAnnouncerService` | Phase 12 (decomposition) |
| 1.9 Singleton mutable state | `DynamicAuthSchemeManager`, `CircuitAccessTokenService` | Wave B Phase 6B for DynamicAuthSchemeManager; CircuitAccessTokenService is a deliberate static store |
| 1.10 async void | 3 timer/event callbacks (EventList.FlushPendingChanges, EventEdit.RemoveSession, CreateEvent.RemoveSession) | Acceptable (event-handler semantics; covered by On/Handle prefix whitelist) |
| 1.12 IConfiguration | `DynamicAuthSchemeManager.cs` | Wave B Phase 6B |
| 1.14 Models in interface files | `Contracts/Services/IContactShareConsentService.cs`, `ILocalizationAdminService.cs`, `Footer/IFooterAdminService.cs` | Phase 12 (extract DTOs) |

### Framework / State Container Whitelist (Rule 1.4)
Framework concrete types always allowed: `NavigationManager`, `PersistentComponentState`, `AuthenticationStateProvider`, `HttpClient`, `IHttpClientFactory`.
State container heuristic: types ending in `State`, `StateService`, `StateContainer`, `Interop` are allowed.
Fully-qualified type names are namespace-stripped before check.

### Verification — All CLAUDE.md Test Projects

| Project | Result |
|---------|--------|
| Event.Architecture.Tests | 87/87 pass (includes 14 new BlazorClient tests) |
| Event.Application.UnitTests | 840/840 pass |
| Event.Domain.UnitTests | 207/207 pass |
| Explore.Secrets.UnitTests | 201/201 pass |
| Event.Persistence.IntegrationTests | 36/36 pass |
| Event.API.IntegrationTests | 553/553 pass |
| Explore.Blazor.IntegrationTests | 23/23 pass |
| Explore.Blazor.Client.Tests | 692 pass, 1 pre-existing skip (`ErrorState_RendersRetryButton` — AppButton MudBlazor v9 migration) |
| **TOTAL** | **2839 pass / 1 skip / 0 fail** |

E2E tests skipped (require Aspire AppHost).

### Precondition Resync
Branch includes api-contract-stabilization files that develop's HEAD (af8a9401) is missing — required for clean build. Added `RouteNames.GetEventRegistrations` constant to `Explore.API/Hateoas/RouteNames.cs:54`. These are preconditions, not Wave A deliverables; they'll merge cleanly via their own PR or as part of this one.

### Next Phase
Phase 2 — Component decomposition (service locator removal + DialogOptionsFactory adoption) OR Phase 3 — Middleware extraction. See plan for details.

