# Tasks: Session & Series UX Refactor

> **Last Updated: 2026-03-16 Europe/Brussels**

## Session Checkpoint (2026-03-13 Europe/Brussels)

- ✅ Shared `SessionEditorWorkflow` extracted and integrated into both `CreateEvent` and `EventEdit`.
- ✅ Shared `TimezoneWorkflow` extracted and integrated into both pages.
- ✅ New unit coverage added for both shared workflows.
- 🟡 Next in-progress extraction: shared session image upload workflow (`UploadSessionImageIfNeeded` duplication in both pages).
- ⚠️ Continue carefully on top of existing uncommitted page changes; do not overwrite unrelated in-progress UX work.

## Session Checkpoint (2026-03-16 Europe/Brussels)

- ✅ No new code changes in this track during the planning session; previous implementation state remains valid.
- ✅ This track is now explicitly referenced by `event-scheduling-refactor` as a dependency/reuse source for session/series UI abstractions.
- 🟡 Direct next implementation task remains **1.9 Extract shared session image-upload workflow**.
- ⚠️ If scheduling implementation starts first, do not duplicate `EventSeriesSection`, `SessionSummaryCard`, `SessionEditorPanel`, or the workflow abstractions already created here.

## Phase 1: Session Summary Cards & Drawer Editor ✅

- [x] **1.3** Extract `SessionEditorModel` to `Models/SessionEditorModel.cs` — S
  - Standalone file with Clone(), ToCreateDto(), ToUpdateDto(), FromDto()
  - Image fields: FeaturedImageId, FeaturedImagePreviewUrl, UseEventImage, PendingImageBytes, PendingImageFileName

- [x] **1.1** Create `SessionSummaryCard.razor` component — M
  - Horizontal card: image thumbnail left, info middle, actions right
  - Edit, Duplicate, Delete buttons; responsive breakpoints

- [x] **1.2** Create `SessionEditorPanel.razor` component (Drawer, not Dialog) — L
  - Grouped sections: Basic, Location, Registration, Languages, Image
  - Image upload with "Use event image" / "Custom image" toggle
  - EventImageUrl parameter for inheritance display

- [x] **1.4** Refactor `CreateEvent.razor` sessions section — L
  - Dynamic single/multi-session UI transition (inline ↔ card list)
  - Drawer sidebar editor (not dialog) with prev/next/add nav buttons
  - Session image upload on submit via UploadSessionImageIfNeeded

- [x] **1.5** Refactor `EventEdit.razor` sessions section — M
  - Same drawer + dynamic UI pattern as CreateEvent

- [x] **1.6** Add session duplication logic — S
  - Clone() resets image to UseEventImage=true, shifts dates +1 day

- [x] **1.7** Extract `SessionEditorWorkflow` shared state machine — M
  - New file: `Pages/Events/Workflows/SessionEditorWorkflow.cs`
  - Both `CreateEvent` and `EventEdit` use the same drawer/session navigation/save/duplicate logic

- [x] **1.8** Extract `TimezoneWorkflow` shared state machine — S
  - New file: `Pages/Events/Workflows/TimezoneWorkflow.cs`
  - Both `CreateEvent` and `EventEdit` use the same timezone search/selection/formatting logic

- [x] **1.9** Extract shared session image-upload workflow — S
  - Actual upload logic currently lives in `Explore.Blazor.Client/Pages/Events/Components/SessionEditorPanel.razor`, so the shared extraction was applied there instead of the stale page-file targets captured in the handoff note.
  - New shared workflow: `Explore.Blazor.Client/Pages/Events/Workflows/SessionImageUploadWorkflow.cs`
  - Acceptance: one shared helper now owns validation/read/preview/upload mutation for `SessionEditorModel`, and focused tests cover success/failure/reset behavior

## Phase 1b: Per-Session Images (Backend) ✅

- [x] **1b.1** Add FeaturedImageId to EventSession domain entity — S
- [x] **1b.2** Update EventSession EF configuration (FK to StorageObject) — S
- [x] **1b.3** Add FeaturedImageId/FeaturedImageUri to all 4 session DTOs — S
- [x] **1b.4** Update EventSessionRepository to Include(FeaturedImage) — S
- [x] **1b.5** Add NSwag partial classes for session DTOs — S
- [x] **1b.6** Update MappingProfile for FeaturedImageUri — S
- [ ] **1b.7** Create EF migration: AddEventSessionFeaturedImage — S ⚠️ (requires DB)

## Phase 1c: Responsive CSS ✅

- [x] **1c.1** SessionSummaryCard.razor.css with responsive breakpoints — S
- [x] **1c.2** SessionEditorPanel.razor.css with image upload styling — S
- [x] **1c.3** StyleGlobal.css: session-drawer__header layout — S

## Phase 2: Event Series UI Integration

- [x] **2.1** Add `EventSeriesId` + `SeriesOrder` to Create/Update DTOs — S
- [x] **2.2** Update command handlers for series mapping — S
- [x] **2.3** Add validator rules for series fields — S
- [ ] **2.4** Regenerate NSwag client — S
- [ ] **2.5** Create `IEventSeriesService` + `EventSeriesService` — M
- [ ] **2.6** Add BFF proxy endpoints for EventSeries — M
- [x] **2.7** Create `EventSeriesSection.razor` component — L
- [x] **2.8** Integrate series section into Create/Edit Event pages — M

## Phase 3: Polish & Mobile UX

- [x] **3.1** Mobile-responsive drawer behavior — S
- [ ] **3.2** Session validation display on summary cards — S
- [x] **3.3** Default inheritance for new sessions — S
- [x] **3.4** CSS styling with BEM methodology — S

## Phase 4: Testing

- [x] **4.1** Unit tests for SessionEditorModel (incl. image fields) — S
- [x] **4.2** Unit tests for Series DTO validators — S
- [ ] **4.3** Integration tests for series assignment — M
- [ ] **4.4** Blazor component tests — M (blocked by pre-existing build errors in test project)
- [x] **4.5** Unit tests for `SessionEditorWorkflow` — S
- [x] **4.6** Unit tests for `TimezoneWorkflow` — S

## Quick Resume

1. Open `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs:725`.
2. Open `Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs:477`.
3. Continue from the shared workflow baseline in `Explore.Blazor.Client/Pages/Events/Workflows/SessionImageUploadWorkflow.cs` if more session-image work is needed.
4. Add tests.
5. Run:
   - `dotnet build "Explore.Blazor.Client/Explore.Blazor.Client.csproj" --configuration Release --verbosity minimal`
   - `dotnet test --project "Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj" --configuration Release --verbosity minimal`

## Session Handoff — 2026-05-03 Europe/Brussels

- [x] No task-state changes were made for this workstream during the sidebar dock refactor handoff session.
- [ ] Reconfirm this workstream's current state from its existing context/plan before resuming implementation.
