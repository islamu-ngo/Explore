# Context: Group Actor Feature

**Last Updated**: 2026-02-19

---

## Session Progress

### Completed
- Implemented group actor publish path across Application, API, and Blazor create-event flow.
- Added `GroupId` validation and org/group mutual exclusivity checks in event validators.
- Added tenant publishing-policy enforcement in event handlers (`OrganizationAndGroupOnly` blocks personal path).
- Added `GroupController` and `GroupMemberController` plus group route constants.
- Extended tenant settings DTO contracts to round-trip publishing policy fields.
- Added group service integration and publish-as group UX in Blazor create event page.
- Added HATEOAS support for groups (assembler + link policies + registrations).
- Added authorization resource-kind mapping for `GroupDto` and `GroupListDto`.
- Ran build and impacted tests; all required suites are green.

### In Progress
- Feature documentation alignment updates in `dev/active/group-actor-feature/*`.

### Blockers
- None.

---

## High-Impact Fixes Applied In This Session

### 1) API 500 on `GET /api/group` (integration test failure)
- Root cause 1: missing group HATEOAS registrations caused controller HAL generation failures.
- Root cause 2: missing `ResourceDescriptorRegistry` mapping for `GroupDto`/`GroupListDto` caused `InvalidOperationException` in `RequirePermission`.
- Resolution:
  - Added `Explore.API/Hateoas/Assemblers/GroupResourceAssembler.cs`.
  - Added `Explore.API/Hateoas/Policies/GroupLinkPolicy.cs`.
  - Registered both in `Explore.API/Extensions/HateoasAssemblerRegistration.cs`.
  - Added group resource kind mappings in `Explore.Application/Authorization/ResourceDescriptorRegistry.cs`.

### 2) Blazor test baseline completion
- Re-ran Blazor tests with supported CLI args only.
- Result: suite passes after prior DI fix for `IGroupService`.

---

## Key Files Touched (Current Feature State)

### Application
- `Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs`
- `Explore.Application/DTOs/Event/Validators/CreateEventWithSessionsDtoValidator.cs`
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
- `Explore.Application/Features/Events/Handlers/Commands/CreateEventWithSessionsCommandHandler.cs`
- `Explore.Application/DTOs/TenantSettings/TenantSettingsDto.cs`
- `Explore.Application/DTOs/TenantSettings/TenantSettingsListDto.cs`
- `Explore.Application/DTOs/TenantSettings/CreateTenantSettingsDto.cs`
- `Explore.Application/DTOs/TenantSettings/UpdateTenantSettingsDto.cs`
- `Explore.Application/Profiles/MappingProfile.cs`
- `Explore.Application/DTOs/GroupMember/GroupMemberListDto.cs`
- `Explore.Application/Authorization/ResourceDescriptorRegistry.cs`

### API
- `Explore.API/Controllers/EventController.cs`
- `Explore.API/Controllers/GroupController.cs`
- `Explore.API/Controllers/GroupMemberController.cs`
- `Explore.API/Hateoas/RouteNames.cs`
- `Explore.API/Hateoas/Assemblers/GroupResourceAssembler.cs`
- `Explore.API/Hateoas/Policies/GroupLinkPolicy.cs`
- `Explore.API/Extensions/HateoasAssemblerRegistration.cs`

### Blazor Client
- `Explore.Blazor.Client/Services/GroupService.cs`
- `Explore.Blazor.Client/Program.cs`
- `Explore.Blazor.Client/Pages/Event/CreateEvent.razor`
- `Explore.Blazor.Client/Pages/Event/CreateEvent.razor.cs`
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs` (contains `CreateEventDto.GroupId` patch)

### Tests
- `Event.Application.UnitTests/Features/Events/Validators/CreateEventDtoValidatorTests.cs`
- `Event.Application.UnitTests/Features/Events/Commands/CreateEventCommandHandlerTests.cs`
- `Explore.Blazor.Client.Tests/Common/MockServiceFactory.cs`
- `Explore.Blazor.Client.Tests/Common/BlazorTestContext.cs`

---

## Verification Evidence (Latest)

- `dotnet build --configuration Release --nologo -clp:ErrorsOnly` -> success, 0 errors.
- `dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet` -> passed (401/401).
- `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet` -> passed (273/273).
- `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet` -> passed (32/32).
- `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet` -> passed (498/498).
- Earlier in same verification wave:
  - `Event.Domain.UnitTests` passed (61/61).
  - `Explore.Secrets.UnitTests` passed (190/190).
  - `Event.Persistence.IntegrationTests` passed (2/2).

Note: solution/test output still contains pre-existing warnings across unrelated files/projects; no new build/test errors remain.

---

## Remaining Actions

1. Optional manual smoke validation for UX/policy matrix:
   - create event as organization,
   - create event as group,
   - verify personal path blocked/allowed per tenant policy.
2. Optional NSwag regeneration to replace the manual `EventApiClient.g.cs` `GroupId` patch if team workflow requires generated-client parity.

---

## Quick Resume

1. Start with `dev/active/group-actor-feature/group-actor-feature-tasks.md` for completion state.
2. If continuing feature hardening, focus first on manual policy-path smoke checks.
3. Keep this context file synchronized when changing generated clients or API contracts.
