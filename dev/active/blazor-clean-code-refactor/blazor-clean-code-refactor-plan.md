ABOUTME: Strategic enterprise-scale Blazor refactor plan for BFF host, WASM client, services, components, CSS, and test infrastructure.
ABOUTME: Organized into 5 delivery waves with CTO-approved risk classification, structured error contract, and operability workstream.

# Plan: Blazor Clean Code Refactor Program (Enterprise-Grade)

> Last Updated: 2026-04-16
> CTO Review: APPROVED with conditions (incorporated below)

## Executive Summary

This is the **full-scale Blazor refactor program** for the Event repository, restructured into **5 delivery waves** after CTO review. The wave model replaces the prior 21 flat phases with a prioritized, risk-aware execution sequence.

**Scope**:

- `Explore.Blazor` (BFF server host)
- `Explore.Blazor.Client` (WASM UI, pages, components, services)
- `Explore.Blazor.IntegrationTests`
- `Explore.Blazor.Client.Tests`
- `Explore.Blazor.Client.E2ETests`

**Explicitly out of scope**:

- `Explore.API`, `Explore.Application`, `Explore.Domain`, `Explore.Persistence`, `Explore.Infrastructure` (covered by api-clean-code-refactor)
- Backend API contract changes
- New feature implementation

This plan covers:

- BFF server architecture and security hardening
- Operability and self-hoster diagnostics (NEW — CTO requirement)
- Component decomposition with explicit state classification (moved earlier)
- Structured service error contract (upgraded from string-only)
- CSS architecture compliance and design token adoption
- Test coverage expansion including workflow/smoke scenarios
- Configuration governance and startup validation

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

**Non-negotiable rule**: No PR may mix STRUCTURAL cleanup with SECURITY or BEHAVIORAL change unless explicitly justified and documented.

---

## Program Goals

By the end of this program, the Blazor codebase should have these characteristics:

1. **No god components.** Soft guardrail: .razor ~300 lines, .razor.cs ~200 lines. Violations require justification based on cohesion, not automatic rejection.
2. **BFF security is enterprise-grade.** CSRF tokens, cookie policy, token forwarding, rate limiting are all correct and documented.
3. **Service layer uses structured error contract.** `ServiceResult<T>` with error codes, failure categories, retryability hints — not just strings.
4. **CSS is fully compliant.** All scoped styles follow BEM, design tokens are used, no hardcoded colors/spacing.
5. **Components are composable and testable.** Shared patterns extracted, dependency counts ≤8, explicit state classification.
6. **Test coverage is meaningful.** Critical paths have unit tests, workflow smoke scenarios exist, architecture tests catch regressions.
7. **ABOUTME headers on every file.** Automated or batched — not competing with high-value engineering.
8. **BFF endpoint handlers are decomposed by capability module.** Auth status vs auth mutation vs setup vs preferences.
9. **State management is explicit and classified before decomposition.** URL for filters/pagination, services for domain state, cascading for UI, page coordinator for complex pages.
10. **Self-hosters get meaningful diagnostics.** Startup validation, configuration error clarity, correlation-aware error surfaces, feature-unavailable vs misconfigured distinction.

---

## Non-Negotiable Constraints

- **InteractiveAuto** is the default render mode (InteractiveServer only for server-only needs)
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

## Baseline Metrics (Pre-Refactor)

### Explore.Blazor (BFF Server)
| Metric | Value |
|--------|-------|
| Total C# files | 28 |
| Files >300 lines | 4 (BffAuthEndpoints 550, DynamicAuthSchemeManager 539, BffSetupSecretEndpoints 346, CircuitAccessTokenService 326) |
| Console.WriteLine instances | 8 (ConfigurationExtension.cs) |
| RequestServices.GetRequiredService calls | 36 |
| Inline middleware lambdas | 5+ (MiddlewareExtensions.cs) |
| Setup secret duplication | 2 locations (YARP + DelegatingHandler) |

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

### User-Visible Behavioral Baselines (CTO Addition)
These flows must work identically before/after refactor:

| Flow | Entry Point | Key Behavior |
|------|-------------|-------------|
| Auth login/logout | /auth/login, /auth/logout | Cookie set/cleared, redirect to IdP, return to app |
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

### Wave A — Safety & Fitness Functions

**Goal**: Protect correctness, establish guardrails, clean logging, validate operability.

Phases: 0 (Baseline), 1 (Guardrails), 5 (Observability Hygiene), partial 17 (State Classification), X (Operability)

---

### Wave B — BFF Hardening

**Goal**: Remove structural BFF risk, harden security, rationalize claims/clients.

Phases: 2 (Security), 3 (Endpoint Decomposition), 4 (Middleware Extraction), 16 (Claims/HttpClient), 6A (Auth Stabilization only)

---

### Wave C — Service Contract Reform

**Goal**: Fix service error semantics, decompose where contract changes are needed.

Phases: 7 (ServiceResult), 12 (Service Decomposition where needed for Phase 7)

---

### Wave D — UI Decomposition

**Goal**: Attack biggest component hotspots with explicit state model.

Phases: 8 (EventList), 9 (EventDetail), 10 (Create/Edit), 11 (Admin), 12 (remaining service splits), 13 (Pattern Extraction)

---

### Wave E — Conformance & Operability

**Goal**: Polish, standardize, expand tests, document, hand off.

Phases: 6B (Auth Refactor), 14 (CSS), 15 (ABOUTME), 18 (Tests), 19 (Conformance), 20 (Handoff)

---

## Phase Definitions

### Phase 0: Safety Baseline & Isolation
**Wave**: A | **Change Type**: STRUCTURAL

**Tasks**:
0.1. Build verification: `dotnet build --configuration Release --verbosity quiet`
0.2. Run all Blazor test suites, document baseline pass/fail counts
0.3. Create git branch `refactor/blazor-clean-code`
0.4. Document any pre-existing test failures (separate from refactor)
0.5. **Capture user-visible behavioral baselines** (CTO addition): Execute manual smoke check of auth login/logout, event list browsing/filtering, event detail + registration flow, admin settings save. Document current behavior for comparison.

**Acceptance**: Build green, test baseline documented, behavioral baselines captured, branch created.

---

### Phase 1: Architecture Guardrails
**Wave**: A | **Change Type**: STRUCTURAL

**Tasks**:
1.1. Add arch test: No component (.razor.cs) injects IEventApiClient directly (already exists — verify)
1.2. Add arch test: No Console.WriteLine in Explore.Blazor or Explore.Blazor.Client production code
1.3. Add arch test: No inline `app.Use(async (ctx, next) => { ... })` lambdas >5 lines in Explore.Blazor
1.4. Add arch test: All [Inject] services use interfaces (not concrete types)
1.5. Add arch test: No `new DialogOptions()` — must use DialogOptionsFactory
1.6. Add arch test: No direct `NavigationManager` in `Components/Common/` or `Components/Collection/` (shared components)
1.7. Add arch test: No `IJSRuntime` in service classes under `Services/` unless explicitly in `Services/Interop/` or `Services/Http/`
1.8. Add arch test: No snackbar injection (`ISnackbar`) in data service classes

**Reframed** (CTO): ABOUTME header checks are governance automation, not architectural tests. Handle via scripting/CI if possible, not arch test suite.

**Acceptance**: All new arch tests written and passing (some may initially fail — that's the point).

---

### Phase 5: Observability Hygiene (moved to Wave A)
**Wave**: A | **Change Type**: BEHAVIORAL + OPERATOR

Broadened from "Console.WriteLine cleanup" to **observability hygiene** (CTO feedback).

**Tasks**:
5.1. Remove 8 Console.WriteLines from ConfigurationExtension.cs. Replace with structured `ILogger` calls or post-build configuration validator.
5.2. Audit all `ILogger` usage in BFF for proper severity semantics:
  - Warning vs Error: errors for unexpected failures, warnings for recoverable/expected degradation
  - No noisy logs on expected states (e.g., no tenant resolved → info not warning)
  - Operator-meaningful messages (include what failed, why, what to check)
5.3. Verify correlation ID propagation works through BFF → YARP → API.
5.4. Add consistent event IDs or log categories where practical for BFF log filtering.
5.5. Verify auth/provider logs do not leak secrets or tokens.

**Acceptance**: Zero Console.WriteLine in Explore.Blazor production code. Logging follows severity semantics. No secret leakage.

---

### Phase 17A: State Classification (moved to Wave A — before decomposition)
**Wave**: A | **Change Type**: STRUCTURAL

CTO feedback: "EventList decomposition without explicit state classification is risky. Do state classification before major extraction."

**Tasks**:
17A.1. Document state management strategy: URL (filters/pagination), CascadingValue (auth/tenant), Scoped Services (domain data), Component Local (UI-only state).
17A.2. Classify EventList's 30+ private fields as: URL state, service/domain state, local UI state, or derived/computed.
17A.3. Classify EventDetail, CreateEvent, EventEdit state similarly.
17A.4. For EventList and EventDetail: design a **page coordinator / page state model** — a page-level orchestration concept defining:
  - query/filter state
  - loading state
  - selected item/detail state
  - commands/actions
  - capability flags
17A.5. Ensure all filter/pagination state is URL-driven (verify existing `[PersistentState]` usage).

**Acceptance**: State management patterns documented. All god-component state classified before extraction begins.

---

### Phase X: Operability & Self-Hoster Diagnostics (NEW — CTO Requirement)
**Wave**: A | **Change Type**: OPERATOR

**Rationale**: For self-hostable enterprise software, operability is core product quality, not nice-to-have.

**Tasks**:

**X.1. Startup Configuration Validation**:
  - Validate required auth/provider configuration at startup (Keycloak authority, realm, client IDs)
  - Validate BFF proxy targets (YARP cluster URLs reachable or clearly logged if not)
  - Validate cookie/security settings consistency
  - Validate known self-hosting misconfigurations early (e.g., HTTPS behind reverse proxy without forwarded headers)
  - Clear, actionable log messages for each validation failure

**X.2. Diagnostics Surfaces**:
  - Structured configuration validation logs at startup
  - Health/readiness signals for BFF dependencies (API backend, identity provider)
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

**X.4. Supportability**:
  - Correlation ID visible in error UI where appropriate (admin/debug contexts)
  - "Copy diagnostics" UX pattern for self-hosters/admins when encountering errors
  - Log messages with tenant/provider/context scope

**X.5. Document which security settings are configurable for self-hosters vs product-enforced**.

**Acceptance**: App starts cleanly from minimal documented config. Misconfiguration produces actionable diagnostics. Feature-unavailable vs misconfigured clearly distinguished.

---

### Phase 2: BFF Security Hardening
**Wave**: B | **Change Type**: SECURITY

**Tasks**:
2.1. **XSRF-TOKEN cookie HttpOnly** [SECURITY]: Document as intentional (double-submit cookie pattern — WASM reads cookie to inject X-CSRF-TOKEN header). Not a bug.
2.2. **Setup secret deduplication** [SECURITY]: Consolidate setup secret forwarding from YARP transform AND SetupSecretForwardingHandler into single shared service.
2.3. **Missing AccessTokenForwardingHandler** [SECURITY]: Verify existence. If missing, implement or fix registration.
2.4. **YARP cluster timeout** [SECURITY]: Add request timeout (start at 30s, tune down). Currently none — could hang indefinitely.
2.5. **Cookie expiration review** [SECURITY]: Document current 7-day + sliding. Assess if shorter is appropriate.
2.6. **Open redirect safety** [SECURITY] (CTO addition): Verify auth return URLs are validated against allowed origins.
2.7. **Anti-cache behavior** [SECURITY] (CTO addition): Verify sensitive auth/setup endpoints have appropriate cache-control headers.
2.8. **Cookie config for reverse proxy** [OPERATOR] (CTO addition): Document cookie naming/config isolation for multi-instance/self-hosted reverse proxy scenarios.
2.9. **Document configurable vs enforced security settings** [OPERATOR]: Which security settings can self-hosters change vs which are product-enforced.

**Acceptance**: No security regressions, setup secret logic is DRY, YARP has timeout, security decisions documented.

---

### Phase 3: BFF Endpoint Decomposition
**Wave**: B | **Change Type**: STRUCTURAL

CTO feedback: "Split by capability module, not just by size. Standardize logger access, error-to-ProblemDetails mapping, route registration patterns."

**Tasks**:
3.1. **BffAuthEndpoints.cs (550 lines)** [STRUCTURAL]: Split by capability module:
  - `BffAuthStatusEndpoints` — auth status/read (Challenge, Status, Providers)
  - `BffAuthMutationEndpoints` — auth mutation (Login, Signout, RefreshSchemes)
  - `BffAuthDebugEndpoints` — debug/diagnostic (Debug endpoint)
  - Standardize: logger access, error-to-ProblemDetails mapping, route naming, authorization policy declaration
3.2. **BffSetupSecretEndpoints.cs (346 lines)** [STRUCTURAL]: Extract individual endpoint handlers within setup module.
3.3. **BffPreferenceEndpoints.cs (310 lines)** [STRUCTURAL]: Extract individual endpoint handlers within preference module.
3.4. **Create `HttpContextExtensions.GetLogger(string name)`** [STRUCTURAL]: Replace 36 instances of `ctx.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("...")`.
3.5. **CircuitAccessTokenService.cs (326 lines)** [STRUCTURAL]: Split SetupSecretSessionService into its own file.

**Acceptance**: No endpoint file >150 lines (soft guardrail), logger resolution is DRY, each module has coherent responsibility boundary. Build green.

---

### Phase 4: BFF Middleware Extraction
**Wave**: B | **Change Type**: STRUCTURAL

**Tasks**:
4.1. Extract **AntiforgeryTokenDistributionMiddleware** [STRUCTURAL] from MiddlewareExtensions.
4.2. Extract **StartupRedirectMiddleware** [STRUCTURAL] from MiddlewareExtensions.
4.3. Extract **AccessTokenCaptureMiddleware** [STRUCTURAL] from MiddlewareExtensions.
4.4. Move Console.CancelKeyPress handler to Program.cs or hosted service [STRUCTURAL].
4.5. Verify middleware pipeline order remains correct after extraction [SECURITY].
4.6. **Document pipeline order as a living table** [OPERATOR] (CTO addition) in `docs/BLAZOR.md` — not just "verify order" but publish the order and reason for each step.

**Acceptance**: MiddlewareExtensions.cs contains only `UseBlazorMiddleware()` composition. Pipeline order documented.

---

### Phase 16: BFF Claims & HttpClient Rationalization
**Wave**: B | **Change Type**: BEHAVIORAL + SECURITY

**Tasks**:
16.1. Extract shared claim type constants to a location accessible from both BFF and Application layers [STRUCTURAL].
16.2. Audit typed client registrations [SECURITY]: Ensure InteractiveServer typed clients route through BFF, not directly to API. Verify no accidental direct API routing bypassing BFF.
16.3. Consolidate BrowserCredentialsMessageHandler constructors [STRUCTURAL].
16.4. Verify consistent auth context propagation across server/WASM boundaries [SECURITY].
16.5. Verify clear server-side vs browser-side client registration ownership [STRUCTURAL].

**Acceptance**: No duplicated claim constants. All typed clients route through BFF. Auth context propagation verified.

---

### Phase 6A: DynamicAuthSchemeManager Stabilization (Wave B — stabilize only)
**Wave**: B | **Change Type**: SECURITY

CTO feedback: "This is a stop-the-line area. Do not combine discovery and surgery in the same pass." Split into 6A (stabilize) and 6B (refactor).

**Architectural question** (CTO): Do we actually need runtime dynamic scheme mutation? For self-hostable software, static provider set at startup + config change + restart may be operationally safer. This must be answered before 6B.

**Tasks**:
6A.1. **Characterize existing state transitions** [SECURITY]: Document current behavior matrix — which providers, which startup paths, which runtime mutation paths.
6A.2. **Document concurrency model** [SECURITY]: Map dual-locking strategy (SemaphoreSlim + object lock), write up failure modes.
6A.3. **Document supported providers matrix** [SECURITY]: Keycloak, Google, Atproto — what state each can be in.
6A.4. **Add integration tests** [SECURITY]: Cover all auth flow paths — login, logout, scheme refresh, provider unavailable.
6A.5. **Complete or remove AtprotoAuthenticationHandler** [SECURITY]: Currently has TODO/incomplete. Either implement or remove dead code.
6A.6. **Document rollback path** [SECURITY]: What happens if scheme registration fails mid-flight.

**Acceptance**: Current behavior fully documented. Auth tests cover all paths. No code changes to core auth logic yet.

---

### Phase 7: Service Layer Error Handling
**Wave**: C | **Change Type**: CONTRACT + BEHAVIORAL

CTO feedback: "ServiceResult must be a real structured error contract, not just string failures. The UI needs to know what class of failure happened."

**Tasks**:

7.1. **Define `ServiceResult<T>` and `ServiceResult` types** [CONTRACT] in Explore.Blazor.Client with:

```
IsSuccess : bool
Value : T (on success)
ErrorCode : string (machine-readable, e.g., "AUTH_SESSION_EXPIRED", "VALIDATION_FAILED")
UserMessage : string (safe for display)
DeveloperMessage : string? (diagnostics, not displayed in production)
ValidationErrors : IReadOnlyDictionary<string, string[]>? (field-level errors)
FailureCategory : enum { Validation, NotFound, Forbidden, SessionExpired, ProviderUnavailable, ProviderMisconfigured, TransientFailure, Unknown }
IsRetryable : bool
HttpStatusCode : int? (if relevant from API)
Exception : Exception? (internal only, not surfaced to UI)
```

Static factories: `Success(T)`, `Failure(ErrorCode, UserMessage)`, `ValidationFailure(errors)`, `NotFound(message)`, `SessionExpired()`, `TransientFailure(message)`, `FromApiException(ApiException)`

7.2. **Convert critical services** [BEHAVIORAL]: EventService, OrganizationService, AdminService, UserService.
7.3. **Convert remaining services** [BEHAVIORAL]: All service methods return ServiceResult.
7.4. **Define UI error handling tiers** [BEHAVIORAL] (CTO addition):
  - Inline validation for user-fixable issues
  - Banners/cards for domain/business failures
  - Snackbar only for minor transient notifications
  - Dedicated error states for load failures
  - Re-auth flow for session expired
  - "Feature unavailable" state for provider/config issues

7.5. Update consuming components for typed error handling [CONTRACT].
7.6. Keep ILogger.LogError calls for observability [STRUCTURAL].

**Before/after behavior matrix required** for this phase.

**Acceptance**: No service method returns null/empty on error. All failures typed via ServiceResult. UI handles each FailureCategory appropriately.

---

### Phase 12: Service Layer Decomposition
**Wave**: C (where needed for Phase 7) + D (remaining) | **Change Type**: STRUCTURAL + CONTRACT

CTO feedback: "Split by responsibility boundaries, not only by size."

**Tasks**:
12.1. **ImageStorageService.cs (972 lines)** [STRUCTURAL]: Natural responsibility split — S3UploadService, ImageProcessingService, ImageValidationService.
12.2. **AdminService.cs (764 lines)** [STRUCTURAL]: Split matching admin mental model — GovernanceAdminService, LookupAdminService, TenantAdminService.
12.3. **InstanceOnboardingService.cs (614 lines)** [STRUCTURAL]: Extract step-specific services. Use boring clear services, not clever pipelines (CTO: "boring clear services often beat clever pipelines unless the workflow is truly reusable").
12.4. Update DI registrations for split services [STRUCTURAL].

**Acceptance**: Each service has single responsibility boundary. DI updated.

---

### Phase 8: God Component Decomposition — EventList
**Wave**: D | **Change Type**: BEHAVIORAL

**Prerequisite**: Phase 17A (state classification) and Phase 7 (ServiceResult) must be complete.

CTO feedback: "Introduce page coordinator / page state model. Prevents code-behind from becoming a slimmer but still chaotic traffic controller."

**Tasks**:
8.1. **Implement EventListPageState** [BEHAVIORAL]: Page-level orchestration model with query/filter state, loading state, selected item state, commands/actions, capability flags.
8.2. Extract **EventListGrid** [STRUCTURAL] — virtualization, infinite scroll, card rendering.
8.3. Extract **EventDetailDrawer** [STRUCTURAL] — right sidebar detail panel.
8.4. Extract **EventListCustomizationDrawer** [STRUCTURAL] — layout/view preferences.
8.5. Extract **EventFilterPanel** [STRUCTURAL] — filters, tag/category selection.
8.6. Extract **EventListPagination** [STRUCTURAL] — pagination mode toggle and controls.
8.7. **EventList.razor.cs** becomes orchestrator delegating to page state + child components via parameters + EventCallback [BEHAVIORAL].
8.8. Reduce [Inject] count from 15 to ≤8 [STRUCTURAL].

**Before/after behavior matrix required**: infinite scroll, filtering, drawer open/close, pagination toggle.

**Acceptance**: EventList state model explicit. All extracted components have tests. Behavioral baselines unchanged.

---

### Phase 9: God Component Decomposition — EventDetail
**Wave**: D | **Change Type**: BEHAVIORAL

**Tasks**:
9.1. **Implement EventDetailPageState** [BEHAVIORAL] if complexity warrants.
9.2. Extract **EventSessionList** [STRUCTURAL] — session listing and management.
9.3. Extract **EventRegistrationPanel** [STRUCTURAL] — registration status, actions.
9.4. Extract **EventReviewSection** [STRUCTURAL] — review display and submission.
9.5. Extract **EventAgendaView** [STRUCTURAL] — agenda/schedule display.

**Before/after behavior matrix required**: session list, registration intent, review submission, agenda display.

**Acceptance**: Behavioral baselines unchanged. Components testable.

---

### Phase 10: God Component Decomposition — CreateEvent & EventEdit
**Wave**: D | **Change Type**: BEHAVIORAL

**Tasks**:
10.1. Extract **EventSessionEditor** [STRUCTURAL] — session form with time/location/room.
10.2. Extract **EventAspectEditor** [STRUCTURAL] — Islamic/Tech aspect forms (consolidate duplicate dialogs).
10.3. Extract **EventSpeakerManager** [STRUCTURAL] — speaker selection and management.
10.4. Extract **EventFormFields** [STRUCTURAL] — shared form fields between Create and Edit.

**Acceptance**: Shared components extracted. No form component excessively large without justification.

---

### Phase 11: God Component Decomposition — Admin Pages
**Wave**: D | **Change Type**: STRUCTURAL

**Tasks**:
11.1. **FooterSettings.razor (781 lines)** [STRUCTURAL]: Extract FooterTemplateEditor, FooterLinkGroupEditor, FooterPreview.
11.2. **InstanceGovernanceSection.razor (738 lines)** [STRUCTURAL]: Extract governance setting groups into separate section components.
11.3. **TenantLookupTablesSection.razor (668 lines)** [STRUCTURAL]: Extract CategoryManagement, TagManagement, LocationManagement, MadhabManagement.

**Acceptance**: Admin components decomposed with clear responsibility boundaries.

---

### Phase 13: Duplicated Pattern Extraction
**Wave**: D (late — after decompositions settle) | **Change Type**: STRUCTURAL

CTO feedback: "Pattern extraction is valuable after decompositions settle. If done too early, you extract abstractions from unstable understanding. Wait for 2-3 convergent implementations."

**Tasks**:
13.1. **Generic TriStateFilter\<T\>** [STRUCTURAL]: Extract from TriStateTagFilterDropdown and TriStateCategoryFilterDropdown.
13.2. **ListDetailPageBase\<TItem\>** [STRUCTURAL]: Extract common list+detail+filter+pagination pattern — only after EventList, MyEvents, MyOrganizations decompositions have stabilized.
13.3. **Dialog standardization** [STRUCTURAL]: Ensure all dialogs use AppDialogShell.
13.4. **SettingsSectionBase** [STRUCTURAL]: Extract common settings section pattern.

**Acceptance**: Duplicated patterns extracted. Existing components refactored to use shared abstractions.

---

### Phase 6B: DynamicAuthSchemeManager Refactor (Wave E — after stabilization)
**Wave**: E | **Change Type**: SECURITY

**Prerequisite**: Phase 6A complete. Architectural decision on runtime vs startup-only scheme mutation answered.

**Tasks**:
6B.1. Extract provider-specific logic: KeycloakSchemeRegistration, GoogleSchemeRegistration, AtprotoSchemeRegistration [SECURITY].
6B.2. Simplify dual-locking to single concurrency primitive where possible [SECURITY].
6B.3. All auth flow integration tests pass [SECURITY].

**Before/after behavior matrix required**. Smoke scenarios executed. Rollback path documented.

**Acceptance**: Auth scheme manager simplified. All auth flows work. Tests comprehensive.

---

### Phase 14: CSS Compliance
**Wave**: E | **Change Type**: STRUCTURAL

CTO feedback: "Prioritize by themeability impact. Since self-hostable, brand customization matters."

Priority order:
1. Hardcoded colors breaking themeability
2. Inline styles harming maintainability
3. Layout values hurting responsiveness/accessibility
4. `::deep` misuse
5. Tokenization cleanup

**Tasks**:
14.1. Replace 28 hardcoded colors in AdminListDetails.razor.css [STRUCTURAL].
14.2. Replace hardcoded px in NotificationPanel.razor.css [STRUCTURAL].
14.3. Replace hardcoded colors in Setup, GroupProfile, OrganizationProfile [STRUCTURAL].
14.4. Move MainLayout inline styles to .razor.css with BEM class [STRUCTURAL].
14.5. Move SetupLayout inline styles to .razor.css [STRUCTURAL].
14.6. Audit remaining 77 inline styles — keep only truly dynamic ones [STRUCTURAL].

**Acceptance**: Zero hardcoded color values in .razor.css. Inline styles only for dynamic values.

---

### Phase 15: ABOUTME Header Sweep
**Wave**: E | **Change Type**: STRUCTURAL

CTO feedback: "Low-value hygiene. Automate, batch, minimize manual focus. Do not compete with high-value engineering work."

**Tasks**:
15.1. Attempt automation (script or template) for ABOUTME insertion on files missing headers.
15.2. If automation impractical, batch manually for all 30+ identified files.
15.3. Verify all new files created during this refactor have ABOUTME headers.

**Acceptance**: All files have ABOUTME headers.

---

### Phase 17B: State Management Standardization (Wave E — after decompositions)
**Wave**: E | **Change Type**: BEHAVIORAL

Post-decomposition standardization (17A was classification, this is enforcement).

**Tasks**:
17B.1. Verify all decomposed components follow the state classification from 17A.
17B.2. Evaluate whether FeatureStateContainer pattern should be standardized across pages.
17B.3. Document final state management conventions.

**Acceptance**: State management patterns documented and consistently applied.

---

### Phase 18: Test Infrastructure Expansion
**Wave**: E | **Change Type**: STRUCTURAL

CTO addition: Include **workflow/smoke scenario tests**, not just unit/rendering tests.

**Tasks**:
18.1. Add component rendering tests for all extracted components from Phases 8-11 [STRUCTURAL].
18.2. Add form validation tests for event creation/editing validators [STRUCTURAL].
18.3. Add error handling tests for ServiceResult error boundaries (Phase 7) [BEHAVIORAL].
18.4. Expand architecture tests for new guardrails (Phase 1) [STRUCTURAL].
18.5. Add accessibility tests for new shared components [STRUCTURAL].
18.6. **Add workflow/smoke scenario tests** [BEHAVIORAL] (CTO addition):
  - Login/auth provider flow
  - Event list browsing/filtering end-to-end
  - Registration flow
  - Admin settings change flow
  Even a few high-value paths massively reduce refactor risk.

**Acceptance**: Every extracted component has at least 1 rendering test. Critical workflow scenarios covered.

---

### Phase 19: Architecture Conformance Sweep
**Wave**: E | **Change Type**: STRUCTURAL + OPERATOR

CTO addition: Include self-hosted deployment verification.

**Tasks**:
19.1. Run all architecture tests — zero failures [STRUCTURAL].
19.2. Run full build — zero errors [STRUCTURAL].
19.3. Run all Blazor test suites — all pass [STRUCTURAL].
19.4. Verify @layer compliance [STRUCTURAL].
19.5. Verify BEM naming compliance in all new .razor.css files [STRUCTURAL].
19.6. Verify wrapper component usage in all new components [STRUCTURAL].
19.7. LSP diagnostics clean on changed files [STRUCTURAL].
19.8. **Self-hosted deployment verification** [OPERATOR] (CTO addition): Verify app starts cleanly from minimal documented self-hosted config. Verify degraded config produces useful diagnostics.

**Acceptance**: All automated checks pass. Self-hosted startup verified.

---

### Phase 20: Verification & Handoff
**Wave**: E | **Change Type**: STRUCTURAL + OPERATOR

CTO feedback: "Strengthen documentation deliverables for self-hosted enterprise software."

**Tasks**:
20.1. Run complete build: `dotnet build --configuration Release` [STRUCTURAL].
20.2. Run all test suites individually, document results [STRUCTURAL].
20.3. Update `dev/_journal/journal.md` with key refactor insights [STRUCTURAL].
20.4. Update `docs/BLAZOR.md` with new patterns/conventions + middleware pipeline table [STRUCTURAL].
20.5. Update `docs/CODEBASE_STRUCTURE.md` for new file locations [STRUCTURAL].
20.6. Update `docs/CODEBASE_INSIGHTS.md` with Blazor-specific insights [STRUCTURAL].
20.7. Update `docs/TROUBLESHOOTING.md` with common Blazor/BFF issues [OPERATOR].
20.8. **Self-hosting config reference** [OPERATOR] (CTO addition): Document auth provider setup, BFF proxy config, cookie/security settings.
20.9. **BFF request flow diagram** [OPERATOR] (CTO addition): Visual diagram of request flow through BFF.
20.10. **Error-handling conventions doc** [OPERATOR] (CTO addition): Document ServiceResult usage patterns for future contributors.
20.11. Update this plan's context and tasks files with final status [STRUCTURAL].

**Acceptance**: All docs updated, build green, all tests pass, operator documentation complete, plan marked complete.

---

## Wave Sequencing & Dependencies

```
WAVE A — Safety & Fitness Functions
  Phase 0 (Baseline)
    ↓
  Phase 1 (Guardrails) ── Phase 5 (Observability) [parallel]
    ↓
  Phase 17A (State Classification) ── Phase X (Operability) [parallel]

WAVE B — BFF Hardening
  Phase 2 (Security) ── Phase 4 (Middleware) [parallel]
    ↓
  Phase 3 (Endpoint Decomp) ── Phase 16 (Claims/HttpClient) [parallel]
    ↓
  Phase 6A (Auth Stabilization — stabilize & test only)

WAVE C — Service Contract Reform
  Phase 7 (ServiceResult) ← depends on Phase X.3 (error state distinction)
    ↓
  Phase 12 (Service Decomp — contract-changing splits only)

WAVE D — UI Decomposition
  Phase 8 (EventList) ← depends on Phase 17A + Phase 7
    ↓
  Phase 9 (EventDetail) ── Phase 10 (Create/Edit) ── Phase 11 (Admin) [parallel]
    ↓
  Phase 12 (Service Decomp — remaining splits)
    ↓
  Phase 13 (Pattern Extraction — after decompositions settle)

WAVE E — Conformance & Operability
  Phase 6B (Auth Refactor — only after 6A complete)
    ↓
  Phase 14 (CSS) ── Phase 15 (ABOUTME) ── Phase 17B (State Standardization) [parallel]
    ↓
  Phase 18 (Test Expansion)
    ↓
  Phase 19 (Conformance)
    ↓
  Phase 20 (Handoff)
```

---

## Risk Register

| Risk | Impact | Probability | Mitigation | Change Type |
|------|--------|-------------|------------|-------------|
| Auth scheme manager refactor breaks login | CRITICAL | Medium | Phase 6 split: stabilize first (6A), refactor after (6B). Behavior matrix required. | SECURITY |
| EventList decomposition breaks infinite scroll | HIGH | Medium | State classification (17A) before extraction (8). Page state model. Behavioral baselines. | BEHAVIORAL |
| ServiceResult contract change breaks UI | HIGH | Medium | Convert one service at a time. UI error handling tiers defined upfront. | CONTRACT |
| CSS token migration breaks visual appearance | MEDIUM | Low | Visual regression check after each file. | STRUCTURAL |
| Middleware extraction changes pipeline order | HIGH | Low | Document before/after order as living table. Integration tests. | SECURITY |
| YARP timeout too aggressive | MEDIUM | Low | Start generous (30s), tune down. | SECURITY |
| Startup validation too strict for existing deployments | MEDIUM | Medium | Validation warns but does not block on non-critical misconfig. | OPERATOR |
| Config diagnostics leaks sensitive info | HIGH | Low | Redact secrets in all diagnostic surfaces. Never log tokens/passwords. | SECURITY |

---

## Acceptance Gates for Semantic Phases (CTO Requirement)

For Phases 6A/6B, 7, 8, 9, 10, 17A/17B:

- [ ] Before/after behavior matrix documented
- [ ] Smoke scenarios executed for affected user flows
- [ ] Operator-visible errors reviewed (no regression in error clarity)

---

## Change Management Discipline (CTO Requirement)

1. **One architectural decision log per major semantic change** (in `dev/_journal/MAJOR_DECISIONS.md`)
2. **PR scope rule**: No mixing SECURITY refactor and UI decomposition in same PR
3. **Mandatory before/after behavior notes** for auth/state/service contract changes
4. **Contributor enforcement**: Templates for new components/services, wrapper component usage guidance, error handling guidance in docs

---

## Deferred Work (Explicitly Out of Scope)

1. **Centralized state management** (Fluxor/Redux) — too large for refactor
2. **NSwag client splitting** — 86k-line generated file; NSwag config changes needed
3. **PWA/offline support** — feature work
4. **Visual regression testing** — requires Playwright infrastructure expansion
5. **Real API integration tests** (non-mocked) — different testing strategy
6. **Refresh token rotation** — security feature
7. **Multi-language rendering tests** — feature-level testing
8. **Client-side caching layer** — performance optimization
