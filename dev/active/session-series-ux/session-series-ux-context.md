# Context: Session & Series UX Refactor

> **Last Updated: 2026-03-16 Europe/Brussels**

## SESSION PROGRESS (2026-03-16 Europe/Brussels)

### ✅ COMPLETED THIS SESSION
- No code changes were made in this track during the current planning session.
- Verified this track is an active dependency for `event-scheduling-refactor`; the scheduling plan must extend the existing series/session UX work instead of duplicating it.
- Confirmed the current extracted components/workflows already exist and are referenced by the scheduling plan:
  - `Explore.Blazor.Client/Pages/Events/Components/EventSeriesSection.razor`
  - `Explore.Blazor.Client/Pages/Events/Components/SessionSummaryCard.razor`
  - `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`
  - `Explore.Blazor.Client/Pages/Events/Workflows/SessionEditorWorkflow.cs`
  - `Explore.Blazor.Client/Pages/Events/Workflows/TimezoneWorkflow.cs`
- Preserved the existing drawer-based session editor UX while extracting the duplicated drawer state machine into `Explore.Blazor.Client/Pages/Events/Workflows/SessionEditorWorkflow.cs`.
- Wired both `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` and `Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs` to the shared `SessionEditorWorkflow` instead of duplicating open/close/save/duplicate/navigation logic.
- Added focused workflow coverage in `Explore.Blazor.Client.Tests/Pages/Events/Workflows/SessionEditorWorkflowTests.cs`.
- Extracted shared timezone logic into `Explore.Blazor.Client/Pages/Events/Workflows/TimezoneWorkflow.cs`.
- Wired both event pages to the shared timezone workflow and added `Explore.Blazor.Client.Tests/Pages/Events/Workflows/TimezoneWorkflowTests.cs`.
- Verified the refactors with `dotnet build "Explore.Blazor.Client/Explore.Blazor.Client.csproj" --configuration Release --verbosity minimal` and `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity minimal`.

### 🟡 IN PROGRESS
- The stale handoff note referencing duplicated page-level `UploadSessionImageIfNeeded` methods no longer matched the working tree. The active session-image upload logic currently lives in `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`.
- Shared session image upload logic has now been extracted into `Explore.Blazor.Client/Pages/Events/Workflows/SessionImageUploadWorkflow.cs` and the panel calls that workflow instead of keeping validation/read/preview/upload logic inline.
- Secondary continuation note: if `event-scheduling-refactor` implementation begins, preserve these extracted UX building blocks and do not re-invent the same session/series components.

### ⚠️ BLOCKERS / RISKS
- `CreateEvent` and `EventEdit` already contained substantial in-progress user changes before this session. Further refactors must continue in place and avoid clobbering unrelated uncommitted work.
- Razor LSP is unavailable in this environment, so `.razor` validation continues to rely on `dotnet build` + test runs rather than Razor diagnostics.
- The scheduling-refactor plan currently contains additional UI/UX work that must be layered onto this track carefully; avoid replacing these abstractions with parallel implementations.

### KEY DECISIONS THIS SESSION
- Do not rewrite the event pages into a new architecture in one jump; extract the shared state machines first while preserving the current UI contract.
- Keep the extraction sequence incremental: session drawer workflow first, timezone workflow second, then image-upload/description/lookup orchestration.
- Use full-word naming (`timezone`, not `tz`) for new workflow APIs and tests.
- Treat `session-series-ux` as the source track for existing session/series editor abstractions during future scheduling implementation.

### FILES MODIFIED THIS SESSION
- `dev/active/session-series-ux/session-series-ux-context.md` — updated cross-track dependency and continuation notes.
- `dev/active/session-series-ux/session-series-ux-tasks.md` — refreshed checkpoint and next-step framing for reset-safe handoff.
- `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor` — now delegates session image mutation to a shared workflow.
- `Explore.Blazor.Client/Pages/Events/Workflows/SessionImageUploadWorkflow.cs` — new shared session image upload workflow.
- `Explore.Blazor.Client.Tests/Pages/Events/Workflows/SessionImageUploadWorkflowTests.cs` — focused unit coverage for session image upload behavior.
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` — replaced duplicated session drawer and timezone logic with shared workflows.
- `Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs` — same shared workflow integration as create page.
- `Explore.Blazor.Client/Pages/Events/Workflows/SessionEditorWorkflow.cs` — new shared session drawer workflow.
- `Explore.Blazor.Client/Pages/Events/Workflows/TimezoneWorkflow.cs` — new shared timezone workflow.
- `Explore.Blazor.Client.Tests/Pages/Events/Workflows/SessionEditorWorkflowTests.cs` — new tests for session workflow.
- `Explore.Blazor.Client.Tests/Pages/Events/Workflows/TimezoneWorkflowTests.cs` — new tests for timezone workflow.

### EXACT HANDOFF STATE
- The next file to inspect for continuation is `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor` together with `Explore.Blazor.Client/Pages/Events/Workflows/SessionImageUploadWorkflow.cs`.
- Goal of the completed change: centralize session image validation/read/preview/upload mutation into one workflow so future event-page integration can reuse it.
- If resuming from the scheduling track first, treat this track as a dependency to reuse, not restart.
- Commands to rerun after restart:
  - `dotnet build "Explore.Blazor.Client/Explore.Blazor.Client.csproj" --configuration Release --verbosity minimal`
  - `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity minimal`

### QUICK RESUME
1. Re-read this file and `session-series-ux-tasks.md`.
2. Open `Explore.Blazor.Client/Pages/Events/Workflows/SessionImageUploadWorkflow.cs` and `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`.
3. Reuse that workflow for any future session-image entry points instead of duplicating upload mutation logic.
4. Rerun the client build/tests before resuming broader page integration.

## Key Files

### Files to Modify

| File | Purpose | Change Type |
|---|---|---|
| `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` | Main create event page (626 lines) | Major refactor — session section |
| `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` | Code-behind (~700 lines) | Major refactor — session dialog logic |
| `Explore.Blazor.Client/Pages/Events/EventEdit.razor` | Edit event page | Major refactor — same session pattern |
| `Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs` | Code-behind | Major refactor — same session pattern |
| `Explore.Blazor.Client/Pages/Events/Components/EventSessionEditor.razor` | Inline session editor (321 lines) | Extract `SessionEditorModel` to shared class |
| `Explore.Application/DTOs/Event/CreateEventDto.cs` | Create event DTO | Add `EventSeriesId`, `SeriesOrder` |
| `Explore.Application/DTOs/Event/UpdateEventDto.cs` | Update event DTO | Add `EventSeriesId`, `SeriesOrder` |
| `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs` | Create handler | Map series fields |
| `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs` | Update handler | Map series fields |
| `Explore.Blazor.Client/Clients/EventApiClient.g.cs` | NSwag generated client | Regenerate |
| `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs` | Shared DI registration | Register `IEventSeriesService` |

### Files to Create

| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Pages/Events/Models/SessionEditorModel.cs` | Extracted session model (from EventSessionEditor inner class) |
| `Explore.Blazor.Client/Pages/Events/Components/SessionSummaryCard.razor` | Compact session card component |
| `Explore.Blazor.Client/Pages/Events/Components/SessionSummaryCard.razor.css` | Scoped styles (BEM) |
| `Explore.Blazor.Client/Pages/Events/Dialogs/SessionEditorDialog.razor` | Unified create/edit session dialog |
| `Explore.Blazor.Client/Pages/Events/Components/EventSeriesSection.razor` | Series selection component |
| `Explore.Blazor.Client/Pages/Events/Components/EventSeriesSection.razor.css` | Scoped styles (BEM) |
| `Explore.Blazor.Client/Services/IEventSeriesService.cs` | Service interface for EventSeries |
| `Explore.Blazor.Client/Services/EventSeriesService.cs` | Service implementation |
| `Explore.Blazor.Client/Pages/Events/Workflows/SessionEditorWorkflow.cs` | Shared drawer/session navigation state machine extracted in 2026-03-13 session |
| `Explore.Blazor.Client/Pages/Events/Workflows/TimezoneWorkflow.cs` | Shared timezone search/selection/formatting workflow extracted in 2026-03-13 session |
| `Explore.Blazor.Client.Tests/Pages/Events/Workflows/SessionEditorWorkflowTests.cs` | Coverage for shared session drawer workflow |
| `Explore.Blazor.Client.Tests/Pages/Events/Workflows/TimezoneWorkflowTests.cs` | Coverage for shared timezone workflow |

### Reference Files (Read-Only)

| File | Relevance |
|---|---|
| `Explore.Domain/Event.cs` | Domain entity — has `EventSeriesId` (nullable FK) and `SeriesOrder` |
| `Explore.Domain/EventSession.cs` | Domain entity — NO image field, NO order field |
| `Explore.Domain/EventSeries.cs` | Domain entity — Title, Slug, Description, FeaturedImageId, ActorId |
| `Explore.Application/DTOs/Event/EventSeriesListDto.cs` | Read DTO — includes EventCount |
| `Explore.Application/DTOs/Event/CreateEventSeriesDto.cs` | Write DTO for creating series |
| `Explore.API/Controllers/EventSeriesController.cs` | Full CRUD API endpoints (✅ exists) |
| `Explore.Blazor.Client/Pages/Events/Dialogs/CreateSessionDialog.razor` | Existing session dialog pattern (to replace) |
| `Explore.Blazor.Client/Pages/Events/Dialogs/EditSessionDialog.razor` | Existing session dialog pattern (to replace) |
| `Explore.Blazor.Client/Pages/Events/Components/EventSessionManager.razor` | Read-only session display (agenda) |
| `Explore.Blazor/Extensions/BffEndpointExtensions.cs` | BFF proxy pattern reference |

---

## Key Decisions

### Decision 1: MudDialog (not MudDrawer) for Session Editor
- **Rationale**: MudBlazor's `MudDrawer` is a navigation component (app sidebar), not a content side-panel. `MudDialog` with `MaxWidth.Medium`, `FullWidth`, and `FullScreen` on mobile is the proven pattern in this codebase.
- **Evidence**: CreateSessionDialog, EditSessionDialog, DescriptionEditorDialog all use MudDialog successfully.

### Decision 2: Hybrid Session Layout (Inline First + Dialog Rest)
- **Rationale**: The Luma-inspired first session inline design is intentional and provides a clean single-session UX (the majority case). Only sessions 2+ cause the stacking problem.
- **Alternative considered**: All sessions as summary cards — rejected to preserve existing design investment.
- **Migration path**: First session can later be moved to card + dialog if desired.

### Decision 3: EventSeriesId Added to Both Create and Update DTOs
- **Rationale**: Series is a structural choice. Users should assign during creation. Backend domain entity already supports `EventSeriesId` FK.
- **Impact**: Requires handler updates, NSwag regen, BFF proxy.

### Decision 4: Session Images Deferred
- **Rationale**: `EventSession` entity has no `FeaturedImageId` field. Adding requires domain change, EF migration, storage upload flow, and inheritance logic (use event image / override). Too large for this refactor scope.
- **Future**: Can be added as a separate epic when needed.

### Decision 5: No Session Order Field Added
- **Rationale**: Sessions are displayed by `StartTime` order. The UX advice mentions "drag reorder if order matters" — for this platform, chronological order is natural and doesn't need explicit ordering.

---

## Key Interface Signatures

### SessionEditorModel (to extract)
```csharp
namespace Explore.Blazor.Client.Pages.Events.Models;

public class SessionEditorModel
{
    public Guid? Id { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public DateTime StartTime { get; set; } = DateTime.Today.AddHours(9);
    public DateTime EndTime { get; set; } = DateTime.Today.AddHours(17);
    public Guid? LocationId { get; set; }
    public int? MaxAudienceAttendees { get; set; }
    public int? RegistrationModeId { get; set; } = 1;
    public IReadOnlyCollection<int> LanguageIds { get; set; } = new HashSet<int>();

    public CreateEventSessionDto ToCreateDto(Guid eventId, Guid tenantId) { ... }
    public UpdateEventSessionDto ToUpdateDto(Guid eventId) { ... }
    public static SessionEditorModel FromDto(EventSessionListDto dto) { ... }
    public SessionEditorModel Clone() { ... } // NEW: for duplication
}
```

### IEventSeriesService
```csharp
namespace Explore.Blazor.Client.Services;

public interface IEventSeriesService
{
    Task<PaginatedResult<EventSeriesListDto>?> GetSeriesListAsync(
        int pageNumber = 1, int pageSize = 10, Guid? actorId = null);
    Task<EventSeriesDto?> GetSeriesDetailAsync(Guid id);
    Task<BaseCommandResponse<Guid>?> CreateSeriesAsync(CreateEventSeriesDto dto);
    Task<IEnumerable<EventSeriesListDto>> SearchSeriesAsync(
        string query, int maxResults = 10);
}
```

### SessionEditorDialog Parameters
```csharp
[CascadingParameter] IMudDialogInstance MudDialog { get; set; }
[Parameter] public SessionEditorModel? Session { get; set; } // null = create mode
[Parameter] public IReadOnlyCollection<LocationListDto> Locations { get; set; }
[Parameter] public IReadOnlyCollection<RegistrationModeDto> RegistrationModes { get; set; }
[Parameter] public IReadOnlyCollection<LanguageDto> Languages { get; set; }
```

### SessionSummaryCard Parameters
```csharp
[Parameter] public SessionEditorModel Session { get; set; }
[Parameter] public int Index { get; set; }
[Parameter] public string? LocationName { get; set; }
[Parameter] public string? RegistrationModeName { get; set; }
[Parameter] public EventCallback OnEdit { get; set; }
[Parameter] public EventCallback OnDuplicate { get; set; }
[Parameter] public EventCallback OnDelete { get; set; }
```

### EventSeriesSection Parameters
```csharp
[Parameter] public Guid? EventSeriesId { get; set; }
[Parameter] public EventCallback<Guid?> EventSeriesIdChanged { get; set; }
[Parameter] public int? SeriesOrder { get; set; }
[Parameter] public EventCallback<int?> SeriesOrderChanged { get; set; }
```

---

## Dependencies

### Internal Dependencies
```
Phase 1:
  Task 1.3 (Extract SessionEditorModel) → Task 1.1, 1.2, 1.4, 1.5, 1.6
  Task 1.1 (SessionSummaryCard) → Task 1.4, 1.5
  Task 1.2 (SessionEditorDialog) → Task 1.4, 1.5
  Task 1.4 (CreateEvent refactor) → Task 1.5 (EventEdit refactor)

Phase 2:
  Task 2.1 (DTOs) → Task 2.2, 2.3, 2.4
  Task 2.4 (NSwag) → Task 2.5, 2.7, 2.8
  Task 2.5 (Service) + Task 2.6 (BFF) → Task 2.7
  Task 2.7 (EventSeriesSection) → Task 2.8

Phase 3: Depends on Phase 1 completion
Phase 4: Depends on Phase 1 + Phase 2 completion
```

### External Dependencies
- MudBlazor v9.0.0 (already installed)
- NSwag tooling for client regeneration
- PostgreSQL (no schema changes needed — EventSeriesId FK already exists on Event entity)

---

## Out of Scope (Explicit)

1. **Session images** — EventSession has no image field; requires domain + migration change
2. **Session ordering field** — sessions display by StartTime; explicit ordering not needed
3. **Drag-and-drop reorder** — not needed when order is chronological
4. **Series management page** — separate epic; this plan only covers series assignment from event pages
5. **Prayer-relative session times** — `SessionStartTimeType` enum exists but UI integration is a separate feature
6. **Redesign of first session inline layout** — preserved as-is; only sessions 2+ change

## Session Handoff — 2026-05-03 Europe/Brussels

No implementation work was performed for this active task during the sidebar dock refactor handoff session. Existing context, plan, and task files remain the authoritative state for this workstream. Do not infer progress or blockers here from the sidebar/dock-specific changes unless a future session explicitly broadens scope.
