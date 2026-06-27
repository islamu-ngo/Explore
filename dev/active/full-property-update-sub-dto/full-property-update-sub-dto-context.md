# Full Property Update Sub-DTO Pattern - Context

Last Updated: 2026-06-27 Europe/Brussels

## SESSION PROGRESS (2026-06-27 Europe/Brussels)

### COMPLETED
- Planning workstream created.
- Current-state report completed with evidence from docs, rules, skills, CodeGraph, and targeted searches.
- Matched intents identified: `add-write-endpoint`, `add-cqrs-handler`.
- Canonical reference pattern identified in `Explore.Application/DTOs/User/` and `UpdateUserCommandHandler`.

### IN PROGRESS
- Awaiting user review of implementation plan.

### NEXT
1. User reviews the plan, especially Section 2.6 unknowns and Section 5 decisions.
2. First implementation agent starts with Phase 1 inventory and classification.
3. Update this context file after the first implementation slice.

### BLOCKERS
- None for planning.
- Implementation decision still needed during Phase 1: exact clear-null representation for nullable fields inside present groups.

## Quick Resume
1. Read `dev/active/full-property-update-sub-dto/full-property-update-sub-dto-plan.md`.
2. Read `dev/active/full-property-update-sub-dto/full-property-update-sub-dto-tasks.md`.
3. Start with Task 1.1 unless the user narrows the implementation batch.
4. Keep plan/context/tasks updated after each implementation slice.

## Key Files And Responsibilities
| Path | Existing/New | Layer | Purpose | Notes |
|---|---|---|---|---|
| `Explore.Application/DTOs/User/UpdateUserDto.cs` | Existing | Application DTO | Canonical wrapper DTO with nullable `Names` and `ProfileImage`. | Missing `ABOUTME` header in current file; fix when touched. |
| `Explore.Application/DTOs/User/Validators/UpdateUserDtoValidator.cs` | Existing | Application validation | Wrapper and group validators for the User pattern. | Manually instantiated by handler. |
| `Explore.Application/Features/Users/Handlers/Commands/UpdateUserCommandHandler.cs` | Existing | Application CQRS | Reference control flow: validate wrapper, load entity, apply present groups, save, cache invalidation. | Also updates Actor/StorageObject for profile image group. |
| `Explore.Application/Profiles/UserMappingProfile.cs` | Existing | Application mapping | Maps `UpdateUserNamesDto` to `User`; NotMapped delegates write into `UserPii`. | Mapping is handler-owned, not repository-owned. |
| `Explore.Application/Features/Actors/Requests/Commands/UpdateActorCommand.cs` | Existing | Application CQRS | Existing single command with nullable `ActorDto` and `AppearanceDto`. | Needs more complete field grouping. |
| `Explore.Application/Features/Actors/Handlers/Commands/UpdateActorCommandHandler.cs` | Existing | Application CQRS | Applies actor and appearance branches, saves once, removes `actor:detail:{id}`. | Good precedent, but broad `ActorDto` overlaps appearance. |
| `Explore.Application/Features/Events/Requests/Commands/UpdateEventCommand.cs` | Existing | Application CQRS/Auth | Existing event update command with `ISecureRequest`. | Shell is right; DTO is still broad. |
| `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs` | Existing | Application CQRS | Validates broad `UpdateEventDto`, maps, applies schedule timezone, saves, invalidates detail/list cache. | High-risk migration. |
| `Explore.Application/DTOs/Event/Validators/UpdateEventDtoValidator.cs` | Existing | Application validation | Large legacy validator with lookup and timezone rules. | Rules must be moved into group validators without loss. |
| `Explore.API/Controllers/UserController.cs` | Existing | API | Current user update endpoint accepts `UpdateUserDto` and sends command. | Existing `PUT` route should stay. |
| `Explore.API/Controllers/CategoryController.cs` | Existing | API | Example old full DTO update route. | Route/body contract needs wrapper update. |
| `docs/API_CHANGELOG.md` | Existing | Docs | Intent-required public API change log. | Must document schema changes during implementation. |
| `Event.Application.UnitTests/**` | Existing | Tests | Handler and validator tests. | Add missing coverage for User and each migration batch. |
| `Event.API.IntegrationTests/**` | Existing | Tests | API auth/contract/integration coverage. | Add representative partial-update body tests. |
| `Event.Architecture.Tests/**` | Existing | Tests | Architecture and context enforcement. | Run after broad Application/API changes. |

## Key Decisions
- Use wrapper DTOs with nullable sub-DTO groups, following `UpdateUserDto`.
- Keep one `Update{Entity}Command` and one `Update{Entity}CommandHandler` per update-eligible entity.
- Group by independently saveable invariant. Use one-property groups where safe, logical groups where atomicity matters.
- Preserve existing routes and route names unless the user approves a route contract change.
- Do not expose writes for read-only/internal/system-owned domain rows.

## Constraints And Rules To Remember
- Repositories return entities only; mapping belongs in handlers.
- Validators are manually instantiated; no DI-injected validators.
- Write endpoints remain `[Authorize]`.
- HAL links are the UI source of truth for mutation affordances.
- Link/junction writes go through repositories, not navigation collection mutation.
- All new/touched files need two `ABOUTME` lines.
- Use `Guid` for aggregates, `int` for lookups, `long` only for cursors/large sizes.
- Do not add backward compatibility shims unless explicitly approved.

## Validation Baseline
```bash
dotnet build --configuration Release --verbosity quiet
dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.API.IntegrationTests/Event.API.IntegrationTests.csproj --configuration Release --verbosity quiet
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

Focused tests should be run per migrated batch using `--filter FullyQualifiedName~...`.

## Current Known Risks / Unknowns
- Clear-null semantics for nullable fields inside present groups.
- Domain actions versus property updates for schedule, publish/archive/moderation, template sync, and settings batches.
- OpenAPI/client regeneration workflow must be verified before public request schemas change.
- Cache invalidation varies per feature and must be inventoried per handler.

## Handoff Notes

### Handoff - 2026-06-27 Europe/Brussels
- **Current state:** Planning docs created. No product code changed.
- **Next action:** Start Task 1.1 inventory and classification.
- **Blockers:** None for planning; clear-null semantics must be decided during inventory before DTO split for nullable fields.
- **Modified files:** `dev/active/full-property-update-sub-dto/full-property-update-sub-dto-plan.md`, `full-property-update-sub-dto-context.md`, `full-property-update-sub-dto-tasks.md`.
- **Validation:** Documentation-only change; no build/test run during planning.
- **Documentation impact:** Implementation must update `docs/API_CHANGELOG.md` when request schemas change.
- **Risks:** High blast radius across DTOs, handlers, controllers, tests, and generated contracts.
- **Notes for next contributor/agent:** Do not start by generating DTOs globally. Inventory first, classify exclusions, and migrate in feature batches.
