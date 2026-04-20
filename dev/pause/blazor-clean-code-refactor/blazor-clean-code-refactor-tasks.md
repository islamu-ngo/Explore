ABOUTME: Task checklist v3 mirror of plan.md, organized by waves with explicit change-type labels and Wave 0 hotfix track.
ABOUTME: Source of truth for Sisyphus session continuity. All boxes start unchecked.

# Tasks: Blazor Clean Code Refactor Program — v3

> Last Updated: 2026-04-18
> Mirror of `blazor-clean-code-refactor-plan.md` v3

Legend: STRUCTURAL · BEHAVIORAL · SECURITY · CONTRACT · OPERATOR · HOTFIX

---

## Wave 0 — Pre-Flight Hotfix Track (BLOCKING)

**Must merge to main BEFORE any Wave A work begins.** Each item is an isolated PR with regression test.

### Phase 0.A — Singleton-Holding-Per-User-State Hotfix [HOTFIX/SECURITY] ✅
- [x] 0.A.1 Removed `static` from SetupSecretSessionService `_store` and `CleanupExpiredEntries()` (Singleton → instance field is sufficient)
- [ ] 0.A.1 Integration test: two concurrent user sessions cannot read each other's setup secret (deferred to Wave A Phase 1)
- [ ] 0.A.2 Split `IDynamicAuthSchemeManager` → deferred to Wave B Phase 6A/6B
- [x] 0.A.3 Arch test: no Singleton with mutable user/circuit state (implemented in Wave A Phase 1 as Rule 1.9)

### Phase 0.B — async void Crash Path Hotfix [HOTFIX/BEHAVIORAL] ✅
- [x] 0.B.1 Converted `AnalyticsInitializer.razor:253 OnLocationChanged` from `async void` to `_ = InvokeAsync(async () => { ... })` fire-and-forget with try/catch
- [x] 0.B.2 Audit complete — only 3 remaining `async void` are Timer/event callbacks (Phase 8-10 will fix)
- [x] 0.B.2 Arch test forbidding `async void` outside whitelisted patterns (Wave A Phase 1, Rule 1.10)
- [x] 0.B.3 NavigationManager subscription properly disposed in IDisposable.Dispose (verified)

### Phase 0.C — .Result Sync-Over-Async Hotfix [HOTFIX/BEHAVIORAL] ✅
- [x] 0.C.1 Replaced all 4 `.Result` sites with `await`: EventDetail:133,725,726; EventList:725,726; OrganizationDetails:141
- [x] 0.C.2 Arch test forbidding `.Result`/`.Wait()` on Task (Wave A Phase 1, Rule 1.11)

### Phase 0.D — Auth Endpoints Cache-Control Hotfix [HOTFIX/SECURITY] ✅
- [x] 0.D.1 Added `Cache-Control: no-store, no-cache` + `Pragma: no-cache` to HandleSignoutAsync, HandleAuthStatus, HandleRefreshSchemesAsync
- [ ] 0.D.2 Integration test asserting headers present (deferred to Phase 2)

### Phase 0.E — YARP Cluster RequestTimeout Hotfix [HOTFIX/SECURITY/OPERATOR] ✅
- [x] 0.E.1 Added `HttpRequest = new ForwarderRequestConfig { ActivityTimeout = TimeSpan.FromSeconds(30) }` to ClusterConfig
- [ ] 0.E.2 Document rationale in `docs/BLAZOR.md` BFF section (deferred to Phase X)

### Phase 0.F — Wave 0 Regression Test Pack [HOTFIX/STRUCTURAL] ✅
- [ ] 0.F.1 Bundle Wave 0 tests under `[Trait("Category", "Wave0Regression")]` (deferred — TUnit uses different categorization)
- [x] 0.F.2 Full test pack verified — zero new failures across all test projects
- [ ] 0.F.3 Document Wave 0 hotfix log in `dev/_journal/MAJOR_DECISIONS.md`

---

## Wave A — Safety, Fitness Functions & Render Mode Correction

### Phase 0 — Safety Baseline & Isolation [STRUCTURAL]
- [ ] 0.1 `dotnet build --configuration Release --verbosity quiet` green
- [ ] 0.2 Run all 9 Blazor test suites, document baseline
- [ ] 0.3 Create branch `refactor/blazor-clean-code` rebased on Wave 0 hotfixes
- [ ] 0.4 Document any pre-existing test failures
- [ ] 0.5 Capture user-visible behavioral baselines via manual smoke (auth, browse/filter, detail+registration, admin save)

### Phase 1 — Architecture Guardrails [STRUCTURAL]
- [x] 1.1 Arch test: No component injects IEventApiClient directly (pre-existing; verified)
- [x] 1.2 Arch test: No Console.WriteLine in production
- [x] 1.3 Arch test: No inline middleware lambdas >5 lines
- [x] 1.4 Arch test: All [Inject] services use interfaces
- [x] 1.5 Arch test: No `new DialogOptions()` outside DialogOptionsFactory
- [x] 1.6 Arch test: No NavigationManager in Components/Common or Components/Collection
- [x] 1.7 Arch test: No IJSRuntime in Services/ unless in Interop/ or Http/
- [x] 1.8 Arch test: No ISnackbar in data services
- [x] 1.9 Arch test: No singleton holding mutable user/circuit/request state
- [x] 1.10 Arch test: No async void outside whitelisted event handlers
- [x] 1.11 Arch test: No `.Result`/`.Wait()` on Task in Blazor projects
- [x] 1.12 Arch test: No IConfiguration injected directly (use IOptions<T>)
- [x] 1.13 Arch test: No GetRequiredService<T>() inside callback bodies
- [x] 1.14 Arch test: No model classes in `*Service.cs` interface files
- [ ] 1.15 Arch test: No new hardcoded user-facing strings outside legal pages (DEFERRED — requires localization inventory; revisit in later phase)

### Phase 5 — Observability Hygiene [BEHAVIORAL/OPERATOR]
- [x] 5.1 Remove Console.WriteLine from ConfigurationExtension.cs + LazyAssemblyLoader.cs + Setup.razor — swapped to structured ILogger (commit `17243e4e`); Rule 1.02 arch guardrail now enforces zero violations
- [ ] 5.2 Audit ILogger severity semantics in BFF
- [ ] 5.3 Verify correlation ID propagation BFF → YARP → API
- [ ] 5.4 Add event IDs / log categories
- [ ] 5.5 Verify no secret/token leakage in logs
- [ ] 5.6 Auth provider startup config validation FAILS on critical misconfig (currently warns only)
- [ ] 5.7 Add tenant-aware log scope (slug + ID via ILogger.BeginScope)
- [ ] 5.8 Replace 36 `RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("...")` with `HttpContextExtensions.GetLogger`

### Phase 17A — State Classification [STRUCTURAL]
- [ ] 17A.1 Document state mgmt strategy (URL/Cascading/Scoped/Local)
- [ ] 17A.2 Classify EventList state (30+ fields)
- [ ] 17A.3 Classify EventDetail/CreateEvent/EventEdit state
- [ ] 17A.4 Design EventListPageState + EventDetailPageState (page coordinator pattern)
- [ ] 17A.5 Verify all filter/pagination state is URL-driven
- [ ] 17A.6 Document page-local presentation model convention (NOT app-wide ViewModel)

### Phase X — Operability & Self-Hoster Diagnostics [OPERATOR]
- [ ] X.1 Startup config validation FAILS on critical missing auth config
- [ ] X.1 Validate BFF proxy targets, cookie/security settings, reverse proxy headers
- [ ] X.2 Add health checks: ApiBackend, OidcProvider; map /healthz + /readyz
- [ ] X.2 Clear feature-disabled reasons in UI (policy/config/unreachable/misconfigured)
- [ ] X.3 Document 8 error states (Validation/NotFound/Forbidden/SessionExpired/ProviderUnavailable/ProviderMisconfigured/TransientFailure/Unknown)
- [ ] X.4 Correlation ID visible in admin error UI; "copy diagnostics" UX
- [ ] X.4 Log messages with tenant/provider/context scope
- [ ] X.5 Document configurable vs product-enforced security settings
- [ ] X.6 Add OpenTelemetry to BFF: AddOpenTelemetry().WithTracing(AspNetCore + HttpClient + custom Auth + Yarp ActivitySources).WithMetrics(AspNetCore + HttpClient + Runtime).Add OTLP exporter (configurable)

### Phase A0 — Render Mode Correction (NEW) [BEHAVIORAL]
- [ ] A0.1 Build eligibility matrix for all 32 pages (Server-only / Auto-eligible / Static SSR)
- [ ] A0.2 Cohort 1: Convert legal/policy pages to Static SSR (no rendermode)
- [ ] A0.3 Cohort 2: Convert public Home/HomeStart/OrganizationDetails/OrganizationReviews/event browse to InteractiveAuto
- [ ] A0.4 Cohort 3: Convert MyEvents/MyReviews/Settings/Notifications to InteractiveAuto
- [ ] A0.5 Cohort 4: Document Server-only pages (onboarding/admin/governance/AuthProviderConfiguration) and reasons
- [ ] A0.6 Service registration audit per Auto cohort (verify both server and WASM register everything)
- [ ] A0.7 PersistentComponentState wiring: ensure App.razor renders <PersistentComponentState>; Home/HomeStart/OrganizationReviews skip re-fetch on hydration
- [ ] A0.8 Prerender hygiene: audit OnInitializedAsync per Auto cohort for prerender-unsafe ops
- [ ] A0.9 Cohort smoke test after each cohort merge

---

## Wave B — BFF Hardening

### Phase 2 — BFF Security Hardening [SECURITY]
- [ ] 2.1 XSRF-TOKEN cookie HttpOnly intentional (double-submit) — documented
- [ ] 2.2 Setup secret deduplication: single shared service
- [ ] 2.3 Verify AccessTokenForwardingHandler exists and is registered
- [ ] 2.4 YARP timeout: review and tune per workload (Wave 0 set 30s)
- [ ] 2.5 Cookie expiration review (current 7-day + sliding)
- [ ] 2.6 Open redirect safety: validate auth return URLs
- [ ] 2.7 Anti-cache headers expanded to all sensitive BFF endpoints (preferences, setup secret)
- [ ] 2.8 Cookie config documented for reverse proxy / multi-instance
- [ ] 2.9 Document configurable vs enforced security settings
- [ ] 2.10 Security headers on ALL responses: X-Frame-Options DENY, X-Content-Type-Options nosniff, Referrer-Policy, Permissions-Policy
- [ ] 2.11 Minimum viable CSP in Content-Security-Policy-Report-Only mode for one release cycle
- [ ] 2.12 IdP token revocation on logout via OIDC end_session_endpoint (Keycloak/Google)
- [ ] 2.13 XSRF token rotation policy decided + implemented
- [ ] 2.14 Front-channel logout documented (implementation deferred unless trivial)

### Phase 3 — BFF Endpoint Decomposition [STRUCTURAL]
- [ ] 3.1 Split BffAuthEndpoints into Status/Mutation/Debug
- [ ] 3.2 BffSetupSecretEndpoints: extract individual handlers
- [ ] 3.3 BffPreferenceEndpoints: extract individual handlers
- [ ] 3.4 Implement HttpContextExtensions.GetLogger
- [ ] 3.5 Split CircuitAccessTokenService → SetupSecretSessionService own file

### Phase 4 — BFF Middleware Extraction [STRUCTURAL]
- [~] 4.1 Extract AntiforgeryTokenDistributionMiddleware — lightweight path taken (lambda → private static `DistributeAntiforgeryTokenAsync`, same file). Full IMiddleware class deferred
- [~] 4.2 Extract StartupRedirectMiddleware — lightweight path taken (lambda → private static `HandleStartupRedirectAsync`). Full IMiddleware class deferred
- [~] 4.3 Extract AccessTokenCaptureMiddleware — lightweight path taken (lambda → private static `CaptureAccessTokenAsync`). Full IMiddleware class deferred
- [~] 4.4 Move Console.CancelKeyPress to Program.cs / hosted service — lambda → private static `OnCancelKeyPress` (same file). Full hosted service deferred
- [ ] 4.5 Verify pipeline order after extraction
- [ ] 4.6 Document pipeline order as living table in docs/BLAZOR.md
- [ ] 4.7 Per-handler timeout overrides in DelegatingHandlers (linked CancellationTokenSource)

**Phase 4 Lightweight Completion Note** (commit `6e5e37f0`): Per user decision, this session performed the *minimal* refactor — extracted all 5 inline middleware lambdas AND 3 shutdown event-handler lambdas into private static methods inside `Explore.Blazor/Extensions/MiddlewareExtensions.cs` (same file, no new classes). Arch guardrail Rule 1.03 now enforces zero violations (`Known_MiddlewareLambda_LongBodies` HashSet is empty). The formal Phase 4 items 4.1–4.4 remain open for future IMiddleware-class extraction + hosted-service isolation.

### Phase 16 — BFF Claims & HttpClient Rationalization [BEHAVIORAL/SECURITY]
- [ ] 16.1 Extract shared claim type constants
- [ ] 16.2 Audit typed client registrations (no direct API bypass)
- [ ] 16.3 Consolidate BrowserCredentialsMessageHandler constructors
- [ ] 16.4 Verify auth context propagation server↔WASM
- [ ] 16.5 Verify clear server-side vs browser-side ownership
- [ ] 16.6 Eliminate magic claim type strings in AuthStateService.cs:44-46
- [ ] 16.7 Eliminate magic config strings in DynamicAuthSchemeManager.cs:63-68 → strongly-typed IOptions<KeycloakOptions>/IOptions<GoogleOptions>

### Phase 6A — DynamicAuthSchemeManager Stabilization [SECURITY]
- [ ] 6A.1 Characterize existing state transitions
- [ ] 6A.2 Document concurrency model (dual-locking)
- [ ] 6A.3 Document supported providers matrix
- [ ] 6A.4 Add integration tests for all auth flow paths
- [ ] 6A.5 Complete or remove AtprotoAuthenticationHandler (TODO/incomplete)
- [ ] 6A.6 Document rollback path for scheme registration mid-flight failures
- [ ] 6A.7 Eliminate service-locator in DynamicAuthSchemeManager.cs:502-503
- [ ] 6A.8 Eliminate service-locator in Explore.Blazor.Client/Program.cs:50-51

---

## Wave C — Service Contract Reform

### Phase 7 — Service Layer Error Handling [CONTRACT/BEHAVIORAL]
- [ ] 7.1 Define ServiceResult<T> + ServiceResult types with FailureCategory enum
- [ ] 7.2 Split IEventService → IEventQueryService / IEventCommandService / IEventSessionService / IEventRegistrationService (BEFORE conversion)
- [ ] 7.3 Convert critical services: 4 new event interfaces, OrganizationService, AdminService (post-12), UserService
- [ ] 7.4 Convert remaining services
- [ ] 7.5 Define UI error handling tiers (inline / banner / snackbar / dedicated state / re-auth / feature-unavailable)
- [ ] 7.6 Update consuming components for typed error handling
- [ ] 7.7 Keep ILogger.LogError calls for observability
- [ ] 7.8 Eliminate empty/swallowing catches: EventService 395,407,98,112,126; UserSettingsService 76; Program.cs 121

### Phase 12-partial — Contract-Driven Service Decomposition [STRUCTURAL/CONTRACT]
- [ ] 12.5 Move 11 model classes out of IFooterAdminService.cs into Models/
- [ ] 12.5 Move UserConsentViewModel/SharedContactViewModel out of IContactShareConsentService.cs into Models/
- [ ] 12.6 LookupCacheService dependency reduction (currently 9 deps → split per lookup domain)

---

## Wave D — UI Decomposition

### Phase 8 — EventList [BEHAVIORAL]
- [ ] 8.1 Implement EventListPageState
- [ ] 8.2 Extract EventListGrid
- [ ] 8.3 Extract EventDetailDrawer
- [ ] 8.4 Extract EventListCustomizationDrawer
- [ ] 8.5 Extract EventFilterPanel
- [ ] 8.6 Extract EventListPagination
- [ ] 8.7 EventList.razor.cs becomes orchestrator
- [ ] 8.8 Reduce [Inject] count from 15 to ≤8
- [ ] 8.9 Add @key to all foreach in EventSeriesRail, EventTimeline, EventFilterBar
- [ ] 8.10 Memoize EventTimeline.GroupedEvents (recompute only on Events change)

### Phase 9 — EventDetail [BEHAVIORAL]
- [ ] 9.1 EventDetailPageState (if warranted)
- [ ] 9.2 Extract EventSessionList
- [ ] 9.3 Extract EventRegistrationPanel
- [ ] 9.4 Extract EventReviewSection
- [ ] 9.5 Extract EventAgendaView
- [ ] 9.6 Fix S3Image.razor StateHasChanged-without-lifecycle-check (gate with !_disposed via InvokeAsync)

### Phase 10 — CreateEvent & EventEdit [BEHAVIORAL]
- [ ] 10.1 Extract EventSessionEditor
- [ ] 10.2 Extract EventAspectEditor (consolidate Islamic/Tech dialogs)
- [ ] 10.3 Extract EventSpeakerManager
- [ ] 10.4 Extract EventFormFields shared
- [ ] 10.5 Form validation consistency: remove dead validators in CreateSessionDialog/EditSessionDialog; choose FluentValidation OR DataAnnotations per form; document in docs/BLAZOR.md

### Phase 11 — Admin Pages [STRUCTURAL]
- [ ] 11.1 FooterSettings → FooterTemplateEditor / FooterLinkGroupEditor / FooterPreview
- [ ] 11.2 InstanceGovernanceSection → setting groups
- [ ] 11.3 TenantLookupTablesSection → CategoryManagement / TagManagement / LocationManagement / MadhabManagement

### Phase 12-rest — Remaining Service Decomposition [STRUCTURAL]
- [ ] 12.1 ImageStorageService → S3UploadService / ImageProcessingService / ImageValidationService
- [ ] 12.2 AdminService → GovernanceAdminService / LookupAdminService / TenantAdminService
- [ ] 12.3 InstanceOnboardingService → step-specific services
- [ ] 12.4 Update DI registrations for split services

### Phase 13 — Pattern Extraction [STRUCTURAL]
- [ ] 13.1 Generic TriStateFilter<T>
- [ ] 13.2 ListDetailPageBase<TItem> (after Phase 8/9 settle)
- [ ] 13.3 Dialog standardization via AppDialogShell
- [ ] 13.4 SettingsSectionBase
- [ ] 13.5 TenantContextProvider OnParametersSetAsync
- [ ] 13.6 EventSessionManager parameter-change detection (ShowRegisterButton)

---

## Wave E — Conformance, Test Hardening & Operability

### Phase 6B — DynamicAuthSchemeManager Refactor [SECURITY]
- [ ] 6B.1 Extract KeycloakSchemeRegistration / GoogleSchemeRegistration / AtprotoSchemeRegistration
- [ ] 6B.2 Simplify dual-locking to single concurrency primitive
- [ ] 6B.3 All auth integration tests pass

### Phase 14 — CSS Compliance + a11y on Touched Components [STRUCTURAL]
- [ ] 14.1 Replace 28 hardcoded colors in AdminListDetails.razor.css
- [ ] 14.2 Replace hardcoded px in NotificationPanel.razor.css
- [ ] 14.3 Replace hardcoded colors in Setup/GroupProfile/OrganizationProfile
- [ ] 14.4 Move MainLayout inline styles to .razor.css with BEM
- [ ] 14.5 Move SetupLayout inline styles to .razor.css
- [ ] 14.6 Audit remaining 77 inline styles (keep only truly dynamic)
- [ ] 14.7 AppButton: AriaLabel parameter wired to aria-label; arch test for icon-only without AriaLabel
- [ ] 14.8 AppIconButton: AriaLabel REQUIRED parameter
- [ ] 14.9 AppTextField: aria-invalid + aria-describedby wired
- [ ] 14.10 S3Image: REQUIRED Alt parameter; add loading="lazy"
- [ ] 14.11 EventCard MudImage: pass Alt + loading="lazy"
- [ ] 14.12 Loading.razor: aria-live="polite" + aria-busy="true"
- [ ] 14.13 ErrorState.razor: aria-live="assertive" on role=alert
- [ ] 14.14 MainLayout: <FocusOnNavigate> + dialog focus-trap audit
- [ ] 14.15 RTL/MudRTLProvider sync via cascading service
- [ ] 14.16 [dir="rtl"] selectors for touched-component CSS

### Phase 15 — ABOUTME Header Sweep [STRUCTURAL]
- [ ] 15.1 Automate ABOUTME insertion if possible
- [ ] 15.2 Otherwise batch manually for 30+ identified files
- [ ] 15.3 Verify all new refactor files have ABOUTME

### Phase 17B — State Management Standardization [BEHAVIORAL]
- [ ] 17B.1 Verify decomposed components follow 17A classification
- [ ] 17B.2 Evaluate FeatureStateContainer pattern standardization
- [ ] 17B.3 Document final state management conventions

### Phase 18 — Test Infrastructure Expansion [STRUCTURAL]
- [ ] 18.1 Component rendering tests for extracted components (Phases 8-11)
- [ ] 18.2 Form validation tests for event creation/editing
- [ ] 18.3 ServiceResult error-boundary tests
- [ ] 18.4 Architecture tests expanded (Phase 1)
- [ ] 18.5 Accessibility tests for new shared components
- [ ] 18.6 Workflow/smoke scenario tests (login, browse, registration, admin save)

### Phase 18B — Test Migration & Coverage Closure (NEW) [STRUCTURAL]
- [ ] 18B.1 Migrate 154 brittle Markup.Contains() in 18 files to data-testid (cut.Find / cut.FindAll)
- [ ] 18B.2 MockServiceFactory: add 20 missing service mocks
- [ ] 18B.3 BlazorTestContext helpers: SetServiceThrows, SimulateNetworkFailure, WaitForRenderComplete
- [ ] 18B.4 PlaywrightFixture: NavigateAndWaitForReady, ScreenshotOnFailure, page object models
- [ ] 18B.5 8 critical E2E user-journey scenarios (login, logout-IdP-terminated, discovery+filter, registration, org+members, admin save, multi-tenant switch, error 404/500/network)
- [ ] 18B.6 Blazor architecture-test suite expansion (2 → 15)
- [ ] 18B.7 DelegatingHandler depth tests (error/timeout/cancellation/retry for AccessToken/BrowserCredentials/SetupSecret handlers)

### Phase 19 — Architecture Conformance Sweep [STRUCTURAL/OPERATOR]
- [ ] 19.1 All architecture tests pass
- [ ] 19.2 Full build green
- [ ] 19.3 All Blazor test suites pass
- [ ] 19.4 @layer compliance verified
- [ ] 19.5 BEM naming compliance in all new .razor.css
- [ ] 19.6 Wrapper component usage verified in new components
- [ ] 19.7 LSP diagnostics clean on changed files
- [ ] 19.8 Self-hosted deployment startup verified

### Phase 20 — Verification & Handoff [STRUCTURAL/OPERATOR]
- [ ] 20.1 Complete build green
- [ ] 20.2 All test suites individually documented
- [ ] 20.3 Update dev/_journal/journal.md
- [ ] 20.4 Update docs/BLAZOR.md (patterns + middleware pipeline + render-mode matrix + form validation policy + a11y conventions)
- [ ] 20.5 Update docs/CODEBASE_STRUCTURE.md
- [ ] 20.6 Update docs/CODEBASE_INSIGHTS.md
- [ ] 20.7 Update docs/TROUBLESHOOTING.md (BFF issues + Wave 0 hotfix lessons)
- [ ] 20.8 Self-hosting config reference (auth, BFF proxy, cookies, OTel, health checks)
- [ ] 20.9 BFF request flow diagram
- [ ] 20.10 Error-handling conventions doc
- [ ] 20.11 Render-mode decision log (eligibility matrix outcomes)
- [ ] 20.12 Plan context/tasks updated with final status
