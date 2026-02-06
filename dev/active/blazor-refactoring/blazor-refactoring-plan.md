# Plan: Blazor Project Comprehensive Refactoring

**Last Updated: 2026-02-06**

---

## Executive Summary

The Explore Blazor projects (`Explore.Blazor` and `Explore.Blazor.Client`) contain **2 CRITICAL security vulnerabilities, 12 HIGH severity issues, 16 MEDIUM severity issues, and 10+ LOW severity issues** across security, architecture, code quality, dead code, and test coverage. The overall test coverage is approximately **9%** (7 test files covering ~81 testable units). This plan addresses all findings systematically across 8 phases, ordered by risk and impact.

### Key Findings Summary

| Category | Critical | High | Medium | Low |
|----------|----------|------|--------|-----|
| Security Vulnerabilities | 2 | 5 | 4 | 1 |
| Dead/Unused Code | 0 | 3 | 4 | 3 |
| Architecture Violations | 0 | 2 | 4 | 2 |
| Code Quality & Standards | 0 | 4 | 6 | 4 |
| Test Coverage Gaps | 0 | 1 | 0 | 0 |
| Performance Issues | 0 | 0 | 3 | 0 |
| **Total** | **2** | **15** | **21** | **10** |

### Current State vs Future State

| Aspect | Current State | Future State |
|--------|--------------|--------------|
| Security | Auth debug endpoints public; cross-user token leakage; CSP disabled | All debug endpoints removed/guarded; scoped token service; CSP enabled |
| Render Mode | InteractiveServer (contradicts docs saying InteractiveAuto) | InteractiveAuto with proper WASM support or cleaned-up Server-only |
| Token Handling | Static ConcurrentDictionary; GetAnyValidToken cross-user risk | Scoped service per-user; no cross-user fallback |
| Test Coverage | ~9% (7 files / ~101 tests) | >60% (~300+ tests across services, pages, components) |
| Code Standards | 50+ files missing ABOUTME; 30+ Console.WriteLine; Dutch/English mix | All files compliant; structured logging; English only |
| Dead Code | 6+ unused files; 3 excluded-from-compilation files | All dead code removed |
| CancellationToken | Zero usage in Blazor Client services | All async methods accept CancellationToken |
| Code Duplication | GetInitials (4x), GetEventColor (4x), image upload (3x) | Shared helpers/base classes; single source of truth |
| Authorization | Missing [Authorize] on EventEdit, CreateOrganization, etc. | All write pages properly guarded |
| Validation | DataAnnotationsValidator used; FluentValidation validators exist but unused | FluentValidation wired throughout; server+client validation |

---

## Phase 1: Critical Security Fixes (Week 1)

**Priority**: CRITICAL / BLOCKING
**Effort**: M
**Related Skills**: `auth-patterns`, `blazor-bff-patterns`

### Task 1.1: Remove/Guard Auth Debug Endpoints

- **File**: `Explore.Blazor/Program.cs`, lines 606-667
- **Acceptance Criteria**:
  - [ ] `/auth/debug` endpoint removed entirely from production code
  - [ ] `/auth/status` returns only `{ isAuthenticated: bool, name: string }` for authenticated users; nothing for unauthenticated
  - [ ] No Keycloak authority, client ID, client secret, or discovery document exposed in any endpoint
  - [ ] Error responses in auth challenge (lines 564-577) return generic message only; details logged server-side
  - [ ] Sign-out error handler (line 601) follows same pattern
- **Effort**: S
- **Dependencies**: None

### Task 1.2: Fix Cross-User Token Leakage in CircuitAccessTokenService

- **File**: `Explore.Blazor/Services/CircuitAccessTokenService.cs`, lines 24-28, 133-158
- **Acceptance Criteria**:
  - [ ] Remove `static ConcurrentDictionary<string, TokenEntry>` shared state
  - [ ] Remove `static _latestToken` fallback
  - [ ] Remove `GetAnyValidToken()` method entirely
  - [ ] Simplify to a scoped service with per-circuit `_accessToken` property matching documented pattern in `blazor-bff-patterns/resources/token-forwarding.md`
  - [ ] If token not found for current circuit, return null (not another user's token)
  - [ ] `AccessTokenForwardingHandler` receives token from scoped `ICircuitAccessTokenService`
  - [ ] All token-related `Console.WriteLine` replaced with `ILogger` at Debug level
- **Effort**: M
- **Dependencies**: Task 1.1

### Task 1.3: Fix Cookie Security Configuration

- **File**: `Explore.Blazor/Program.cs`, lines 193, 463-472
- **Acceptance Criteria**:
  - [ ] Auth cookie `SecurePolicy` set to `CookieSecurePolicy.Always` in production, `SameAsRequest` in development only
  - [ ] XSRF-TOKEN cookie has explicit `Path = "/"`
  - [ ] XSRF-TOKEN cookie `Secure` flag is `true` in production (not `ctx.Request.IsHttps`)
  - [ ] Forwarded headers (lines 374-381) have documented known-network restriction or explicit comment about trusted-proxy-only deployment
- **Effort**: S
- **Dependencies**: None

### Task 1.4: Enable Content Security Policy

- **File**: `Explore.Blazor/Components/App.razor`, lines 14-20
- **Acceptance Criteria**:
  - [ ] CSP meta tag uncommented with production-ready policy
  - [ ] `script-src` restricts to `'self'` and `_content/MudBlazor/` path
  - [ ] `style-src` includes `'unsafe-inline'` (required by MudBlazor popover positioning) and `fonts.googleapis.com`
  - [ ] `connect-src` restricts to API and auth endpoints
  - [ ] CSP does not break MudBlazor functionality (tested manually)
- **Effort**: S
- **Dependencies**: None

### Task 1.5: Add Missing Authorization Attributes on Pages

- **Files**: 
  - `Explore.Blazor.Client/Pages/Event/EventEdit.razor`
  - `Explore.Blazor.Client/Pages/Event/EventCreated.razor`
  - `Explore.Blazor.Client/Pages/Organization/CreateOrganization.razor`
- **Acceptance Criteria**:
  - [ ] `EventEdit.razor` has `@attribute [Authorize]`
  - [ ] `EventCreated.razor` has `@attribute [Authorize]`
  - [ ] `CreateOrganization.razor` has `@attribute [Authorize]`
  - [ ] Admin routes in `Routes.razor` (lines 82-87) have proper admin role guards
  - [ ] All User/ pages (`UserProfile`, `Settings`, `MyRegistrations`, `MyReviews`) have `@attribute [Authorize]`
- **Effort**: S
- **Dependencies**: None

---

## Phase 2: Dead Code Removal & Code Cleanup (Week 1-2)

**Priority**: HIGH
**Effort**: M
**Related Skills**: `clean-architecture-rules`

### Task 2.1: Remove Dead Files from Explore.Blazor

- **Files to delete**:
  - `Explore.Blazor/AuthorizationHandler.cs` (unused, buggy, never registered)
  - `Explore.Blazor/Extensions/BffApiExtensions.cs` (excluded from compilation, references Duende.Bff)
  - `Explore.Blazor/Extensions/BffEndpointRoutes.cs` (same)
  - `Explore.Blazor/Extensions/BffMappingExtensions.cs` (same)
  - `Explore.Blazor/entrypoint.sh` (not referenced by Dockerfile)
- **Acceptance Criteria**:
  - [ ] All 5 files deleted
  - [ ] `<Compile Remove="...">` directives removed from `.csproj` (lines 33-36)
  - [ ] No compilation errors after removal
  - [ ] `ServerCookieForwardingHandler` either wired into an HttpClient or removed (with its DI registration at Program.cs line 76)
  - [ ] `PersistingServerAuthenticationStateProvider` either registered in DI or removed
- **Effort**: S

### Task 2.2: Remove Dead Code from Explore.Blazor.Client

- **Files to delete/move**:
  - `Explore.Blazor.Client/Pages/Weather.razor` (template demo page)
  - `Explore.Blazor.Client/Pages/Counter.razor` (template demo page)
  - `Explore.Blazor.Client/Pages/UsersHome.razor` (dead page, replaced by Landing pages, uses JS `eval` anti-pattern)
  - `Explore.Blazor.Client/Pages/Loading.razor` -- move to `Components/` (has no `@page` directive)
- **Acceptance Criteria**:
  - [ ] Template pages deleted
  - [ ] `UsersHome.razor` deleted (references to it removed from NavMenu/routes)
  - [ ] `Loading.razor` moved to `Components/Loading.razor`
  - [ ] All commented-out code in `Program.cs` (lines 102-122) removed
  - [ ] `shutdownCts` variable in `Explore.Blazor/Program.cs` (line 35) either used properly or removed
  - [ ] Duplicate `ResponseType` assignment in `Explore.Blazor/Program.cs` (line 202) removed
- **Effort**: S

### Task 2.3: Replace Console.WriteLine with ILogger

- **Files**: `Explore.Blazor/Services/CircuitAccessTokenService.cs` (16 occurrences), `Explore.Blazor/Extensions/ConfigurationExtension.cs` (14 occurrences), `Explore.Blazor/Components/App.razor` (4 occurrences)
- **Acceptance Criteria**:
  - [ ] Zero `Console.WriteLine` calls in entire `Explore.Blazor` project
  - [ ] Token-related logging at `Debug` or `Trace` level (not `Information`)
  - [ ] OIDC event handlers (Program.cs lines 237-282) log at `Debug` level for routine events, `Error` for failures
  - [ ] Forwarded headers middleware (lines 386-401) logs at `Debug` level
  - [ ] `AccessTokenForwardingHandler` reduces per-request logging to single `Debug` call on success, `Warning` on missing token
- **Effort**: M

### Task 2.4: Fix Blocking Async Call in App.razor

- **File**: `Explore.Blazor/Components/App.razor`, line 44
- **Acceptance Criteria**:
  - [ ] `.GetAwaiter().GetResult()` replaced with proper async pattern
  - [ ] Token retrieval moved to `OnInitializedAsync` lifecycle method or a server-side prerender approach
  - [ ] No thread pool blocking in any Blazor component
- **Effort**: S

### Task 2.5: Consolidate Duplicate Constants

- **Files**: `Explore.Blazor/Program.cs` (line 317), `Explore.Blazor/Services/CircuitAccessTokenService.cs` (line 208)
- **Acceptance Criteria**:
  - [ ] Single shared `DefaultTenantId` constant (consistent type: `Guid`)
  - [ ] Both `Program.cs` and `AccessTokenForwardingHandler` reference the shared constant
  - [ ] Hardcoded client ID in `ConfigurationExtension.cs` (line 122) replaced with configuration-driven value
  - [ ] Hardcoded wrong tenant ID in `CreateEvent.razor.cs` (line 427, `00000000-...`) replaced with `TenantConstants.DefaultTenantId`
- **Effort**: S

---

## Phase 3: Architecture & Render Mode Alignment (Week 2-3)

**Priority**: HIGH
**Effort**: L
**Related Skills**: `blazor-ui-conventions`, `blazor-bff-patterns`

### Task 3.1: Resolve Render Mode Inconsistency

- **File**: `Explore.Blazor/Components/App.razor`, line 71
- **Decision Required**: Choose one of:
  - **(A) Switch to InteractiveAuto** (per documentation): Enable WASM, keep both registrations, test WASM fallback
  - **(B) Commit to InteractiveServer**: Remove `AddInteractiveWebAssemblyComponents()`, `AddAuthenticationStateSerialization`, and WASM-related registrations
- **Acceptance Criteria**:
  - [ ] Render mode matches documentation or documentation updated to match implementation
  - [ ] If InteractiveAuto: verify WASM payload actually activates; test both render paths
  - [ ] If InteractiveServer: remove unused WASM infrastructure to reduce payload
  - [ ] `HeadOutlet` has prerendering enabled for SEO (currently disabled at line 29)
- **Effort**: L
- **Dependencies**: Task 1.2 (token handling must be scoped before WASM can work)

### Task 3.2: Remove Token Cascading Through Component Tree

- **Files**: `Explore.Blazor/Components/App.razor` (lines 69-71), `Explore.Blazor/Components/Routes.razor` (lines 36-37, 91-108)
- **Acceptance Criteria**:
  - [ ] `CascadingValue` for `AccessToken` string removed from `App.razor`
  - [ ] `CascadingParameter` for `AccessToken` removed from `Routes.razor`
  - [ ] `CircuitAccessTokenService.SetToken()` call preserved as the sole token-passing mechanism
  - [ ] No raw token strings flow through the Blazor component tree
- **Effort**: S

### Task 3.3: Add ErrorBoundary to App.razor

- **File**: `Explore.Blazor/Components/App.razor`
- **Acceptance Criteria**:
  - [ ] `<ErrorBoundary>` wraps the Routes component per documented pattern in BLAZOR.md (lines 1392-1406)
  - [ ] Fallback UI uses MudBlazor `MudAlert` with Severity.Error
  - [ ] Error is logged server-side
  - [ ] User sees recovery option (refresh page button)
- **Effort**: S

### Task 3.4: Fix Package Dependencies

- **File**: `Explore.Blazor/Explore.Blazor.csproj`
- **Acceptance Criteria**:
  - [ ] Remove `Microsoft.AspNetCore.Authentication.Cookies` (version 2.3.0) -- included in shared framework
  - [ ] Pin `Microsoft.AspNetCore.Components.WebAssembly.Server` to specific version (not `9.*`)
  - [ ] French placeholder secret in `appsettings.Development.json` replaced with English
- **File**: `Explore.Blazor.Client/Explore.Blazor.Client.csproj`
- **Acceptance Criteria**:
  - [ ] `Microsoft.Extensions.Http` version aligned with target framework (not 10.0.1 on net9.0)
  - [ ] Consider migrating `Newtonsoft.Json` to `System.Text.Json` for smaller WASM bundle
  - [ ] Package versions are consistent (no mixed pinned/wildcard)
  - [ ] Remove `<Folder Include="Helpers\" />` empty folder inclusion
- **Effort**: S

### Task 3.5: Fix TenantConfiguration DI Registration

- **File**: `Explore.Blazor.Client/Program.cs`
- **Acceptance Criteria**:
  - [ ] `TenantConfiguration` properly registered via `builder.Services.Configure<TenantConfiguration>(...)` 
  - [ ] `AuthStateService` can resolve `IOptions<TenantConfiguration>` without runtime exception
  - [ ] Redundant `builder.Services.AddSingleton<IConfiguration>(builder.Configuration)` removed (line 14)
- **Effort**: S

---

## Phase 4: Code Quality & Standards Compliance (Week 3-4)

**Priority**: HIGH
**Effort**: XL
**Related Skills**: `blazor-ui-conventions`, `clean-architecture-rules`

### Task 4.1: Add ABOUTME Comments to All Files

- **Files**: ~50+ files across both Blazor projects (see context document for full list)
- **Acceptance Criteria**:
  - [ ] Every `.cs`, `.razor`, and `.razor.cs` file starts with a two-line `ABOUTME:` comment
  - [ ] Comment accurately describes what the file does
  - [ ] Follows format: `// ABOUTME: [Line 1 description]` / `// ABOUTME: [Line 2 description]`
- **Effort**: M

### Task 4.2: Add CancellationToken to All Blazor Client Service Methods

- **Files**: All 9+ service files in `Explore.Blazor.Client/Services/`
- **Acceptance Criteria**:
  - [ ] Every async method in every service accepts `CancellationToken cancellationToken = default` parameter
  - [ ] CancellationToken passed through to API client calls
  - [ ] Interface definitions updated to include CancellationToken
  - [ ] Page components pass `CancellationToken` from lifecycle methods where available
- **Effort**: L

### Task 4.3: Extract Duplicate Code to Shared Helpers

- **Duplicated code to extract**:
  1. **GetInitials** (4 copies) -> `Explore.Blazor.Client/Helpers/DisplayHelper.cs`
  2. **GetEventColor/GetEventColorCode** (4 copies) -> `Explore.Blazor.Client/Helpers/EventColorHelper.cs`
  3. **GetEventImageUrl** (3 copies, different placeholder services) -> `Explore.Blazor.Client/Helpers/ImageHelper.cs`
  4. **GetTruncatedDescription** (2 copies) -> `Explore.Blazor.Client/Helpers/StringHelper.cs`
  5. **Image upload logic** (3 copies) -> Extract to `ImageUploadService` or shared base class
  6. **Role checking with magic numbers** (5 files) -> Use existing `OrganizationRole` enum
- **Acceptance Criteria**:
  - [ ] Zero duplicate GetInitials methods (single shared helper)
  - [ ] Zero duplicate color mapping methods (single shared helper)
  - [ ] Zero duplicate image URL methods (single shared helper)
  - [ ] Image upload logic appears in exactly one place (service or base class)
  - [ ] All role checks use `OrganizationRole` enum, not magic numbers (1, 2, 3)
  - [ ] Placeholder image URL is a single constant, not 3 different services
- **Effort**: M

### Task 4.4: Replace Magic Numbers with Enums/Constants

- **Files**: Multiple (see context document for full list)
- **Acceptance Criteria**:
  - [ ] All role checks (`is 1 or 2 or 3`) replaced with `OrganizationRole` enum comparisons
  - [ ] Event format checks (`EventFormatId == 2`) replaced with `EventFormat` enum
  - [ ] Approval status checks replaced with named constants
  - [ ] Hardcoded `pageSize: 100` replaced with `Constants.DefaultPageSize`
  - [ ] Country lists replaced with API lookup or shared constants file
- **Effort**: M

### Task 4.5: Translate Dutch to English

- **File**: `Explore.Blazor.Client/Pages/Organization/CreateOrganization.razor` and `.razor.cs`
- **Acceptance Criteria**:
  - [ ] All Dutch comments translated to English (10+ instances)
  - [ ] All Dutch user-facing strings translated to English
  - [ ] No non-English text remains in any file across the entire Blazor project
- **Effort**: S

### Task 4.6: Fix Route Inconsistencies

- **Files**: NavMenu.razor, MyOrganizations.razor, EventDetail.razor, EventEdit.razor
- **Acceptance Criteria**:
  - [ ] NavMenu link for organizations matches actual page route (`/organizations/my` not `/organization/my`)
  - [ ] Consistent route patterns: `/events`, `/events/{id}`, `/events/{id}/edit`, `/events/create`
  - [ ] No mismatched routes between navigation and pages
- **Effort**: S

### Task 4.7: Fix Naming Inconsistencies

- **Files**: All code-behind files
- **Acceptance Criteria**:
  - [ ] All private fields use `_camelCase` convention (not `camelCase` without underscore)
  - [ ] All `[Inject]` properties use consistent access modifier (private recommended)
  - [ ] All inject defaults use `= default!` consistently (not mixed `= null!`)
  - [ ] Consistent DI logger usage (`ILogger<T>` always, never `Console.WriteLine`)
- **Effort**: M

---

## Phase 5: Error Handling & Resilience (Week 4-5)

**Priority**: MEDIUM-HIGH
**Effort**: L
**Related Skills**: `error-tracking`, `blazor-ui-conventions`

### Task 5.1: Implement Proper Error Handling in Services

- **Files**: All service files in `Explore.Blazor.Client/Services/`
- **Acceptance Criteria**:
  - [ ] Services return `Result<T>` or similar discriminated response (not null/empty on error)
  - [ ] Callers can distinguish "no data" from "error occurred"
  - [ ] No bare `catch { }` blocks that swallow all exceptions
  - [ ] Specific exception types caught separately (`ApiException` by status code, then generic `Exception`)
  - [ ] All exceptions logged with structured context (service name, method, entity ID)
  - [ ] 401 errors trigger re-authentication flow (not silently return empty)
- **Effort**: L

### Task 5.2: Add User-Facing Error States to All Pages

- **Files**: All page files in `Explore.Blazor.Client/Pages/`
- **Acceptance Criteria**:
  - [ ] Every page that loads data displays an error state on failure (MudAlert with retry button)
  - [ ] Consistent error display pattern across all pages (extracted to shared component if possible)
  - [ ] Error messages are user-friendly (not technical stack traces)
  - [ ] Retry button re-triggers data load
- **Effort**: M

### Task 5.3: Implement IDisposable/IAsyncDisposable

- **Files**: Components with async operations or event subscriptions
- **Acceptance Criteria**:
  - [ ] `S3Image.razor` implements `IAsyncDisposable` with `CancellationTokenSource`
  - [ ] `EventList.razor` cancels pending API calls on disposal
  - [ ] `MainLayout.razor` disposes JS interop resources
  - [ ] All components with `IJSRuntime` or timer usage implement proper disposal
- **Effort**: M

### Task 5.4: Fix Unnecessary StateHasChanged Calls

- **Files**: `EventList.razor.cs`, `AdminList.razor.cs`
- **Acceptance Criteria**:
  - [ ] All unnecessary `StateHasChanged()` calls removed
  - [ ] Only called after operations where Blazor won't auto-detect changes (e.g., timer callbacks, event handler completions from non-Blazor sources)
- **Effort**: S

---

## Phase 6: Validation & Form Improvements (Week 5-6)

**Priority**: MEDIUM
**Effort**: M
**Related Skills**: `cqrs-mediatr-guidelines`, `blazor-ui-conventions`

### Task 6.1: Wire FluentValidation into Blazor Forms

- **Files**: `CreateEvent.razor`, `EventEdit.razor`, `CreateOrganization.razor`
- **Acceptance Criteria**:
  - [ ] `DataAnnotationsValidator` replaced with FluentValidation integration
  - [ ] Existing validators in `Validators/` folder are used (not new ones created)
  - [ ] Form fields show real-time validation feedback (`Immediate="true"`)
  - [ ] Server-side validation errors from API displayed in form
  - [ ] Manual `ValidateForm()` methods (e.g., CreateEvent.razor.cs lines 332-373) replaced with FluentValidation
- **Effort**: M

### Task 6.2: Replace Bootstrap Classes with MudBlazor

- **File**: `Explore.Blazor.Client/Pages/Event/EventEdit.razor`, lines 34-36
- **Acceptance Criteria**:
  - [ ] All Bootstrap classes (`container`, `row`, `col-md-8`, `d-flex justify-content-between`) replaced with MudBlazor equivalents (`MudContainer`, `MudGrid`, `MudItem`, `MudStack`)
  - [ ] No Bootstrap CSS utility classes in any Blazor Client file
- **Effort**: S

### Task 6.3: Add Accessibility Attributes

- **Files**: Footer.razor, EventDetail.razor, NavMenu.razor, AdminList.razor
- **Acceptance Criteria**:
  - [ ] All `MudIconButton` instances have `aria-label` attributes
  - [ ] Dropdown triggers have `role="button"`, `tabindex`, and `aria-expanded`
  - [ ] Keyboard navigation works on all interactive elements
  - [ ] Profile images have descriptive `Alt` text (not generic "Profile")
- **Effort**: S

---

## Phase 7: Performance Optimization (Week 6-7)

**Priority**: MEDIUM
**Effort**: M
**Related Skills**: `blazor-ui-conventions`, `dotnet-efcore-guidelines`

### Task 7.1: Fix N+1 API Call Pattern

- **File**: `Explore.Blazor.Client/Pages/Event/EventList.razor.cs`, line 101
- **Acceptance Criteria**:
  - [ ] `LoadUserRegistrationsAsync` fetches sessions in batch (not per-registration)
  - [ ] Single API call replaces N individual `GetSessionByIdAsync` calls
  - [ ] Or: backend provides a bulk sessions endpoint
- **Effort**: M

### Task 7.2: Implement Server-Side Filtering

- **Files**: `EventList.razor.cs`, `MyEvents.razor.cs`
- **Acceptance Criteria**:
  - [ ] Event filtering uses server-side query parameters instead of loading all events then filtering client-side
  - [ ] Only requested page of data loaded from API
  - [ ] Filter changes trigger new API call with filter parameters
  - [ ] Client-side filtering used only for instant-response scenarios (<100 items)
- **Effort**: L

### Task 7.3: Add Virtualization for Large Lists

- **Files**: `EventList.razor`, `MyEvents.razor`
- **Acceptance Criteria**:
  - [ ] `@foreach` replaced with `<Virtualize>` component for event card lists
  - [ ] Or: pagination implemented properly (not loading 100 items at once)
  - [ ] Computed properties (`AllFilteredEvents`, `FilteredEvents`, `TotalPages`) cached and only recalculated when inputs change
- **Effort**: S

### Task 7.4: Reduce Initial Data Load

- **File**: `EventList.razor.cs`, lines 130-173
- **Acceptance Criteria**:
  - [ ] 11 parallel API calls reduced to essential data only (events + needed lookups)
  - [ ] Lookup data loaded lazily or cached at application level
  - [ ] Page loads with meaningful content in <500ms on typical network
- **Effort**: M

---

## Phase 8: Test Coverage Expansion (Week 7-10)

**Priority**: HIGH
**Effort**: XL
**Related Skills**: `blazor-ui-conventions`

### Task 8.1: Fix Existing Test Anti-Patterns

- **Files**: `HomeTests.cs`, `EventListTests.cs`, `CreateEventTests.cs`, `AuthenticationFlowTests.cs`
- **Acceptance Criteria**:
  - [ ] All `Task.Delay` replaced with `WaitForState`/`WaitForAssertion`
  - [ ] 5 mock-verification-only tests in `CreateEventTests.cs` rewritten as component behavior tests
  - [ ] Weak `DoesNotContain("Loading")` assertions replaced with positive content assertions
  - [ ] Incomplete filter test in `EventListTests.cs` completed or documented as limitation
  - [ ] ABOUTME comments added to all test files
- **Effort**: S

### Task 8.2: Add Critical Service Tests

- **New test files**:
  - `Services/AdminServiceTests.cs` (~25 tests)
  - `Services/UserServiceTests.cs` (~8 tests)
  - `Services/CategoryServiceTests.cs` (~8 tests)
  - `Services/TagServiceTests.cs` (~8 tests)
  - `Services/LocationServiceTests.cs` (~8 tests)
  - `Services/ImageStorageServiceTests.cs` (~10 tests)
- **Acceptance Criteria**:
  - [ ] Follow `EventServiceTests` pattern exactly (AAA, NSubstitute mocks, real service logic)
  - [ ] Test success paths, null responses, API exceptions (404, 500, 401)
  - [ ] Test edge cases (empty collections, null DTOs)
  - [ ] No mock-verification-only tests
  - [ ] All tests pass: `dotnet test Explore.Blazor.Client.Tests`
- **Effort**: L

### Task 8.3: Add Critical Page Tests

- **New test files**:
  - `Pages/Event/EventDetailTests.cs` (~10 tests)
  - `Pages/Event/EventEditTests.cs` (~8 tests)
  - `Pages/Event/MyEventsTests.cs` (~8 tests)
  - `Pages/Organization/CreateOrganizationTests.cs` (~8 tests)
  - `Pages/Organization/MyOrganizationsTests.cs` (~6 tests)
  - `Pages/Admin/AdminListTests.cs` (~10 tests)
- **Acceptance Criteria**:
  - [ ] Follow `EventListTests` pattern
  - [ ] Test loading states, error states, empty states, data display
  - [ ] Test authorization (authenticated vs anonymous rendering)
  - [ ] Use `BlazorTestContext` and `MockServiceFactory`
  - [ ] All tests pass: `dotnet test Explore.Blazor.Client.Tests`
- **Effort**: L

### Task 8.4: Add Critical Component Tests

- **New test files**:
  - `Components/EventRegistrationTests.cs` (~6 tests)
  - `Components/Event/EventSessionManagerTests.cs` (~8 tests)
  - `Components/Event/DeleteEventDialogTests.cs` (~4 tests)
  - `Components/ImageUploadTests.cs` (~6 tests)
- **Acceptance Criteria**:
  - [ ] Components tested in isolation with parameter injection
  - [ ] EventCallback invocations verified
  - [ ] Dialog results verified
  - [ ] All tests pass: `dotnet test Explore.Blazor.Client.Tests`
- **Effort**: M

### Task 8.5: Add Layout Tests

- **New test files**:
  - `Layout/MainLayoutTests.cs` (~6 tests: error boundary, theme toggle, responsive)
  - `Layout/NavMenuTests.cs` (~8 tests: auth states, navigation links, user dropdown)
- **Acceptance Criteria**:
  - [ ] MainLayout error boundary behavior tested
  - [ ] Theme toggle persistence tested
  - [ ] NavMenu shows correct items for authenticated/anonymous/admin users
  - [ ] All tests pass
- **Effort**: M

---

## Risk Assessment & Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|------------|
| Render mode change breaks existing functionality | Medium | High | Feature-flag the change; test both paths; Phase 3 gated behind full test suite |
| Token service refactor causes auth regression | Medium | Critical | Keep old implementation behind feature flag; roll back if issues |
| Bootstrap removal changes UI appearance | Low | Medium | Visual regression tests; side-by-side comparison |
| FluentValidation integration breaks existing forms | Medium | Medium | Implement one form first as proof-of-concept |
| Large refactoring introduces new bugs | Medium | High | Phase 8 tests BEFORE Phase 4-7 refactoring where possible |

---

## Success Metrics

| Metric | Current | Target | Measurement |
|--------|---------|--------|-------------|
| Security vulnerabilities (Critical/High) | 7 | 0 | Manual review |
| Console.WriteLine calls | 34+ | 0 | `grep -r "Console.WriteLine" Explore.Blazor*` |
| Test count | ~101 | 300+ | `dotnet test --list-tests` |
| Test coverage (files) | 9% | 60%+ | Files with tests / total testable files |
| ABOUTME compliance | ~12 files | 100% | Files with ABOUTME / total files |
| Code duplication instances | 15+ | 0 | Manual review |
| Dead code files | 8+ | 0 | No excluded/unused files |
| Magic numbers in UI code | 20+ | 0 | `grep -r "is 1 or\|== 1\|== 2\|== 3" Pages/` |

---

## Required Resources & Dependencies

### Technical Dependencies
- .NET 10 SDK (for C# 14 features and EF Core 10 named filters)
- MudBlazor 8.15.0 (current stable)
- bUnit 2.5.x (latest for .NET 10 support)
- TUnit 1.11.x (current test framework)

### External Dependencies
- Keycloak instance for auth testing
- Decision on render mode (InteractiveAuto vs InteractiveServer) from project lead

### Effort Estimates Summary

| Phase | Effort | Estimated Duration |
|-------|--------|-------------------|
| Phase 1: Security Fixes | M | 3-4 days |
| Phase 2: Dead Code Removal | M | 2-3 days |
| Phase 3: Architecture Alignment | L | 4-5 days |
| Phase 4: Code Quality | XL | 7-10 days |
| Phase 5: Error Handling | L | 4-5 days |
| Phase 6: Validation & Forms | M | 3-4 days |
| Phase 7: Performance | M | 3-4 days |
| Phase 8: Test Coverage | XL | 10-14 days |
| **Total** | | **~36-49 days** |
