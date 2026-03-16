Last Updated: 2026-03-16 Europe/Brussels

# Context: Event Scheduling Refactor

## SESSION PROGRESS (2026-03-16 Europe/Brussels)

### ✅ COMPLETED
- Audited core docs and relevant skill files.
- Verified current event domain entities, EF configurations, repositories, controllers, DTOs, and Blazor pages/components.
- Confirmed `EventSeries` already exists in domain, persistence, application, and API layers.
- Confirmed `EventRegistration` is still session-scoped and is the core semantic mismatch to fix.
- Confirmed no existing `EventDay`, `EventAgendaItem`, `LocationRoom`, `RegistrationScope`, or registration-child tables/classes.
- Wrote the first iterative planning set for this refactor under `dev/active/event-scheduling-refactor/`.
- Confirmed the Blazor client currently uses `Explore.Blazor.Client/Clients/DtoPartials.cs` to patch missing generated DTO fields like `EventSeriesId`, `SeriesOrder`, and session image fields.
- Confirmed the UI already has a “Register for ALL Sessions” flow in `Explore.Blazor.Client/Pages/Events/Dialogs/SessionSelectionDialog.razor`, but it still submits a session ID list rather than a whole-event registration intent.
- Confirmed several session/series UI components already exist from active work (`EventSeriesSection`, `SessionSummaryCard`, `SessionEditorPanel`) and should be reused.
- Confirmed `Explore.Blazor.Client/Services/EventService.cs` is the main event orchestration surface in the UI and already exposes `GetSessionsByEventAsync`, `RegisterForEventSessionAsync`, and `GetRegistrationsByUserAsync`, which are consumed across event and user pages.
- Confirmed registration data is reused outside event detail, including user-facing pages such as `Explore.Blazor.Client/Pages/User/MyRegistrations.razor.cs` and `Explore.Blazor.Client/Pages/User/UserProfile.razor.cs`.
- Collected the final planning-review output and locked the main sequencing guidance: additive schema first, nullable FKs first, isolated registration phase, OpenAPI/NSwag boundary before broad Blazor work, and atomic commit slices.
- Incorporated follow-up feedback that the registration refactor should add a parent intent/group layer while keeping child rows as the concrete session entitlements/access records.
- Captured the validator strategy that same room + overlapping time should be rejected first in DTO validators via async FluentValidation checks.
- Finalized the plan wording so registration is modeled as parent intent/group semantics plus child concrete session entitlements/access rows.
- Preserved the existing UI/UX planning in the plan file and restricted this session's changes to non-UI architecture, sequencing, migration wording, validator strategy, and handoff quality.
- Collected Tavily research for intent-vs-entitlement registration modeling and Context7 FluentValidation guidance for async repository-backed overlap validation.
- Updated the active-task docs specifically for context-reset continuity.

### 🟡 IN PROGRESS
- Convert the finalized plan into implementation-ready PR/issue slices.
- Decide whether the first coding slice should start with ADR/tests or additive schema entities.
- No code implementation has started for this track yet; the repo state for this task is planning-only.

### ⚠️ BLOCKERS / RISKS
- `dotnet build --configuration Release --verbosity quiet` currently fails with a pre-existing file lock on `Event.Architecture.Tests.dll`, so solution-wide verification is not yet a clean baseline.
- Relevant event UI work is already in progress under `dev/active/session-series-ux/`; this refactor must build on it rather than fork it.
- Current API/UI assume event registration equals one or more session rows, so registration refactor will be cross-cutting.
- The generated API client is currently incomplete enough to require partial DTO patching, so schema/client drift is already a live issue before this refactor begins.
- The biggest sequencing risk is attempting schema changes, contract changes, NSwag regeneration, and Blazor UI rewrites in the same PR.
- The biggest modeling risk is naming the target badly: if implementers think child session rows are no longer the concrete access unit, they can muddle capacity, attendance, refunds, and approvals.
- `event-scheduling-refactor-plan.md` still contains extensive UI/UX planning detail that was user-directed; future work should not casually replace it during non-UI phases.

### KEY DECISIONS THIS SESSION
- Keep the registration architecture phrased as: parent rows preserve registration intent/policy semantics, child rows remain the concrete session entitlements/access records.
- Do not describe the target as “EventRegistration becomes the abstract parent” because that muddies existing session-level semantics and makes migration harder.
- Same room plus overlapping time should fail fast in DTO validators first, using async FluentValidation rules with repository-backed checks. Stronger persistence hardening can follow later if required.
- Keep UI/UX planning as-is for now; this session only tightened the non-UI plan and documentation continuity.

### FILES MODIFIED THIS SESSION
- `dev/active/event-scheduling-refactor/event-scheduling-refactor-plan.md` — revised registration semantics, validator strategy, sequencing notes, and risk wording.
- `dev/active/event-scheduling-refactor/event-scheduling-refactor-context.md` — added final planning decisions, blockers, and restart notes.
- `dev/active/event-scheduling-refactor/event-scheduling-refactor-tasks.md` — updated checklist wording to reflect parent-intent/group plus child-entitlement direction.
- `dev/_journal/journal.md` — recorded hard-to-rediscover planning insights and handoff risks.
- `dev/_journal/MAJOR_DECISIONS.md` — recorded the registration architecture decision.

### EXACT HANDOFF STATE
- Primary current file is `dev/active/event-scheduling-refactor/event-scheduling-refactor-plan.md` around the registration section and locked-planning-decisions section; this was the last area revised this session.
- Current goal on resume: break the approved plan into implementation PR slices without changing the underlying architecture decisions again unless new repo evidence demands it.
- There is no partially completed feature implementation for this track yet; the work product is the finalized planning documentation.
- Commands to run after restart if implementation begins:
  - `dotnet build --configuration Release --verbosity quiet`
  - Then the per-project test matrix from `CLAUDE.md`, starting with architecture/domain/application slices before UI.
- Existing known baseline issue before implementation: release build can fail due to a pre-existing lock on `Event.Architecture.Tests.dll`.

## Key Verified Files

### Domain

| File | Verified relevance |
|---|---|
| `Explore.Domain/Event.cs` | Existing `EventSeriesId`, `SeriesOrder`, timezone fields, session summary fields |
| `Explore.Domain/EventSeries.cs` | Existing umbrella grouping entity |
| `Explore.Domain/EventSession.cs` | Current schedulable session model |
| `Explore.Domain/EventSessionAgendaItem.cs` | Current session-internal agenda item model |
| `Explore.Domain/EventRegistration.cs` | Current session-scoped registration model |

### Persistence

| File | Verified relevance |
|---|---|
| `Explore.Persistence/ExploreDbContext.cs` | DbSets for current event/session/registration/series only |
| `Explore.Persistence/Configurations/Entities/EventConfiguration.cs` | Event indexes and constraints |
| `Explore.Persistence/Configurations/Entities/EventSessionConfiguration.cs` | Current session mapping |
| `Explore.Persistence/Configurations/Entities/EventSessionAgendaItemConfiguration.cs` | Current session agenda mapping |
| `Explore.Persistence/Configurations/Entities/EventRegistrationConfiguration.cs` | Unique `(EventSessionId, UserId)` constraint |
| `Explore.Persistence/Configurations/Entities/EventSeriesConfiguration.cs` | Existing event-series table mapping |
| `Explore.Persistence/Configurations/Entities/EventSessionSpeakerConfiguration.cs` | No uniqueness yet on `(EventSessionId, ActorId)` |
| `Explore.Persistence/Configurations/Entities/EventCategoriesConfiguration.cs` | No uniqueness yet on `(EventId, CategoryId)` |
| `Explore.Persistence/Configurations/Entities/EventTagsConfiguration.cs` | No uniqueness yet on `(EventId, TagId)` |
| `schemas/islamu-event.md` | Current DBML/schema reference; confirms missing target tables |

### Application

| File | Verified relevance |
|---|---|
| `Explore.Application/DTOs/Event/CreateEventDto.cs` | Missing `EventSeriesId` despite domain support |
| `Explore.Blazor.Client/Clients/DtoPartials.cs` | Client-side patch for missing generated scheduling/series fields |
| `Explore.Application/DTOs/Event/EventDto.cs` | Event read DTO still lacks new scheduling concepts |
| `Explore.Application/DTOs/EventSession/CreateEventSessionDto.cs` | Current session contract |
| `Explore.Application/DTOs/EventRegistration/CreateEventRegistrationDto.cs` | Current session registration create payload |
| `Explore.Application/DTOs/EventSession/Validators/CreateEventSessionDtoValidator.cs` | Current timing/location validation |
| `Explore.Application/DTOs/EventRegistration/Validators/CreateEventRegistrationDtoValidator.cs` | Current duplicate session registration validation |
| `Explore.Application/Profiles/MappingProfile.cs` | Single mapping profile; already maps current event/session/registration and some series list data |
| `Explore.Application/Features/Events/Handlers/Commands/CreateEventWithSessionsCommandHandler.cs` | Creates events + sessions; computes date summaries from UTC session times |
| `Explore.Application/Features/EventSessions/Handlers/Commands/CreateEventSessionCommandHandler.cs` | Creates sessions; inherits tenant from parent event |
| `Explore.Application/Features/EventRegistrations/Handlers/Commands/CreateEventRegistrationCommandHandler.cs` | Creates session-scoped registration rows |
| `Explore.Application/Features/EventSessions/Handlers/Queries/GetSessionsByEventRequestHandler.cs` | Current session-by-event query surface |
| `Explore.Application/Features/EventRegistrations/Handlers/Queries/GetRegistrationsByUserRequestHandler.cs` | Current user registration query surface |

### API

| File | Verified relevance |
|---|---|
| `Explore.API/Controllers/EventController.cs` | Event CRUD + create-with-sessions |
| `Explore.API/Controllers/EventSessionController.cs` | Session CRUD + sessions by event |
| `Explore.API/Controllers/EventRegistrationController.cs` | Session-level registration CRUD |
| `Explore.API/Controllers/EventSeriesController.cs` | Existing EventSeries CRUD |

### Blazor UI

| File | Verified relevance |
|---|---|
| `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` | Event detail logic; loads sessions and only primary-session agenda items |
| `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` | Existing create flow; currently imports nested `SessionEditorModel` from `EventSessionEditor` |
| `Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs` | Existing edit flow with session preloading |
| `Explore.Blazor.Client/Services/EventService.cs` | Primary Blazor event orchestration service; registration/session APIs are currently session-centric |
| `Explore.Blazor.Client/Pages/Events/Components/EventRegistration.razor` | Session-only registration dialog |
| `Explore.Blazor.Client/Pages/Events/Components/EventSessionManager.razor` | Session list with expandable session agenda items |
| `Explore.Blazor.Client/Pages/Events/Dialogs/SessionSelectionDialog.razor` | Existing “register all sessions” or multi-select dialog |
| `Explore.Blazor.Client/Pages/Events/Components/EventSeriesSection.razor` | Existing active-work component for series UI |
| `Explore.Blazor.Client/Pages/Events/Components/SessionSummaryCard.razor` | Existing active-work component for compact session cards |
| `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor` | Existing active-work component for session editing surfaces |
| `Explore.Blazor.Client.Tests/Services/EventRegistrationServiceTests.cs` | Existing client-service coverage centered on session registrations |
| `Explore.Blazor.Client.Tests/Pages/Event/CreateEventTests.cs` | Existing event-editor tests that will be impacted by event-day/room/policy additions |
| `Explore.Blazor.Client/Components/Collection/EventTimeline.razor` | Simple grouped event list timeline, not real agenda rendering |
| `dev/active/session-series-ux/session-series-ux-plan.md` | Existing active plan touching EventSeries and session UX |
| `dev/active/session-series-ux/session-series-ux-context.md` | Current in-progress UI extraction state |

## Confirmed Gaps

- No `EventDay` entity/table/query/UI.
- No `EventAgendaItem` entity/table/query/UI.
- No `LocationRoom` entity/table/query/UI.
- No parent-child registration intent model.
- No registration scope or event registration policy model.
- No cached local-day/local-time projection fields on sessions or agenda items.
- No session taxonomy junction tables.
- No unique constraints yet for event tags/categories and session speakers.
- No room-aware agenda UI; only simple timeline/session cards exist.
- No clean official API schema for some already-used scheduling fields in the Blazor client; partial DTO shims exist.
- No room/day-aware client service abstractions yet; the current `EventService` surface is event/session/registration oriented.

## Important Existing Behaviors to Preserve or Deliberately Replace

- Repositories return entities; mapping happens in handlers.
- Validators are manually instantiated.
- Tenant isolation is enforced by named query filters in `ExploreDbContext`.
- GET endpoints are generally anonymous; writes are authorized.
- Event creation currently supports `POST /api/event/with-sessions`; this may remain as a compatibility path while internals change.
- Blazor currently uses `InteractiveAuto`; timezone/browser-local display logic must remain prerender-safe.
- The current multi-session registration UX must be treated as a UI precursor to the new scope model, not as proof the domain already supports whole-event registration.
- Refactoring registration contracts will ripple into event pages and user profile/registration pages, not just event detail.

## External Guidance Already Captured

- Official .NET guidance confirms:
  - use `TimeZoneInfo.ConvertTime(DateTimeOffset, TimeZoneInfo)` for timezone-aware conversion,
  - guard ambiguous/invalid local times around DST,
  - use named query filters in EF Core 10,
  - use composite/filtered indexes to match schedule query patterns,
  - defer JS timezone detection to `OnAfterRenderAsync` in Blazor.
- Planning review guidance confirms:
  - keep new FKs nullable in the first rollout,
  - isolate the registration semantic change into its own phase,
  - avoid renaming `EventSessionAgendaItem` in the first rollout,
  - regenerate OpenAPI/NSwag only at a phase boundary after API contracts stabilize,
  - use atomic commit slices and TDD-oriented sequencing.
- Additional docs/research captured:
  - FluentValidation officially supports `MustAsync`/`CustomAsync` and custom `AddFailure` flows for cross-property and repository-backed validation.
  - Registration modeling feedback now explicitly treats parent rows as intent/policy semantics and child rows as concrete session entitlements.

## Quick Resume

1. Read `event-scheduling-refactor-plan.md`.
2. Start from the registration section and locked-planning-decisions section; they were the main revisions in this session.
3. Break the finalized plan into implementation-ready issue/PR chunks.
4. Start with ADR + architecture test guardrails or additive schema entities only.
5. Keep registration semantics isolated from the first schema slice.
6. Keep same-room overlap enforcement in the first rollout at validator level, not as a surprise late hardening task.
