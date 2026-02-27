# Blazor-API Synchronization - Task Checklist

> **Detailed task tracking for Blazor frontend synchronization**
>
> Created: 2026-01-31

---

## Session Update (2026-02-27 Europe/Brussels - Contracts Layout)

- [x] Created root `Explore.Blazor.Client/Contracts/` hierarchy (`Services/Lookup`, `Services/Events`, `Services/Organizations`, `Providers`, `Interop`).
- [x] Moved existing contract interfaces out of `Services/Contracts` and `Services/Lookup/Contracts`.
- [x] Updated client/server/test namespace imports and Razor `@using` directives to new contract namespaces.
- [x] Verified build and Blazor client tests after refactor.
- [ ] Normalize remaining historical task paths below that still point to old `Services/Contracts/*` locations.

## Phase 1: HATEOAS Foundation (Optional - Can Defer)

- [ ] Task 1.1: Create `HateoasLink.cs` model
  - File: `Explore.Blazor.Client/Models/HateoasLink.cs`
  - Acceptance: Model deserializes HAL-format links

- [ ] Task 1.2: Create `IHateoasClient.cs` interface
  - File: `Explore.Blazor.Client/Services/Contracts/IHateoasClient.cs`
  - Acceptance: Interface compiles with FollowLinkAsync, GetWithLinksAsync

- [ ] Task 1.3: Implement `HateoasClient.cs`
  - File: `Explore.Blazor.Client/Services/HateoasClient.cs`
  - Acceptance: Can GET resource and extract `_links`

- [ ] Task 1.4: Create `IHasLinks.cs` interface
  - File: `Explore.Blazor.Client/Models/IHasLinks.cs`
  - Acceptance: Interface defined

- [ ] Task 1.5: Register HateoasClient in DI
  - File: `Explore.Blazor.Client/Program.cs`
  - Acceptance: `IHateoasClient` resolves correctly

---

## Phase 2: Event Aspects Service Layer (HIGH PRIORITY)

- [ ] Task 2.1: Create `IEventAspectService.cs` interface
  - File: `Explore.Blazor.Client/Contracts/Services/Events/IEventAspectService.cs`
  - Methods:
    - `GetIslamicAspectAsync(Guid eventId)`
    - `GetTechAspectAsync(Guid eventId)`
    - `UpsertIslamicAspectAsync(Guid eventId, CreateUpdateIslamicAspectDto dto)`
    - `UpsertTechAspectAsync(Guid eventId, CreateUpdateTechAspectDto dto)`
    - `DeleteIslamicAspectAsync(Guid eventId)`
    - `DeleteTechAspectAsync(Guid eventId)`
  - Acceptance: Interface compiles

- [ ] Task 2.2: Implement `EventAspectService.cs`
  - File: `Explore.Blazor.Client/Services/EventAspectService.cs`
  - Pattern: Follow EventService conventions
  - Acceptance: All methods call correct API endpoints

- [ ] Task 2.3: Register EventAspectService in DI
  - File: `Explore.Blazor.Client/Program.cs`
  - Acceptance: Service resolves correctly

---

## Phase 3: Event Aspects UI Components (HIGH PRIORITY)

- [ ] Task 3.1: Create `EventIslamicAspectCard.razor`
  - File: `Explore.Blazor.Client/Components/Event/EventIslamicAspectCard.razor`
  - Parameters: `EventIslamicAspectDto Aspect`, `Guid EventId`, `EventCallback OnEdit`, `EventCallback OnDelete`
  - Display: Madhab, PrayerTime, GenderMode, QuranRecitation, Language
  - Styling: BEM (`.islamic-aspect-card`)
  - Acceptance: Renders correctly

- [ ] Task 3.2: Create `IslamicAspectEditDialog.razor`
  - File: `Explore.Blazor.Client/Components/Event/IslamicAspectEditDialog.razor`
  - Form Fields:
    - MadhabId (MudSelect)
    - ReferencePrayer (MudSelect - nullable)
    - PrayerTimeOffset (MudNumericField)
    - GenderMode (MudSelect)
    - IncludesQuranRecitation (MudCheckbox)
    - PrimaryLanguageId (MudSelect - nullable)
  - Acceptance: Form validates, submits, closes

- [ ] Task 3.3: Create `EventTechAspectCard.razor`
  - File: `Explore.Blazor.Client/Components/Event/EventTechAspectCard.razor`
  - Parameters: `EventTechAspectDto Aspect`, `Guid EventId`, `EventCallback OnEdit`, `EventCallback OnDelete`
  - Display: SkillLevel, GitHub, Hackathon, TechStack, Competition, Prize
  - Styling: BEM (`.tech-aspect-card`)
  - Acceptance: Renders correctly

- [ ] Task 3.4: Create `TechAspectEditDialog.razor`
  - File: `Explore.Blazor.Client/Components/Event/TechAspectEditDialog.razor`
  - Form Fields:
    - SkillLevel (MudSelect)
    - GithubRepoUrl (MudTextField)
    - HackathonTrack (MudTextField)
    - TechStackTags (MudTextField or MudChipSet)
    - RequiresLaptop (MudCheckbox)
    - IsCodingCompetition (MudCheckbox)
    - MaxTeamSize (MudNumericField - nullable)
    - PrizePool (MudNumericField - nullable)
    - PrizeCurrencyCode (MudTextField)
  - Acceptance: Form validates, submits, closes

- [ ] Task 3.5: Update `EventDetail.razor` for aspects
  - File: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`
  - Changes:
    - Add aspects section after description
    - Show IslamicAspectCard if `IslamicAspect != null`
    - Show TechAspectCard if `TechAspect != null`
    - Show "Add Aspect" buttons based on AvailableAspects
  - Acceptance: Aspects display and edit correctly

- [ ] Task 3.6: Update `EventEdit.razor` for aspects
  - File: `Explore.Blazor.Client/Pages/Event/EventEdit.razor`
  - Changes: Add MudExpansionPanel for Aspects
  - Acceptance: Can edit aspects during event edit

---

## Phase 4: Create Flow Integration (MEDIUM PRIORITY)

- [ ] Task 4.1: Update `CreateEvent.razor`
  - File: `Explore.Blazor.Client/Pages/Event/CreateEvent.razor`
  - Changes:
    - Add "Event Characteristics" section
    - Checkboxes for Islamic/Tech event
    - Conditionally show aspect fields
  - Note: Aspects created after event (separate API calls)
  - Acceptance: Can set initial aspects during creation

---

## Phase 5: HATEOAS-Driven Navigation (MEDIUM PRIORITY)

- [ ] Task 5.1: Update EventService to use links
  - File: `Explore.Blazor.Client/Services/EventService.cs`
  - Changes:
    - Store `_links` from responses
    - Use IHateoasClient for related resources
  - Acceptance: Navigation works via links

- [ ] Task 5.2: Update EventDetail navigation
  - File: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`
  - Changes:
    - Use links for edit button
    - Use links for sessions section
  - Acceptance: Hrefs from API links

- [ ] Task 5.3: Add link-based action buttons
  - Purpose: Show/hide actions based on available links
  - Acceptance: UI reflects available API actions

---

## Phase 6: Lookup Data Sync (MEDIUM PRIORITY)

- [ ] Task 6.1: Verify MadhabService completeness
  - File: `Explore.Blazor.Client/Services/MadhabService.cs`
  - Acceptance: GetAllAsync works

- [ ] Task 6.2: Verify LanguageService completeness
  - File: `Explore.Blazor.Client/Services/LanguageService.cs`
  - Acceptance: GetAllAsync works

- [ ] Task 6.3: Verify SkillLevel enum
  - Location: NSwag client or manual
  - Values: AllLevels=0, Beginner=1, Intermediate=2, Advanced=3
  - Acceptance: Enum values match API

- [ ] Task 6.4: Verify PrayerTime enum
  - Location: NSwag client or manual
  - Values: Fajr=1, Sunrise=2, Dhuhr=3, Asr=4, Maghrib=5, Isha=6
  - Acceptance: Enum values match API

- [ ] Task 6.5: Verify GenderSegregationMode enum
  - Location: NSwag client or manual
  - Values: Mixed=0, MenOnly=1, WomenOnly=2, Segregated=3, Family=4
  - Acceptance: Enum values match API

---

## Phase 7: Styling and Polish (LOW PRIORITY)

- [ ] Task 7.1: Create `aspects.css`
  - File: `Explore.Blazor.Client/wwwroot/css/aspects.css`
  - Contents: BEM styles for aspect cards/dialogs
  - Acceptance: Styles match existing design

- [ ] Task 7.2: Add aspect icons
  - Islamic: Mosque icon
  - Tech: Code/Computer icon
  - Acceptance: Icons render

- [ ] Task 7.3: Test responsive layout
  - Purpose: Ensure mobile display
  - Acceptance: Cards stack properly

---

## Quick Start Guide

### First Session Focus (Recommended)

Start with these tasks in order:

1. **Task 2.1**: Create IEventAspectService interface
2. **Task 2.2**: Implement EventAspectService
3. **Task 2.3**: Register in DI
4. **Task 3.1**: Create EventIslamicAspectCard
5. **Task 3.2**: Create IslamicAspectEditDialog
6. **Task 3.5**: Update EventDetail.razor (Islamic only first)

This gives a working vertical slice for Islamic aspects.

### Second Session Focus

1. **Task 3.3**: Create EventTechAspectCard
2. **Task 3.4**: Create TechAspectEditDialog
3. **Task 3.5**: Complete EventDetail.razor (Tech aspects)
4. **Task 3.6**: Update EventEdit.razor

### Third Session Focus

1. **Task 4.1**: CreateEvent integration
2. **Phase 6**: Verify lookups
3. **Phase 7**: Styling polish

---

## Verification Checklist

After implementation, verify:

- [ ] EventDetail shows Islamic aspect when present
- [ ] EventDetail shows Tech aspect when present
- [ ] Can open edit dialog for Islamic aspect
- [ ] Can open edit dialog for Tech aspect
- [ ] Can save changes to Islamic aspect
- [ ] Can save changes to Tech aspect
- [ ] Can delete Islamic aspect
- [ ] Can delete Tech aspect
- [ ] Can add Islamic aspect to event without one
- [ ] Can add Tech aspect to event without one
- [ ] All existing event functionality still works
- [ ] Build succeeds with no errors
- [ ] Tests pass
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.

## Context Reset Session Update (2026-02-23 18:12 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority focused on admin consolidation handoff in navbar customization track.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in this task file.

## Context Reset Session Update (2026-02-23 18:47 Europe/Brussels)

- Current implementation state: No direct implementation changes in this track during this session.
- Key decisions made this session: Prioritized completion and verification of admin consolidation in the navbar customization track.
- Files modified and why: None for this specific track in this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from the highest-priority unchecked tasks in this track's tasks file.

---

## Session Checkpoint (2026-02-27 Europe/Brussels)

- [x] Reviewed task continuity status for context reset handoff.
- [ ] Resume implementation work from this task latest documented in-progress section.
- [ ] Re-validate with build/tests once implementation resumes.
