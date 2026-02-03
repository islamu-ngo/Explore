# Blazor-API Synchronization - Implementation Plan

> **Comprehensive refactor to bring Blazor frontend in sync with API capabilities**
>
> Created: 2026-01-31

## Executive Summary

The API has significantly evolved with HATEOAS support, Event Aspects (Islamic/Tech), enhanced DTOs, and new federation features. The Blazor frontend currently uses hardcoded routes and lacks UI for Event Aspects entirely. This plan addresses the gap by:

1. Implementing a HATEOAS-aware service layer for hypermedia-driven navigation
2. Creating Event Aspects UI components (Islamic and Tech)
3. Synchronizing all domain entities between API and Blazor
4. Maintaining existing patterns (MudBlazor, BEM, BFF architecture)

**Estimated Effort**: 3-4 development sessions
**Risk Level**: Medium (non-breaking changes, additive functionality)

---

## Current State Analysis

### API Capabilities (Available)

| Feature | Status | Notes |
|---------|--------|-------|
| HATEOAS Links | Implemented | 28+ link policies, HAL format responses |
| Event Aspects | Implemented | Islamic and Tech aspects with full CQRS |
| Pagination | Implemented | PagedResponse with metadata |
| Federation (ATProto) | Implemented | PDS sync outbox, IndexedDid |
| Multi-tenancy | Implemented | Virtual tenant masking |
| Module System | Partial | ModuleDefinition entity exists |

### Blazor Capabilities (Current)

| Feature | Status | Gap |
|---------|--------|-----|
| HATEOAS Consumption | Missing | Hardcoded routes in services |
| Event Aspects UI | Missing | No components, no services |
| Pagination UI | Partial | Uses PagedResponse but no MudTable virtualization |
| Federation UI | Missing | No ATProto management UI |
| Multi-tenancy UI | Missing | No tenant switching UI |

### Critical Gaps Identified

1. **HATEOAS Not Consumed**: EventService uses `_apiClient.EventGET2Async(eventId)` with hardcoded method names instead of following `_links`
2. **Event Aspects Missing**: EventDto includes `IslamicAspect` and `TechAspect` but Blazor ignores them
3. **NSwag Client Ignores Links**: Generated client returns DTOs but discards HATEOAS metadata
4. **No Link-Based Navigation**: Blazor uses `Href="/events"` instead of following API links

---

## Proposed Architecture

### HATEOAS Consumption Strategy

**Option A: Link-Aware Service Wrapper** (Recommended)
```
EventService -> HateoasClient -> NSwag Client -> API
                    |
                    v
              Link Resolution
```

**Option B: Custom HTTP Client with Link Parsing**
```
EventService -> HateoasHttpClient -> API
                    |
                    v
              Parse _links from response
```

**Decision**: Option A - Wrap existing NSwag client to preserve type safety while adding link awareness.

### Component Architecture for Event Aspects

```
EventDetail.razor
    |
    +-- EventIslamicAspectCard.razor (conditional)
    |       |
    |       +-- IslamicAspectEditDialog.razor
    |
    +-- EventTechAspectCard.razor (conditional)
            |
            +-- TechAspectEditDialog.razor
```

---

## Implementation Phases

### Phase 1: HATEOAS Foundation (Priority: High)

**Goal**: Create infrastructure for consuming HATEOAS links from API responses

#### Task 1.1: Create HateoasLink Model
- **File**: `Explore.Blazor.Client/Models/HateoasLink.cs`
- **Purpose**: Represent `_links` from API responses
- **Acceptance**: Model can deserialize HAL-format links

#### Task 1.2: Create IHateoasClient Interface
- **File**: `Explore.Blazor.Client/Services/Contracts/IHateoasClient.cs`
- **Purpose**: Define contract for link-aware API calls
- **Methods**:
  - `Task<T?> FollowLinkAsync<T>(HateoasLink link)`
  - `Task<T?> GetWithLinksAsync<T>(string url)` where T : IHasLinks
  - `Task<TResult?> ExecuteActionAsync<TResult>(HateoasLink link, object? payload)`
- **Acceptance**: Interface compiles, follows SOLID principles

#### Task 1.3: Implement HateoasClient
- **File**: `Explore.Blazor.Client/Services/HateoasClient.cs`
- **Purpose**: HTTP client that parses and follows HATEOAS links
- **Dependencies**: BffClient (existing), JsonSerializer
- **Acceptance**: Can GET a resource and extract its `_links`

#### Task 1.4: Create IHasLinks Interface
- **File**: `Explore.Blazor.Client/Models/IHasLinks.cs`
- **Purpose**: Mark DTOs that have HATEOAS links
- **Acceptance**: EventDto, OrganizationDto implement interface

#### Task 1.5: Register HateoasClient in DI
- **File**: `Explore.Blazor.Client/Program.cs`
- **Acceptance**: `IHateoasClient` resolves correctly

### Phase 2: Event Aspects Service Layer (Priority: High)

**Goal**: Add service methods for Event Aspects CRUD

#### Task 2.1: Create IEventAspectService Interface
- **File**: `Explore.Blazor.Client/Services/Contracts/IEventAspectService.cs`
- **Methods**:
  - `Task<EventIslamicAspectDto?> GetIslamicAspectAsync(Guid eventId)`
  - `Task<EventTechAspectDto?> GetTechAspectAsync(Guid eventId)`
  - `Task<bool> UpsertIslamicAspectAsync(Guid eventId, CreateUpdateIslamicAspectDto dto)`
  - `Task<bool> UpsertTechAspectAsync(Guid eventId, CreateUpdateTechAspectDto dto)`
  - `Task<bool> DeleteIslamicAspectAsync(Guid eventId)`
  - `Task<bool> DeleteTechAspectAsync(Guid eventId)`
- **Acceptance**: Interface compiles

#### Task 2.2: Implement EventAspectService
- **File**: `Explore.Blazor.Client/Services/EventAspectService.cs`
- **Dependencies**: IEventApiClient
- **Pattern**: Follow existing EventService conventions
- **Acceptance**: All methods work against API endpoints

#### Task 2.3: Register EventAspectService in DI
- **File**: `Explore.Blazor.Client/Program.cs`
- **Acceptance**: Service resolves correctly

### Phase 3: Event Aspects UI Components (Priority: High)

**Goal**: Create UI components for viewing and editing Event Aspects

#### Task 3.1: Create EventIslamicAspectCard Component
- **File**: `Explore.Blazor.Client/Components/Event/EventIslamicAspectCard.razor`
- **Parameters**: `EventIslamicAspectDto Aspect`, `Guid EventId`, `EventCallback OnEdit`
- **Display**:
  - Madhab badge
  - Prayer time reference
  - Gender segregation mode
  - Quran recitation indicator
  - Primary language
- **Styling**: BEM methodology (`.islamic-aspect-card`, `.islamic-aspect-card__madhab`, etc.)
- **Acceptance**: Renders correctly when aspect exists

#### Task 3.2: Create IslamicAspectEditDialog Component
- **File**: `Explore.Blazor.Client/Components/Event/IslamicAspectEditDialog.razor`
- **Purpose**: MudDialog for creating/editing Islamic aspects
- **Form Fields**:
  - MadhabId (MudSelect)
  - ReferencePrayer (MudSelect - nullable enum)
  - PrayerTimeOffset (MudNumericField - nullable)
  - GenderMode (MudSelect)
  - IncludesQuranRecitation (MudCheckbox)
  - PrimaryLanguageId (MudSelect - nullable)
- **Validation**: FluentValidation from CreateUpdateIslamicAspectDtoValidator
- **Acceptance**: Form validates, submits to API, closes dialog

#### Task 3.3: Create EventTechAspectCard Component
- **File**: `Explore.Blazor.Client/Components/Event/EventTechAspectCard.razor`
- **Parameters**: `EventTechAspectDto Aspect`, `Guid EventId`, `EventCallback OnEdit`
- **Display**:
  - Skill level badge
  - GitHub repo link
  - Hackathon track
  - Tech stack tags (chips)
  - Requires laptop indicator
  - Competition badge (if applicable)
  - Prize pool (if applicable)
- **Acceptance**: Renders correctly when aspect exists

#### Task 3.4: Create TechAspectEditDialog Component
- **File**: `Explore.Blazor.Client/Components/Event/TechAspectEditDialog.razor`
- **Purpose**: MudDialog for creating/editing Tech aspects
- **Form Fields**:
  - SkillLevel (MudSelect - enum)
  - GithubRepoUrl (MudTextField)
  - HackathonTrack (MudTextField)
  - TechStackTags (MudTextField or MudChipSet)
  - RequiresLaptop (MudCheckbox)
  - IsCodingCompetition (MudCheckbox)
  - MaxTeamSize (MudNumericField - nullable)
  - PrizePool (MudNumericField - nullable)
  - PrizeCurrencyCode (MudTextField)
- **Acceptance**: Form validates, submits to API, closes dialog

#### Task 3.5: Update EventDetail.razor for Aspects
- **File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`
- **Changes**:
  - Add aspects section after description
  - Show EventIslamicAspectCard if `_eventDetails.IslamicAspect != null`
  - Show EventTechAspectCard if `_eventDetails.TechAspect != null`
  - Show "Add Aspect" buttons based on `AvailableAspects` list
- **Styling**: Follow existing section pattern (`.event-detail__section-card`)
- **Acceptance**: Aspects display correctly, edit dialogs work

#### Task 3.6: Update EventEdit.razor for Aspects
- **File**: `Explore.Blazor.Client/Pages/Event/EventEdit.razor`
- **Changes**:
  - Add MudExpansionPanel for Aspects
  - Include IslamicAspectEditDialog inline or as step
  - Include TechAspectEditDialog inline or as step
- **Acceptance**: Can edit aspects during event edit flow

### Phase 4: Event Aspects Integration with Create Flow (Priority: Medium)

**Goal**: Allow setting aspects during event creation

#### Task 4.1: Update CreateEvent.razor
- **File**: `Explore.Blazor.Client/Pages/Event/CreateEvent.razor`
- **Changes**:
  - Add "Event Characteristics" step/section
  - Checkboxes for "This is an Islamic event" / "This is a Tech event"
  - Conditionally show aspect form fields
- **Note**: Aspects are created after event creation (separate API calls)
- **Acceptance**: Can set initial aspects during creation

### Phase 5: HATEOAS-Driven Navigation (Priority: Medium)

**Goal**: Refactor services to use HATEOAS links instead of hardcoded routes

#### Task 5.1: Update EventService to Use Links
- **File**: `Explore.Blazor.Client/Services/EventService.cs`
- **Changes**:
  - Store `_links` from responses
  - Use `IHateoasClient` to follow links for related resources
  - Provide methods like `GetSessionsAsync(EventDto event)` that follow `sessions` link
- **Acceptance**: Navigation works via links

#### Task 5.2: Update EventDetail.razor Navigation
- **File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`
- **Changes**:
  - Use link from `_eventDetails._links["edit"]` for edit button href
  - Use link from `_eventDetails._links["sessions"]` for sessions section
- **Acceptance**: Navigation driven by API links

#### Task 5.3: Add Link-Based Action Buttons
- **Purpose**: Show/hide actions based on available links
- **Logic**: If `_links["delete"]` exists, show delete button
- **Acceptance**: UI reflects available actions from API

### Phase 6: Lookup Data Synchronization (Priority: Medium)

**Goal**: Ensure all lookup tables used by aspects are available in Blazor

#### Task 6.1: Create MadhabService (if not exists)
- **File**: `Explore.Blazor.Client/Services/MadhabService.cs`
- **Verify**: Already exists - check completeness
- **Acceptance**: `GetAllAsync()` returns all Madhabs

#### Task 6.2: Create LanguageService (if not exists)
- **File**: `Explore.Blazor.Client/Services/LanguageService.cs`
- **Verify**: Already exists - check completeness
- **Acceptance**: `GetAllAsync()` returns all Languages

#### Task 6.3: Verify SkillLevel Enum in Client
- **File**: NSwag-generated or manual enum
- **Check**: `SkillLevel` enum matches API
- **Acceptance**: Enum values align

#### Task 6.4: Verify PrayerTime Enum in Client
- **File**: NSwag-generated or manual enum
- **Check**: `PrayerTime` enum matches API
- **Acceptance**: Enum values align

#### Task 6.5: Verify GenderSegregationMode Enum in Client
- **File**: NSwag-generated or manual enum
- **Check**: `GenderSegregationMode` enum matches API
- **Acceptance**: Enum values align

### Phase 7: Styling and Polish (Priority: Low)

**Goal**: Ensure consistent UI and UX for new components

#### Task 7.1: Create aspects.css
- **File**: `Explore.Blazor.Client/wwwroot/css/aspects.css`
- **Contents**: BEM styles for aspect cards and dialogs
- **Acceptance**: Styles match existing event detail design

#### Task 7.2: Add Aspect Icons
- **Purpose**: Use appropriate MudBlazor icons for aspects
- **Islamic**: `Icons.Material.Filled.Mosque` or similar
- **Tech**: `Icons.Material.Filled.Code` or `Computer`
- **Acceptance**: Icons render correctly

#### Task 7.3: Test Responsive Layout
- **Purpose**: Ensure aspects display well on mobile
- **Acceptance**: Cards stack properly on small screens

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| NSwag client regeneration breaks changes | Medium | High | Document which files are generated vs manual |
| HATEOAS parsing breaks on API changes | Low | Medium | Version API, use schema validation |
| Performance impact from extra HTTP calls | Low | Low | Cache links, batch requests where possible |
| Existing tests fail | Medium | Medium | Run tests before and after each phase |

---

## Success Metrics

1. **Event Aspects Display**: EventDetail shows Islamic/Tech aspects when present
2. **Aspect CRUD Works**: Can create, update, delete aspects from UI
3. **HATEOAS Foundation**: At least EventService uses link-based navigation
4. **No Regressions**: All existing functionality continues to work
5. **Tests Pass**: Unit and integration tests green

---

## Dependencies

- **API must be running**: All Blazor development depends on API endpoints
- **NSwag regeneration**: May need to regenerate client after API changes
- **MudBlazor version**: Ensure compatible with current project

---

## Timeline Estimate

| Phase | Estimated Time |
|-------|---------------|
| Phase 1: HATEOAS Foundation | 2-3 hours |
| Phase 2: Event Aspects Service | 1-2 hours |
| Phase 3: Event Aspects UI | 3-4 hours |
| Phase 4: Create Flow Integration | 1-2 hours |
| Phase 5: HATEOAS Navigation | 2-3 hours |
| Phase 6: Lookup Sync | 1 hour |
| Phase 7: Styling | 1-2 hours |
| **Total** | **11-17 hours** |

---

## Related Documentation

- [Blazor UI Conventions Skill](../../../.claude/skills/blazor-ui-conventions/SKILL.md)
- [Clean Architecture Rules](../../../.claude/skills/clean-architecture-rules/SKILL.md)
- [API Documentation](../../docs/API.md)
- [ARCHITECTURE.md](../../docs/ARCHITECTURE.md)
