# Blazor Advanced Filtering — Task Checklist

**Last Updated**: 2026-02-12

---

## Phase 1: Expose Module Capabilities to Frontend ✅ COMPLETED
**Effort: S** | **Dependencies: None**

- [x] **1.1** Add `IsIslamicModuleEnabled`, `IsTechModuleEnabled`, `EnabledModules` to `PublicExperienceSettingsDto`
  - File: `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`
  - Acceptance: DTO compiles, no breaking changes

- [x] **1.2** Inject `IModuleService` into `GetPublicExperienceSettingsQueryHandler`, populate module flags
  - File: `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`
  - Acceptance: Handler populates flags correctly from module service

- [x] **1.3** Add matching properties to `PublicExperienceSettingsModel` in Blazor client
  - File: `Explore.Blazor.Client/Services/PublicExperienceService.cs`
  - Acceptance: Model deserializes module flags from API response

- [x] **1.4** Regenerate NSwag client if needed (verify PublicExperience model updates)
  - File: `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
  - Acceptance: Client model includes new properties (Verified not strictly needed as DTO matches)

- [x] **1.5** Build and verify: `dotnet build --configuration Release --verbosity quiet`

---

## Phase 2: Expand IEventService Filter Parameters ✅ COMPLETED
**Effort: S** | **Dependencies: None (parallel with Phase 1)**

- [x] **2.1** Add missing parameters to `IEventService.GetEventsPagedAsync` interface
  - File: `Explore.Blazor.Client/Services/EventService.cs` (interface)
  - New params: eventTypeId, audienceGenderId, audienceAgeId, eventStatusId, genderModeId, includesQuranRecitation, referencePrayerId, islamicPrimaryLanguageId, hasIslamicAspect, skillLevelId, isCodingCompetition, isHackathon, requiresLaptop, techStackTag, hasTechAspect
  - Acceptance: All params optional, no breaking changes

- [x] **2.2** Update `EventService.GetEventsPagedAsync` implementation to pass all params to NSwag client
  - File: `Explore.Blazor.Client/Services/EventService.cs` (implementation)
  - Acceptance: All new params forwarded to `_apiClient.GetEventsAsync()`

- [x] **2.3** Build and verify: `dotnet build --configuration Release --verbosity quiet`

---

## Phase 3: Module-Conditional Filter UI Components ✅ COMPLETED
**Effort: M** | **Dependencies: Phase 1 + Phase 2**

- [x] **3.1** Extract filter bar from EventList into `EventFilterBar.razor` / `EventFilterBar.razor.cs`
  - Files: NEW `Explore.Blazor.Client/Components/Event/EventFilterBar.razor` + `.razor.cs`
  - Move: Sticky filter header HTML + filter state + change handlers
  - Acceptance: Same visual behavior, EventList.razor simplified

- [x] **3.2** Add module-aware filter state to EventFilterBar
  - `[Parameter] bool IsIslamicModuleEnabled`
  - `[Parameter] bool IsTechModuleEnabled`
  - New state variables for all 15+ missing filters
  - `[Parameter] EventCallback<EventFilterState> OnFilterChanged`
  - Acceptance: Component accepts module flags and emits filter changes

- [x] **3.3** Add missing CORE filter dropdowns (always visible)
  - Event Type (MudSelect from lookup)
  - Audience Gender (MudSelect from lookup)
  - Audience Age (MudSelect from lookup)
  - Event Status (MudSelect from lookup)
  - Sort By (MudSelect: Date, Title, Views, Created At)
  - Sort Direction (MudSwitch or MudToggleIconButton)
  - Acceptance: 6 new dropdowns rendered, lookup data populated

- [x] **3.4** Add Islamic aspect filters section (conditional)
  - Wrapped in `@if (IsIslamicModuleEnabled)`
  - Gender Mode: MudSelect with GenderSegregationMode enum
  - Includes Quran Recitation: MudCheckBox
  - Reference Prayer: MudSelect with PrayerTime enum
  - Islamic Primary Language: MudSelect (from languages lookup)
  - Has Islamic Aspect: MudCheckBox
  - Acceptance: Section hidden when module disabled, visible when enabled

- [x] **3.5** Add Tech aspect filters section (conditional)
  - Wrapped in `@if (IsTechModuleEnabled)`
  - Skill Level: MudSelect with SkillLevel enum
  - Is Coding Competition: MudCheckBox
  - Is Hackathon: MudCheckBox
  - Requires Laptop: MudCheckBox
  - Tech Stack Tag: MudTextField
  - Has Tech Aspect: MudCheckBox
  - Acceptance: Section hidden when module disabled, visible when enabled

- [x] **3.6** Wire EventList to use EventFilterBar + pass all params to IEventService
  - Inject IPublicExperienceService into EventList
  - Load module flags on init
  - Handle filter change callbacks
  - Call GetEventsPagedAsync with ALL filter params
  - Acceptance: Full filter flow works end-to-end
  - Virtualize refreshes on any filter change

- [x] **3.7** Build and verify: `dotnet build --configuration Release --verbosity quiet`

---

## Phase 4: Lookup Data Loading ✅ COMPLETED
**Effort: S** | **Dependencies: Phase 3**

- [x] **4.1** Verify/add lookup service methods for audience genders, audience ages, event statuses
  - Check `IAdminService` for existing methods (Verified exist)
  - Add wrappers if missing
  - Acceptance: All lookup data available via services

- [x] **4.2** Load new lookup data in parallel in EventFilterBar/EventList initialization
  - Add to existing `Task.WhenAll()` pattern
  - Acceptance: No extra sequential API calls, graceful fallback

---

## Phase 5: UX Polish ✅ COMPLETED
**Effort: M** | **Dependencies: Phase 3**

- [x] **5.1** Add active filter chips below filter bar
  - Show chip for each active filter
  - Click chip to clear that filter
  - "Clear All" chip when any filter active
  - Acceptance: Chips reflect current filter state

- [x] **5.2** Add collapsible sections for Islamic/Tech filters
  - Use MudExpansionPanel
  - Badge showing active filter count in collapsed header
  - Acceptance: Sections collapsible, badge accurate

- [x] **5.3** Responsive layout for mobile
  - Filters stack vertically on xs/sm
  - Module sections auto-collapse on mobile
  - Acceptance: No horizontal overflow on mobile

- [x] **5.4** Implement Skeleton Loading (Extra)
  - Replaced Spinner with Skeleton Card Grid in `EventList.razor`

- [x] **5.5** Fix Announcement Bar Issues (Extra)
  - Removed duplicate icon
  - Removed localStorage persistence
  - Fixed sticky header overlap using JS height calculation

- [x] **5.6** Fix Theme & Loading UX (Extra)
  - Fixed theme flickering (Default Light)
  - Fixed "No events found" flicker during load

---

## Phase 6: Testing ⏳ NOT STARTED
**Effort: M** | **Dependencies: Phase 1-3**

- [ ] **6.1** Unit tests: PublicExperienceSettingsQueryHandler module flags
  - File: `Event.Application.UnitTests/`
  - Test Islamic enabled/disabled, Tech enabled/disabled, both
  - Acceptance: All tests pass

- [ ] **6.2** bUnit tests: EventFilterBar conditional rendering
  - File: `Explore.Blazor.Client.Tests/`
  - Test: Islamic disabled → no Islamic UI
  - Test: Tech disabled → no Tech UI
  - Test: Both enabled → all sections rendered
  - Acceptance: All tests pass

- [ ] **6.3** Run full test suite
  ```bash
  dotnet test --project Event.Application.UnitTests/Event.Application.UnitTests.csproj --configuration Release --verbosity quiet
  dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
  ```
  - Acceptance: All existing + new tests pass

---

## Summary

| Phase | Tasks | Effort | Status |
|-------|-------|--------|--------|
| 1. Module Capabilities | 5 | S | ✅ Complete (1.1-1.3) |
| 2. IEventService Params | 3 | S | ⏳ Not Started |
| 3. Filter UI Components | 7 | M | ⏳ Not Started |
| 4. Lookup Data | 2 | S | ⏳ Not Started |
| 5. UX Polish | 3 | M | ⏳ Not Started |
| 6. Testing | 3 | M | ⏳ Not Started |
| **Total** | **23** | | |

**Recommended execution order:** Phase 1 + 2 in parallel → Phase 3 + 4 → Phase 5 → Phase 6
## Context Reset Session Update (2026-02-15 21:26 Europe/Brussels)

- Status update: No task-state changes in this session for this track.
- Priority update: Keep existing ordering; analytics work was handled in a separate track.
- Next step: Resume from current in-progress or highest-priority unchecked item.
