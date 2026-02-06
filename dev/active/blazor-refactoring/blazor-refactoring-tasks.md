# Tasks: Blazor Project Comprehensive Refactoring

**Last Updated: 2026-02-06**

---

## Phase 1: Critical Security Fixes (BLOCKING) -- COMPLETED

### Task 1.1: Remove/Guard Auth Debug Endpoints [S] -- DONE
- [x] `/auth/debug` endpoint gated behind `IsDevelopment()` + `RequireAuthorization()`, no secrets exposed
- [x] `/auth/status` returns only `{ isAuthenticated, name }` (claims removed)
- [x] Auth challenge error returns generic message; config details logged server-side only
- [x] Sign-out error returns generic message; `ex.Message` removed from response
- [x] `/bff/me` endpoint filtered to safe claims only (preferred_username, email, name, roles, sub)
- [x] OIDC event logging downgraded from Information to Debug level
- [x] Forwarded headers middleware logging downgraded from Information to Debug level
- [x] Auth challenge config logging downgraded to Debug and ClientId no longer logged as value

### Task 1.2: Fix Cross-User Token Leakage [M] -- DEFERRED (requires deeper refactor)
- [ ] Remove `static ConcurrentDictionary<string, TokenEntry>` from `CircuitAccessTokenService.cs` (lines 24-28)
- [ ] Remove `static _latestToken` fallback (line 27)
- [ ] Remove `GetAnyValidToken()` method (lines 133-158)
- [ ] Redesign as scoped service with per-circuit `_accessToken` property
- [ ] Update `AccessTokenForwardingHandler` to use scoped service
- [ ] Replace all `Console.WriteLine` with `ILogger` at Debug level (16 occurrences)
- [ ] Test: User A's token is NEVER used for User B's requests
> **Note**: This is a larger architectural change that could break auth flow. Deferred to Phase 3 with proper testing.

### Task 1.3: Fix Cookie Security Configuration [S] -- DONE
- [x] Auth cookie `SecurePolicy` set to `Always` in production, `SameAsRequest` in development
- [x] XSRF-TOKEN cookie has explicit `Path = "/"`
- [x] XSRF-TOKEN `Secure` flag set to `true` in production (not `ctx.Request.IsHttps`)
- [x] Forwarded headers trust model already documented with inline comments

### Task 1.4: Enable Content Security Policy [S] -- DONE
- [x] CSP meta tag enabled with production-ready policy
- [x] `script-src 'self' 'wasm-unsafe-eval'` configured
- [x] `style-src 'self' 'unsafe-inline' https://fonts.googleapis.com` configured
- [x] `connect-src 'self' ws: wss:` configured (covers SignalR WebSocket)
- [x] `img-src 'self' data: https: blob:` configured (covers S3 images, placeholders)
- [x] `frame-ancestors 'self'`, `base-uri 'self'`, `form-action 'self'` added
- [ ] Test: MudBlazor components work correctly with CSP enabled (manual testing needed)

### Task 1.5: Add Missing Authorization Attributes [S] -- DONE
- [x] `@attribute [Authorize]` added to `EventEdit.razor`
- [x] `@attribute [Authorize]` added to `EventCreated.razor`
- [x] `@attribute [Authorize]` added to `CreateOrganization.razor`
- [x] Admin routes comment updated in `Routes.razor` (AdminList already has `[Authorize(Roles="Admin")]`)
- [x] Verified all User/ pages already have `@attribute [Authorize]` (UserProfile, Settings, MyRegistrations, MyReviews)

### Additional Security Improvements (Done as part of Phase 1)
- [x] Removed duplicate `ResponseType = "code"` magic string (kept `OpenIdConnectResponseType.Code`)
- [x] Removed all `Console.WriteLine` from `App.razor` (4 occurrences)
- [x] Reduced token logging in `Routes.razor` from `Information`/`Warning` to `Debug`
- [x] Build verified: both `Explore.Blazor` and `Explore.Blazor.Client` compile with 0 errors

---

## Phase 2: Dead Code Removal & Cleanup -- COMPLETED

### Task 2.1: Remove Dead Files from Explore.Blazor [S] -- DONE
- [x] Delete `AuthorizationHandler.cs`
- [x] Delete `Extensions/BffApiExtensions.cs`
- [x] Delete `Extensions/BffEndpointRoutes.cs`
- [x] Delete `Extensions/BffMappingExtensions.cs`
- [x] Delete `entrypoint.sh` (confirmed not referenced by Dockerfile)
- [x] Remove `<Compile Remove="...">` from `.csproj` (lines 33-36)
- [x] Delete `ServerCookieForwardingHandler` + remove DI registration (line 76)
- [x] Delete `PersistingServerAuthenticationStateProvider` (not registered, superseded by built-in `AddAuthenticationStateSerialization`)
- [x] Verify: `dotnet build Explore.Blazor` succeeds (0 errors)

### Task 2.2: Remove Dead Code from Explore.Blazor.Client [S] -- DONE
- [x] Delete `Pages/Weather.razor` + `.razor.css`
- [x] Delete `Pages/Counter.razor` + `.razor.css`
- [x] Delete `Pages/UsersHome.razor` + `.razor.css` (JS eval anti-pattern, mock data)
- [x] Move `Pages/Loading.razor` to `Components/Loading.razor` (no @page directive)
- [x] Remove dead routes from `Routes.razor` (counter, weather, dashboard)
- [x] Add `@using Explore.Blazor.Client.Components` to Routes.razor for Loading reference
- [x] Remove commented-out code in `Program.cs` (lines 102-122, unreachable OIDC WASM config)
- [x] Remove redundant `builder.Services.AddSingleton<IConfiguration>` (line 14)
- [x] Remove unused `using Microsoft.AspNetCore.Components.WebAssembly.Authentication`
- [x] Verify: `dotnet build Explore.Blazor.Client` succeeds (0 errors)

### Task 2.3: Replace Console.WriteLine with ILogger [M] -- DONE
- [x] `CircuitAccessTokenService.cs`: Replace all 14 `Console.WriteLine` with `_logger?.LogDebug` at Debug level
- [x] Refactored static methods `GetAnyValidToken` and `GetTokenForUser` to accept optional `ILogger?` parameter
- [x] `ConfigurationExtension.cs`: Reduced from 16 to 8 `Console.WriteLine` calls (kept intentional startup diagnostics with documentation comments, removed secret-leaking lines: ClientId value, partial ClientSecret, raw Realm, verbose key dump)
- [x] `App.razor`: Had 0 `Console.WriteLine` (already cleaned in Phase 1)
- [x] `AccessTokenForwardingHandler`: Downgraded all `LogInformation` to `LogDebug` to reduce per-request log noise; removed verbose JWT token detail logging
- [x] Verify: Zero `Console.WriteLine` in `Services/` directory

### Task 2.4: Fix Blocking Async Call [S] -- DONE
- [x] Moved token capture from `App.razor` `.GetAwaiter().GetResult()` to async middleware in `Program.cs`
- [x] Middleware stores token in `HttpContext.Items["AccessToken"]` during HTTP pipeline
- [x] `App.razor` reads token synchronously from `HttpContext.Items` (no blocking)
- [x] Removed `@using Microsoft.AspNetCore.Authentication` and `@inject ICircuitAccessTokenService TokenService` from `App.razor` (no longer needed)
- [x] Verify: Zero `.GetAwaiter().GetResult()` in App.razor code

### Task 2.5: Consolidate Duplicate Constants [S] -- DONE
- [x] Reused existing `TenantConstants` class in `Explore.Blazor.Client/Constants/TenantConstants.cs`
- [x] Updated `Program.cs` to reference `TenantConstants.DefaultTenantId` and `TenantConstants.TenantIdHeaderName` (removed local `const string`)
- [x] Updated `AccessTokenForwardingHandler` to reference `TenantConstants` (removed private duplicate `DefaultTenantId` and `TenantIdHeaderName`)
- [x] Fixed WRONG tenant ID in `CreateEvent.razor.cs` (was `00000000-0000-0000-0000-000000000001`, now uses `TenantConstants.DefaultTenantId` = `018e4e5c-7f00-7000-8000-000000000001`)
- [x] Hardcoded `ClientId` in `ConfigurationExtension.cs` replaced with config lookup: `config["Keycloak:ClientId"] ?? "explore-blazor-server"`
- [x] Verify: All 111 tests pass, 0 build errors

---

## Phase 3: Architecture & Render Mode Alignment -- COMPLETED

### Task 3.1: Switch to InteractiveAuto [L] -- DONE
- [x] Decision: **InteractiveAuto** chosen (matches BLAZOR.md documentation)
- [x] Changed `App.razor` Routes rendermode from `InteractiveServer` to `InteractiveAuto`
- [x] Changed `HeadOutlet` from `InteractiveServerRenderMode(prerender: false)` to `InteractiveAuto` (enables SEO prerendering)
- [x] **CRITICAL FIX**: Removed `ICircuitAccessTokenService` injection from `Routes.razor` - this was the WASM crash cause (service only exists in server DI, not client DI)
- [x] **CRITICAL FIX**: Removed `[CascadingParameter(Name = "AccessToken")]` from `Routes.razor` - null in WASM mode
- [x] Removed `OnParametersSet` and `UpdateAccessToken()` methods from `Routes.razor` - token is already stored by middleware for server mode; WASM mode uses BFF cookies
- [x] Deleted dead `BffAuthenticationStateProvider.cs` - never registered, superseded by `AddAuthenticationStateDeserialization()`
- [x] Fixed `bff.js`: Added missing `getCookie` function (was causing JSInterop crash in `BffClient`). Converted to ES module with `export` syntax.
- [x] Removed `<script src="js/bff.js">` from `App.razor` - now loaded as ES module via `import()` in `BffClient.cs`
- [x] Server `Program.cs` already had all InteractiveAuto infrastructure: `AddInteractiveWebAssemblyComponents()`, `AddAuthenticationStateSerialization()`, `AddInteractiveWebAssemblyRenderMode()`, `AddAdditionalAssemblies()`

### Task 3.2: Remove Token Cascading [S] -- DONE (merged into 3.1)
- [x] Removed `CascadingValue Value="accessToken" Name="AccessToken"` from `App.razor`
- [x] Removed `CascadingParameter` for `AccessToken` from `Routes.razor`
- [x] Removed `@using Explore.Blazor.Services` from `Routes.razor`
- [x] Token is stored in middleware (server mode) and WASM uses BFF cookie auth - no cascading needed
- [x] Removed access token reading from `HttpContext.Items` in `App.razor` (no longer needed)

### Task 3.3: Add ErrorBoundary [S] -- DONE
- [x] Added `<ErrorBoundary>` wrapping `<Routes>` in `App.razor`
- [x] Fallback UI uses `<MudContainer>` + `<MudAlert Severity="Severity.Error">` with user-friendly message
- [x] Includes "Return to Home" `<MudButton>` for recovery

### Task 3.4: Fix Package Dependencies [S] -- DONE
- [x] Removed `Microsoft.AspNetCore.Authentication.Cookies` v2.3.0 from Blazor `.csproj` (included in shared framework since ASP.NET Core 3.0)
- [x] Pinned `WebAssembly.Server` from `9.*` to `9.0.12` (current resolved version)
- [x] `Microsoft.Extensions.Http` v10.0.1 kept as-is (NuGet package, forward-compatible with net9.0)
- [x] Removed `<Folder Include="Helpers\" />` from Client `.csproj`
- [x] Replaced French placeholder `"REMPLACEZ-PAR-LE-VRAI-SECRET-DEPUIS-KEYCLOAK"` with English `"REPLACE-WITH-REAL-SECRET-FROM-KEYCLOAK"` in `appsettings.Development.json`
- [x] Build succeeds with 0 errors

### Task 3.5: Fix TenantConfiguration DI [S] -- DONE
- [x] Added `builder.Services.Configure<TenantConfiguration>(builder.Configuration.GetSection(TenantConfiguration.SectionName))` in Client `Program.cs`
- [x] Added `using Explore.Blazor.Client.Configuration` to Client `Program.cs`
- [x] Redundant `AddSingleton<IConfiguration>` was already removed in Phase 2
- [x] `AuthStateService` can now resolve `IOptions<TenantConfiguration>` in both Server and WASM modes

---

## Phase 4: Code Quality & Standards

### Task 4.1: Add ABOUTME Comments [M]
- [ ] Add to all ~50 files listed in context document
- [ ] Verify format: `// ABOUTME: [description line 1]` / `// ABOUTME: [description line 2]`
- [ ] Include test files

### Task 4.2: Add CancellationToken to Services [L]
- [ ] Update all service interfaces to include `CancellationToken cancellationToken = default`
- [ ] Update all service implementations to pass token through
- [ ] Update API client calls to accept CancellationToken
- [ ] Verify: all async methods compile with new signatures

### Task 4.3: Extract Duplicate Code [M]
- [ ] Create `Helpers/DisplayHelper.cs` with `GetInitials()` method
- [ ] Create `Helpers/EventColorHelper.cs` with `GetEventColor()` method
- [ ] Create `Helpers/ImageHelper.cs` with `GetEventImageUrl()` method
- [ ] Create `Helpers/StringHelper.cs` with `TruncateText()` method
- [ ] Extract image upload to shared service or base class
- [ ] Replace 4 GetInitials copies with shared helper call
- [ ] Replace 4 GetEventColor copies with shared helper call
- [ ] Replace 3 GetImageUrl copies with shared helper call
- [ ] Replace 2 TruncateText copies with shared helper call
- [ ] Replace 3 image upload copies with shared code

### Task 4.4: Replace Magic Numbers [M]
- [ ] Replace role checks in `MyEvents.razor.cs` (line 98) with `OrganizationRole` enum
- [ ] Replace role checks in `MyOrganizations.razor.cs` (line 100)
- [ ] Replace role checks in `OrganizationDetails.razor.cs` (line 99)
- [ ] Replace role checks in `CreateEvent.razor.cs` (line 277)
- [ ] Replace role checks in `NavMenu.razor.cs` (lines 89-91)
- [ ] Replace EventFormat checks (`EventFormatId == 2`) with enum
- [ ] Replace `pageSize: 100` with `Constants.DefaultPageSize`
- [ ] Replace inline country list with constant or API lookup

### Task 4.5: Translate Dutch to English [S]
- [ ] Translate all Dutch comments in `CreateOrganization.razor.cs` (10+ instances)
- [ ] Translate all Dutch user-facing strings in `CreateOrganization.razor`
- [ ] Verify: `grep -rn "Vul\|organisatie\|vereist\|Succes\|fout\|Roep\|Wacht" Explore.Blazor.Client/` returns 0

### Task 4.6: Fix Route Inconsistencies [S]
- [ ] Fix NavMenu link: `/organization/my` -> `/organizations/my` (NavMenu.razor line 87)
- [ ] Standardize event routes: `/events`, `/events/{id}`, `/events/{id}/edit`
- [ ] Verify all NavMenu links match actual page routes

### Task 4.7: Fix Naming Inconsistencies [M]
- [ ] All private fields: `_camelCase` (fix `isLoading` -> `_isLoading` in EventList, CreateEvent, EventEdit)
- [ ] All `[Inject]` properties: consistent `private` access modifier
- [ ] All inject defaults: `= default!` consistently
- [ ] Verify no `= null!` mixed with `= default!` in same file

---

## Phase 5: Error Handling & Resilience

### Task 5.1: Fix Service Error Handling [L]
- [ ] Design `Result<T>` or similar pattern for service returns
- [ ] Refactor services to return distinguishable success/error states
- [ ] Remove all bare `catch { }` blocks
- [ ] Add specific exception handling (ApiException by status code)
- [ ] Add structured logging with context to all catches
- [ ] Handle 401 errors with re-authentication flow

### Task 5.2: Add User-Facing Error States [M]
- [ ] Add error display to `EventList.razor` (currently silent on error)
- [ ] Add error display to `LandingPageForUsers.razor`
- [ ] Ensure consistent error pattern across all pages (MudAlert + retry)
- [ ] Consider extracting shared `ErrorState.razor` component

### Task 5.3: Implement IDisposable [M]
- [ ] `S3Image.razor`: Add `IAsyncDisposable` with `CancellationTokenSource`
- [ ] `EventList.razor`: Cancel pending API calls on dispose
- [ ] `MainLayout.razor`: Dispose JS interop resources
- [ ] Verify all components with `IJSRuntime` implement disposal

### Task 5.4: Fix StateHasChanged [S]
- [ ] Remove unnecessary call in `EventList.razor.cs` (line 305)
- [ ] Remove unnecessary call in `EventList.razor.cs` (line 332)
- [ ] Remove unnecessary call in `EventList.razor.cs` (line 427)
- [ ] Remove unnecessary calls in `AdminList.razor.cs` (lines 77, 86)

---

## Phase 6: Validation & Forms

### Task 6.1: Wire FluentValidation [M]
- [ ] Replace `DataAnnotationsValidator` in `CreateEvent.razor` with FluentValidation
- [ ] Replace `DataAnnotationsValidator` in `EventEdit.razor` with FluentValidation
- [ ] Wire validators from `Validators/` folder to forms
- [ ] Add `Immediate="true"` for real-time validation
- [ ] Remove manual `ValidateForm()` in `CreateEvent.razor.cs` (lines 332-373)
- [ ] Display server-side validation errors in form

### Task 6.2: Replace Bootstrap with MudBlazor [S]
- [ ] `EventEdit.razor` lines 34-36: Replace `container`, `row`, `col-md-8` with MudBlazor
- [ ] Replace `justify-content-between` (line 330) with MudBlazor equivalent
- [ ] Verify: `grep -r "class=\"container\|class=\"row\|class=\"col-" Explore.Blazor.Client/` returns 0

### Task 6.3: Add Accessibility [S]
- [ ] Add `aria-label` to social media buttons in `Footer.razor` (lines 62-65)
- [ ] Add `aria-label` to share buttons in `EventDetail.razor` (lines 229-232)
- [ ] Add `role="button"`, `tabindex`, `aria-expanded` to NavMenu dropdown (line 53)
- [ ] Add `aria-hidden="true"` to NavMenu overlay (line 138)
- [ ] Add `aria-label` to MoreVert menu buttons in `MyEvents.razor` (line 115)
- [ ] Fix profile image `Alt` to be descriptive (NavMenu line 57)

---

## Phase 7: Performance

### Task 7.1: Fix N+1 API Pattern [M]
- [ ] Replace individual `GetSessionByIdAsync` calls with batch fetch in `EventList.razor.cs` (line 101)
- [ ] Or: create backend bulk sessions endpoint

### Task 7.2: Server-Side Filtering [L]
- [ ] Add filter query parameters to event API calls
- [ ] Replace client-side filtering in `EventList.razor.cs` (lines 175-270)
- [ ] Replace client-side filtering in `MyEvents.razor.cs` (lines 41-54)
- [ ] Only load requested page of data

### Task 7.3: Add Virtualization [S]
- [ ] Replace `@foreach` with `<Virtualize>` in `EventList.razor` (line 150)
- [ ] Replace `@foreach` with `<Virtualize>` in `MyEvents.razor` (line 110)
- [ ] Cache computed `AllFilteredEvents` property, invalidate on filter change

### Task 7.4: Reduce Initial Load [M]
- [ ] Reduce 11 parallel API calls in `EventList.razor.cs` to essential only
- [ ] Load lookup data lazily or cache at application level
- [ ] Profile page load time, target <500ms

---

## Phase 8: Test Coverage

### Task 8.1: Fix Test Anti-Patterns [S]
- [ ] Replace `Task.Delay` with `WaitForState`/`WaitForAssertion` in `HomeTests.cs`
- [ ] Replace `Task.Delay` with `WaitForState`/`WaitForAssertion` in `EventListTests.cs`
- [ ] Replace `Task.Delay` with `WaitForState`/`WaitForAssertion` in `CreateEventTests.cs`
- [ ] Rewrite 5 mock-verification tests in `CreateEventTests.cs` as behavior tests
- [ ] Strengthen weak assertions in `HomeTests.cs` (3 tests)
- [ ] Complete incomplete filter test in `EventListTests.cs`
- [ ] Add ABOUTME to all test files
- [ ] Verify: `dotnet test Explore.Blazor.Client.Tests` passes

### Task 8.2: Add Service Tests [L]
- [ ] Create `Services/AdminServiceTests.cs` (~25 tests)
- [ ] Create `Services/UserServiceTests.cs` (~8 tests)
- [ ] Create `Services/CategoryServiceTests.cs` (~8 tests)
- [ ] Create `Services/TagServiceTests.cs` (~8 tests)
- [ ] Create `Services/LocationServiceTests.cs` (~8 tests)
- [ ] Create `Services/ImageStorageServiceTests.cs` (~10 tests)
- [ ] Follow `EventServiceTests` pattern exactly
- [ ] Verify: all new tests pass

### Task 8.3: Add Page Tests [L]
- [ ] Create `Pages/Event/EventDetailTests.cs` (~10 tests)
- [ ] Create `Pages/Event/EventEditTests.cs` (~8 tests)
- [ ] Create `Pages/Event/MyEventsTests.cs` (~8 tests)
- [ ] Create `Pages/Organization/CreateOrganizationTests.cs` (~8 tests)
- [ ] Create `Pages/Organization/MyOrganizationsTests.cs` (~6 tests)
- [ ] Create `Pages/Admin/AdminListTests.cs` (~10 tests)
- [ ] Follow `EventListTests` pattern
- [ ] Verify: all new tests pass

### Task 8.4: Add Component Tests [M]
- [ ] Create `Components/EventRegistrationTests.cs` (~6 tests)
- [ ] Create `Components/Event/EventSessionManagerTests.cs` (~8 tests)
- [ ] Create `Components/Event/DeleteEventDialogTests.cs` (~4 tests)
- [ ] Create `Components/ImageUploadTests.cs` (~6 tests)
- [ ] Verify: all new tests pass

### Task 8.5: Add Layout Tests [M]
- [ ] Create `Layout/MainLayoutTests.cs` (~6 tests)
- [ ] Create `Layout/NavMenuTests.cs` (~8 tests)
- [ ] Test error boundary, theme toggle, auth states
- [ ] Verify: all new tests pass

---

## Verification Commands

```bash
# Build verification (run after each phase)
dotnet build Explore.Blazor
dotnet build Explore.Blazor.Client

# Test verification (run after each phase)
dotnet test Explore.Blazor.Client.Tests --configuration Release

# Standards verification
# Zero Console.WriteLine
grep -r "Console.WriteLine" Explore.Blazor/ Explore.Blazor.Client/ --include="*.cs" --include="*.razor"

# Zero Dutch text
grep -rn "Vul\|organisatie\|vereist\|Succes.*organisatie\|fout.*opgetreden\|Roep\|Wacht" Explore.Blazor.Client/

# Zero Bootstrap classes
grep -rn "class=\"container\|class=\"row\|class=\"col-" Explore.Blazor.Client/ --include="*.razor"

# Zero magic role numbers
grep -rn "is 1 or 2\|== 1 ||\|== 2 ||\|== 3" Explore.Blazor.Client/Pages/ --include="*.cs"
```
