# NSwag API Client Refactor - Context

**Last Updated**: 2026-01-11

---

## SESSION PROGRESS (2026-01-11)

### Completed
- [x] Analyzed current codebase architecture
- [x] Reviewed generated NSwag client (`EventApiClient.g.cs`)
- [x] Identified all existing services using manual HTTP calls
- [x] Created comprehensive implementation plan
- [x] Created dev-docs structure

### In Progress
- [ ] None - Ready for implementation

### Blockers
- None identified

---

## Key Files

### Generated NSwag Client

**`Explore.Blazor.Client/Clients/EventApiClient.g.cs`**
- Auto-generated from OpenAPI spec
- Contains `IEventApiClient` interface with all API operations
- Contains all DTO classes matching API contracts
- Contains `ApiException` for error handling
- ~15,000+ lines covering entire API surface

### Current Services (To Be Refactored)

**`Explore.Blazor.Client/Services/EventService.cs`**
- Interface: `IEventService`
- Uses: `HttpClient` with manual calls to `/bff/api/Event`
- Methods: `GetAllEventsAsync`, `GetMyEventsAsync`, `GetEventByIdAsync`, `CreateEventAsync`, `UpdateEventAsync`, `DeleteEventAsync`
- Issues: Manual JSON handling, duplicate DTOs, legacy methods

**`Explore.Blazor.Client/Services/OrganizationService.cs`**
- Interface: `IOrganizationService`
- Uses: `HttpClient` with manual calls to `/bff/api/Organization`
- Methods: `CreateOrganizationAsync`, `GetStatusTypesAsync`, `GetMyOrganizationsAsync`, `GetOrganizationByIdAsync`, `UpdateOrganizationAsync`
- Issues: Manual JSON handling, BaseCommandResponse parsing

**`Explore.Blazor.Client/Services/UserService.cs`**
- User profile operations

**`Explore.Blazor.Client/Services/AdminService.cs`**
- Admin operations for organizations

**`Explore.Blazor.Client/Services/OrganizationMemberService.cs`**
- Organization membership management

**`Explore.Blazor.Client/Services/OrganizationReviewService.cs`**
- Organization review CRUD

**`Explore.Blazor.Client/Services/ProgramService.cs`**
- Program/event program operations

### BFF Configuration

**`Explore.Blazor/Program.cs`**
- ~900 lines
- Contains ~80 manual BFF endpoint mappings (`/bff/api/*`)
- Handles auth token injection manually
- Two HttpClient configurations: `ExploreApi` (authenticated), `ExploreApiPublic` (anonymous)
- Uses Duende AccessTokenManagement for token refresh

### DI Registration

**`Explore.Blazor.Client/Program.cs`**
- WebAssembly host setup
- Registers all services with `HttpClient`
- Uses `AnonymousAuthenticationStateProvider`
- HttpClient base address: `builder.HostEnvironment.BaseAddress`

### Existing DTOs (To Be Replaced)

**`Explore.Blazor.Client/Models/DTOs/`**
- `EventDto.cs` - Contains `EventDetailsDto`
- `EventListDto.cs`
- `CreateEventDto.cs`
- `UpdateEventDto.cs`
- `OrganizationDto.cs` - Contains `OrganizationDto`, `OrganizationListDto`, `OrganizationCreateDto`
- `UserDto.cs`
- `UpdateUserDto.cs`
- And more...

These will be replaced by generated DTOs from `EventApiClient.g.cs`.

---

## Important Decisions

### Decision 1: Hybrid Approach for Server vs WASM

**Decision**: Use direct `IEventApiClient` for Blazor Server, keep BFF pattern for WASM

**Rationale**:
- Server-side can directly call API with auth token handler
- WASM cannot store tokens securely, needs BFF proxy
- Reduces BFF complexity while maintaining security

### Decision 2: Keep Service Layer Abstraction

**Decision**: Keep `IEventService`, `IOrganizationService` interfaces, implement with NSwag client

**Rationale**:
- Maintains testability
- Allows different implementations for Server vs WASM if needed
- Provides consistent API to Razor components

### Decision 3: Use Generated DTOs

**Decision**: Replace local DTOs with generated DTOs from NSwag client

**Rationale**:
- Single source of truth
- Type safety with API contract
- No manual DTO synchronization

### Decision 4: Partial Class for Client Customization

**Decision**: Use partial class pattern for NSwag client customization

**Rationale**:
- Allows adding custom behavior without modifying generated code
- Supports logging, custom headers, error handling

---

## Technical Notes

### NSwag Client Constructor

```csharp
// Generated client accepts HttpClient
public EventApiClient(HttpClient httpClient)
```

### Base URL Configuration

The generated client uses relative URLs. Base URL must be set on HttpClient:
```csharp
services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:7039/");
});
```

### Auth Token Handler Pattern

```csharp
public class ApiAuthHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await GetAccessTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await base.SendAsync(request, cancellationToken);
    }
}
```

### Generated DTO Namespace

All generated DTOs are in: `Explore.Blazor.Client.Clients`

Examples:
- `Explore.Blazor.Client.Clients.EventDto`
- `Explore.Blazor.Client.Clients.EventListDto`
- `Explore.Blazor.Client.Clients.CreateEventDto`
- `Explore.Blazor.Client.Clients.OrganizationDto`

---

## Dependencies & Relationships

```
Explore.Blazor (Server)
├── References: Explore.Blazor.Client
├── Uses: IEventApiClient (direct, with auth handler)
└── BFF endpoints call API for WASM clients

Explore.Blazor.Client (WASM)
├── Contains: EventApiClient.g.cs
├── Contains: Services (EventService, OrganizationService, etc.)
└── Services use: IEventApiClient (via BFF or direct depending on render mode)
```

---

## Quick Resume Instructions

To continue this refactoring task:

1. **Read this context file** to understand current state
2. **Check tasks file** for current progress
3. **Start with Phase 1** if not started
4. **Key first step**: Create `ApiAuthHandler.cs` in `Explore.Blazor/Infrastructure/`
5. **Test early**: After Phase 1, verify auth token flows correctly

### Key Commands

```bash
# Build to check for errors
dotnet build

# Run with Aspire
dotnet run --project Explore.AppHost

# Regenerate NSwag client (if needed)
nswag openapi2csclient /input:https://localhost:7039/swagger/v1/swagger.json /output:Explore.Blazor.Client/Clients/EventApiClient.g.cs /namespace:Explore.Blazor.Client.Clients /classname:EventApiClient
```

---

## Related Files

- **Plan**: `dev/active/nswag-client-refactor/nswag-client-refactor-plan.md`
- **Tasks**: `dev/active/nswag-client-refactor/nswag-client-refactor-tasks.md`
- **Project Docs**: `CLAUDE.md`, `docs/ARCHITECTURE.md`
