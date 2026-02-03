# NSwag API Client Refactor - Task Checklist

**Last Updated**: 2026-01-11  
**Status**: Not Started

---

## Overview

Refactor Blazor projects to use NSwag-generated `IEventApiClient` instead of manual HTTP calls.

**Total Tasks**: 17  
**Estimated Time**: 8-12 hours

---

## Phase 1: Infrastructure Setup (2 hours)

### Task 1.1: Create Auth Token Handler
- [ ] Create `Explore.Blazor/Infrastructure/ApiAuthHandler.cs`
- [ ] Implement `DelegatingHandler` to attach Bearer token
- [ ] Integrate with Duende token management (`GetUserAccessTokenAsync`)
- [ ] Handle token refresh scenarios

**Acceptance Criteria**:
- Handler retrieves access token from HttpContext
- Token attached to Authorization header
- Works with existing Keycloak OIDC setup

**File**: `Explore.Blazor/Infrastructure/ApiAuthHandler.cs`

---

### Task 1.2: Register NSwag Client in Blazor Server DI
- [ ] Add `IEventApiClient` registration in `Program.cs`
- [ ] Configure `HttpClient` with base URL from config
- [ ] Attach `ApiAuthHandler` to HttpClient pipeline
- [ ] Add SSL bypass for development (localhost)

**Acceptance Criteria**:
- `IEventApiClient` injectable in Blazor Server components
- Base URL configurable via `appsettings.json`
- Auth handler automatically attaches tokens

**File**: `Explore.Blazor/Program.cs`

---

### Task 1.3: Register NSwag Client in Blazor WASM DI
- [ ] Add `IEventApiClient` registration in WASM `Program.cs`
- [ ] Configure base URL pointing to self (BFF pattern)
- [ ] Ensure works with anonymous state provider

**Acceptance Criteria**:
- `IEventApiClient` injectable in WASM components
- Calls go through BFF (same origin)

**File**: `Explore.Blazor.Client/Program.cs`

---

## Phase 2: Service Layer Refactor (4 hours)

### Task 2.1: Create Service Wrapper Pattern
- [ ] Create `IApiServiceBase` interface if needed
- [ ] Define error handling conventions
- [ ] Document usage pattern for services

**Acceptance Criteria**:
- Clear pattern for wrapping NSwag client
- Consistent error handling approach

**File**: `Explore.Blazor.Client/Services/IApiServiceBase.cs` (optional)

---

### Task 2.2: Refactor EventService
- [ ] Inject `IEventApiClient` instead of `HttpClient`
- [ ] Replace `GetAllEventsAsync` with `_client.EventAllAsync()`
- [ ] Replace `GetMyEventsAsync` with `_client.MyAsync()`
- [ ] Replace `GetEventByIdAsync` with `_client.EventGETAsync()`
- [ ] Replace `CreateEventAsync` with `_client.EventPOSTAsync()`
- [ ] Replace `UpdateEventAsync` with `_client.EventPUTAsync()`
- [ ] Replace `DeleteEventAsync` with `_client.EventDELETEAsync()`
- [ ] Update return types to generated DTOs
- [ ] Remove legacy sync methods
- [ ] Add `ApiException` error handling

**Acceptance Criteria**:
- All methods use `IEventApiClient`
- No direct `HttpClient` usage
- Uses generated `EventDto`, `EventListDto` types
- Proper error handling with `ApiException`

**File**: `Explore.Blazor.Client/Services/EventService.cs`

---

### Task 2.3: Refactor OrganizationService
- [ ] Inject `IEventApiClient` instead of `HttpClient`
- [ ] Replace `CreateOrganizationAsync` with `_client.OrganizationPOSTAsync()`
- [ ] Replace `GetMyOrganizationsAsync` with `_client.Organization2Async()` (my endpoint)
- [ ] Replace `GetOrganizationByIdAsync` with `_client.OrganizationGETAsync()`
- [ ] Replace `UpdateOrganizationAsync` with `_client.OrganizationPUTAsync()`
- [ ] Replace `GetStatusTypesAsync` with `_client.ApprovalStatusAllAsync()`
- [ ] Update return types to generated DTOs
- [ ] Add `ApiException` error handling

**Acceptance Criteria**:
- All methods use `IEventApiClient`
- Uses generated `OrganizationDto`, `OrganizationListDto` types
- Consistent error handling

**File**: `Explore.Blazor.Client/Services/OrganizationService.cs`

---

### Task 2.4: Refactor UserService
- [ ] Inject `IEventApiClient`
- [ ] Replace user operations with generated client methods
- [ ] Update return types

**File**: `Explore.Blazor.Client/Services/UserService.cs`

---

### Task 2.5: Refactor AdminService
- [ ] Inject `IEventApiClient`
- [ ] Replace admin operations with generated client methods
- [ ] Update return types

**File**: `Explore.Blazor.Client/Services/AdminService.cs`

---

### Task 2.6: Refactor OrganizationMemberService
- [ ] Inject `IEventApiClient`
- [ ] Replace operations with `OrganizationMember*Async` methods
- [ ] Update return types

**File**: `Explore.Blazor.Client/Services/OrganizationMemberService.cs`

---

### Task 2.7: Refactor OrganizationReviewService
- [ ] Inject `IEventApiClient`
- [ ] Replace operations with `OrganizationReview*Async` methods
- [ ] Update return types

**File**: `Explore.Blazor.Client/Services/OrganizationReviewService.cs`

---

### Task 2.8: Refactor ProgramService
- [ ] Inject `IEventApiClient`
- [ ] Map to appropriate Event/Session operations
- [ ] Update return types

**File**: `Explore.Blazor.Client/Services/ProgramService.cs`

---

## Phase 3: DTO Migration (2 hours)

### Task 3.1: Audit Existing DTOs
- [ ] List all DTOs in `Models/DTOs/`
- [ ] Compare with generated DTOs in `EventApiClient.g.cs`
- [ ] Identify exact matches vs differences
- [ ] Document migration mapping

**Acceptance Criteria**:
- Clear list of DTOs to remove
- List of any DTOs to keep (view models)
- Name mapping documented

**Files**: `Explore.Blazor.Client/Models/DTOs/*.cs`

---

### Task 3.2: Update Razor Components
- [ ] Find all usages of local DTOs
- [ ] Replace with `Explore.Blazor.Client.Clients.*Dto`
- [ ] Add using statement: `@using Explore.Blazor.Client.Clients`
- [ ] Update property bindings if names differ
- [ ] Test each component after update

**Acceptance Criteria**:
- All components compile
- UI renders correctly
- No runtime errors

**Files**: `Explore.Blazor.Client/Pages/**/*.razor`

---

### Task 3.3: Remove Obsolete DTOs
- [ ] Delete duplicate DTO files
- [ ] Keep `Models/` folder for view models if needed
- [ ] Update any remaining references
- [ ] Verify build succeeds

**Acceptance Criteria**:
- Duplicate DTOs removed
- Clean build
- No unused DTO files

**Files**: `Explore.Blazor.Client/Models/DTOs/*.cs`

---

## Phase 4: BFF Simplification (2 hours)

### Task 4.1: Evaluate BFF Necessity
- [ ] Identify which endpoints still need BFF
- [ ] Document endpoints that can use direct client
- [ ] Plan BFF simplification strategy

**Acceptance Criteria**:
- Clear list of BFF-required vs direct endpoints
- Strategy documented

---

### Task 4.2: Simplify/Remove Manual BFF Endpoints
- [ ] Remove redundant BFF mappings from `Program.cs`
- [ ] Keep auth-required endpoints with simplified handler
- [ ] Consider creating `BffController` if needed
- [ ] Update WASM client configuration

**Acceptance Criteria**:
- `Program.cs` significantly shorter
- Auth flow still works
- All features functional

**File**: `Explore.Blazor/Program.cs`

---

### Task 4.3: Update Configuration
- [ ] Add/update `ExploreApi:BaseUrl` in `appsettings.json`
- [ ] Document environment-specific configuration
- [ ] Test both development and production URLs

**Acceptance Criteria**:
- Configuration works in all environments
- Documented in README or config section

**Files**: `appsettings.json`, `appsettings.Development.json`

---

## Phase 5: Testing & Validation (2 hours)

### Task 5.1: Manual Testing
- [ ] Test: Event list loads (anonymous)
- [ ] Test: Event details load
- [ ] Test: Create event (authenticated)
- [ ] Test: Update event
- [ ] Test: Delete event
- [ ] Test: Organization list (my orgs)
- [ ] Test: Create organization
- [ ] Test: Update organization
- [ ] Test: User profile view
- [ ] Test: User profile update
- [ ] Test: Logout/Login flow still works

**Acceptance Criteria**:
- All test cases pass
- No console errors
- UI behaves as before

---

### Task 5.2: Fix Breaking Changes
- [ ] Address any issues found in testing
- [ ] Update components as needed
- [ ] Re-test fixed areas

**Acceptance Criteria**:
- All issues resolved
- Full functionality restored

---

### Task 5.3: Code Cleanup
- [ ] Remove unused using statements
- [ ] Remove commented-out code
- [ ] Ensure consistent formatting
- [ ] Update any outdated comments

**Acceptance Criteria**:
- Clean, readable code
- No dead code

---

## Progress Summary

| Phase | Status | Tasks | Completed |
|-------|--------|-------|-----------|
| Phase 1: Infrastructure | Not Started | 3 | 0 |
| Phase 2: Service Refactor | Not Started | 8 | 0 |
| Phase 3: DTO Migration | Not Started | 3 | 0 |
| Phase 4: BFF Simplification | Not Started | 3 | 0 |
| Phase 5: Testing | Not Started | 3 | 0 |
| **Total** | **Not Started** | **20** | **0** |

---

## Notes

- Start with Phase 1 - infrastructure must be in place first
- Phase 2 can be done service-by-service
- Phase 3 may require coordination with Phase 2
- Phase 4 can be simplified if BFF is still needed for WASM
- Test incrementally after each major change
