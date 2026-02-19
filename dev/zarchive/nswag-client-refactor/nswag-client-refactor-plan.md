# NSwag API Client Refactor Plan

**Last Updated**: 2026-01-11  
**Status**: Planning  
**Estimated Effort**: Medium (8-12 hours)

---

## Executive Summary

Refactor the Blazor Server (`Explore.Blazor`) and Blazor WebAssembly (`Explore.Blazor.Client`) projects to use the newly generated NSwag API client (`EventApiClient.g.cs`) instead of the current manual HTTP client approach with BFF proxy endpoints.

### Current State
- **Manual HTTP calls** via `HttpClient` in service classes (e.g., `EventService`, `OrganizationService`)
- **BFF (Backend-for-Frontend) proxy pattern** in `Explore.Blazor/Program.cs` with ~80 manual endpoint mappings
- **Duplicate DTOs** in `Explore.Blazor.Client/Models/DTOs/` that don't match API DTOs
- **Inconsistent error handling** with try/catch and manual JSON deserialization

### Proposed Future State
- **Generated NSwag client** (`IEventApiClient`) for all API calls
- **Type-safe DTOs** from the generated client (no duplicate DTOs)
- **Simplified BFF layer** - only handles auth token injection, not endpoint mapping
- **Consistent error handling** via `ApiException` from NSwag client
- **Reduced maintenance** - regenerate client when API changes

### Key Benefits
1. **Type Safety**: Compile-time errors when API contracts change
2. **Reduced Code**: Remove ~80 manual BFF endpoint mappings
3. **Consistency**: Single source of truth for DTOs from OpenAPI spec
4. **Maintainability**: Regenerate client instead of manual updates
5. **Error Handling**: Consistent `ApiException` handling

---

## Current State Analysis

### Architecture Overview

```
Current Flow:
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Blazor WASM    │───▶│  Blazor Server  │───▶│   Explore.API   │
│  (Client)       │    │  (BFF Proxy)    │    │   (REST API)    │
└─────────────────┘    └─────────────────┘    └─────────────────┘
        │                      │
        │ HttpClient           │ HttpClient + Token
        │ /bff/api/*           │ api/*
        └──────────────────────┘

Services use: HttpClient.GetFromJsonAsync("/bff/api/Event")
```

### Key Files (Current)

| File | Purpose | Lines | Issues |
|------|---------|-------|--------|
| `Explore.Blazor/Program.cs` | BFF proxy endpoints | ~900 | 80+ manual mappings |
| `Explore.Blazor.Client/Services/EventService.cs` | Event operations | ~165 | Manual HTTP calls |
| `Explore.Blazor.Client/Services/OrganizationService.cs` | Organization ops | ~150 | Manual HTTP calls |
| `Explore.Blazor.Client/Models/DTOs/*.cs` | Local DTOs | ~15 files | Duplicate/outdated |
| `Explore.Blazor.Client/Program.cs` | WASM DI setup | ~50 | HttpClient only |

### Generated NSwag Client

| File | Purpose | Content |
|------|---------|---------|
| `EventApiClient.g.cs` | Generated client | ~15,000+ lines |
| `IEventApiClient` | Interface | All API operations |
| `*Dto` classes | Generated DTOs | Match API exactly |
| `ApiException` | Error type | Consistent error handling |

### API Endpoints Covered by Generated Client

The generated `IEventApiClient` covers all API endpoints:
- **Actor**: CRUD + ByDid, ByTenant
- **ActorKeyStore**: CRUD
- **ActorType**: GetAll, GetById
- **ApprovalStatus**: CRUD
- **AtprotoRecord**: CRUD
- **AudienceAge**: GetAll, GetById
- **AudienceGender**: GetAll, GetById
- **Category**: CRUD
- **DidCustodyType**: GetAll, GetById
- **Event**: CRUD + My, ByCategory
- **EventCategories**: CRUD + ByEvent, ByCategory
- **EventFormat**: GetAll, GetById
- **EventRegistration**: CRUD
- **EventSession**: CRUD + ByEvent
- **EventSessionAgendaItem**: CRUD
- **EventSessionLanguage**: CRUD
- **EventSessionSpeaker**: CRUD
- **EventStatus**: GetAll, GetById
- **EventTags**: CRUD
- **EventType**: GetAll, GetById
- **FileType**: GetAll, GetById
- **IndexedDid**: CRUD
- **Language**: GetAll, GetById
- **Location**: CRUD
- **Madhab**: GetAll, GetById
- **Organization**: CRUD + My
- **OrganizationMember**: CRUD + ByOrganization, ByUser
- **OrganizationPosition**: GetAll, GetById
- **OrganizationReview**: CRUD
- **OrganizationRole**: GetAll, GetById
- **RegistrationMode**: GetAll, GetById
- **StorageObject**: CRUD
- **SyncState**: CRUD
- **Tag**: CRUD
- **TagType**: CRUD
- **TagTypeTags**: CRUD
- **Tenant**: CRUD
- **TenantSettings**: CRUD
- **TenantUser**: CRUD
- **User**: CRUD + Sync, Me
- **UserAuthenticationToken**: CRUD
- **UserExternalLogin**: CRUD
- **UserRole**: CRUD
- **VisibilityType**: GetAll, GetById

---

## Proposed Future State

### Architecture Overview

```
Future Flow (Option A - Direct Client):
┌─────────────────┐    ┌─────────────────┐
│  Blazor Server  │───▶│   Explore.API   │
│  (SSR + WASM)   │    │   (REST API)    │
└─────────────────┘    └─────────────────┘
        │
        │ IEventApiClient
        │ (with auth token handler)
        └──────────────────────

Services use: IEventApiClient.EventAllAsync()
```

### Two Integration Approaches

#### Option A: Direct API Client (Recommended for Server)
- Blazor Server uses `IEventApiClient` directly
- Token attached via `DelegatingHandler`
- No BFF proxy needed for server-rendered pages
- Best for: SSR pages, admin pages

#### Option B: BFF with Generated Client (For WASM)
- WASM calls simplified BFF endpoints
- BFF uses `IEventApiClient` internally
- BFF still handles token injection
- Best for: Client-side interactive components

### Recommended Hybrid Approach

1. **Blazor Server (SSR)**: Use `IEventApiClient` directly with auth handler
2. **Blazor WASM (Interactive)**: Call BFF, which uses `IEventApiClient`
3. **Shared Services**: Wrap `IEventApiClient` in service interfaces
4. **Remove duplicate DTOs**: Use generated DTOs from client

---

## Implementation Phases

### Phase 1: Infrastructure Setup (2 hours)

Set up NSwag client registration and authentication handlers.

#### Task 1.1: Create Auth Token Handler for NSwag Client
- **File**: `Explore.Blazor/Infrastructure/ApiAuthHandler.cs`
- **Purpose**: Attach Bearer token to NSwag client requests
- **Acceptance Criteria**:
  - [ ] Handler retrieves access token from HttpContext
  - [ ] Token attached to Authorization header
  - [ ] Handler works with Duende token management
- **Effort**: S
- **Dependencies**: None

#### Task 1.2: Register NSwag Client in Blazor Server DI
- **File**: `Explore.Blazor/Program.cs`
- **Purpose**: Configure `IEventApiClient` for dependency injection
- **Acceptance Criteria**:
  - [ ] `IEventApiClient` registered as scoped service
  - [ ] HttpClient configured with base URL
  - [ ] Auth handler attached to HttpClient pipeline
  - [ ] SSL certificate handling for development
- **Effort**: S
- **Dependencies**: Task 1.1

#### Task 1.3: Register NSwag Client in Blazor WASM DI
- **File**: `Explore.Blazor.Client/Program.cs`
- **Purpose**: Configure client for WebAssembly (via BFF)
- **Acceptance Criteria**:
  - [ ] `IEventApiClient` registered pointing to BFF base URL
  - [ ] Works with existing anonymous auth state provider
- **Effort**: S
- **Dependencies**: None

### Phase 2: Service Layer Refactor (4 hours)

Replace manual HTTP calls with NSwag client.

#### Task 2.1: Create Wrapper Service Interface Pattern
- **File**: `Explore.Blazor.Client/Services/IApiService.cs`
- **Purpose**: Define abstraction over NSwag client for testability
- **Acceptance Criteria**:
  - [ ] Interface wraps common API operations
  - [ ] Can be implemented differently for Server vs WASM
- **Effort**: S
- **Dependencies**: Phase 1

#### Task 2.2: Refactor EventService to Use NSwag Client
- **File**: `Explore.Blazor.Client/Services/EventService.cs`
- **Purpose**: Replace HttpClient calls with IEventApiClient
- **Acceptance Criteria**:
  - [ ] All methods use `IEventApiClient`
  - [ ] Error handling uses `ApiException`
  - [ ] Remove manual JSON serialization
  - [ ] Use generated `EventDto`, `EventListDto` types
  - [ ] Legacy methods removed or deprecated
- **Effort**: M
- **Dependencies**: Task 2.1

#### Task 2.3: Refactor OrganizationService to Use NSwag Client
- **File**: `Explore.Blazor.Client/Services/OrganizationService.cs`
- **Purpose**: Replace HttpClient calls with IEventApiClient
- **Acceptance Criteria**:
  - [ ] All methods use `IEventApiClient`
  - [ ] Use generated `OrganizationDto`, `OrganizationListDto` types
  - [ ] Error handling consistent with EventService
- **Effort**: M
- **Dependencies**: Task 2.1

#### Task 2.4: Refactor Remaining Services
- **Files**: 
  - `UserService.cs`
  - `AdminService.cs`
  - `OrganizationMemberService.cs`
  - `OrganizationReviewService.cs`
  - `ProgramService.cs`
- **Purpose**: Convert all services to use NSwag client
- **Acceptance Criteria**:
  - [ ] All services use `IEventApiClient`
  - [ ] Consistent error handling pattern
- **Effort**: M
- **Dependencies**: Task 2.2, 2.3

### Phase 3: DTO Migration (2 hours)

Remove duplicate DTOs and use generated types.

#### Task 3.1: Audit Existing DTOs vs Generated DTOs
- **Files**: `Explore.Blazor.Client/Models/DTOs/*.cs`
- **Purpose**: Identify which DTOs can be replaced
- **Acceptance Criteria**:
  - [ ] List of DTOs to remove
  - [ ] List of DTOs to keep (if any)
  - [ ] Mapping between old and new DTO names
- **Effort**: S
- **Dependencies**: None

#### Task 3.2: Update Razor Components to Use Generated DTOs
- **Files**: `Explore.Blazor.Client/Pages/**/*.razor`
- **Purpose**: Change type references to generated types
- **Acceptance Criteria**:
  - [ ] All components use `Explore.Blazor.Client.Clients.*Dto`
  - [ ] No compiler errors
  - [ ] UI renders correctly
- **Effort**: M
- **Dependencies**: Task 3.1

#### Task 3.3: Remove Obsolete DTO Files
- **Files**: `Explore.Blazor.Client/Models/DTOs/*.cs`
- **Purpose**: Clean up duplicate types
- **Acceptance Criteria**:
  - [ ] Duplicate DTOs removed
  - [ ] No compiler errors
  - [ ] `Models/DTOs/` folder cleaned or removed
- **Effort**: S
- **Dependencies**: Task 3.2

### Phase 4: BFF Simplification (2 hours)

Simplify or remove manual BFF proxy endpoints.

#### Task 4.1: Create Simplified BFF Controller (Option B)
- **File**: `Explore.Blazor/Controllers/BffController.cs`
- **Purpose**: Centralized BFF that uses NSwag client
- **Acceptance Criteria**:
  - [ ] Generic pass-through for authenticated requests
  - [ ] Uses `IEventApiClient` internally
  - [ ] Handles auth token injection
- **Effort**: M
- **Dependencies**: Phase 2

#### Task 4.2: Remove Manual BFF Endpoints from Program.cs
- **File**: `Explore.Blazor/Program.cs`
- **Purpose**: Remove ~80 manual endpoint mappings
- **Acceptance Criteria**:
  - [ ] Manual `/bff/api/*` mappings removed
  - [ ] Replaced with controller-based or direct client approach
  - [ ] Program.cs significantly shorter
- **Effort**: M
- **Dependencies**: Task 4.1

#### Task 4.3: Update Client Base URL Configuration
- **Files**: `appsettings.json`, `Program.cs`
- **Purpose**: Configure proper base URLs for different environments
- **Acceptance Criteria**:
  - [ ] Development URLs work
  - [ ] Production configuration documented
- **Effort**: S
- **Dependencies**: Task 4.2

### Phase 5: Testing & Validation (2 hours)

#### Task 5.1: Manual Testing of Key Flows
- **Purpose**: Verify refactored code works
- **Test Cases**:
  - [ ] Event list loads (anonymous)
  - [ ] Event details load
  - [ ] Create event (authenticated)
  - [ ] Update event
  - [ ] Delete event
  - [ ] Organization CRUD
  - [ ] User profile operations
- **Effort**: M
- **Dependencies**: All previous phases

#### Task 5.2: Fix Any Breaking Changes
- **Purpose**: Address issues found during testing
- **Acceptance Criteria**:
  - [ ] All test cases pass
  - [ ] No console errors
  - [ ] API calls work end-to-end
- **Effort**: Variable
- **Dependencies**: Task 5.1

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Generated DTOs don't match UI expectations | Medium | Medium | Keep some local DTOs as view models |
| Auth token not properly attached | Low | High | Test auth flow early in Phase 1 |
| NSwag client has different error handling | Medium | Low | Wrap in try/catch, map to UI errors |
| Breaking existing Razor components | Medium | Medium | Incremental migration, test each page |
| Performance regression | Low | Medium | Compare response times before/after |

---

## Success Metrics

1. **Code Reduction**: Remove 500+ lines from `Program.cs`
2. **DTO Reduction**: Remove 10+ duplicate DTO files
3. **Type Safety**: All API calls use generated types
4. **Build Success**: No compiler errors after migration
5. **Functionality**: All existing features work correctly

---

## Required Resources

### NuGet Packages (Already Present)
- `NSwag.ApiDescription.Client` - For client generation
- `Newtonsoft.Json` - Used by generated client

### Configuration
- API base URL in `appsettings.json`
- NSwag generation config (if regeneration needed)

### Files to Modify
- `Explore.Blazor/Program.cs` (major changes)
- `Explore.Blazor.Client/Program.cs` (minor changes)
- `Explore.Blazor.Client/Services/*.cs` (all services)
- `Explore.Blazor.Client/Pages/**/*.razor` (DTO references)
- `Explore.Blazor.Client/Models/DTOs/*.cs` (remove)

---

## Implementation Notes

### NSwag Client Partial Class Extension

The generated client supports partial methods for customization:

```csharp
// Explore.Blazor.Client/Clients/EventApiClient.Extensions.cs
namespace Explore.Blazor.Client.Clients;

public partial class EventApiClient
{
    partial void Initialize()
    {
        // Custom initialization
    }

    partial void PrepareRequest(HttpClient client, HttpRequestMessage request, string url)
    {
        // Add custom headers, logging, etc.
    }

    partial void ProcessResponse(HttpClient client, HttpResponseMessage response)
    {
        // Custom response processing
    }
}
```

### Error Handling Pattern

```csharp
try
{
    var events = await _apiClient.EventAllAsync();
    return events.ToList();
}
catch (ApiException ex) when (ex.StatusCode == 401)
{
    // Handle unauthorized
    throw new UnauthorizedAccessException("Please log in again");
}
catch (ApiException ex) when (ex.StatusCode == 404)
{
    // Handle not found
    return null;
}
catch (ApiException ex)
{
    // Log and rethrow or return error
    _logger.LogError(ex, "API call failed: {Status}", ex.StatusCode);
    throw;
}
```

### Regenerating the Client

If API changes, regenerate the client:

```bash
# Using NSwag CLI
nswag openapi2csclient /input:https://localhost:7039/swagger/swagger.json /output:Clients/EventApiClient.g.cs /namespace:Explore.Blazor.Client.Clients /classname:EventApiClient
```

Or configure in `.csproj` for build-time generation.

---

## Related Documentation

- **CLAUDE.md** - Project overview
- **docs/API.md** - API endpoints documentation
- **docs/ARCHITECTURE.md** - Clean Architecture patterns
- **.claude/skills/blazor-mudblazor-guidelines** - Blazor best practices
