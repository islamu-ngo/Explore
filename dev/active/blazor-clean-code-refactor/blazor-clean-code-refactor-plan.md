ABOUTME: Strategic enterprise-scale Blazor refactor plan v3 for BFF host, WASM client, services, components, CSS, and test infrastructure.
ABOUTME: Six-wave delivery with mandatory pre-flight Wave 0 (stop-the-line defects), oracle-reviewed scope cuts, and explicit deferral track.

# Plan: Blazor Clean Code Refactor Program (Enterprise-Grade) — v3

> Last Updated: 2026-04-18
> v2 → v3 Review: Oracle-reviewed. Scope tightened, Wave 0 added, app-wide i18n & view-model layer & .NET 10 platform modernization moved out of program.

---

## Changed from v2 (READ FIRST)

v2 (CTO-approved 2026-04-16) is structurally sound but missed several stop-the-line defects discovered during deeper analysis. v3 incorporates findings from five parallel exploration agents (render-mode/lifecycle, DI/clean-architecture, a11y/i18n/UX, BFF security/observability, test infrastructure) and a librarian research pass on .NET 10 / MudBlazor v9 / BFF best practices.

**Material changes:**

1. **NEW Wave 0 — Pre-Flight Hotfix Track (BLOCKING)**: Stop-the-line defects that can invalidate every later test result. Must merge before any Wave A work begins. Treated as isolated hotfix PRs, not refactor work.
2. **NEW Phase A0 — Render Mode Correction (Wave A)**: Dedicated cohort-based migration of 32 pages from hardcoded `InteractiveServer` to declared `InteractiveAuto` (the documented default). Eligibility matrix gates per-page migration. Page-by-page edits are forbidden.
3. **EXPANDED Wave B Security**: Added security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy), `Cache-Control: no-store` on auth endpoints, IdP token revocation on logout, YARP `RequestTimeout`, XSRF rotation policy decision, tenant-aware logging scope.
4. **EXPANDED Wave A Observability**: Auth provider startup config validation must FAIL on critical misconfig (currently only logs warnings).
5. **EXPANDED Phase X**: Added health checks for API/Keycloak/DB dependencies; OpenTelemetry minimum (ActivitySource for auth/token-refresh/yarp; HTTP client + ASP.NET Core instrumentation; OTLP exporter).
6. **TIGHTENED Wave D**: Page-local presentation state (NOT a global ViewModel layer); IEventService split is mandatory inside Phase 7/12 because the ISP violation actively blocks ServiceResult conversion.
7. **EXPANDED Phase 18 + new Wave E phase**: bUnit `data-testid` migration off 154 brittle `Markup.Contains()` assertions; MockServiceFactory closure (20 missing services); 8 critical E2E user journey scenarios; Blazor architecture-test suite expansion (currently zero).
8. **EXPANDED Phase 14**: a11y compliance for touched components only — aria-label parameters on AppButton/AppIconButton, aria-invalid/aria-describedby on AppTextField, focus-trap audit on dialogs, alt parameter on S3Image, `loading="lazy"` on EventCard image, RTL/MudRTLProvider sync, `[dir="rtl"]` selectors in CSS for touched files.
9. **EXPLICITLY DEFERRED — Separate Modernization Track (NOT in this program)**: app-wide IStringLocalizer + .resx migration; full app-wide ViewModel layer; LazyAssemblyLoader; NativeAOT for WASM; WebSocket compression for Blazor Server; PWA/service worker; Microsoft.FeatureManagement adoption; nonce-based CSP. Documented in "Deferred Work — Separate Tracks" section.
10. **REWRITTEN Risk Register**: Top 5 risks now lead with cross-user/circuit state leakage, render-mode migration regressions, auth hardening regressions, ServiceResult contract churn, and test blind spots.

The CTO-approved v2 wave skeleton (A through E), change-type classification, soft size guardrails, ServiceResult shape, error-state distinction, page coordinator pattern, 6A/6B split, and acceptance gates are **preserved unchanged**. v3 only ADDS Wave 0 + Phase A0 and EXPANDS specific phases.

---

## Executive Summary

Full-scale Blazor refactor program for the Event repository. Six delivery waves (0 + A through E). Wave 0 is a blocking pre-flight hotfix track (2–4 days). Waves A–E execute the strategic refactor (~7–10 weeks). All app-wide platform modernization (i18n, ViewModels, NativeAOT, PWA) is excluded from this program and tracked separately to avoid scope creep.

**Scope:**
- `Explore.Blazor` (BFF server host)
- `Explore.Blazor.Client` (WASM UI, pages, components, services)
- `Explore.Blazor.IntegrationTests`
- `Explore.Blazor.Client.Tests`
- `Explore.Blazor.Client.E2ETests`

**Explicitly out of scope:**
- `Explore.API`, `Explore.Application`, `Explore.Domain`, `Explore.Persistence`, `Explore.Infrastructure` (covered by api-clean-code-refactor)
- Backend API contract changes
- New feature implementation
- App-wide i18n / ViewModel layer / .NET 10 platform modernization (separate tracks)

This plan covers:
- BFF server architecture and security hardening (headers, CSP-MVP, token revocation, YARP timeouts, OTel)
- Render-mode correction (32 pages from InteractiveServer to InteractiveAuto via cohorts)
- Operability and self-hoster diagnostics (CTO requirement)
- Component decomposition with explicit page-state classification
- Structured service error contract (ServiceResult<T>) with IEventService ISP split
- CSS architecture compliance, design token adoption, a11y on touched components
- Test coverage expansion (data-testid migration, MockServiceFactory closure, E2E journeys)

The plan does **not** assume backward compatibility. This is development mode. Every phase item is classified by change type for review discipline.

---

## Change Type Classification (CTO Requirement)

Every task is labeled with one of:

| Label | Meaning | Review Discipline |
|-------|---------|-------------------|
| **STRUCTURAL** | File splits, code moves, extraction — no behavior change | Standard review |
| **BEHAVIORAL** | Changes error handling, state flow, data propagation | Before/after behavior matrix required |
| **SECURITY** | Auth, CSRF, cookies, tokens, headers | Dedicated security review, smoke test mandatory |
| **CONTRACT** | Service interfaces, component parameters, public API | Consuming code updated in same PR |
| **OPERATOR** | Config validation, diagnostics, deployment behavior | Self-hoster impact assessment |
| **HOTFIX** *(new in v3)* | Wave 0 stop-the-line defect | Smallest possible patch, isolated PR, regression test required |

**Non-negotiable rule**: No PR may mix STRUCTURAL cleanup with SECURITY or BEHAVIORAL change unless explicitly justified and documented. Wave 0 HOTFIX PRs are single-defect by definition.

---

## Program Goals

By end of program, the Blazor codebase has these characteristics:

1. **No god components.** Soft guardrail: .razor ~300 lines, .razor.cs ~200 lines. Violations require justification based on cohesion, not automatic rejection.
2. **BFF security is enterprise-grade.** Security headers, CSRF tokens, cookie policy, token forwarding, IdP logout, YARP timeouts, rate limiting are correct and documented.
3. **Service layer uses structured error contract.** `ServiceResult<T>` with error codes, failure categories, retryability hints — not just strings. `IEventService` split per Interface Segregation Principle.
4. **Render mode is intentional.** Every page declares its render mode; `InteractiveAuto` is the documented default; `InteractiveServer` only on the eligibility matrix.
5. **CSS is fully compliant.** All scoped styles follow BEM, design tokens are used, no hardcoded colors/spacing on touched files; a11y compliance on touched components.
6. **Components are composable and testable.** Shared patterns extracted, dependency counts ≤8, explicit page-state classification.
7. **Test coverage is meaningful.** Critical paths have unit tests, workflow E2E scenarios exist, architecture tests catch regressions, brittle Markup.Contains() assertions migrated to data-testid.
8. **ABOUTME headers on every file.** Automated or batched.
9. **BFF endpoint handlers are decomposed by capability module.** Auth status vs auth mutation vs setup vs preferences.
10. **State management is explicit and classified before decomposition.** URL for filters/pagination, services for domain state, cascading for UI, page coordinator for complex pages. Page-local presentation models on refactored hotspots only.
11. **Self-hosters get meaningful diagnostics.** Startup validation FAILS on critical misconfig, configuration error clarity, correlation-aware error surfaces, feature-unavailable vs misconfigured distinction, dependency health checks.
12. **OpenTelemetry minimum present.** ActivitySource on auth/token-refresh/YARP; HTTP+AspNetCore instrumentation; OTLP exporter; tenant-aware log scope.

---

## Non-Negotiable Constraints

- **InteractiveAuto** is the default render mode. `InteractiveServer` only on the documented eligibility matrix (onboarding/setup/admin pages requiring HttpContext)
- **No HttpContext** in InteractiveAuto/WASM components
- **Service layer wraps NSwag client** — components never inject IEventApiClient directly
- **BEM naming** in all .razor.css files
- **::deep only** for third-party (MudBlazor) internals — always wrapped in BEM block
- **@layer system** is authoritative: `reset → base → tokens → mudblazor-overrides → components → utilities`
- **Wrapper components** (AppButton, AppCard, AppTextField, AppIconButton, AppDialogShell) are the preferred MudBlazor surface
- **DialogOptionsFactory** presets only — never create `DialogOptions` manually
- **ABOUTME** two-line header on every file
- **File-scoped namespaces** for new C# files
- **BFF boundary**: browser never sees tokens; YARP proxies API calls
- **XSRF-TOKEN** distributed via cookie (intentionally NOT HttpOnly — double-submit pattern), validated via `X-CSRF-TOKEN` header
- Middleware pipeline order is **CRITICAL** — do not rearrange without understanding
- **No NavigationManager** in lower-level shared components unless justified
- **No IJSRuntime** in domain-ish service classes unless explicitly boundary-oriented
- **No snackbar/UI notifications** directly inside data services (use ServiceResult, let UI decide presentation)
- **No singleton service may hold per-user, per-circuit, or per-request state** *(new in v3)*
- **No `async void`** anywhere except UI event handlers; NavigationManager subscriptions use `LocationChanged += handler` with `async Task` wrapper *(new in v3)*
- **No `.Result` / `.Wait()`** on Task in any Blazor Server / Razor code path *(new in v3)*
- **JS interop calls must use `InvokeAsync` with explicit timeout** (`TimeSpan` overload) *(new in v3)*
- **No new hardcoded English strings** in touched files. Existing localization stack (TranslationService / TMS / JSON bundles per `docs/LOCALIZATION.md`) is the only sanctioned mechanism. `IStringLocalizer` + `.resx` are NOT introduced in this program. *(new in v3)*

---

## Size Guardrails (Soft — CTO Clarification)

These are **warning thresholds**, not hard acceptance gates. Violations require justification.

| Target | Soft Limit | Real Quality Signals |
|--------|-----------|---------------------|
| .razor file | ~300 lines | Cohesion, single render responsibility |
| .razor.cs code-behind | ~200 lines | Dependency count ≤8, state clarity, mutation surface |
| Service class | ~300 lines | Single domain boundary, testability |
| BFF endpoint file | ~150 lines | Capability module boundary |

A 340-line orchestrator with clear state model and 4 dependencies is better than a 180-line component with 12 injections and tangled state.

---

## Baseline Metrics (Pre-Refactor — v3 Updated)

### Explore.Blazor (BFF Server)
| Metric | Value |
|--------|-------|
| Total C# files | 28 |
| Files >300 lines | 4 (BffAuthEndpoints 550, DynamicAuthSchemeManager 539, BffSetupSecretEndpoints 346, CircuitAccessTokenService 326) |
| Console.WriteLine instances | 8 (ConfigurationExtension.cs) |
| RequestServices.GetRequiredService calls | 36 |
| Inline middleware lambdas | 5+ (MiddlewareExtensions.cs) |
| Setup secret duplication | 2 locations (YARP + DelegatingHandler) |
| **Singleton services holding per-user/per-circuit state** *(v3)* | **2 — SetupSecretSessionService, IDynamicAuthSchemeManager** |
| **Security headers missing** *(v3)* | **5 — CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy, Permissions-Policy** |
| **YARP cluster RequestTimeout** *(v3)* | **None — thread pool exhaustion risk** |
| **Auth endpoints with Cache-Control: no-store** *(v3)* | **0 of /auth/status, /auth/signout, /bff/auth/refresh-schemes** |
| **IdP token revocation on logout** *(v3)* | **No — tokens valid 7d post-signout** |
| **OpenTelemetry presence** *(v3)* | **Serilog only — no ActivitySource, no metrics** |

### Explore.Blazor.Client (UI)
| Metric | Value |
|--------|-------|
| .razor files >300 lines | 11 (EventList 1094, FooterSettings 781, InstanceGovernanceSection 738, CreateEvent 674, TenantLookupTablesSection 668, EventDetail 625, EventEdit 593, etc.) |
| .razor.cs files >200 lines | 11 (EventList 1557, EventDetail 1122, CreateEvent 729, EventEdit 531, NavMenu 307, etc.) |
| Services >300 lines | 6 (ImageStorageService 972, AdminService 764, InstanceOnboardingService 614, EventService 418, FooterAdminService 346, GroupService 285) |
| Missing ABOUTME headers | 30+ files |
| CSS files using ::deep | 52 (57% of 93 .razor.css files) |
| Inline style attributes | 77 |
| Hardcoded color values | 62 across 19 files |
| Direct IEventApiClient in components | 0 (good) |
| Max component [Inject] count | 15 (EventList) |
| **Pages with hardcoded `@rendermode InteractiveServer`** *(v3)* | **32 of 32 — no `InteractiveAuto` anywhere** |
| **Pages using `[PersistentState]` without rendermode declaration** *(v3)* | **3 (Home, HomeStart, OrganizationReviews) — defeats SSR↔WASM handoff** |
| **`async void` non-event-handler instances** *(v3)* | **1 critical (AnalyticsInitializer.razor:253 OnLocationChanged)** |
| **`.Result` sync-over-async sites** *(v3)* | **6 (EventDetail.razor.cs:133,725,726; EventList.razor.cs:725,726; OrganizationDetails.razor.cs:141)** |
| **Empty/swallowing catch blocks** *(v3)* | **8+ (EventService, UserSettingsService, Program.cs)** |
| **Service interfaces violating ISP** *(v3)* | **3 — IEventService (16 methods), LookupCacheService (9 deps), ImageStorageService (12+ methods)** |
| **Foreach loops missing `@key`** *(v3)* | **6+ (EventSeriesRail, EventTimeline, EventFilterBar, OrganizationReviews Virtualize)** |
| **JS interop without explicit timeout** *(v3)* | **11+ sites (LanguageProvider, Setup, LoginPromptDialog)** |
| **AppButton aria-label parameter** *(v3)* | **Missing** |
| **AppTextField aria-invalid/aria-describedby** *(v3)* | **Missing** |
| **EventCard `<MudImage>` alt + loading="lazy"** *(v3)* | **Missing** |

### User-Visible Behavioral Baselines
These flows must work identically before/after refactor:

| Flow | Entry Point | Key Behavior |
|------|-------------|-------------|
| Auth login/logout | /auth/login, /auth/logout | Cookie set/cleared, redirect to IdP, return to app, IdP session terminated *(v3)* |
| Provider discovery | /auth/providers | Dynamic scheme list, Keycloak/Google/Atproto |
| Event list browse/filter | /events | Infinite scroll, tag/category filters, search |
| Event detail + registration | /events/{id} | Session list, registration intent, approval flow |
| Admin settings save | /admin/governance | Setting toggle, save, confirmation |
| Setup onboarding | /setup | Multi-step wizard, secret validation, tenant creation |

### Test Coverage
| Suite | Methods | Status |
|-------|---------|--------|
| Blazor Integration | 21 | All pass |
| Blazor Client Unit | 686 | All pass |
| Blazor Client E2E | 2 | Requires Aspire |
| Architecture (Blazor) | 2 | All pass |
| Total | 711 | |

| Test Quality Metric *(v3)* | Value |
|----------------------------|-------|
| Pages with zero tests | 24 |
| Services with zero tests | 15 |
| Components with zero tests | 14 |
| Brittle `Markup.Contains()` assertions | 154 across 18 files |
| `GetByRole` / `GetByTestId` / `data-testid` usage | 0 |
| MockServiceFactory missing services | 20 |
| E2E user-journey tests | 2 (HTTP 200 + /auth/status anonymous) |
| Blazor architecture tests | 2 (target: 10+) |
| Accessibility tests | 1 file |
| Performance/load tests | 0 |

### CSS & Design System
| Metric | Value |
|--------|-------|
| Global CSS files (@layer) | 7 — all compliant |
| Wrapper components | 5 — all compliant |
| Design token references | 290+ |
| BEM compliance | High (42 files use Class= with BEM) |
| Hardcoded px values | Multiple files (NotificationPanel, EventCard, MainLayout) |

---

## Delivery Waves

### Wave 0 — Pre-Flight Hotfix Track *(NEW in v3 — BLOCKING)*

**Goal**: Eliminate stop-the-line defects that can invalidate every later test result and pollute production data. Treated as isolated hotfix PRs, NOT refactor work. **Must merge to main before any Wave A work begins.**

**Effort**: Short (2–4 days)

Phases: 0.A (DI lifetime hotfix), 0.B (async void crash hotfix), 0.C (.Result removal), 0.D (auth Cache-Control), 0.E (YARP RequestTimeout), 0.F (Wave 0 regression test pack)

---

### Wave A — Safety, Fitness Functions & Render Mode Correction

**Goal**: Protect correctness, establish guardrails, clean logging, validate operability, standardize render mode by cohort.

Phases: 0 (Baseline), 1 (Guardrails — expanded with v3 arch tests), 5 (Observability Hygiene — expanded with startup validation FAIL), 17A (State Classification), X (Operability — expanded with health checks + OTel minimum), **A0 (Render Mode Correction — NEW)**

**Effort**: Medium (~1 week)

---

### Wave B — BFF Hardening

**Goal**: Remove structural BFF risk, harden security, rationalize claims/clients.

Phases: 2 (Security — expanded with headers, CSP-MVP, token revocation, XSRF rotation), 3 (Endpoint Decomposition), 4 (Middleware Extraction), 16 (Claims/HttpClient), 6A (Auth Stabilization only)

**Effort**: Medium (1–1.5 weeks)

---

### Wave C — Service Contract Reform

**Goal**: Fix service error semantics, decompose where contract changes are needed, split IEventService per ISP.

Phases: 7 (ServiceResult — includes IEventService split), 12-partial (Service Decomposition where needed for Phase 7)

**Effort**: Medium (1–1.5 weeks)

---

### Wave D — UI Decomposition

**Goal**: Attack biggest component hotspots with explicit page-state model. Page-local presentation state only — no app-wide ViewModel layer.

Phases: 8 (EventList), 9 (EventDetail), 10 (Create/Edit), 11 (Admin), 12-rest (remaining service splits), 13 (Pattern Extraction)

**Effort**: Large (2–3 weeks)

---

### Wave E — Conformance, Test Hardening & Operability

**Goal**: Polish, standardize, expand tests, document, hand off. Includes data-testid migration, MockServiceFactory closure, E2E journey scenarios, a11y on touched components.

Phases: 6B (Auth Refactor), 14 (CSS + a11y on touched components), 15 (ABOUTME), 17B (State Standardization), 18 (Tests — expanded), **18B (Test Migration — data-testid + MockServiceFactory + E2E journeys, NEW)**, 19 (Conformance), 20 (Handoff)

**Effort**: Medium (~1 week)

---

## Phase Definitions

### Wave 0 Phases (NEW — BLOCKING PRE-FLIGHT)

#### Phase 0.A: Singleton-Holding-Per-User-State Hotfix
**Wave**: 0 | **Change Type**: HOTFIX (SECURITY)

**Defects:**
- `SetupSecretSessionService` registered Singleton in `Explore.Blazor/Extensions/ServiceRegistrationExtensions.cs:51-52` but holds per-user secrets. **Cross-user secret leakage.**
- `IDynamicAuthSchemeManager` registered Singleton in `Explore.Blazor/Extensions/AuthenticationExtensions.cs:67` but maintains per-circuit state (`HashSet _registeredSchemes`). **Cross-circuit state contamination.**

**Tasks:**
0.A.1. **SetupSecretSessionService**: Change DI lifetime to `Scoped`. Verify all consumers tolerate scoped resolution. Add a regression test that two simultaneous user sessions cannot read each other's secret.
0.A.2. **IDynamicAuthSchemeManager**: Split into a global `IAuthSchemeRegistry` (true singleton, immutable scheme catalog) and a scoped/per-circuit `IAuthSchemeContext`. The Singleton retains only the global scheme catalog and registration coordinator; per-circuit transient state moves to the scoped service. If runtime mutation cannot be made stateless quickly, **freeze runtime provider refresh and require app restart for provider changes** (escalation per Oracle).
0.A.3. Add architecture test: Singleton-registered services must not hold mutable user/circuit/request state (heuristic: no `Dictionary<,>`/`HashSet<>`/`List<>` instance fields except for app-startup-only registries).

**Acceptance**: Both services registered at correct lifetime. Two concurrent sessions verified to be isolated via integration test. Architecture test passing.

---

#### Phase 0.B: async void Crash Path Hotfix
**Wave**: 0 | **Change Type**: HOTFIX (BEHAVIORAL)

**Defects:**
- `AnalyticsInitializer.razor:253`: `async void OnLocationChanged()` subscribed to `NavigationManager.LocationChanged`. Any unhandled exception in the navigation handler crashes the host process (Blazor Server) or terminates the WASM runtime.

**Tasks:**
0.B.1. Convert `async void OnLocationChanged` to `async Task` wrapped in a fire-and-forget pattern with try/catch and ILogger.LogError on the catch — OR convert to a synchronous handler that posts to a queue if the work is non-trivial.
0.B.2. Audit `Explore.Blazor.Client` for any other non-event-handler `async void`. Add architecture test forbidding `async void` outside `[EventHandler]`-attributed methods or methods named `OnClick`/`OnChange`/`OnInput`-style event delegates.
0.B.3. Verify NavigationManager subscription is properly disposed in `IDisposable.Dispose()`.

**Acceptance**: Zero `async void` outside whitelisted event-handler patterns. Architecture test passing. Manual nav-during-error smoke test does not crash.

---

#### Phase 0.C: .Result Sync-Over-Async Hotfix
**Wave**: 0 | **Change Type**: HOTFIX (BEHAVIORAL)

**Defects (Blazor Server deadlock risk):**
- `EventDetail.razor.cs:133` — `agendaTask.Result`
- `EventDetail.razor.cs:725-726` — `detailTask.Result`, `sessionsTask.Result`
- `EventList.razor.cs:725-726` — same pattern
- `OrganizationDetails.razor.cs:141` — `eventsTask.Result`

**Tasks:**
0.C.1. Replace each `.Result` with `await` (the enclosing methods are already async; verify and propagate if needed).
0.C.2. Add architecture test forbidding `.Result` and `.Wait()` on Task within `Explore.Blazor` and `Explore.Blazor.Client` (whitelisting only `Program.cs` startup if absolutely required).

**Acceptance**: Zero `.Result` / `.Wait()` in Blazor projects. Architecture test passing.

---

#### Phase 0.D: Auth Endpoints Cache-Control Hotfix
**Wave**: 0 | **Change Type**: HOTFIX (SECURITY)

**Defects:**
- `/auth/status`, `/auth/signout`, `/bff/auth/refresh-schemes` lack `Cache-Control: no-store, no-cache, must-revalidate, private`. Browser/proxy may cache an authenticated response and leak it to a subsequent unauthenticated request.

**Tasks:**
0.D.1. Add response header middleware/filter that forces `Cache-Control: no-store, no-cache, must-revalidate, private` and `Pragma: no-cache` on all `/auth/*` and `/bff/auth/*` endpoints.
0.D.2. Add integration test asserting headers present on each endpoint.

**Acceptance**: Headers present on all auth endpoints. Test passing.

---

#### Phase 0.E: YARP Cluster RequestTimeout Hotfix
**Wave**: 0 | **Change Type**: HOTFIX (SECURITY + OPERATOR)

**Defects:**
- `YarpProxyExtensions.cs:41-55` ClusterConfig has no `RequestTimeout`. A hung backend response holds the upstream connection and thread until OS timeouts, exhausting Kestrel under load.

**Tasks:**
0.E.1. Add `HttpRequest.Timeout = TimeSpan.FromSeconds(30)` (start generous, tune later — see Phase 2.4) to ClusterConfig.HttpRequest.
0.E.2. Document the rationale in `docs/BLAZOR.md` BFF section.

**Acceptance**: Timeout configured. Smoke test verifies a slow backend cancels at the configured timeout.

---

#### Phase 0.F: Wave 0 Regression Test Pack
**Wave**: 0 | **Change Type**: HOTFIX (STRUCTURAL)

**Tasks:**
0.F.1. Bundle all hotfix-specific tests into a single tagged test category (`[Trait("Category", "Wave0Regression")]`) so they can be run as a fast smoke check.
0.F.2. Run `dotnet test` for all 9 Blazor test projects and confirm zero new failures introduced by hotfixes.
0.F.3. Document the Wave 0 hotfix log in `dev/_journal/MAJOR_DECISIONS.md`.

**Acceptance**: All Wave 0 tests green. Full test baseline preserved (no new failures). Journal entry written.

---

### Wave A Phases

#### Phase 0: Safety Baseline & Isolation
**Wave**: A | **Change Type**: STRUCTURAL

**Tasks:**
0.1. Build verification: `dotnet build --configuration Release --verbosity quiet`
0.2. Run all Blazor test suites individually, document baseline pass/fail counts
0.3. Create git branch `refactor/blazor-clean-code` (rebased on top of Wave 0 hotfixes)
0.4. Document any pre-existing test failures (separate from refactor)
0.5. **Capture user-visible behavioral baselines**: Execute manual smoke check of auth login/logout, event list browsing/filtering, event detail + registration flow, admin settings save. Document current behavior for comparison.

**Acceptance**: Build green, test baseline documented (post-Wave 0), behavioral baselines captured, branch created.

---

#### Phase 1: Architecture Guardrails (EXPANDED in v3)
**Wave**: A | **Change Type**: STRUCTURAL

**Tasks:**
1.1. Add arch test: No component (.razor.cs) injects IEventApiClient directly (already exists — verify)
1.2. Add arch test: No Console.WriteLine in Explore.Blazor or Explore.Blazor.Client production code
1.3. Add arch test: No inline `app.Use(async (ctx, next) => { ... })` lambdas >5 lines in Explore.Blazor
1.4. Add arch test: All [Inject] services use interfaces (not concrete types)
1.5. Add arch test: No `new DialogOptions()` — must use DialogOptionsFactory
1.6. Add arch test: No direct `NavigationManager` in `Components/Common/` or `Components/Collection/` (shared components)
1.7. Add arch test: No `IJSRuntime` in service classes under `Services/` unless explicitly in `Services/Interop/` or `Services/Http/`
1.8. Add arch test: No snackbar injection (`ISnackbar`) in data service classes
1.9. *(v3)* Add arch test: No singleton-registered service holds mutable user/circuit/request state (heuristic from Phase 0.A.3)
1.10. *(v3)* Add arch test: No `async void` outside whitelisted event-handler methods
1.11. *(v3)* Add arch test: No `.Result` / `.Wait()` on Task in Blazor projects
1.12. *(v3)* Add arch test: No `IConfiguration` directly injected in components/services (must go through `IOptions<T>` / `IOptionsSnapshot<T>`)
1.13. *(v3)* Add arch test: Service-locator pattern — no `GetRequiredService<T>()` calls inside callback/lambda bodies (whitelisted only in DI factory delegates and middleware composition)
1.14. *(v3)* Add arch test: Model/view-model classes are not declared in `*Service.cs` interface files (e.g., `IFooterAdminService.cs`); they live in `Models/` or `Contracts/`
1.15. *(v3)* Add arch test: No new hardcoded user-facing string literals in `.razor` files outside the `Pages/` policy/legal pages — strings must go through the existing translation stack

**Reframed**: ABOUTME header checks are governance automation, not architectural tests. Handle via scripting/CI if possible, not arch test suite.

**Acceptance**: All new arch tests written and passing (some may initially fail — that's the point; failures get tracked into existing phases).

---

#### Phase 5: Observability Hygiene (EXPANDED in v3)
**Wave**: A | **Change Type**: BEHAVIORAL + OPERATOR

**Tasks:**
5.1. Remove 8 Console.WriteLines from ConfigurationExtension.cs. Replace with structured `ILogger` calls or post-build configuration validator.
5.2. Audit all `ILogger` usage in BFF for proper severity semantics.
5.3. Verify correlation ID propagation works through BFF → YARP → API.
5.4. Add consistent event IDs or log categories where practical for BFF log filtering.
5.5. Verify auth/provider logs do not leak secrets or tokens.
5.6. *(v3)* **Auth provider startup config validation MUST FAIL on critical misconfig**: `DynamicAuthSchemeManager.InitializeAsync` currently logs warnings on missing/invalid Keycloak/Google config and proceeds. Change to throw `OptionsValidationException` on critical issues (missing authority, missing client ID for an enabled provider). Log warnings only on optional/degraded config.
5.7. *(v3)* Add **tenant-aware log scope**: `MiddlewareExtensions` push tenant slug + tenant ID into `ILogger.BeginScope` so all downstream BFF logs carry tenant context.
5.8. *(v3)* Replace 36 `RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("...")` sites with new `HttpContextExtensions.GetLogger(string name)` (also feeds Phase 3).

**Acceptance**: Zero Console.WriteLine. Logging follows severity semantics. No secret leakage. Critical auth misconfig fails startup. Tenant in log scope. `GetLogger` extension used everywhere.

---

#### Phase 17A: State Classification
**Wave**: A | **Change Type**: STRUCTURAL

**Tasks:**
17A.1. Document state management strategy: URL (filters/pagination), CascadingValue (auth/tenant), Scoped Services (domain data), Component Local (UI-only state).
17A.2. Classify EventList's 30+ private fields as: URL state, service/domain state, local UI state, or derived/computed.
17A.3. Classify EventDetail, CreateEvent, EventEdit state similarly.
17A.4. For EventList and EventDetail: design a **page coordinator / page state model** with query/filter state, loading state, selected item/detail state, commands/actions, capability flags.
17A.5. Ensure all filter/pagination state is URL-driven (verify existing `[PersistentState]` usage).
17A.6. *(v3)* Document the **page-local presentation model** convention (NOT app-wide ViewModel layer): mapping NSwag DTOs to page-scoped presentation records lives in `Pages/<Feature>/State/<Page>PresentationMapper.cs`. This is opt-in per page and only required when `[Inject]` count exceeds 8 or when the same DTO is rendered in 3+ shapes within the page.

**Acceptance**: State management patterns documented. All god-component state classified before extraction begins. Page-local presentation model convention written.

---

#### Phase X: Operability & Self-Hoster Diagnostics (EXPANDED in v3)
**Wave**: A | **Change Type**: OPERATOR

**Tasks:**

**X.1. Startup Configuration Validation:**
- Validate required auth/provider configuration at startup (Keycloak authority, realm, client IDs) — **fails startup on missing required config** *(v3)*
- Validate BFF proxy targets (YARP cluster URLs reachable or clearly logged if not)
- Validate cookie/security settings consistency
- Validate known self-hosting misconfigurations early (e.g., HTTPS behind reverse proxy without forwarded headers)
- Clear, actionable log messages for each validation failure

**X.2. Diagnostics Surfaces:**
- Structured configuration validation logs at startup
- Health/readiness signals for BFF dependencies *(v3 — was "consideration", now mandatory)*: `AddHealthChecks().AddCheck<ApiBackendHealthCheck>("api-backend").AddCheck<KeycloakHealthCheck>("oidc-provider")`. `MapHealthChecks("/healthz")` (basic liveness) and `/readyz` (full dependency check).
- Clear feature-disabled reasons in UI/admin areas (disabled by policy vs unavailable by config vs provider unreachable vs provider misconfigured)

**X.3. Error State Distinction** (feeds into Phase 7 ServiceResult design):
- Feature disabled by policy
- Feature unavailable by config
- Provider unreachable
- Provider misconfigured
- Permission denied
- Session expired
- Validation error
- Transient backend failure

**X.4. Supportability:**
- Correlation ID visible in error UI where appropriate (admin/debug contexts)
- "Copy diagnostics" UX pattern for self-hosters/admins when encountering errors
- Log messages with tenant/provider/context scope

**X.5. Document configurable vs product-enforced security settings.**

**X.6.** *(v3)* **OpenTelemetry Minimum**: Add OTel to BFF only (Client OTel is deferred):
- `AddOpenTelemetry().ConfigureResource(r => r.AddService("Explore.Blazor"))`
- `WithTracing(t => t.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddSource("Explore.Blazor.Auth").AddSource("Explore.Blazor.Yarp"))`
- `WithMetrics(m => m.AddAspNetCoreInstrumentation().AddHttpClientInstrumentation().AddRuntimeInstrumentation())`
- OTLP exporter wired via configuration (no exporter in `Testing` env)
- Custom `ActivitySource("Explore.Blazor.Auth")` instruments login, logout, scheme refresh, token forwarding
- Custom `ActivitySource("Explore.Blazor.Yarp")` instruments YARP transform pipeline

**Acceptance**: App starts cleanly from minimal documented config. Misconfiguration produces actionable diagnostics OR fails startup (depending on severity). Feature-unavailable vs misconfigured clearly distinguished. Health/readiness endpoints respond. OTel traces flow for auth+YARP operations.

---

#### Phase A0: Render Mode Correction *(NEW in v3 — DEDICATED PHASE)*
**Wave**: A | **Change Type**: BEHAVIORAL

**Rationale**: All 32 pages currently hardcode `@rendermode InteractiveServer` despite `docs/BLAZOR.md` documenting `InteractiveAuto` as the default. `[PersistentState]` is used on Home/HomeStart/OrganizationReviews without any rendermode declared, which silently disables the SSR↔WASM state handoff. Per Oracle: this is a behavioral axis and requires its own phase, not per-page edits.

**Strategy**: Cohort migration with eligibility matrix. NO ad-hoc per-page edits. NO single-PR app-wide flip.

**Tasks:**

A0.1. **Build Eligibility Matrix**: For each of the 32 pages, classify:
  - **Server-only (keep InteractiveServer)**: depends on HttpContext, depends on server-only services (e.g., DbContext directly), uses circuit-bound features. Examples: setup/onboarding wizard, admin pages bound to BFF context.
  - **Auto-eligible**: pure data fetch + render, no server-only dependencies. Examples: most public browse pages, profile pages.
  - **Static SSR**: read-only legal/policy pages with no interactivity. Examples: PrivacyPolicy, TermsOfService, CommunityGuidelines.

A0.2. **Cohort 1 — Static SSR pages** (lowest risk): Convert legal/policy pages (PrivacyPolicy, TermsOfService, CommunityGuidelines, AboutUs, Contact) to no `@rendermode` declaration (default static SSR).

A0.3. **Cohort 2 — Auto-eligible public pages** (medium risk): Convert Home, HomeStart, OrganizationDetails, OrganizationReviews, public Event browse pages to `@rendermode InteractiveAuto` after auditing service registrations on both server and WASM sides.

A0.4. **Cohort 3 — Auto-eligible authenticated pages**: Convert MyEvents, MyReviews, Settings, Notifications.

A0.5. **Cohort 4 — Audit and confirm Server-only**: Onboarding/setup wizard, all admin pages, FooterSettings, InstanceGovernanceSection, TenantLookupTablesSection, AuthProviderConfiguration. Document why each must stay InteractiveServer.

A0.6. **Service Registration Audit**: For each Auto cohort, verify every service the page touches is registered on BOTH server-side (Explore.Blazor `Program.cs`) AND WASM-side (Explore.Blazor.Client `Program.cs`). Audit log goes into `dev/_journal/MAJOR_DECISIONS.md`.

A0.7. **PersistentComponentState wiring**: For pages using `[PersistentState]`, ensure `<PersistentComponentState>` is rendered in App.razor and the page now declares `@rendermode InteractiveAuto`. Verify SSR→WASM handoff: data is fetched once during prerender, persisted, hydrated client-side without re-fetch.

A0.8. **Prerender hygiene**: For each Auto-converted page, audit `OnInitializedAsync` for prerender-unsafe operations (auth-required calls before auth state is hydrated, JS interop). Use `OnAfterRenderAsync(firstRender)` for client-only side effects.

A0.9. **Cohort smoke test**: After each cohort merges, run the Wave 0 regression pack + a manual smoke of the cohort's pages.

**Before/after behavior matrix required** per cohort.

**Acceptance**: Every page has explicit render mode (or none, for static SSR). Eligibility matrix documented in `dev/_journal/MAJOR_DECISIONS.md`. PersistentComponentState pages verified to skip re-fetch on hydration. Cohort smoke tests pass.

---

### Wave B Phases

#### Phase 2: BFF Security Hardening (EXPANDED in v3)
**Wave**: B | **Change Type**: SECURITY

**Tasks:**
2.1. **XSRF-TOKEN cookie HttpOnly** [SECURITY]: Document as intentional (double-submit cookie pattern). Not a bug.
2.2. **Setup secret deduplication** [SECURITY]: Consolidate setup secret forwarding from YARP transform AND SetupSecretForwardingHandler into single shared service.
2.3. **AccessTokenForwardingHandler** [SECURITY]: Verify existence and registration.
2.4. **YARP cluster timeout** [SECURITY]: Wave 0 added 30s. Phase 2.4 reviews and tunes per endpoint workload.
2.5. **Cookie expiration review** [SECURITY]: Document current 7-day + sliding. Assess if shorter is appropriate.
2.6. **Open redirect safety** [SECURITY]: Verify auth return URLs validated against allowed origins.
2.7. **Anti-cache behavior** [SECURITY]: Wave 0 added Cache-Control on auth endpoints. Phase 2.7 expands to all sensitive BFF endpoints (preferences, setup secret).
2.8. **Cookie config for reverse proxy** [OPERATOR]: Document cookie naming/config isolation for multi-instance/self-hosted reverse proxy.
2.9. **Document configurable vs enforced security settings** [OPERATOR].
2.10. *(v3)* **Security Headers** [SECURITY]: Add response headers to ALL responses:
  - `X-Frame-Options: DENY`
  - `X-Content-Type-Options: nosniff`
  - `Referrer-Policy: strict-origin-when-cross-origin`
  - `Permissions-Policy: camera=(), microphone=(), geolocation=(), interest-cohort=()`
2.11. *(v3)* **Minimum Viable CSP** [SECURITY]: Per Oracle's recommendation, deploy a conservative CSP first; nonce-based CSP is deferred. Initial policy:
  ```
  default-src 'self';
  base-uri 'self';
  object-src 'none';
  frame-ancestors 'none';
  form-action 'self' https://*.{configured-idp-domains};
  connect-src 'self' https: wss:;
  img-src 'self' data: https:;
  font-src 'self' data:;
  style-src 'self' 'unsafe-inline';
  script-src 'self' 'unsafe-inline' 'wasm-unsafe-eval';
  ```
  Deploy in `Content-Security-Policy-Report-Only` mode for one release cycle to inventory violations before enforcement. Tightening to nonce-based CSP is deferred to a separate modernization track.
2.12. *(v3)* **IdP Token Revocation on Logout** [SECURITY]: Current `BffAuthEndpoints` logout calls `SignOutAsync` only — IdP session and access token remain valid for 7 days. Add OIDC RP-initiated logout (`end_session_endpoint`) call to Keycloak/Google with `id_token_hint` so IdP terminates its session. Document configuration requirement.
2.13. *(v3)* **XSRF token rotation policy** [SECURITY]: Document decision — rotate on auth state change (login/logout/scheme refresh) vs rotate on every authenticated request vs keep session-long. Implement chosen strategy.
2.14. *(v3)* **Front-channel logout / single sign-out** [MEDIUM]: Document multi-tab logout behavior. Defer implementation to separate track unless trivial to add via OIDC `frontchannel_logout_uri`.

**Acceptance**: No security regressions, setup secret logic DRY, YARP timeout tuned, security headers present on all responses, CSP in report-only mode, IdP logout works end-to-end, XSRF rotation policy documented and implemented.

---

#### Phase 3: BFF Endpoint Decomposition
**Wave**: B | **Change Type**: STRUCTURAL

**Tasks:**
3.1. **BffAuthEndpoints.cs (550 lines)** [STRUCTURAL]: Split into BffAuthStatusEndpoints, BffAuthMutationEndpoints, BffAuthDebugEndpoints.
3.2. **BffSetupSecretEndpoints.cs (346 lines)** [STRUCTURAL]: Extract individual endpoint handlers within setup module.
3.3. **BffPreferenceEndpoints.cs (310 lines)** [STRUCTURAL]: Extract individual endpoint handlers within preference module.
3.4. **`HttpContextExtensions.GetLogger(string name)`** [STRUCTURAL]: Implementation lives here (Phase 5.8 covers consumer migration).
3.5. **CircuitAccessTokenService.cs (326 lines)** [STRUCTURAL]: Split SetupSecretSessionService into its own file (already touched in Phase 0.A but the file split is structural cleanup).

**Acceptance**: No endpoint file >150 lines (soft guardrail), logger resolution DRY, each module has coherent responsibility boundary.

---

#### Phase 4: BFF Middleware Extraction
**Wave**: B | **Change Type**: STRUCTURAL

**Tasks:**
4.1. Extract **AntiforgeryTokenDistributionMiddleware** [STRUCTURAL].
4.2. Extract **StartupRedirectMiddleware** [STRUCTURAL].
4.3. Extract **AccessTokenCaptureMiddleware** [STRUCTURAL].
4.4. Move Console.CancelKeyPress handler to Program.cs or hosted service [STRUCTURAL].
4.5. Verify middleware pipeline order remains correct after extraction [SECURITY].
4.6. **Document pipeline order as a living table** [OPERATOR] in `docs/BLAZOR.md`.
4.7. *(v3)* **Per-handler timeout overrides** [STRUCTURAL]: HttpClient total timeouts (15s/30s/60s) exist but DelegatingHandlers can hang. Add `CancellationTokenSource(TimeSpan.FromSeconds(x))` linked to incoming request token in each DelegatingHandler.

**Acceptance**: MiddlewareExtensions.cs contains only `UseBlazorMiddleware()` composition. Pipeline order documented.

---

#### Phase 16: BFF Claims & HttpClient Rationalization
**Wave**: B | **Change Type**: BEHAVIORAL + SECURITY

**Tasks:**
16.1. Extract shared claim type constants to a location accessible from both BFF and Application layers [STRUCTURAL].
16.2. Audit typed client registrations [SECURITY].
16.3. Consolidate BrowserCredentialsMessageHandler constructors [STRUCTURAL].
16.4. Verify consistent auth context propagation across server/WASM boundaries [SECURITY].
16.5. Verify clear server-side vs browser-side client registration ownership [STRUCTURAL].
16.6. *(v3)* Eliminate magic claim type strings — `AuthStateService.cs:44-46` uses literals `"sub"`, `"nameidentifier"`, `"sid"`, `"tenant_id"`, `"tenantId"`, `"tid"`. Replace with constants from the shared claim type class.
16.7. *(v3)* Eliminate magic config strings — `DynamicAuthSchemeManager.cs:63-68` uses `"Keycloak:Authority"`, `"Keycloak:ClientId"`, `"Google:ClientId"`. Replace with strongly-typed `IOptions<KeycloakOptions>` / `IOptions<GoogleOptions>`.

**Acceptance**: No duplicated claim constants. All typed clients route through BFF. Auth context propagation verified. No magic config strings.

---

#### Phase 6A: DynamicAuthSchemeManager Stabilization
**Wave**: B | **Change Type**: SECURITY

CTO+v3 note: Wave 0.A already split the singleton state. Phase 6A continues the documentation/test work; Phase 6B (Wave E) does the deeper refactor.

**Architectural question** (CTO+Oracle): Do we need runtime dynamic scheme mutation? For self-hostable software, static provider set at startup + config change + restart may be operationally safer. Per Oracle's escalation: if runtime mutation cannot be made stateless quickly, freeze runtime provider refresh and require restart — already executed if needed in Phase 0.A.2.

**Tasks:**
6A.1. **Characterize existing state transitions** [SECURITY]
6A.2. **Document concurrency model** [SECURITY]: Map dual-locking strategy (SemaphoreSlim + object lock).
6A.3. **Document supported providers matrix** [SECURITY]: Keycloak, Google, Atproto.
6A.4. **Add integration tests** [SECURITY]: Cover all auth flow paths.
6A.5. **Complete or remove AtprotoAuthenticationHandler** [SECURITY]: Currently has TODO/incomplete.
6A.6. **Document rollback path** [SECURITY].
6A.7. *(v3)* **Eliminate service-locator pattern in `DynamicAuthSchemeManager.cs:502-503`**: Inject `ILoggerFactory` (or `ILogger<DynamicAuthSchemeManager>`) at construction; do not call `GetRequiredService<ILoggerFactory>()` inside `OnRemoteFailure`.
6A.8. *(v3)* **Eliminate service-locator pattern in `Explore.Blazor.Client/Program.cs:50-51`**: Refactor DI factory delegate to use proper constructor injection or named registrations.

**Acceptance**: Current behavior fully documented. Auth tests cover all paths. Service-locator instances removed.

---

### Wave C Phases

#### Phase 7: Service Layer Error Handling (EXPANDED in v3 — includes IEventService split)
**Wave**: C | **Change Type**: CONTRACT + BEHAVIORAL

**Tasks:**

7.1. **Define `ServiceResult<T>` and `ServiceResult` types** [CONTRACT] — shape unchanged from v2:

```
IsSuccess : bool
Value : T (on success)
ErrorCode : string (machine-readable, e.g., "AUTH_SESSION_EXPIRED", "VALIDATION_FAILED")
UserMessage : string (safe for display)
DeveloperMessage : string? (diagnostics, not displayed in production)
ValidationErrors : IReadOnlyDictionary<string, string[]>? (field-level errors)
FailureCategory : enum { None, Validation, NotFound, Forbidden, SessionExpired, ProviderUnavailable, ProviderMisconfigured, TransientFailure, Unknown }
IsRetryable : bool
HttpStatusCode : int? (if relevant from API)
Exception : Exception? (internal only, not surfaced to UI)
```

Static factories: `Success(T)`, `Failure(ErrorCode, UserMessage)`, `ValidationFailure(errors)`, `NotFound(message)`, `SessionExpired()`, `TransientFailure(message)`, `FromApiException(ApiException)`

7.2. *(v3 — moved up)* **Split IEventService per Interface Segregation Principle** [CONTRACT] BEFORE converting to ServiceResult. The 16-method interface mixes events, sessions, registrations and actively blocks ServiceResult conversion because each subdomain has different failure modes:
  - `IEventQueryService` — list, search, get-by-id, get-recommended
  - `IEventCommandService` — create, update, delete, publish, archive
  - `IEventSessionService` — list/CRUD sessions for an event
  - `IEventRegistrationService` — register, unregister, list registrations, approve

7.3. **Convert critical services** [BEHAVIORAL]: New IEventQueryService/IEventCommandService/IEventSessionService/IEventRegistrationService, OrganizationService, AdminService (post Phase 12), UserService.

7.4. **Convert remaining services** [BEHAVIORAL].

7.5. **Define UI error handling tiers** [BEHAVIORAL]:
  - Inline validation for user-fixable issues
  - Banners/cards for domain/business failures
  - Snackbar only for minor transient notifications
  - Dedicated error states for load failures
  - Re-auth flow for session expired
  - "Feature unavailable" state for provider/config issues

7.6. Update consuming components for typed error handling [CONTRACT].

7.7. Keep ILogger.LogError calls for observability [STRUCTURAL].

7.8. *(v3)* **Eliminate empty/swallowing catch blocks** [BEHAVIORAL]: Convert each to ServiceResult.Failure with appropriate FailureCategory. Sites: EventService.cs:395,407 (return false swallowing), EventService.cs:98,112,126 (catch-all), UserSettingsService.cs:76 (catch-all), Program.cs:121 (empty).

**Before/after behavior matrix required** for this phase.

**Acceptance**: No service method returns null/empty/false on error. All failures typed via ServiceResult. UI handles each FailureCategory appropriately. IEventService split into 4 interfaces. Empty catches eliminated.

---

#### Phase 12: Service Layer Decomposition
**Wave**: C (where needed for Phase 7) + D (remaining) | **Change Type**: STRUCTURAL + CONTRACT

**Tasks:**
12.1. **ImageStorageService.cs (972 lines)** [STRUCTURAL]: Split — S3UploadService, ImageProcessingService, ImageValidationService.
12.2. **AdminService.cs (764 lines)** [STRUCTURAL]: Split — GovernanceAdminService, LookupAdminService, TenantAdminService.
12.3. **InstanceOnboardingService.cs (614 lines)** [STRUCTURAL]: Extract step-specific services. Boring clear services > clever pipelines.
12.4. Update DI registrations for split services [STRUCTURAL].
12.5. *(v3)* **Move model classes out of interface files** [STRUCTURAL]: 11 model classes inside `IFooterAdminService.cs:39-127`; UserConsentViewModel/SharedContactViewModel inside `IContactShareConsentService.cs:55-80`. Move to `Models/` or `Contracts/`.
12.6. *(v3)* **LookupCacheService dependency reduction** [STRUCTURAL]: 9 dependencies — ISP violation. Split into focused caches per lookup domain.

**Acceptance**: Each service has single responsibility boundary. DI updated. No model classes in interface files.

---

### Wave D Phases

#### Phase 8: God Component Decomposition — EventList
**Wave**: D | **Change Type**: BEHAVIORAL

**Prerequisite**: Phase 17A (state classification) and Phase 7 (ServiceResult + IEventService split) must be complete.

**Tasks:**
8.1. **Implement EventListPageState** [BEHAVIORAL]
8.2. Extract **EventListGrid** [STRUCTURAL]
8.3. Extract **EventDetailDrawer** [STRUCTURAL]
8.4. Extract **EventListCustomizationDrawer** [STRUCTURAL]
8.5. Extract **EventFilterPanel** [STRUCTURAL]
8.6. Extract **EventListPagination** [STRUCTURAL]
8.7. **EventList.razor.cs** becomes orchestrator delegating to page state + child components [BEHAVIORAL]
8.8. Reduce [Inject] count from 15 to ≤8 [STRUCTURAL]
8.9. *(v3)* Add `@key` to all foreach loops in extracted components (currently missing in EventSeriesRail:24, EventTimeline:16,28, EventFilterBar:53-98)
8.10. *(v3)* Memoize computed properties: EventTimeline.GroupedEvents currently recomputes GroupBy/OrderBy/Select every render — extract to a backing field, recompute only on Events parameter change

**Before/after behavior matrix required**: infinite scroll, filtering, drawer open/close, pagination toggle.

**Acceptance**: EventList state model explicit. All extracted components have tests. Behavioral baselines unchanged. @key present on all loops.

---

#### Phase 9: God Component Decomposition — EventDetail
**Wave**: D | **Change Type**: BEHAVIORAL

**Tasks:**
9.1. **EventDetailPageState** [BEHAVIORAL] if complexity warrants.
9.2. Extract **EventSessionList** [STRUCTURAL]
9.3. Extract **EventRegistrationPanel** [STRUCTURAL]
9.4. Extract **EventReviewSection** [STRUCTURAL]
9.5. Extract **EventAgendaView** [STRUCTURAL]
9.6. *(v3)* Fix S3Image.razor parameter handling: `OnImageLoaded`/`OnImageError` call `StateHasChanged` without lifecycle check — gate with `if (!_disposed)` and use `InvokeAsync`.

**Before/after behavior matrix required**.

**Acceptance**: Behavioral baselines unchanged. Components testable.

---

#### Phase 10: God Component Decomposition — CreateEvent & EventEdit
**Wave**: D | **Change Type**: BEHAVIORAL

**Tasks:**
10.1. Extract **EventSessionEditor** [STRUCTURAL]
10.2. Extract **EventAspectEditor** [STRUCTURAL] — consolidate Islamic/Tech aspect dialogs
10.3. Extract **EventSpeakerManager** [STRUCTURAL]
10.4. Extract **EventFormFields** [STRUCTURAL]
10.5. *(v3)* **Form validation consistency** [STRUCTURAL]: Remove dead validators in `CreateSessionDialog.razor:37` and `EditSessionDialog.razor:37` (instantiated but never used because MudForm uses DataAnnotations). Decide policy per-form: FluentValidation OR DataAnnotations, not both. Document choice in `docs/BLAZOR.md`.

**Acceptance**: Shared components extracted. No form component excessively large. Validation pattern unified per-form.

---

#### Phase 11: God Component Decomposition — Admin Pages
**Wave**: D | **Change Type**: STRUCTURAL

**Tasks:**
11.1. **FooterSettings.razor (781 lines)** [STRUCTURAL]: Extract FooterTemplateEditor, FooterLinkGroupEditor, FooterPreview.
11.2. **InstanceGovernanceSection.razor (738 lines)** [STRUCTURAL]: Extract governance setting groups.
11.3. **TenantLookupTablesSection.razor (668 lines)** [STRUCTURAL]: Extract CategoryManagement, TagManagement, LocationManagement, MadhabManagement.

**Acceptance**: Admin components decomposed with clear responsibility boundaries.

---

#### Phase 13: Duplicated Pattern Extraction
**Wave**: D (late) | **Change Type**: STRUCTURAL

**Tasks:**
13.1. **Generic TriStateFilter\<T\>** [STRUCTURAL]
13.2. **ListDetailPageBase\<TItem\>** [STRUCTURAL] — only after EventList, MyEvents, MyOrganizations decompositions stabilize.
13.3. **Dialog standardization** [STRUCTURAL]: All dialogs use AppDialogShell.
13.4. **SettingsSectionBase** [STRUCTURAL]
13.5. *(v3)* **TenantContextProvider parameter detection** [STRUCTURAL]: Add `OnParametersSetAsync` to react to parameter changes (currently only OnInitializedAsync).
13.6. *(v3)* **EventSessionManager parameter detection** [STRUCTURAL]: Watch `ShowRegisterButton` parameter changes (currently only checks `EventId`).

**Acceptance**: Duplicated patterns extracted. Existing components refactored to use shared abstractions.

---

### Wave E Phases

#### Phase 6B: DynamicAuthSchemeManager Refactor
**Wave**: E | **Change Type**: SECURITY

**Prerequisite**: Phase 6A complete. Architectural decision answered.

**Tasks:**
6B.1. Extract provider-specific logic: KeycloakSchemeRegistration, GoogleSchemeRegistration, AtprotoSchemeRegistration [SECURITY].
6B.2. Simplify dual-locking to single concurrency primitive where possible [SECURITY].
6B.3. All auth flow integration tests pass [SECURITY].

**Before/after behavior matrix required**.

**Acceptance**: Auth scheme manager simplified. All auth flows work. Tests comprehensive.

---

#### Phase 14: CSS Compliance + a11y on Touched Components (EXPANDED in v3)
**Wave**: E | **Change Type**: STRUCTURAL

Priority order:
1. Hardcoded colors breaking themeability
2. Inline styles harming maintainability
3. Layout values hurting responsiveness/accessibility
4. `::deep` misuse
5. Tokenization cleanup
6. *(v3)* a11y compliance on touched components

**CSS Tasks:**
14.1. Replace 28 hardcoded colors in AdminListDetails.razor.css [STRUCTURAL].
14.2. Replace hardcoded px in NotificationPanel.razor.css [STRUCTURAL].
14.3. Replace hardcoded colors in Setup, GroupProfile, OrganizationProfile [STRUCTURAL].
14.4. Move MainLayout inline styles to .razor.css with BEM class [STRUCTURAL].
14.5. Move SetupLayout inline styles to .razor.css [STRUCTURAL].
14.6. Audit remaining 77 inline styles — keep only truly dynamic ones [STRUCTURAL].

**a11y Tasks (touched components only — NOT app-wide audit):**
14.7. *(v3)* **AppButton**: Add `AriaLabel` parameter (string, optional) wired to `aria-label` attribute. Architecture test fails if any AppButton instance with only an `Icon` parameter (no `Text`) lacks `AriaLabel`.
14.8. *(v3)* **AppIconButton**: Add `AriaLabel` parameter (string, REQUIRED).
14.9. *(v3)* **AppTextField**: Wire `aria-invalid` to validation error state, `aria-describedby` to helper text / error message id.
14.10. *(v3)* **S3Image**: Add `Alt` parameter (REQUIRED). Wire `loading="lazy"` attribute on underlying img tag.
14.11. *(v3)* **EventCard `<MudImage>`**: Pass `Alt` from event title; add `loading="lazy"`.
14.12. *(v3)* **Loading.razor**: Add `aria-live="polite"` and `aria-busy="true"` to loading container.
14.13. *(v3)* **ErrorState.razor**: Add explicit `aria-live="assertive"` to role="alert" container.
14.14. *(v3)* **MainLayout focus management**: Add `<FocusOnNavigate RouteData="..." Selector="h1" />` so screen readers announce route changes. Add focus-trap audit for MudDialog (verify default focus + restoration).
14.15. *(v3)* **RTL/MudRTLProvider sync**: `LanguageProvider` sets RTL via JS but `MainLayout` MudRTLProvider state may not match. Wire MudRTLProvider `RightToLeft` to a cascading service that LanguageProvider also updates.
14.16. *(v3)* **`[dir="rtl"]` selectors in CSS**: For touched components in this refactor, add `[dir="rtl"] .block { margin-right: ...; margin-left: ...; }` blocks where margin/padding flipping matters.

**Acceptance**: Zero hardcoded color values in .razor.css. Inline styles only for dynamic values. All touched wrapper components support a11y. Architecture tests for AriaLabel + Alt parameters passing.

---

#### Phase 15: ABOUTME Header Sweep
**Wave**: E | **Change Type**: STRUCTURAL

**Tasks:**
15.1. Attempt automation (script or template) for ABOUTME insertion on files missing headers.
15.2. If automation impractical, batch manually for 30+ identified files.
15.3. Verify all new files created during this refactor have ABOUTME headers.

**Acceptance**: All files have ABOUTME headers.

---

#### Phase 17B: State Management Standardization
**Wave**: E | **Change Type**: BEHAVIORAL

**Tasks:**
17B.1. Verify all decomposed components follow the state classification from 17A.
17B.2. Evaluate whether FeatureStateContainer pattern should be standardized across pages.
17B.3. Document final state management conventions.

**Acceptance**: State management patterns documented and consistently applied.

---

#### Phase 18: Test Infrastructure Expansion
**Wave**: E | **Change Type**: STRUCTURAL

**Tasks:**
18.1. Component rendering tests for all extracted components from Phases 8-11 [STRUCTURAL]
18.2. Form validation tests for event creation/editing validators [STRUCTURAL]
18.3. Error handling tests for ServiceResult error boundaries [BEHAVIORAL]
18.4. Expand architecture tests for new guardrails [STRUCTURAL]
18.5. Accessibility tests for new shared components [STRUCTURAL]
18.6. **Workflow/smoke scenario tests** [BEHAVIORAL]

**Acceptance**: Every extracted component has at least 1 rendering test. Critical workflow scenarios covered.

---

#### Phase 18B: Test Migration & Coverage Closure *(NEW in v3)*
**Wave**: E | **Change Type**: STRUCTURAL

**Tasks:**
18B.1. **bUnit data-testid migration**: Migrate the 18 test files using brittle `Markup.Contains()` (154 assertions) to use `cut.Find("[data-testid='...']")` and `cut.FindAll("[data-testid='...']")`. Add `data-testid` to extracted components in Phases 8–11 by default.
18B.2. **MockServiceFactory closure**: Add the 20 missing service mocks (IAdminService, IDialogService, ISnackbar, IUserSettingsService, IContactShareConsentService, IPublicExperienceService, IEventRegistrationService, IOrganizationMemberService, IOrganizationReviewService, IEventSessionSpeakerService, IEventAspectService, IFooterAdminService, IExternalApiKeyService, IFeatureFlagClientService, ILanguagePreferenceService, ILocalLocalizationAdminService, IMapsService, IEventSessionAgendaItemService, ILandingPageService, IRuntimeRenderPolicyService).
18B.3. **BlazorTestContext helpers**: Add `SetServiceThrows<T>(Exception)`, `SimulateNetworkFailure()`, `WaitForRenderComplete()`.
18B.4. **PlaywrightFixture expansion**: Add `NavigateAndWaitForReady`, `ScreenshotOnFailure`, `Page object models` for top user journeys.
18B.5. **8 critical E2E user-journey scenarios** (replace today's 2):
  1. Login OAuth flow (Keycloak)
  2. Logout (verify IdP session terminated)
  3. Event discovery + filtering
  4. Event registration workflow
  5. Organization creation + member management
  6. Admin settings save
  7. Multi-tenant context switching
  8. Error handling (404 / 500 / network failure)
18B.6. **Blazor architecture-test suite**: Expand from 2 tests to all v3 Phase 1 entries (15 tests).
18B.7. **DelegatingHandler depth**: Add error/timeout/cancellation/retry scenarios for AccessTokenForwardingHandler, BrowserCredentialsMessageHandler, SetupSecretForwardingHandler.

**Acceptance**: Zero `Markup.Contains` in unit tests for components touched by Phases 8–11. MockServiceFactory covers 100% of injected services. 8 E2E journeys passing. 15 architecture tests passing.

---

#### Phase 19: Architecture Conformance Sweep
**Wave**: E | **Change Type**: STRUCTURAL + OPERATOR

**Tasks:**
19.1. Run all architecture tests — zero failures
19.2. Run full build — zero errors
19.3. Run all Blazor test suites — all pass
19.4. Verify @layer compliance
19.5. Verify BEM naming compliance in all new .razor.css files
19.6. Verify wrapper component usage in all new components
19.7. LSP diagnostics clean on changed files
19.8. **Self-hosted deployment verification**

**Acceptance**: All automated checks pass. Self-hosted startup verified.

---

#### Phase 20: Verification & Handoff
**Wave**: E | **Change Type**: STRUCTURAL + OPERATOR

**Tasks:**
20.1. Run complete build
20.2. Run all test suites individually, document results
20.3. Update `dev/_journal/journal.md` with key refactor insights
20.4. Update `docs/BLAZOR.md` with new patterns/conventions + middleware pipeline table + render-mode eligibility matrix + form validation policy + a11y conventions
20.5. Update `docs/CODEBASE_STRUCTURE.md`
20.6. Update `docs/CODEBASE_INSIGHTS.md`
20.7. Update `docs/TROUBLESHOOTING.md` with common Blazor/BFF issues + Wave 0 hotfix lessons
20.8. **Self-hosting config reference** — auth provider, BFF proxy, cookie/security, OTel exporter, health checks
20.9. **BFF request flow diagram**
20.10. **Error-handling conventions doc** — ServiceResult usage
20.11. *(v3)* **Render-mode decision log** — eligibility matrix outcomes for each cohort
20.12. Update this plan's context and tasks files with final status

**Acceptance**: All docs updated, build green, all tests pass, operator documentation complete, plan marked complete.

---

## Wave Sequencing & Dependencies

```
WAVE 0 — Pre-Flight Hotfix Track (BLOCKING)
  Phase 0.A (DI lifetime hotfix) ── Phase 0.B (async void) ── Phase 0.C (.Result) [parallel]
    ↓
  Phase 0.D (Cache-Control) ── Phase 0.E (YARP timeout) [parallel]
    ↓
  Phase 0.F (Wave 0 regression test pack)

WAVE A — Safety, Fitness Functions & Render Mode Correction
  Phase 0 (Baseline)
    ↓
  Phase 1 (Guardrails) ── Phase 5 (Observability) [parallel]
    ↓
  Phase 17A (State Classification) ── Phase X (Operability + OTel + Health) [parallel]
    ↓
  Phase A0 (Render Mode Correction — cohort-by-cohort)

WAVE B — BFF Hardening
  Phase 2 (Security — incl. headers, CSP-MVP, IdP logout, XSRF) ── Phase 4 (Middleware) [parallel]
    ↓
  Phase 3 (Endpoint Decomp) ── Phase 16 (Claims/HttpClient) [parallel]
    ↓
  Phase 6A (Auth Stabilization)

WAVE C — Service Contract Reform
  Phase 7.2 (IEventService split) ← prerequisite for 7.3+
    ↓
  Phase 7 (ServiceResult conversion) ← depends on Phase X.3 (error state distinction)
    ↓
  Phase 12-partial (Service Decomp — contract-changing splits only)

WAVE D — UI Decomposition
  Phase 8 (EventList) ← depends on Phase 17A + Phase 7
    ↓
  Phase 9 (EventDetail) ── Phase 10 (Create/Edit) ── Phase 11 (Admin) [parallel]
    ↓
  Phase 12-rest (Service Decomp — remaining splits)
    ↓
  Phase 13 (Pattern Extraction)

WAVE E — Conformance, Test Hardening & Operability
  Phase 6B (Auth Refactor — only after 6A complete)
    ↓
  Phase 14 (CSS + a11y) ── Phase 15 (ABOUTME) ── Phase 17B (State Standardization) [parallel]
    ↓
  Phase 18 (Test Expansion) ── Phase 18B (Test Migration + E2E + arch tests) [parallel]
    ↓
  Phase 19 (Conformance)
    ↓
  Phase 20 (Handoff)
```

---

## Risk Register (REWRITTEN in v3)

**Top 5 risks per Oracle review:**

| # | Risk | Impact | Probability | Mitigation | Change Type |
|---|------|--------|-------------|------------|-------------|
| 1 | **Cross-user/circuit state leakage** in singleton services causing secret/auth contamination | **CRITICAL** | **Confirmed (currently shipping)** | Wave 0.A hotfix; arch test 1.9; integration test for two concurrent sessions; freeze runtime auth scheme mutation if needed | HOTFIX/SECURITY |
| 2 | **Render-mode migration regressions** from prerender, double initialization, missing service registration, JS interop timing | HIGH | Medium-High | Phase A0 cohort-based migration; eligibility matrix; service registration audit per cohort; cohort smoke test; PersistentComponentState verification | BEHAVIORAL |
| 3 | **Auth hardening regressions** breaking login/logout/provider refresh when changing scheme management, cache headers, or CSP | HIGH | Medium | 6A stabilize first, 6B refactor after; CSP in report-only mode for one release; integration test for all auth paths; rollback path documented | SECURITY |
| 4 | **ServiceResult contract churn** from rippling through many components with weak test coverage | HIGH | Medium | Convert one service at a time; UI error tiers defined upfront in Phase X.3; IEventService split BEFORE ServiceResult conversion; before/after behavior matrix | CONTRACT |
| 5 | **Test blind spots** allowing UI/a11y/render regressions to ship because most pages/components lack meaningful tests | HIGH | High | Phase 18B `data-testid` migration off 154 brittle assertions; MockServiceFactory closure (20 services); 8 E2E journeys; arch test suite expansion to 15 tests | STRUCTURAL |

**Other tracked risks:**

| Risk | Impact | Probability | Mitigation | Change Type |
|------|--------|-------------|------------|-------------|
| EventList decomposition breaks infinite scroll | HIGH | Medium | State classification (17A) before extraction (8). Page state model. Behavioral baselines. | BEHAVIORAL |
| CSS token migration breaks visual appearance | MEDIUM | Low | Visual check after each file. | STRUCTURAL |
| Middleware extraction changes pipeline order | HIGH | Low | Document before/after order as living table. Integration tests. | SECURITY |
| YARP timeout too aggressive | MEDIUM | Low | Start at 30s in Wave 0, tune in Phase 2.4. | SECURITY |
| Startup validation too strict for existing deployments | MEDIUM | Medium | Validation FAILS only on critical missing config; warns on optional/degraded. | OPERATOR |
| Config diagnostics leaks sensitive info | HIGH | Low | Redact secrets in all diagnostic surfaces. Never log tokens/passwords. | SECURITY |
| CSP breaks Blazor Server inline scripts/styles | MEDIUM | Medium | Deploy in `Content-Security-Policy-Report-Only` mode for one release; nonce-based CSP deferred. | SECURITY |
| IdP logout misconfiguration | MEDIUM | Medium | Document Keycloak/Google end_session_endpoint config; integration test for logout-then-IdP-session-status. | SECURITY |
| OTel exporter endpoint misconfigured in self-hosted deployments | LOW | Medium | Document in self-hosting reference; OTel exporter is opt-in via configuration. | OPERATOR |

---

## Acceptance Gates for Semantic Phases

For Phases 0.A, 0.B, 0.C, 6A/6B, 7, 8, 9, 10, 17A/17B, A0:

- [ ] Before/after behavior matrix documented
- [ ] Smoke scenarios executed for affected user flows
- [ ] Operator-visible errors reviewed (no regression in error clarity)
- [ ] *(Wave 0 only)* Wave 0 regression test pack passes

---

## Change Management Discipline

1. **One architectural decision log per major semantic change** (in `dev/_journal/MAJOR_DECISIONS.md`)
2. **PR scope rule**: No mixing SECURITY refactor and UI decomposition in same PR. Wave 0 HOTFIX PRs are single-defect.
3. **Mandatory before/after behavior notes** for auth/state/service contract/render-mode changes
4. **Contributor enforcement**: Templates for new components/services, wrapper component usage guidance, error handling guidance in docs
5. *(v3)* **Wave 0 is BLOCKING**: No Wave A work begins until Wave 0 hotfixes merged to main and regression pack green.
6. *(v3)* **Render-mode changes go through Phase A0 cohort review only**. No ad-hoc per-page render-mode edits.

---

## Effort Estimate Summary

| Wave | Effort Tier | Calendar Estimate |
|------|-------------|-------------------|
| Wave 0 | Short (HOTFIX) | 2–4 days |
| Wave A | Medium | ~1 week |
| Wave B | Medium | 1–1.5 weeks |
| Wave C | Medium | 1–1.5 weeks |
| Wave D | Large | 2–3 weeks |
| Wave E | Medium | ~1 week |
| **Program total** | | **~7–10 weeks** |

---

## Deferred Work (Explicitly Out of Scope — Separate Tracks)

These items are **NOT** part of this refactor program. They are tracked as separate workstreams to avoid scope creep.

### Original v2 Deferrals
1. **Centralized state management** (Fluxor/Redux) — too large for refactor
2. **NSwag client splitting** — 86k-line generated file; NSwag config changes needed
3. **PWA/offline support** — feature work
4. **Visual regression testing** — requires Playwright infrastructure expansion
5. **Real API integration tests** (non-mocked) — different testing strategy
6. **Refresh token rotation** — security feature
7. **Multi-language rendering tests** — feature-level testing
8. **Client-side caching layer** — performance optimization

### v3 Deferrals (NEW — Per Oracle Recommendation)
9. **App-wide IStringLocalizer + .resx migration** — 50+ hardcoded strings, 100+ files. The repo already has a documented translation architecture (`docs/LOCALIZATION.md` — TranslationService / TMS / JSON bundles). Introducing `IStringLocalizer` + `.resx` would create parallel architecture. Use the existing stack on touched files; full app-wide migration is a separate program.
10. **App-wide ViewModel/presentation layer** — 65 pages and 46 services. Per Oracle: introduce page-local presentation models in Phase 17A.6 for refactored hotspots only; do NOT call this a "ViewModel layer" because that invites app-wide rollout.
11. **LazyAssemblyLoader / feature-based assembly groups** — .NET 10 platform modernization
12. **NativeAOT for WASM** — .NET 10 platform modernization, evaluate compute-heavy WASM separately
13. **WASM preloading hints** (`<link rel=modulepreload>`) — performance optimization
14. **WebSocket compression for Blazor Server** — performance optimization
15. **Microsoft.FeatureManagement adoption** — feature work
16. **Nonce-based CSP** — Wave B Phase 2.11 ships report-only conservative CSP; nonce-based requires inventorying all inline scripts/styles
17. **Front-channel logout / single sign-out** — Wave B Phase 2.14 documents only; implementation deferred unless trivial
18. **Client-side OpenTelemetry** — Wave A Phase X.6 covers BFF only
19. **Bundle size monitoring + Core Web Vitals** — performance program
20. **axe-playwright integration for full-app a11y audit** — Phase 14 covers touched components only

### v3 Spike (Allowed but Time-Boxed)
- **Wave F (.NET 10 modernization)** — NOT in this program. If product wants to evaluate, spin a Short (2–3d) spike to assess `[PersistentState]` adoption beyond render-mode-driven sites, LazyAssemblyLoader feasibility, NativeAOT viability for the hot WASM path. Output: a separate plan, not absorbed into this refactor.

---

## Escalation Triggers (NEW in v3 — Per Oracle)

If any of these occur during execution, halt the affected wave and re-plan:

1. **InteractiveAuto migration exposes widespread server-only dependencies** or broken shared service registration → pause Phase A0; do server/client service-registration audit first.
2. **Auth scheme mutation cannot be made stateless quickly** → freeze runtime provider refresh; require restart until Phase 6B lands.
3. **Localization is made a release gate** → spin into a parallel dedicated workstream; do NOT absorb into this refactor by stealth.
4. **Wave 0 regression pack fails post-merge** → revert offending hotfix; consult Oracle; do not stack Wave A on top.
5. **CSP report-only mode shows >10 violation categories** → tighten incrementally per category; do not enforce until violation set is fully understood.

---

## Bottom Line (v3)

- **Wave 0** is non-negotiable and blocking. Three CRITICAL bugs (singleton state leakage × 2, async void crash) and three HIGH defects (.Result deadlocks, missing Cache-Control, missing YARP timeout) ship in production today. Fix them before doing anything else.
- **Render mode** is its own phase, not cleanup. 32-page hardcoded `InteractiveServer` plus 3 pages with `[PersistentState]` and no rendermode is a behavioral defect, not a style issue.
- **Scope is tight on purpose**. App-wide i18n, app-wide ViewModel layer, .NET 10 platform modernization, PWA, NativeAOT, Microsoft.FeatureManagement — all out. The team is not bored. Adding them would burn 4× the calendar with no operability gain.
- **Tests are the gate, not the polish**. Phase 18B exists because shipping a refactor with 154 brittle assertions, 20 missing mocks, and 2 E2E tests is shipping a refactor with no safety net.
- **Operability is product quality**. Phase X.6 (OTel + health checks) is required, not optional.
