# Blazor Advanced Filtering — Implementation Plan

**Last Updated**: 2026-02-12

---

## Executive Summary

The API layer has a fully implemented advanced query specification system with **33 query parameters** for events, including module-conditional Islamic and Tech aspect filters. The Blazor frontend currently only exposes **8 of these filters** and has **zero module-capability awareness**. This plan covers the work needed to bring the Blazor EventList page to full parity with the API, with filters conditionally displayed based on the tenant's enabled modules.

### Key Gap

| Layer | Filters Exposed | Module Awareness |
|-------|----------------|------------------|
| API (EventController) | 33 params | Yes (handler checks `IModuleService`) |
| NSwag Client (auto-generated) | 33 params | N/A (pass-through) |
| IEventService wrapper | 13 params | No |
| EventList UI | 8 dropdowns | No |

---

## Current State Analysis

### What the API Supports (EventController.GetAll)

**Always-available filters (16):**
- searchTerm, categoryId, tagId, formatId, madhabId, locationId, registrationModeId, languageId, dateFrom, dateTo, eventTypeId, audienceGenderId, audienceAgeId, eventStatusId, sortBy, sortDescending

**Islamic aspect filters (5) — gated by `Mod_Islamic`:**
- genderModeId, includesQuranRecitation, referencePrayerId, islamicPrimaryLanguageId, hasIslamicAspect

**Tech aspect filters (6) — gated by `Mod_Tech`:**
- skillLevelId, isCodingCompetition, isHackathon, requiresLaptop, techStackTag, hasTechAspect

**JSONB metadata filters (2):**
- metadataJsonContains, metadataJsonKeyExists

### What Blazor Currently Has

**EventList.razor** — 8 filter dropdowns:
- Date (predefined ranges), Category, Location, Format, Madhab, Registration Mode, Language, Tags

**IEventService.GetEventsPagedAsync** — passes 13 params:
- pageNumber, pageSize, searchTerm, categoryId, tagId, formatId, madhabId, locationId, registrationModeId, languageId, dateFrom, dateTo, sortBy, sortDescending

**Missing from IEventService:**
- eventTypeId, audienceGenderId, audienceAgeId, eventStatusId
- All 5 Islamic aspect filters
- All 6 Tech aspect filters
- 2 JSONB metadata filters

**PublicExperienceSettingsDto** — has NO module capability info:
- TenantId, DeploymentMode, PreferredHomePage, BrandDisplayName, BrandLogoUrl, BrandFaviconUrl, BrandCustomCssUrl, InstanceBaseDomain, Subdomain, CustomDomain
- **Zero module enablement flags**

### Blazor Client Already Has

- Enums: `GenderSegregationMode`, `PrayerTime`, `SkillLevel` in `Models/Responses/EventAspectEnums.cs`
- Aspect display: `EventIslamicAspectCard.razor`, `EventTechAspectCard.razor`
- Aspect edit dialogs: `IslamicAspectEditDialog.razor`, `TechAspectEditDialog.razor`
- NSwag client: Already accepts ALL 33 parameters in `GetEventsAsync()`

---

## Proposed Future State

### Architecture

```
PublicExperience/settings endpoint
    └─ Returns: PublicExperienceSettingsDto (ENHANCED with module flags)
        ├─ IsIslamicModuleEnabled: bool
        ├─ IsTechModuleEnabled: bool
        └─ EnabledModules: List<string>  (future extensibility)

EventList.razor
    ├─ Injects IPublicExperienceService
    ├─ On init: loads settings → gets enabled modules
    ├─ Renders CORE filter section (always visible)
    ├─ Conditionally renders ISLAMIC filter section (if Mod_Islamic enabled)
    ├─ Conditionally renders TECH filter section (if Mod_Tech enabled)
    ├─ Renders SORTING section (always visible)
    └─ Calls IEventService with ALL applicable filter params

IEventService.GetEventsPagedAsync
    └─ Updated signature with ALL 33 parameters
        └─ Delegates to NSwag client (already supports all params)
```

### UI Layout (Filter Bar)

```
┌─── Always Visible ────────────────────────────────────────────┐
│ [Date▼] [Category▼] [Location▼] [Format▼] [Event Type▼]     │
│ [Madhab▼] [Registration▼] [Language▼] [Tags▼]               │
│ [Audience Gender▼] [Audience Age▼] [Status▼]                │
│ [Sort By▼] [Sort Direction▼]                                  │
├─── Islamic Module (conditional) ──────────────────────────────┤
│ [Gender Mode▼] [☐ Quran Recitation] [Prayer▼] [Language▼]   │
│ [☐ Islamic Events Only]                                       │
├─── Tech Module (conditional) ─────────────────────────────────┤
│ [Skill Level▼] [☐ Coding Comp] [☐ Hackathon]                │
│ [☐ Requires Laptop] [Tech Stack: ____] [☐ Tech Events Only] │
└───────────────────────────────────────────────────────────────┘
```

---

## Implementation Phases

### Phase 1: Expose Module Capabilities to Frontend (API + DTO)
**Effort: S** | **Risk: Low**

**Goal:** Add module enablement flags to `PublicExperienceSettingsDto` so the Blazor frontend can know which modules are enabled for the current tenant.

#### Task 1.1: Add Module Flags to PublicExperienceSettingsDto
- **File:** `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs`
- **Change:** Add `IsIslamicModuleEnabled`, `IsTechModuleEnabled`, `EnabledModules` properties
- **Acceptance:**
  - [ ] DTO has boolean flags for Islamic/Tech modules
  - [ ] DTO has `List<string> EnabledModules` for future extensibility
  - [ ] No breaking changes to existing properties

#### Task 1.2: Update GetPublicExperienceSettingsQueryHandler
- **File:** `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs`
- **Change:** Inject `IModuleService`, call `GetEnabledModulesAsync()`, populate new DTO fields
- **Acceptance:**
  - [ ] Handler injects `IModuleService`
  - [ ] Module flags correctly populated in response
  - [ ] Cached appropriately (module flags change rarely)

#### Task 1.3: Update Blazor Client Model
- **File:** `Explore.Blazor.Client/Services/PublicExperienceService.cs`
- **Change:** Add matching properties to `PublicExperienceSettingsModel`
- **Acceptance:**
  - [ ] Model includes `IsIslamicModuleEnabled`, `IsTechModuleEnabled`
  - [ ] Deserialization works correctly

#### Task 1.4: Regenerate NSwag Client (if needed)
- **File:** `Explore.Blazor.Client/Clients/EventApiClient.g.cs`
- **Change:** Regenerate if PublicExperience endpoint DTO change requires it
- **Note:** The GetEventsAsync already has all parameters, so this may only be needed for the PublicExperience model

---

### Phase 2: Expand IEventService Filter Parameters
**Effort: S** | **Risk: Low**

**Goal:** Update `IEventService.GetEventsPagedAsync` to accept all API filter parameters.

#### Task 2.1: Update IEventService Interface
- **File:** `Explore.Blazor.Client/Services/EventService.cs` (interface section)
- **Change:** Add missing parameters to `GetEventsPagedAsync` signature:
  - `int? eventTypeId`
  - `int? audienceGenderId`
  - `int? audienceAgeId`
  - `int? eventStatusId`
  - Islamic filters: `int? genderModeId`, `bool? includesQuranRecitation`, `int? referencePrayerId`, `int? islamicPrimaryLanguageId`, `bool? hasIslamicAspect`
  - Tech filters: `int? skillLevelId`, `bool? isCodingCompetition`, `bool? isHackathon`, `bool? requiresLaptop`, `string? techStackTag`, `bool? hasTechAspect`
- **Acceptance:**
  - [ ] Interface has all parameters (matching API controller)
  - [ ] All new parameters are optional with defaults
  - [ ] Existing callers continue to work (no breaking changes)

#### Task 2.2: Update EventService Implementation
- **File:** `Explore.Blazor.Client/Services/EventService.cs` (implementation section)
- **Change:** Pass new parameters through to `_apiClient.GetEventsAsync()`
- **Acceptance:**
  - [ ] All new params forwarded to NSwag client
  - [ ] Existing behavior unchanged when new params are null

---

### Phase 3: Module-Conditional Filter UI Components
**Effort: M** | **Risk: Medium**

**Goal:** Create the Blazor UI that conditionally renders filter sections based on enabled modules.

#### Task 3.1: Extract Event Filter Bar to Dedicated Component
- **Files:**
  - Create: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor`
  - Create: `Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs`
- **Change:** Extract the sticky filter header from EventList.razor into a reusable component
- **Rationale:** The filter bar is becoming complex (8 → 25+ controls). Extract to keep EventList.razor manageable.
- **Acceptance:**
  - [ ] Filter bar is a standalone component
  - [ ] Emits filter state changes via `EventCallback`
  - [ ] EventList.razor is simplified
  - [ ] Same visual behavior as before extraction

#### Task 3.2: Add Module-Aware Filter State
- **File:** `EventFilterBar.razor.cs`
- **Change:** Accept `IsIslamicModuleEnabled` and `IsTechModuleEnabled` as `[Parameter]` properties
- **Acceptance:**
  - [ ] Component accepts module flags
  - [ ] New filter state variables for all missing filters:
    - `int? selectedEventTypeId`, `int? selectedAudienceGenderId`, `int? selectedAudienceAgeId`, `int? selectedEventStatusId`
    - Islamic: `int? selectedGenderModeId`, `bool? selectedIncludesQuranRecitation`, `int? selectedReferencePrayerId`, `int? selectedIslamicPrimaryLanguageId`, `bool? selectedHasIslamicAspect`
    - Tech: `int? selectedSkillLevelId`, `bool? selectedIsCodingCompetition`, `bool? selectedIsHackathon`, `bool? selectedRequiresLaptop`, `string? selectedTechStackTag`, `bool? selectedHasTechAspect`

#### Task 3.3: Render Core Filters (Always Visible)
- **File:** `EventFilterBar.razor`
- **Change:** Add missing core filter dropdowns: Event Type, Audience Gender, Audience Age, Event Status, Sort By, Sort Direction
- **Acceptance:**
  - [ ] Event Type dropdown populated from lookup
  - [ ] Audience Gender dropdown populated from lookup
  - [ ] Audience Age dropdown populated from lookup
  - [ ] Event Status dropdown populated from lookup
  - [ ] Sort By dropdown with: Date, Title, Views, Created At
  - [ ] Sort Direction toggle

#### Task 3.4: Render Islamic Aspect Filters (Conditional)
- **File:** `EventFilterBar.razor`
- **Change:** Add Islamic filter section wrapped in `@if (IsIslamicModuleEnabled)`
- **UI Controls:**
  - Gender Mode: MudSelect with `GenderSegregationMode` enum values
  - Includes Quran Recitation: MudCheckBox
  - Reference Prayer: MudSelect with `PrayerTime` enum values
  - Islamic Primary Language: MudSelect (from languages lookup)
  - Has Islamic Aspect: MudCheckBox ("Show Islamic events only")
- **Acceptance:**
  - [ ] Section hidden when `IsIslamicModuleEnabled = false`
  - [ ] Section visible with proper label when enabled
  - [ ] All 5 Islamic filters rendered with appropriate controls
  - [ ] Enum values display human-readable labels

#### Task 3.5: Render Tech Aspect Filters (Conditional)
- **File:** `EventFilterBar.razor`
- **Change:** Add Tech filter section wrapped in `@if (IsTechModuleEnabled)`
- **UI Controls:**
  - Skill Level: MudSelect with `SkillLevel` enum values
  - Is Coding Competition: MudCheckBox
  - Is Hackathon: MudCheckBox
  - Requires Laptop: MudCheckBox
  - Tech Stack Tag: MudTextField (free text)
  - Has Tech Aspect: MudCheckBox ("Show tech events only")
- **Acceptance:**
  - [ ] Section hidden when `IsTechModuleEnabled = false`
  - [ ] Section visible with proper label when enabled
  - [ ] All 6 Tech filters rendered with appropriate controls

#### Task 3.6: Wire Up EventList to Use New Filter Bar
- **File:** `EventList.razor`, `EventList.razor.cs`
- **Change:** Replace inline filter HTML with `<EventFilterBar>` component, wire callbacks
- **Acceptance:**
  - [ ] EventList injects `IPublicExperienceService`
  - [ ] On init: fetches public experience settings, extracts module flags
  - [ ] Passes module flags to `<EventFilterBar>`
  - [ ] Handles filter change callbacks
  - [ ] Calls `IEventService.GetEventsPagedAsync()` with ALL filter params
  - [ ] Virtualize refreshes on any filter change

---

### Phase 4: Lookup Data Loading for New Filters
**Effort: S** | **Risk: Low**

**Goal:** Load lookup data needed for the new filter dropdowns.

#### Task 4.1: Load Additional Lookup Data
- **File:** `EventFilterBar.razor.cs` or `EventList.razor.cs`
- **Change:** Load event types, audience genders, audience ages, event statuses on init
- **Note:** Event types and formats are already loaded. Need to add:
  - Audience genders (via `IAdminService.GetAudienceGendersAsync()` or similar)
  - Audience ages (via `IAdminService.GetAudienceAgesAsync()` or similar)
  - Event statuses (via `IAdminService.GetEventStatusesAsync()` or similar)
- **Acceptance:**
  - [ ] All lookup data loaded in parallel
  - [ ] Graceful fallback if any lookup fails
  - [ ] No additional API calls for Islamic/Tech enums (use client-side enums)

#### Task 4.2: Verify Admin Service Methods Exist
- **Depends on:** Checking `IAdminService` interface
- **Change:** Add any missing lookup methods to `IAdminService`/`AdminService`
- **Note:** May already exist in NSwag client but not wrapped in service
- **Acceptance:**
  - [ ] `IAdminService` exposes all needed lookup methods
  - [ ] Methods correctly call NSwag client

---

### Phase 5: UX Polish & Active Filter Indicators
**Effort: M** | **Risk: Low**

**Goal:** Improve the filter UX with active filter chips, clear-all, and responsive layout.

#### Task 5.1: Active Filter Chips
- **File:** `EventFilterBar.razor`
- **Change:** Show active filter chips below the filter bar for quick visibility
- **Acceptance:**
  - [ ] Chips appear for each active filter
  - [ ] Clicking chip removes that filter
  - [ ] "Clear All" chip when any filter is active

#### Task 5.2: Collapsible Filter Sections
- **File:** `EventFilterBar.razor`
- **Change:** Use MudExpansionPanel for Islamic/Tech sections to reduce visual clutter
- **Acceptance:**
  - [ ] Module sections are collapsible
  - [ ] Badge shows count of active filters in collapsed header
  - [ ] Sections remember expanded/collapsed state during session

#### Task 5.3: Responsive Layout
- **File:** `EventFilterBar.razor`
- **Change:** Ensure filter bar works well on mobile (stacked layout)
- **Acceptance:**
  - [ ] Filters stack vertically on small screens
  - [ ] Module sections collapse to save space on mobile
  - [ ] No horizontal overflow

---

### Phase 6: Testing
**Effort: M** | **Risk: Low**

#### Task 6.1: Unit Tests — PublicExperienceSettingsDto Module Flags
- **File:** `Event.Application.UnitTests/Features/PublicExperience/`
- **Change:** Test that handler correctly populates module flags
- **Acceptance:**
  - [ ] Test: Islamic module enabled → flag is true
  - [ ] Test: Islamic module disabled → flag is false
  - [ ] Test: Tech module enabled → flag is true
  - [ ] Test: Both modules → both flags true

#### Task 6.2: Blazor Component Tests — EventFilterBar
- **File:** `Explore.Blazor.Client.Tests/Components/Event/`
- **Change:** bUnit tests for conditional rendering
- **Acceptance:**
  - [ ] Test: Islamic disabled → Islamic filters not rendered
  - [ ] Test: Tech disabled → Tech filters not rendered
  - [ ] Test: Both enabled → all sections rendered
  - [ ] Test: Filter changes emit correct callbacks

#### Task 6.3: Integration Test — Filter Parameter Pass-Through
- **File:** `Event.API.IntegrationTests/`
- **Change:** Verify aspect filters are silently ignored when module disabled
- **Acceptance:**
  - [ ] Test: Islamic filter with module disabled → 200 OK, filters ignored
  - [ ] Test: Islamic filter with module enabled → 200 OK, filters applied

---

## Risk Assessment

| Risk | Impact | Likelihood | Mitigation |
|------|--------|------------|------------|
| NSwag client regeneration breaks existing code | High | Low | Client already has all params; regeneration only for PublicExperience model |
| Module flags add latency to PublicExperience endpoint | Low | Low | ModuleService has caching built-in |
| Too many filters overwhelm UI | Medium | Medium | Use collapsible sections + active filter chips |
| Lookup data loading adds startup latency | Low | Low | Load in parallel, same pattern as existing |
| Breaking IEventService interface | Medium | Low | All new params optional with defaults |

---

## Success Metrics

1. **Parity**: All 33 API filter parameters are accessible from Blazor UI
2. **Conditional rendering**: Islamic/Tech filter sections only visible when module is enabled
3. **No regression**: Existing 8 filters continue working identically
4. **Performance**: No measurable increase in page load time
5. **Tests passing**: All unit and component tests green

---

## Dependencies

- `IModuleService` already exists and has `GetEnabledModulesAsync()`
- NSwag client already supports all 33 parameters
- Blazor client already has `GenderSegregationMode`, `PrayerTime`, `SkillLevel` enums
- MudBlazor provides all needed UI components (MudSelect, MudCheckBox, MudExpansionPanel, MudChip)

---

## Files to Modify (Summary)

### Application Layer
- `Explore.Application/DTOs/Onboarding/PublicExperienceSettingsDto.cs` — Add module flags
- `Explore.Application/Features/PublicExperience/Handlers/Queries/GetPublicExperienceSettingsQueryHandler.cs` — Inject IModuleService

### Blazor Client
- `Explore.Blazor.Client/Services/PublicExperienceService.cs` — Update model
- `Explore.Blazor.Client/Services/EventService.cs` — Expand filter params
- `Explore.Blazor.Client/Pages/Event/EventList.razor` — Use new filter bar component
- `Explore.Blazor.Client/Pages/Event/EventList.razor.cs` — Integrate module awareness
- **NEW:** `Explore.Blazor.Client/Components/Event/EventFilterBar.razor` — Extracted filter bar
- **NEW:** `Explore.Blazor.Client/Components/Event/EventFilterBar.razor.cs` — Filter bar code-behind

### Tests
- `Event.Application.UnitTests/Features/PublicExperience/` — Module flag tests
- `Explore.Blazor.Client.Tests/Components/Event/` — bUnit filter bar tests
