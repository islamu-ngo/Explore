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
