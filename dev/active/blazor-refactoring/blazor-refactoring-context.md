# Context: Blazor Project Comprehensive Refactoring

**Last Updated: 2026-02-06**

---

## SESSION PROGRESS (2026-02-06)

### COMPLETED
- Phase 1: Critical Security Fixes (all tasks except 1.2 deferred)
- Phase 2: Dead Code Removal & Cleanup (ALL 5 tasks completed)
  - Task 2.1: Deleted 8 dead files from Explore.Blazor (AuthorizationHandler, 3x Bff*, entrypoint.sh, ServerCookieForwardingHandler, PersistingServerAuthenticationStateProvider) + removed Compile Remove directives + DI registration cleanup
  - Task 2.2: Deleted 3 dead client pages (Weather, Counter, UsersHome) + moved Loading.razor to Components + removed dead routes from Routes.razor + cleaned up Client Program.cs (commented code, unused imports, redundant IConfiguration)
  - Task 2.3: Replaced all Console.WriteLine in CircuitAccessTokenService.cs with ILogger (Debug level). Cleaned ConfigurationExtension.cs (removed secret-leaking lines, kept intentional startup diagnostics). Downgraded AccessTokenForwardingHandler logging from Information to Debug.
  - Task 2.4: Fixed blocking .GetAwaiter().GetResult() anti-pattern in App.razor by moving token capture to async middleware in Program.cs
  - Task 2.5: Consolidated duplicate TenantId constants - both Program.cs and AccessTokenForwardingHandler now use shared TenantConstants class. Fixed WRONG tenant ID bug in CreateEvent.razor.cs (was 00000000-..., now correct 018e4e5c-...). Made ConfigurationExtension.cs ClientId config-driven.

- Phase 3: Architecture & Render Mode Alignment (ALL 5 tasks completed)
  - Task 3.1: Switched to InteractiveAuto render mode. Fixed TWO critical WASM blockers: (1) Removed `ICircuitAccessTokenService` from Routes.razor (server-only service crashed in WASM DI), (2) Removed `AccessToken` cascading parameter (null in WASM, caused auth to break on second page load). Deleted dead `BffAuthenticationStateProvider.cs`. Fixed `bff.js` missing `getCookie` function. Enabled HeadOutlet prerendering for SEO.
  - Task 3.2: Removed all token cascading from component tree. Server-side token is stored in middleware; WASM uses BFF cookie auth.
  - Task 3.3: Added `<ErrorBoundary>` around Routes with MudAlert fallback UI and "Return to Home" recovery button.
  - Task 3.4: Removed ancient `Microsoft.AspNetCore.Authentication.Cookies` v2.3.0. Pinned `WebAssembly.Server` to `9.0.12`. Removed empty `Helpers` folder inclusion. Fixed French placeholder.
  - Task 3.5: Registered `TenantConfiguration` in WASM Client Program.cs for `AuthStateService` DI resolution.

- Phase 4: Code Quality & Standards (ALL 7 tasks completed)
  - Task 4.1: Added ABOUTME comments to ~73 files
  - Task 4.2: Added CancellationToken to 10 service interfaces (24 methods) + implementations + 14 test mocks
  - Task 4.3: Created 6 shared helpers (DisplayHelper, EventColorHelper, ImageHelper, StringHelper, RoleHelper, ApiConstants). Replaced duplicates in 12 files.
  - Task 4.4: Replaced magic numbers with RoleHelper/ApiConstants. Fixed BUG: EventFormatId==1 was mapped as "Online" (should be 2).
  - Task 4.5: Translated Dutch OrganizationSuccess.razor to English
  - Task 4.6: Route consistency verified (already correct)
  - Task 4.7: Standardized [Inject] convention to `protected` + `= null!`

- Phase 5: Error Handling & Resilience (ALL 4 tasks completed)
  - Task 5.1: Created ServiceResult<T> pattern. Hardened 4 major services with 2-tier ApiException+Exception pattern. Fixed 5 bare catch blocks.
  - Task 5.2: Created shared ErrorState.razor component
  - Task 5.3: S3Image.razor IAsyncDisposable with CancellationTokenSource. Removed unused IJSRuntime.
  - Task 5.4: Removed 19 unnecessary StateHasChanged() calls across 7 files.

- Phase 6: Validation & Forms (ALL 3 tasks completed)
  - Task 6.1: Added Blazored.FluentValidation, created CreateEventDtoValidator, replaced DataAnnotationsValidator in CreateEvent.razor, simplified ValidateForm()
  - Task 6.2: No Bootstrap found (all utility classes are MudBlazor's own)
  - Task 6.3: Added aria-labels to 11 buttons, NavMenu keyboard accessibility (role, tabindex, aria-expanded, aria-haspopup), dynamic profile alt text, overlay aria-hidden

### IN PROGRESS
- Nothing currently in progress

### NEXT STEPS
- Phase 7: Performance (N+1 fix, server-side filtering, virtualization)
- Phase 8: Test Coverage

### VERIFICATION
- Build: 0 errors, 108 warnings (MudBlazor analyzer warnings from new AriaLabel attributes)
- Tests: 111/111 passing (unchanged from baseline)

---

## Key Architectural Decisions

### Decision 1: Render Mode (REQUIRES PROJECT LEAD INPUT)
- **Current**: `InteractiveServer` (App.razor line 71)
- **Documented**: `InteractiveAuto` (BLAZOR.md, ARCHITECTURE.md)
- **Options**:
  - (A) Switch to `InteractiveAuto` -- requires scoped token service, WASM testing
  - (B) Stay with `InteractiveServer` -- remove WASM infrastructure, update docs
- **Impact**: Affects Phase 3 entirely; blocks token service redesign approach

### Decision 2: Token Service Architecture
- **Current**: Static `ConcurrentDictionary` with cross-user `GetAnyValidToken()` fallback
- **Documented Pattern**: Scoped service with per-circuit `_accessToken` property
- **Decision**: Simplify to documented pattern; no cross-user fallback ever

### Decision 3: Error Handling Pattern
- **Current**: Services return `null`/empty on error; no distinction between "not found" and "error"
- **Proposed**: Introduce `Result<T>` pattern or standard error notification to callers
- **Impact**: Touches every service and every page that consumes services

### Decision 4: Validation Strategy
- **Current**: Mix of `DataAnnotationsValidator`, manual validation, and unused FluentValidation validators
- **Proposed**: FluentValidation throughout; `MudForm` with FluentValidation integration
- **Impact**: All form pages need updating

---

## Key Files Reference

### Explore.Blazor (Server/BFF) -- Critical Files

| File | Purpose | Issues Found |
|------|---------|--------------|
| `Program.cs` | DI, OIDC, YARP, middleware pipeline | Auth debug endpoints (lines 606-667), cookie security (line 193), duplicate constants (line 317), XSRF config (lines 463-472), middleware ordering (lines 479-506) |
| `Components/App.razor` | Root component, render mode, token cascading | Blocking async (line 44), CSP disabled (lines 14-20), InteractiveServer (line 71), HeadOutlet prerender disabled (line 29), missing ErrorBoundary |
| `Components/Routes.razor` | Routing, admin guards | Missing admin auth guards (lines 82-87), token cascading (lines 36-37) |
| `Services/CircuitAccessTokenService.cs` | Token storage and forwarding | Static state (lines 24-28), cross-user leakage (lines 133-158), Console.WriteLine (16x), hardcoded tenant ID (line 208) |
| `Services/ServerCookieForwardingHandler.cs` | Cookie forwarding | Registered but never used |
| `Services/PersistingServerAuthenticationStateProvider.cs` | Auth state serialization | Not registered in DI |
| `Extensions/ConfigurationExtension.cs` | Config mapping | Console.WriteLine (14x), hardcoded client ID (line 122) |
| `AuthorizationHandler.cs` | Dead file | Never registered, has typo, wrong auth scheme |
| `Explore.Blazor.csproj` | Dependencies | Ancient cookies package (line 24), wildcard version (line 26), excluded files (lines 33-36) |
| `appsettings.Development.json` | Dev config | French placeholder secret (line 12) |

### Explore.Blazor.Client (WASM/UI) -- Critical Files

| File | Purpose | Issues Found |
|------|---------|--------------|
| `Program.cs` | WASM DI registration | Redundant IConfiguration (line 14), commented-out code (lines 102-122), TenantConfiguration never registered |
| `Pages/Event/EventList.razor.cs` | Event discovery page | 11 parallel API calls, client-side filtering of all events, N+1 registration loading, unnecessary StateHasChanged, inconsistent field naming |
| `Pages/Event/EventEdit.razor` | Event editing | Missing `[Authorize]`, Bootstrap classes, DataAnnotationsValidator, inline styles |
| `Pages/Event/CreateEvent.razor.cs` | Event creation | Manual validation (lines 332-373), hardcoded wrong tenant ID (line 427), duplicated image upload logic |
| `Pages/Event/MyEvents.razor.cs` | User's events | Magic numbers for roles (line 98), duplicated helpers (lines 181-211) |
| `Pages/Organization/CreateOrganization.razor.cs` | Org creation | Missing `[Authorize]`, Dutch comments/strings (10+ instances), duplicated image upload |
| `Pages/Organization/MyOrganizations.razor.cs` | User's orgs | Magic numbers for roles (line 100), duplicated GetInitials |
| `Pages/Admin/AdminList.razor.cs` | Admin dashboard | Unnecessary StateHasChanged (lines 77, 86), proper admin auth |
| `Pages/Landing/LandingPageForUsers.razor.cs` | Landing page | Error caught but no user feedback (lines 31-33) |
| `Pages/UsersHome.razor` | Dead page | JS eval anti-pattern, mock data, replaced by Landing pages |
| `Layout/NavMenu.razor` | Navigation | Route mismatch for organizations (line 87 links `/organization/my`, page is `/organizations/my`), swallowed exceptions |
| `Layout/MainLayout.razor.cs` | App shell | Missing IDisposable for JS interop |
| `Services/EventService.cs` | Event API facade | No CancellationToken, swallowed exceptions, returns empty on error |
| `Services/AdminService.cs` | Lookup table facade | No CancellationToken, 25+ methods |
| `Services/UserService.cs` | User API facade | Returns null for both "not found" and "error" |
| `Services/ImageStorageService.cs` | S3 uploads | No external CancellationToken, internal CTS only, TODO at line 657 |
| `Services/AuthStateService.cs` | Auth state | Depends on unregistered TenantConfiguration |
| `Helpers/DateTimeHelper.cs` | Date formatting | Only shared helper; others duplicated across pages |
| `Validators/*.cs` | FluentValidation validators | Exist but are NOT wired into any Blazor forms |

### Explore.Blazor.Client.Tests -- Critical Files

| File | Purpose | Issues Found |
|------|---------|--------------|
| `Common/BlazorTestContext.cs` | bUnit test context | Excellent quality, well-structured |
| `Common/MockServiceFactory.cs` | Mock factory | Excellent, covers all core services |
| `Common/ComponentDataBuilder.cs` | Fake data generators | Excellent, Bogus-based |
| `Common/Authentication/*.cs` | Auth test infrastructure | Excellent, fluent builders |
| `Services/EventServiceTests.cs` | EventService tests | HIGH quality, ~25 tests |
| `Services/OrganizationServiceTests.cs` | OrgService tests | HIGH quality, ~18 tests |
| `Services/AuthStateServiceTests.cs` | AuthState tests | HIGH quality, ~14 tests |
| `Pages/HomeTests.cs` | Home page tests | MODERATE quality, Task.Delay anti-pattern |
| `Pages/Event/EventListTests.cs` | EventList tests | MODERATE-HIGH, incomplete filter test |
| `Pages/Event/CreateEventTests.cs` | CreateEvent tests | LOW-MODERATE, 5/9 tests are mock-verification only |
| `Integration/AuthenticationFlowTests.cs` | Auth flow tests | HIGH for auth, MODERATE for components |

### Dead Files (To Delete)

| File | Reason |
|------|--------|
| `Explore.Blazor/AuthorizationHandler.cs` | Never registered, buggy, wrong auth scheme, typo |
| `Explore.Blazor/Extensions/BffApiExtensions.cs` | Excluded from compilation, references Duende.Bff |
| `Explore.Blazor/Extensions/BffEndpointRoutes.cs` | Same |
| `Explore.Blazor/Extensions/BffMappingExtensions.cs` | Same |
| `Explore.Blazor/entrypoint.sh` | Not referenced by Dockerfile |
| `Explore.Blazor.Client/Pages/Weather.razor` | Template demo page |
| `Explore.Blazor.Client/Pages/Counter.razor` | Template demo page |
| `Explore.Blazor.Client/Pages/UsersHome.razor` | Dead page, JS eval anti-pattern |

---

## Duplicated Code Map

### GetInitials (4 copies -- extract to `Helpers/DisplayHelper.cs`)
1. `Layout/NavMenu.razor.cs:78-85`
2. `Pages/Event/MyEvents.razor.cs:205-211` (as `GetActorInitials`)
3. `Pages/Event/EventList.razor.cs:496-502` (as `GetActorInitials`)
4. `Pages/Organization/MyOrganizations.razor.cs:150-161`

### GetEventColor (4 copies -- extract to `Helpers/EventColorHelper.cs`)
1. `Pages/Event/MyEvents.razor.cs:181-188` (as `GetEventColorCode`)
2. `Pages/Event/EventList.razor.cs:475-483` (as `GetEventColorForEvent`)
3. `Pages/Event/EventDetail.razor.cs:274-280` (as `GetEventColor`)
4. `Pages/Event/EventEdit.razor:538-546` (as `GetCategoryColor`)

### GetEventImageUrl (3 copies -- extract to `Helpers/ImageHelper.cs`)
1. `Pages/Event/MyEvents.razor.cs:191-197` (uses `placehold.co`)
2. `Pages/Event/EventList.razor.cs:467-473` (uses `placehold.co`)
3. `Pages/Event/EventDetail.razor.cs:304` (uses `via.placeholder.com`)
4. `Pages/Event/EventDetail.razor:85` (uses `dummyimage.com`)

### GetTruncatedDescription (2 copies -- extract to `Helpers/StringHelper.cs`)
1. `Pages/Event/EventList.razor.cs:486-494`
2. `Pages/Landing/LandingPageForUsers.razor.cs:55-61` (as `TruncateText`)

### Image Upload Logic (3 copies -- extract to `Services/ImageUploadOrchestrator.cs` or base class)
1. `Pages/Event/EventEdit.razor:622-712`
2. `Pages/Event/CreateEvent.razor.cs:96-179`
3. `Pages/Organization/CreateOrganization.razor.cs:166-245`

### Magic Number Role Checks (5 files -- replace with `OrganizationRole` enum)
1. `Pages/Event/MyEvents.razor.cs:98` (`org.CurrentUserRole is 1 or 2 or 3`)
2. `Pages/Organization/MyOrganizations.razor.cs:100` (`org.CurrentUserRole.Value is 1 or 2 or 3`)
3. `Pages/Organization/OrganizationDetails.razor.cs:99` (`currentUserRole == 1 || ...`)
4. `Pages/Event/CreateEvent.razor.cs:277` (`organization.CurrentUserRole.Value == 1 || ...`)
5. `Layout/NavMenu.razor.cs:89-91` (hardcoded "Admin" role strings)

---

## Test Coverage Gap Summary

### Services (13% covered)

| Service | Tested | Priority |
|---------|--------|----------|
| EventService | YES | - |
| OrganizationService | YES | - |
| AuthStateService | YES | - |
| AdminService | NO | CRITICAL |
| UserService | NO | CRITICAL |
| CategoryService | NO | MEDIUM |
| TagService | NO | MEDIUM |
| LocationService | NO | MEDIUM |
| ImageStorageService | NO | MEDIUM |
| EventRegistrationService | NO | CRITICAL |
| LandingPageService | NO | HIGH |
| OrganizationMemberService | NO | HIGH |
| OrganizationReviewService | NO | HIGH |
| All other thin services (~10) | NO | LOW |

### Pages (11% covered)

| Page | Tested | Priority |
|------|--------|----------|
| Home | YES | - |
| EventList | YES | - |
| CreateEvent | YES (partial) | - |
| EventDetail | NO | CRITICAL |
| EventEdit | NO | CRITICAL |
| MyEvents | NO | HIGH |
| CreateOrganization | NO | HIGH |
| OrganizationDetails | NO | HIGH |
| MyOrganizations | NO | MEDIUM |
| AdminList | NO | MEDIUM |
| All User pages (4) | NO | HIGH |
| All Admin sub-pages (10) | NO | MEDIUM |
| Landing pages (2) | NO | MEDIUM |

### Components (0% covered)

| Component | Priority |
|-----------|----------|
| EventRegistration | CRITICAL |
| EventSessionManager | CRITICAL |
| DeleteEventDialog | HIGH |
| ImageUpload | HIGH |
| EventReviewDialog | HIGH |
| All other components (~20) | MEDIUM-LOW |

---

## Technology Research Key Findings

### MudBlazor
- Current stable: v8.15.0; v9 in preview with breaking changes
- Use `IMudDialogInstance` (not concrete class) -- future-proof for v9
- `DialogOptions` is an immutable record -- use `with` syntax
- `MudPopoverProvider` required in bUnit tests for overlays
- `ServerData` + virtualization for DataGrid >100 rows
- FluentValidation integration via `Validation` parameter on form fields
- `style-src 'unsafe-inline'` required in CSP for MudBlazor popover positioning

### .NET 10 / C# 14 Relevant Features
- `[PersistentState]` attribute -- eliminates prerender state boilerplate
- `NotFoundPage` on Router -- proper 404 handling
- Named query filters (EF Core 10) -- already in project rules
- `field` keyword -- cleaner property accessors
- Null-conditional assignment (`customer?.Order = value;`)
- Extension members (`extension` blocks for properties, static extensions)

### bUnit 2.5.x
- Latest for .NET 10; `FindByTestId`, generic typed `Find<TComponent, TElement>`
- `WaitForAssertion` preferred over `Task.Delay`
- `AuthenticationState` in services container for auth testing

---

## Dependencies Between Tasks

```
Phase 1 (Security) -- no dependencies, start immediately
  |
Phase 2 (Dead Code) -- no dependencies, can parallel with Phase 1
  |
Phase 3 (Architecture) -- depends on Phase 1 (Task 1.2 token fix)
  |                     -- requires render mode decision from project lead
  |
Phase 4 (Code Quality) -- depends on Phase 2 (dead code removed first)
  |
Phase 5 (Error Handling) -- depends on Phase 4 (naming conventions fixed)
  |
Phase 6 (Validation) -- depends on Phase 4 (Bootstrap removal)
  |
Phase 7 (Performance) -- depends on Phase 5 (error handling in place)
  |
Phase 8 (Tests) -- Task 8.1 can start in parallel with Phase 2
               -- Tasks 8.2-8.5 should follow Phase 4-7 to avoid churn
```

---

## File Counts

| Project | Total Files | Files with Issues | % Affected |
|---------|-------------|-------------------|------------|
| Explore.Blazor | ~15 | 12 | 80% |
| Explore.Blazor.Client | ~80 | 55+ | 69% |
| Explore.Blazor.Client.Tests | ~12 | 5 | 42% |
| **Total** | **~107** | **72+** | **67%** |
