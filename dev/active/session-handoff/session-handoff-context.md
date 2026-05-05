<!-- ABOUTME: Session handoff context for corrected Event Program Management implementation. -->
<!-- ABOUTME: Lists verified files, decisions, progress, commands, and resume instructions. -->

# Session Handoff - Context

Last Updated: 2026-05-05

## Session Progress

### Completed

- Corrected product/domain direction: “subevent” means session/program item, not child `Event`.
- Rolled back wrong `Event.ParentEventId` / child-event hierarchy artifacts from server source and generated contracts.
- Added `EventSessionGroup` and `EventSessionGroupSession` domain model and EF persistence mapping.
- Added migration `Explore.Persistence/Migrations/20260505151339_AddEventSessionGroups.cs`.
- Added read-only Application/API/HAL surface for session groups and sessions-by-group.
- Exposed session group assignments on `EventSessionDto` and `EventSessionListDto`.
- Regenerated `Explore.API/swagger.json` and `Explore.Blazor.Client/Clients/EventApiClient.g.cs`.
- Collected independent review and Oracle review; addressed visibility, soft-delete, sessions-by-group, assignment DTO, and HAL feedback.

### In Progress / Next

- Add write commands, validators, authorization, and HAL write links for session groups and assignments.
- Add tests specifically covering rollback invariants and session-group write behavior.
- Integrate Blazor create event flow with Add session / dedicated program-item page.

### Known Repository State

- The repository has unrelated pre-existing dirty files. Do not revert unrelated changes.
- Full solution build has unrelated `Explore.Blazor.Client.E2ETests` fixture/analyzer warnings-as-errors. Use targeted project builds/tests until that unrelated issue is fixed.

## Verified Key Files

### Domain

- `Explore.Domain/Event.cs`
  - Contains `Sessions`, `SessionGroups`, and `AgendaItems` collections.
  - Does not contain `ParentEventId` or `ChildEvents` for program modeling.
- `Explore.Domain/EventSession.cs`
  - Session/program item model.
  - Contains `SessionGroups` assignment collection.
- `Explore.Domain/EventSessionGroup.cs`
  - Track/devroom/stage/program-section entity.
  - Implements tenant, audit, soft-delete, concurrency interfaces.
  - Uses `LocationRoom? Room`.
- `Explore.Domain/EventSessionGroupSession.cs`
  - Explicit session-group assignment join entity.
  - Implements tenant, audit, and soft-delete interfaces.
  - Carries `EventSessionGroupId`, `EventSessionId`, `EventId`, `TenantId`, `IsPrimary`, and `SortOrder`.

### Application

- `Explore.Application/DTOs/EventSessionGroup/EventSessionGroupDto.cs`
- `Explore.Application/DTOs/EventSessionGroup/EventSessionGroupListDto.cs`
- `Explore.Application/DTOs/EventSession/EventSessionGroupAssignmentDto.cs`
- `Explore.Application/Features/EventSessionGroups/Requests/Queries/GetEventSessionGroupsByEventRequest.cs`
- `Explore.Application/Features/EventSessionGroups/Requests/Queries/GetEventSessionGroupDetailRequest.cs`
- `Explore.Application/Features/EventSessionGroups/Requests/Queries/GetEventSessionGroupSessionsRequest.cs`
- `Explore.Application/Features/EventSessionGroups/Handlers/Queries/GetEventSessionGroupsByEventRequestHandler.cs`
- `Explore.Application/Features/EventSessionGroups/Handlers/Queries/GetEventSessionGroupDetailRequestHandler.cs`
- `Explore.Application/Features/EventSessionGroups/Handlers/Queries/GetEventSessionGroupSessionsRequestHandler.cs`
- `Explore.Application/Contracts/Persistence/IEventSessionGroupRepository.cs`
  - `Task<EventSessionGroup?> GetWithDetailsAsync(Guid id, CancellationToken cancellationToken);`
  - `Task<List<EventSessionGroup>> GetByEventAsync(Guid eventId, CancellationToken cancellationToken);`
- `Explore.Application/Contracts/Persistence/IEventSessionGroupSessionRepository.cs`
  - `Task<List<EventSessionGroupSession>> GetByGroupAsync(Guid eventSessionGroupId, CancellationToken cancellationToken);`
  - `Task<List<EventSessionGroupSession>> GetBySessionAsync(Guid eventSessionId, CancellationToken cancellationToken);`

### Persistence

- `Explore.Persistence/Configurations/Entities/EventSessionGroupConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventSessionGroupSessionConfiguration.cs`
- `Explore.Persistence/Repositories/EventSessionGroupRepository.cs`
- `Explore.Persistence/Repositories/EventSessionGroupSessionRepository.cs`
- `Explore.Persistence/ExploreDbContext.DbSets.cs`
- `Explore.Persistence/ExploreDbContext.QueryFilters.cs`
- `Explore.Persistence/PersistenceServicesRegistration.cs`
- `Explore.Persistence/Migrations/20260505151339_AddEventSessionGroups.cs`

### API/HAL

- `Explore.API/Controllers/EventSessionGroupController.cs`
  - `GET /api/eventsessiongroup/by-event/{eventId}`
  - `GET /api/eventsessiongroup/{id}`
  - `GET /api/eventsessiongroup/{id}/sessions`
- `Explore.API/Hateoas/Policies/EventSessionGroupLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- `Explore.API/Hateoas/RouteNames.cs`
- `Explore.API/swagger.json`

### Blazor Generated Client

- `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
  - Contains generated session-group operations.
  - Does not contain rejected parent-candidate/subevents/program child-event operations.

## Important Decisions

1. `Event.ParentEventId` is not the program-management model and has been rolled back.
2. `EventSessionGroup` is internal naming; UI labels should say Track, Devroom, Stage, Program section, or Section.
3. `EventSession` remains the rich model for talks/workshops/panels/classes/activities.
4. `EventAgendaItem` remains the logistics model for breaks, meals, prayer slots, and transitions.
5. `EventSessionGroupSession` is an explicit join entity because sessions can appear in multiple groupings.
6. Public read endpoints show published groups only. Future management endpoints need authenticated queries for draft groups.
7. HAL links are the only source of truth for Blazor action affordances.

## Verification Already Completed

- LSP diagnostics: no errors in touched Application/Persistence/API files and exact files checked after review fixes.
- Builds passed:
  - `dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet`
  - `dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet`
  - `dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet`
- Tests passed:
  - `dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet`
  - `dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet`
- OpenAPI assertion passed: rejected child-event paths absent; session-group paths present.
- Scoped diff/whitespace checks passed.

## Quick Resume

1. Read this file first.
2. Read `session-handoff-tasks.md` for the remaining checklist.
3. Start with Phase 1 regression tests if you need a safe continuation point.
4. Then implement write DTOs/validators/commands for session groups and assignments.
5. Keep using targeted project builds/tests; do not rely on full solution build until unrelated E2E fixture/analyzer issues are resolved.

## Commands To Reuse

```bash
# Server source rejected-artifact check
rg -n "ParentEventId|parent_event_id|ParentEventTitle|ChildEventCount|GetEventParentCandidates|GetEventSubevents|GetEventProgram|EventParentCandidateDto|EventProgramDto|EventProgramItemDto|LinkRelations\.Program|public const string Program" \
  Explore.Domain Explore.Application Explore.Persistence Explore.API Event.Application.UnitTests \
  --glob '*.cs' --glob '!**/bin/**' --glob '!**/obj/**'

# Targeted builds
dotnet build Explore.API/Explore.API.csproj --configuration Release --verbosity quiet
dotnet build Explore.Persistence/Explore.Persistence.csproj --configuration Release --verbosity quiet
dotnet build Explore.Blazor.Client/Explore.Blazor.Client.csproj --configuration Release --verbosity quiet

# Targeted tests
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Persistence.IntegrationTests/Event.Persistence.IntegrationTests.csproj --configuration Release --verbosity quiet
```
