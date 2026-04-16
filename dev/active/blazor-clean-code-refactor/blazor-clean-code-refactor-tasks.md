ABOUTME: Task breakdown for Blazor clean code refactor organized by delivery waves with change type labels.
ABOUTME: Tracks implementation progress across 5 waves (A-E) with CTO-approved risk classification.

# Tasks: Blazor Clean Code Refactor

> Status: PLANNING COMPLETE (v2 — CTO-reviewed) — Ready for implementation
> Last Updated: 2026-04-16

---

## WAVE A — Safety & Fitness Functions

### Phase 0: Safety Baseline [STRUCTURAL]
- [ ] Build verification passes
- [ ] All Blazor test suites run, baseline documented
- [ ] Git branch created
- [ ] Pre-existing failures documented
- [ ] User-visible behavioral baselines captured (auth, event list, detail, admin, setup)

### Phase 1: Architecture Guardrails [STRUCTURAL]
- [ ] Arch test: No Console.WriteLine in Blazor production code
- [ ] Arch test: No inline middleware lambdas >5 lines
- [ ] Arch test: All [Inject] use interfaces
- [ ] Arch test: No `new DialogOptions()` outside DialogOptionsFactory
- [ ] Arch test: No NavigationManager in Common/Collection components
- [ ] Arch test: No IJSRuntime in Services/ (except Interop/Http/)
- [ ] Arch test: No ISnackbar in data service classes
- [ ] Verify existing arch test: No IEventApiClient in components

### Phase 5: Observability Hygiene [BEHAVIORAL + OPERATOR]
- [ ] Remove 8 Console.WriteLines from ConfigurationExtension.cs
- [ ] Audit ILogger severity semantics (warning vs error, operator-meaningful messages)
- [ ] Verify correlation ID propagation through BFF → YARP → API
- [ ] Add consistent log categories for BFF filtering
- [ ] Verify no secret/token leakage in auth/provider logs

### Phase 17A: State Classification [STRUCTURAL]
- [ ] Document state management strategy (URL / CascadingValue / Scoped Service / Local)
- [ ] Classify EventList 30+ private fields by state category
- [ ] Classify EventDetail, CreateEvent, EventEdit state
- [ ] Design page coordinator / page state model for EventList
- [ ] Design page state model for EventDetail
- [ ] Verify filter/pagination state is URL-driven

### Phase X: Operability & Self-Hoster Diagnostics [OPERATOR]
- [ ] Startup config validation (auth provider, YARP targets, cookie settings)
- [ ] Self-hosting misconfiguration detection (HTTPS+reverse proxy, forwarded headers)
- [ ] Health/readiness signals for BFF dependencies
- [ ] Feature-unavailable vs misconfigured distinction in UI
- [ ] Correlation ID visible in error UI (admin contexts)
- [ ] Document configurable vs product-enforced security settings

---

## WAVE B — BFF Hardening

### Phase 2: BFF Security Hardening [SECURITY]
- [ ] Document XSRF-TOKEN HttpOnly decision (double-submit cookie pattern)
- [ ] Consolidate setup secret forwarding (YARP + DelegatingHandler → shared service)
- [ ] Verify/implement AccessTokenForwardingHandler
- [ ] Add YARP cluster request timeout (30s initial)
- [ ] Document cookie expiration policy
- [ ] Verify open redirect safety on auth return URLs
- [ ] Verify anti-cache headers on sensitive auth/setup endpoints
- [ ] Document cookie config for reverse proxy scenarios
- [ ] Document configurable vs enforced security settings

### Phase 4: BFF Middleware Extraction [STRUCTURAL]
- [ ] Extract AntiforgeryTokenDistributionMiddleware
- [ ] Extract StartupRedirectMiddleware
- [ ] Extract AccessTokenCaptureMiddleware
- [ ] Move Console.CancelKeyPress handler to Program.cs
- [ ] Verify middleware pipeline order unchanged
- [ ] Document pipeline order as living table in docs/BLAZOR.md

### Phase 3: BFF Endpoint Decomposition [STRUCTURAL]
- [ ] Split BffAuthEndpoints by capability: Status, Mutation, Debug modules
- [ ] Standardize endpoint patterns (logger, error mapping, route naming, auth policy)
- [ ] Extract BffSetupSecretEndpoints individual handlers
- [ ] Extract BffPreferenceEndpoints individual handlers
- [ ] Create HttpContextExtensions.GetLogger() — replace 36 duplicates
- [ ] Split CircuitAccessTokenService — extract SetupSecretSessionService

### Phase 16: Claims & HttpClient Rationalization [BEHAVIORAL + SECURITY]
- [ ] Extract shared claim type constants
- [ ] Audit typed client registrations (ensure BFF routing, no direct API bypass)
- [ ] Consolidate BrowserCredentialsMessageHandler constructors
- [ ] Verify auth context propagation across server/WASM boundaries
- [ ] Verify server-side vs browser-side client registration ownership

### Phase 6A: Auth Scheme Stabilization [SECURITY]
- [ ] Document current state transitions and behavior matrix
- [ ] Document concurrency model (dual-locking strategy)
- [ ] Document supported providers matrix (Keycloak, Google, Atproto)
- [ ] Add integration tests covering all auth flow paths
- [ ] Complete or remove AtprotoAuthenticationHandler (TODO/incomplete)
- [ ] Document rollback path for scheme registration failures
- [ ] Answer: Do we need runtime dynamic scheme mutation? (architectural decision)

---

## WAVE C — Service Contract Reform

### Phase 7: Service Layer Error Handling [CONTRACT + BEHAVIORAL]
- [ ] Define ServiceResult<T> with structured error contract (ErrorCode, FailureCategory, ValidationErrors, IsRetryable, HttpStatusCode)
- [ ] Define FailureCategory enum (Validation, NotFound, Forbidden, SessionExpired, ProviderUnavailable, ProviderMisconfigured, TransientFailure, Unknown)
- [ ] Define UI error handling tiers (inline validation, banners, snackbar, error states, re-auth, feature unavailable)
- [ ] Convert EventService to ServiceResult pattern
- [ ] Convert OrganizationService to ServiceResult pattern
- [ ] Convert AdminService to ServiceResult pattern
- [ ] Convert UserService to ServiceResult pattern
- [ ] Convert remaining services to ServiceResult pattern
- [ ] Update consuming components for typed error handling
- [ ] Before/after behavior matrix documented

### Phase 12 (partial): Service Decomposition — Contract-Changing [STRUCTURAL + CONTRACT]
- [ ] ImageStorageService (972 lines) → S3UploadService, ImageProcessingService, ImageValidationService
- [ ] AdminService (764 lines) → GovernanceAdminService, LookupAdminService, TenantAdminService
- [ ] Update DI registrations

---

## WAVE D — UI Decomposition

### Phase 8: EventList Decomposition [BEHAVIORAL]
- [ ] Implement EventListPageState (page coordinator model)
- [ ] Extract EventListGrid component
- [ ] Extract EventDetailDrawer component
- [ ] Extract EventListCustomizationDrawer component
- [ ] Extract EventFilterPanel component
- [ ] Extract EventListPagination component
- [ ] Reduce EventList.razor.cs [Inject] count to ≤8
- [ ] Before/after behavior matrix (infinite scroll, filters, drawer, pagination)

### Phase 9: EventDetail Decomposition [BEHAVIORAL]
- [ ] Implement EventDetailPageState (if warranted)
- [ ] Extract EventSessionList component
- [ ] Extract EventRegistrationPanel component
- [ ] Extract EventReviewSection component
- [ ] Extract EventAgendaView component
- [ ] Before/after behavior matrix (sessions, registration, reviews, agenda)

### Phase 10: CreateEvent & EventEdit Decomposition [BEHAVIORAL]
- [ ] Extract EventSessionEditor component
- [ ] Extract EventAspectEditor (consolidate Islamic/Tech dialogs)
- [ ] Extract EventSpeakerManager component
- [ ] Extract EventFormFields (shared between Create/Edit)

### Phase 11: Admin Page Decomposition [STRUCTURAL]
- [ ] FooterSettings → FooterTemplateEditor, FooterLinkGroupEditor, FooterPreview
- [ ] InstanceGovernanceSection → setting group components
- [ ] TenantLookupTablesSection → Category/Tag/Location/Madhab components

### Phase 12 (remaining): Service Decomposition [STRUCTURAL]
- [ ] InstanceOnboardingService (614 lines) → step-specific services
- [ ] Update DI registrations

### Phase 13: Duplicated Pattern Extraction [STRUCTURAL]
- [ ] Extract generic TriStateFilter<T> component
- [ ] Extract ListDetailPageBase<TItem> (only after decompositions stabilize)
- [ ] Standardize dialog structure via AppDialogShell
- [ ] Extract SettingsSectionBase pattern

---

## WAVE E — Conformance & Operability

### Phase 6B: Auth Scheme Manager Refactor [SECURITY]
- [ ] Extract provider-specific logic (Keycloak, Google, Atproto)
- [ ] Simplify concurrency primitives
- [ ] All auth flow integration tests pass
- [ ] Before/after behavior matrix documented
- [ ] Smoke scenarios executed

### Phase 14: CSS Compliance [STRUCTURAL]
- [ ] Replace 28 hardcoded colors in AdminListDetails.razor.css
- [ ] Replace hardcoded px in NotificationPanel.razor.css
- [ ] Replace hardcoded colors in Setup, GroupProfile, OrganizationProfile
- [ ] Move MainLayout inline styles to .razor.css
- [ ] Move SetupLayout inline styles to .razor.css
- [ ] Audit remaining 77 inline styles

### Phase 15: ABOUTME Header Sweep [STRUCTURAL]
- [ ] Automate or batch ABOUTME insertion for 30+ missing files
- [ ] Verify all new refactor files have ABOUTME

### Phase 17B: State Management Standardization [BEHAVIORAL]
- [ ] Verify decomposed components follow 17A classification
- [ ] Evaluate FeatureStateContainer standardization
- [ ] Document final state management conventions

### Phase 18: Test Expansion [STRUCTURAL + BEHAVIORAL]
- [ ] Component rendering tests for extracted components (Phases 8-11)
- [ ] Form validation tests
- [ ] ServiceResult error handling tests
- [ ] Architecture test expansion
- [ ] Accessibility tests for new components
- [ ] Workflow smoke scenario tests (auth, event list, registration, admin settings)

### Phase 19: Architecture Conformance [STRUCTURAL + OPERATOR]
- [ ] All arch tests pass
- [ ] Full build green
- [ ] All Blazor test suites pass
- [ ] @layer compliance verified
- [ ] BEM naming verified
- [ ] Wrapper component usage verified
- [ ] LSP diagnostics clean on changed files
- [ ] Self-hosted deployment start verification

### Phase 20: Verification & Handoff [STRUCTURAL + OPERATOR]
- [ ] Complete build verification
- [ ] All test suites documented
- [ ] Journal updated
- [ ] BLAZOR.md updated (patterns + middleware pipeline table)
- [ ] CODEBASE_STRUCTURE.md updated
- [ ] CODEBASE_INSIGHTS.md updated
- [ ] TROUBLESHOOTING.md updated with Blazor/BFF issues
- [ ] Self-hosting config reference document
- [ ] BFF request flow diagram
- [ ] Error-handling conventions doc (ServiceResult patterns)
- [ ] Plan context/tasks updated with final status
