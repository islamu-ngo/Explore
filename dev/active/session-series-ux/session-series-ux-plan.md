# Plan: Session & Series UX Refactor

> **Last Updated: 2025-07-26**

## Executive Summary

Restructure the Create/Edit Event pages to replace inline session-form stacking with a **compact session list + dialog editor** pattern, and integrate **Event Series** selection directly into the event form. This addresses the UX pain of pages growing unboundedly when multiple sessions are added, and exposes the fully-implemented (but UI-disconnected) EventSeries backend to users.

### Key Outcomes
1. Sessions displayed as compact summary cards on the event page
2. Session editing via MudDialog (medium width on desktop, fullscreen on mobile)
3. Session duplication support (Duplicate button on each card)
4. Event Series section on Create/Edit Event pages (standalone vs. part of series)
5. BFF proxy + Blazor client service for EventSeries API

---

## Current State Analysis

### Session Management — Current UX
- **CreateEvent.razor** (626 lines): Luma-inspired two-column layout (MudGrid 5/7 on sm+)
- **First session**: Fields embedded directly in the main event form (start/end datetime, location picker, registration mode, languages, capacity)
- **Sessions 2+**: Rendered as full `EventSessionEditor` components inside `MudPaper` cards, stacked vertically — each is ~320 lines of form fields
- **AddSession()**: Appends a new `SessionEditorModel` to an in-memory `List<SessionEditorModel>`
- **HandleSubmit()**: Creates event → updates first session (auto-created by backend) → creates sessions 2+ sequentially via API calls
- **EventEdit.razor**: Similar pattern — sessions loaded from API, rendered inline

### Session Components — Current State
| Component | Location | Purpose |
|---|---|---|
| `EventSessionEditor.razor` | `Components/` | Full inline session form (321 lines). Contains `SessionEditorModel` inner class |
| `EventSessionManager.razor` | `Components/` | Read-only agenda display on EventDetail page |
| `CreateSessionDialog.razor` | `Dialogs/` | Basic MudDialog for creating sessions (used on EventEdit, NOT CreateEvent) |
| `EditSessionDialog.razor` | `Dialogs/` | Basic MudDialog for editing sessions |

### SessionEditorModel Properties
```csharp
Guid? Id, string? Title, string? Description,
DateTime StartTime, DateTime EndTime,
Guid? LocationId, int? MaxAudienceAttendees,
int? RegistrationModeId (default 1),
IReadOnlyCollection<int> LanguageIds
```
Methods: `ToCreateDto(eventId, tenantId)`, `ToUpdateDto(eventId)`, `FromDto(EventSessionListDto)`

### EventSeries — Backend Status (✅ Fully Implemented)
| Layer | Status | Key Files |
|---|---|---|
| Domain | ✅ | `EventSeries.cs` (Title, Slug, Description, FeaturedImageId, ActorId, IsPublished, StartDateUtc/EndDateUtc) |
| Application Commands | ✅ | `CreateEventSeriesCommand`, `UpdateEventSeriesCommand`, `DeleteEventSeriesCommand` |
| Application Queries | ✅ | `GetEventSeriesDetailRequest`, `GetEventSeriesListRequest`, `GetTopEventSeriesRequest` |
| DTOs | ✅ | `CreateEventSeriesDto`, `UpdateEventSeriesDto`, `EventSeriesDto`, `EventSeriesListDto` |
| Repository | ✅ | `IEventSeriesRepository` implemented |
| API Controller | ✅ | `EventSeriesController` — full CRUD + list + top |

### EventSeries — Frontend Status (❌ Not Wired)
| Component | Status |
|---|---|
| BFF proxy endpoints | ❌ Missing — Explore.Blazor has no EventSeries forwarding |
| IEventSeriesService | ❌ Missing — no Blazor client service |
| EventSeries UI | ❌ Missing — no series section on Create/Edit Event pages |
| CreateEventDto.EventSeriesId | ❌ Missing — not in Application DTO |
| UpdateEventDto.EventSeriesId | ❌ Missing — not in Application DTO |
| Event.EventSeriesId in NSwag | Read-only in EventListDto only |

### EventSession Domain — Missing Fields
| Field | Status | Notes |
|---|---|---|
| FeaturedImageId / SessionImageId | ❌ Not in domain | Session images deferred to future phase |
| SessionOrder / DisplayOrder | ❌ Not in domain | Sessions ordered implicitly by StartTime |

---

## Proposed Future State

### Architecture Decision: Dialog vs. Drawer
**Decision: MudDialog** (not MudDrawer)
- MudDrawer in MudBlazor is a navigation component, not a content panel
- MudDialog supports `MaxWidth`, `FullWidth`, `FullScreen`, `CloseButton` — all needed
- Existing codebase already uses MudDialog extensively for session dialogs
- For mobile: `FullScreen = true` via breakpoint detection
- Consistent with existing patterns (CreateSessionDialog, EditSessionDialog)

### Architecture Decision: First Session Handling
**Decision: Hybrid approach (Option B)**
- First session stays inline on the event form (preserves the Luma-inspired design)
- Sessions 2+ are shown as compact summary cards
- Add/Edit sessions 2+ via MudDialog
- First session can optionally be edited in dialog too (expand button)
- This is pragmatic: avoids a full redesign of the Luma layout while fixing the stacking problem

### Architecture Decision: Series Assignment Flow
**Decision: Add EventSeriesId to both Create and Update DTOs**
- Series is a structural choice, not a post-hoc association
- Users should be able to assign during creation, not just after
- Requires DTO changes + handler updates + NSwag regeneration

---

## Implementation Phases

### Phase 1: Session Summary Cards & Dialog Editor (Blazor UI)
**Goal**: Replace inline session stacking with compact cards + dialog editing

#### Task 1.1: Create `SessionSummaryCard.razor` Component
- **File**: `Explore.Blazor.Client/Pages/Events/Components/SessionSummaryCard.razor`
- **Effort**: M
- **Description**: Compact card showing session summary with action buttons
- **Acceptance Criteria**:
  - [ ] Displays: Title (or "Session N"), date/time range, location name, registration mode
  - [ ] Truncated/one-line layout per card
  - [ ] Action buttons: Edit (pencil icon), Duplicate (copy icon), Delete (trash icon)
  - [ ] Visual indicator for validation errors (e.g., missing end time → red border/badge)
  - [ ] Optional session number badge
  - [ ] Responsive: stacks gracefully on mobile
- **Parameters**: `SessionEditorModel Session`, `int Index`, `string? LocationName`, `EventCallback OnEdit`, `EventCallback OnDuplicate`, `EventCallback OnDelete`
- **Related Skills**: `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 1.2: Create `SessionEditorDialog.razor` Component
- **File**: `Explore.Blazor.Client/Pages/Events/Dialogs/SessionEditorDialog.razor`
- **Effort**: L
- **Description**: Full-featured MudDialog for creating/editing a session, replacing inline forms. Replaces both CreateSessionDialog and EditSessionDialog with a unified component.
- **Acceptance Criteria**:
  - [ ] MudDialog with `MaxWidth.Medium`, `FullWidth=true`, `CloseButton=true`
  - [ ] Fullscreen on mobile (detect via `IBreakpointService` or `MudBreakpointProvider`)
  - [ ] Grouped sections with clear headings:
    - Basic Info: Title, Description, Start Date/Time, End Date/Time
    - Location: Venue picker (reuse existing location selector)
    - Registration: Registration mode, max capacity
    - Languages: Language multi-select
    - Advanced: Collapsible section for future fields
  - [ ] Progressive disclosure: Advanced section collapsed by default
  - [ ] Returns `DialogResult.Ok(SessionEditorModel)` on save
  - [ ] Accepts `SessionEditorModel?` parameter for edit mode (null = create)
  - [ ] Validates before closing (inline validation messages)
  - [ ] Title changes: "Create Session" vs "Edit Session" based on mode
- **Dependencies**: Reuse `SessionEditorModel` from `EventSessionEditor.razor`
- **Related Skills**: `blazor-ui-conventions`

#### Task 1.3: Extract `SessionEditorModel` to Shared Location
- **File**: `Explore.Blazor.Client/Pages/Events/Models/SessionEditorModel.cs`
- **Effort**: S
- **Description**: Move `SessionEditorModel` from `EventSessionEditor.razor` inner class to a standalone file. Both `SessionEditorDialog` and `CreateEvent` need access.
- **Acceptance Criteria**:
  - [ ] File-scoped namespace
  - [ ] All existing properties preserved (Id, Title, Description, StartTime, EndTime, LocationId, MaxAudienceAttendees, RegistrationModeId, LanguageIds)
  - [ ] `ToCreateDto()`, `ToUpdateDto()`, `FromDto()` methods preserved
  - [ ] Add `Clone()` method for duplication support
  - [ ] `EventSessionEditor.razor` updated to use the extracted class
  - [ ] No behavioral changes

#### Task 1.4: Refactor `CreateEvent.razor` Sessions Section
- **File**: `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` + `.razor.cs`
- **Effort**: L
- **Description**: Replace the `@for` loop of `EventSessionEditor` components with `SessionSummaryCard` list + dialog invocation
- **Acceptance Criteria**:
  - [ ] Sessions 2+ rendered as `SessionSummaryCard` components (not full `EventSessionEditor`)
  - [ ] "Add Session" button opens `SessionEditorDialog` via `IDialogService.ShowAsync<SessionEditorDialog>()`
  - [ ] Edit action on card opens dialog pre-filled with session data
  - [ ] Duplicate action clones session, opens dialog with cloned data (new title = original + " (Copy)")
  - [ ] Delete action shows confirmation dialog, removes from list
  - [ ] Session count shown: "N sessions" text above the list
  - [ ] First session remains inline (existing Luma-style fields)
  - [ ] `HandleSubmit()` logic unchanged (still creates event → updates first session → creates rest)
  - [ ] Pass `locations` and `registrationModes` to dialog as parameters

#### Task 1.5: Refactor `EventEdit.razor` Sessions Section
- **File**: `Explore.Blazor.Client/Pages/Events/EventEdit.razor` + `.razor.cs`
- **Effort**: M
- **Description**: Same refactoring as CreateEvent, but for the edit page. May already use dialogs partially.
- **Acceptance Criteria**:
  - [ ] Session list uses `SessionSummaryCard` components
  - [ ] Add/Edit/Duplicate/Delete use `SessionEditorDialog`
  - [ ] Existing sessions loaded from API render as summary cards
  - [ ] New sessions created via dialog are persisted immediately (or on save)

#### Task 1.6: Add Session Duplication Logic
- **File**: `CreateEvent.razor.cs`, `EventEdit.razor.cs`, `SessionEditorModel.cs`
- **Effort**: S
- **Description**: Implement session cloning via `Clone()` method
- **Acceptance Criteria**:
  - [ ] `SessionEditorModel.Clone()` creates deep copy with `Id = null`, `Title = original + " (Copy)"`
  - [ ] Duplicate button on `SessionSummaryCard` calls clone, opens dialog for editing
  - [ ] Start/End times shifted +1 day from original (sensible default for recurring sessions)
  - [ ] User can modify all fields before saving

---

### Phase 2: Event Series UI Integration

#### Task 2.1: Add `EventSeriesId` to Create/Update DTOs
- **Files**:
  - `Explore.Application/DTOs/Event/CreateEventDto.cs`
  - `Explore.Application/DTOs/Event/UpdateEventDto.cs`
- **Effort**: S
- **Description**: Add nullable `EventSeriesId` and `SeriesOrder` properties
- **Acceptance Criteria**:
  - [ ] `Guid? EventSeriesId` added to both DTOs
  - [ ] `int? SeriesOrder` added to both DTOs
  - [ ] No validation required (nullable FK)

#### Task 2.2: Update Command Handlers for Series Assignment
- **Files**:
  - `Explore.Application/Features/Events/Handlers/Commands/CreateEventCommandHandler.cs`
  - `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs`
- **Effort**: S
- **Description**: Map `EventSeriesId` and `SeriesOrder` from DTO to domain entity
- **Acceptance Criteria**:
  - [ ] CreateEventCommandHandler sets `event.EventSeriesId = dto.EventSeriesId`
  - [ ] CreateEventCommandHandler sets `event.SeriesOrder = dto.SeriesOrder`
  - [ ] UpdateEventCommandHandler sets the same
  - [ ] Null values = standalone event (no series)

#### Task 2.3: Add Validator Rules for Series Fields
- **File**: `Explore.Application/DTOs/Event/Validators/CreateEventDtoValidator.cs`
- **Effort**: S
- **Description**: Add FluentValidation rules for series fields
- **Acceptance Criteria**:
  - [ ] If `EventSeriesId` is provided, validate it's a valid GUID (not empty)
  - [ ] `SeriesOrder` must be >= 0 if provided
  - [ ] Both fields remain optional

#### Task 2.4: Regenerate NSwag Client
- **Effort**: S
- **Description**: Run NSwag generation to pick up new DTO properties
- **Acceptance Criteria**:
  - [ ] `EventApiClient.g.cs` updated with `EventSeriesId` and `SeriesOrder` on create/update DTOs
  - [ ] Existing API calls unaffected

#### Task 2.5: Create `IEventSeriesService` + `EventSeriesService`
- **Files**:
  - `Explore.Blazor.Client/Services/IEventSeriesService.cs`
  - `Explore.Blazor.Client/Services/EventSeriesService.cs`
- **Effort**: M
- **Description**: Blazor client service for EventSeries operations
- **Acceptance Criteria**:
  - [ ] `GetSeriesListAsync(int pageNumber, int pageSize)` → `PaginatedResult<EventSeriesListDto>`
  - [ ] `GetSeriesDetailAsync(Guid id)` → `EventSeriesDto`
  - [ ] `CreateSeriesAsync(CreateEventSeriesDto dto)` → `BaseCommandResponse<Guid>`
  - [ ] `SearchSeriesAsync(string query)` → filtered list for autocomplete
  - [ ] Registered in DI via `AddSharedApplicationServices()`
  - [ ] Uses `HttpClient` named "BackendApi" (or equivalent BFF pattern)

#### Task 2.6: Add BFF Proxy Endpoints for EventSeries
- **File**: `Explore.Blazor/Extensions/BffEndpointExtensions.cs` (or new file)
- **Effort**: M
- **Description**: Forward EventSeries API calls through the Blazor BFF server
- **Acceptance Criteria**:
  - [ ] `GET /api/bff/event-series` → proxies to backend `GET /api/EventSeries`
  - [ ] `GET /api/bff/event-series/{id}` → proxies to backend `GET /api/EventSeries/{id}`
  - [ ] `POST /api/bff/event-series` → proxies to backend `POST /api/EventSeries`
  - [ ] Token forwarding included (authenticated endpoints)
  - [ ] Uses YARP or manual HttpClient proxy (follow existing BFF patterns)

#### Task 2.7: Create `EventSeriesSection.razor` Component
- **File**: `Explore.Blazor.Client/Pages/Events/Components/EventSeriesSection.razor`
- **Effort**: L
- **Description**: Series selection section for Create/Edit Event pages
- **Acceptance Criteria**:
  - [ ] Radio group: "This is a standalone event" / "This event belongs to a series"
  - [ ] When "belongs to a series" selected:
    - Show `MudAutocomplete<EventSeriesListDto>` for searching existing series
    - Show "or Create new series" link/button
  - [ ] When "Create new series" clicked:
    - Inline mini-form (or MudDialog): Series Title (required), Description (optional)
    - Creates series via `IEventSeriesService.CreateSeriesAsync()`
    - Auto-selects the newly created series
  - [ ] Selected series shown with name + event count badge
  - [ ] "Remove from series" action to reset to standalone
  - [ ] Two-way binding: `Guid? EventSeriesId`, `int? SeriesOrder`
  - [ ] `EventCallback<Guid?> EventSeriesIdChanged`

#### Task 2.8: Integrate Series Section into Create/Edit Pages
- **Files**:
  - `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` + `.razor.cs`
  - `Explore.Blazor.Client/Pages/Events/EventEdit.razor` + `.razor.cs`
- **Effort**: M
- **Description**: Add `EventSeriesSection` to event form, wire up to DTOs
- **Acceptance Criteria**:
  - [ ] Section appears between "Sessions" and "Publish settings" (or contextually appropriate position)
  - [ ] `EventSeriesId` mapped to `createDto.EventSeriesId` / `updateDto.EventSeriesId` on submit
  - [ ] On edit page: pre-fill series selection from loaded event data
  - [ ] Series section loads available series on initialization

---

### Phase 3: Polish & Mobile UX

#### Task 3.1: Mobile-Responsive Dialog Behavior
- **File**: `SessionEditorDialog.razor`
- **Effort**: S
- **Description**: Detect breakpoint and switch to fullscreen dialog on mobile
- **Acceptance Criteria**:
  - [ ] Use `IBreakpointService` to detect `Breakpoint.Xs` or `Breakpoint.Sm`
  - [ ] Desktop: `MaxWidth.Medium`, `FullWidth=true`
  - [ ] Mobile (≤sm): `FullScreen=true`
  - [ ] Smooth transition — no layout jump

#### Task 3.2: Session Validation Display on Cards
- **File**: `SessionSummaryCard.razor`, `CreateEvent.razor.cs`
- **Effort**: S
- **Description**: Show validation status on session cards
- **Acceptance Criteria**:
  - [ ] Invalid sessions show red border or warning icon
  - [ ] Tooltip/text: "Missing end time", "Start time after end time", etc.
  - [ ] Validation runs on the `SessionEditorModel` before submit

#### Task 3.3: Default Inheritance UX
- **File**: `SessionEditorDialog.razor`
- **Effort**: S
- **Description**: Pre-fill new sessions with event-level defaults
- **Acceptance Criteria**:
  - [ ] New sessions inherit: LocationId from first session
  - [ ] New sessions inherit: RegistrationModeId from first session
  - [ ] New sessions inherit: LanguageIds from first session
  - [ ] Date/time: next day from last session (already implemented in AddSession)
  - [ ] User can override any inherited value

#### Task 3.4: CSS Styling with BEM Methodology
- **Files**: New `.razor.css` files for SessionSummaryCard, SessionEditorDialog, EventSeriesSection
- **Effort**: S
- **Description**: Scoped styles following BEM conventions
- **Acceptance Criteria**:
  - [ ] BEM class names: `session-card`, `session-card__title`, `session-card__actions`, etc.
  - [ ] `::deep` selectors for MudBlazor child components where needed
  - [ ] Consistent with existing component styles

---

### Phase 4: Testing

#### Task 4.1: Unit Tests for SessionEditorModel
- **File**: `Event.Application.UnitTests/` or `Explore.Blazor.Client.Tests/`
- **Effort**: S
- **Acceptance Criteria**:
  - [ ] Test `Clone()` method: new Id is null, Title appended " (Copy)", dates shifted
  - [ ] Test `ToCreateDto()` and `ToUpdateDto()` conversion
  - [ ] Test `FromDto()` factory method

#### Task 4.2: Unit Tests for Series DTOs/Validators
- **File**: `Event.Application.UnitTests/`
- **Effort**: S
- **Acceptance Criteria**:
  - [ ] Test CreateEventDto validation with EventSeriesId
  - [ ] Test UpdateEventDto validation with EventSeriesId
  - [ ] Test empty Guid rejected, null accepted

#### Task 4.3: Integration Tests for Series Assignment
- **File**: `Event.API.IntegrationTests/`
- **Effort**: M
- **Acceptance Criteria**:
  - [ ] Create event with EventSeriesId → event associated with series
  - [ ] Update event to add/remove series → persisted correctly
  - [ ] Create event without EventSeriesId → standalone event

#### Task 4.4: Blazor Component Tests
- **File**: `Explore.Blazor.Client.Tests/`
- **Effort**: M
- **Acceptance Criteria**:
  - [ ] SessionSummaryCard renders session data correctly
  - [ ] SessionSummaryCard fires Edit/Duplicate/Delete callbacks
  - [ ] EventSeriesSection toggles between standalone and series modes

---

## Risk Assessment & Mitigation

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| MudDialog fullscreen detection unreliable on tablet breakpoints | Medium | Low | Test on multiple viewport sizes; fallback to always MaxWidth.Medium |
| NSwag client regeneration introduces unrelated changes | Medium | Medium | Diff carefully, commit NSwag separately |
| BFF proxy complexity for EventSeries (token forwarding, error handling) | Medium | Medium | Follow exact pattern of existing BFF endpoints |
| First session inline + sessions 2+ in dialog feels inconsistent | Low | Medium | Add "expand to dialog" button on first session for consistency |
| Session duplication with shifted dates may confuse users | Low | Low | Clear UI: "(Copy)" suffix, dialog opens for review before saving |

---

## Success Metrics

1. **Page length**: Create Event page no longer grows unboundedly with sessions — capped at summary card list height
2. **Session management speed**: Adding/editing sessions 2+ takes ≤3 clicks (Add → fill → Save)
3. **Duplication**: Duplicate + date change takes ≤30 seconds
4. **Series assignment**: Can assign event to series during creation (zero post-hoc steps)
5. **Mobile**: Session editing works on 375px viewport without horizontal scroll

---

## Potential Risks & Unknowns

The **most likely complexity** is in **Phase 2, Task 2.6 (BFF proxy endpoints)**. The Blazor BFF layer has a specific pattern for proxying API calls with token forwarding, and getting this wrong can cause silent auth failures (as documented in the HTTP error handling memory — `PostAsJsonAsync` doesn't check status codes). The EventSeries controller requires `[Authorize]` for write operations, so the BFF must forward the access token correctly.

A secondary risk is the **NSwag regeneration** (Task 2.4) — the generated client file is ~28,000 lines and regeneration often pulls in unrelated DTO changes from other endpoints, creating a noisy diff. This should be committed separately.

The **session image feature** (mentioned in the external UX advice) is intentionally **deferred** — `EventSession` has no image FK in the domain model. Adding it requires a migration, new upload flow, and inheritance logic. It's a separate epic, not part of this refactor.
