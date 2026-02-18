# Blazor-API Synchronization - Context

> **Key files, decisions, and quick resume information**
>
> Created: 2026-01-31
> **Last Updated: 2026-01-31 (Session Handoff)**

---

## SESSION HANDOFF (2026-01-31)

### What Was Accomplished This Session

1. **Event Aspects API Implementation (COMPLETED)**
   - Created full Clean Architecture layers for EventIslamicAspect and EventTechAspect
   - Repository interfaces and implementations
   - CQRS queries and commands with handlers
   - Validators for aspect DTOs
   - 6 new endpoints added to EventController
   - HATEOAS links for aspects in EventLinkPolicy
   - All builds pass, 29 tests passing

2. **Blazor Sync Planning (COMPLETED)**
   - Comprehensive analysis of Blazor codebase
   - Identified critical gaps (aspects UI missing, HATEOAS not consumed)
   - Created 3-file dev docs structure (plan, context, tasks)

### Files to Reference

**This folder contains everything needed:**
```
dev/active/blazor-api-sync/
├── blazor-api-sync-plan.md      # 7-phase implementation plan (11-17 hours)
├── blazor-api-sync-context.md   # THIS FILE - key decisions, files, constraints
└── blazor-api-sync-tasks.md     # 35+ checkable tasks with priority order
```

### Immediate Next Steps

**START HERE - Phase 2 (Event Aspects Service Layer):**

1. Create `Explore.Blazor.Client/Services/Contracts/IEventAspectService.cs`
2. Create `Explore.Blazor.Client/Services/EventAspectService.cs`
3. Register in `Explore.Blazor.Client/Program.cs`

Then move to Phase 3 (UI Components).

### Build Status

```bash
# Verified working:
dotnet build  # SUCCESS
dotnet test   # 29 tests passing
```

### No Uncommitted Critical Changes

All API changes from previous session are committed. Dev docs are new files (untracked).

---

## SESSION PROGRESS (2026-01-31)

### COMPLETED
- Analyzed API HATEOAS implementation (28+ link policies)
- Analyzed Blazor service layer (25+ services, hardcoded routes)
- Identified critical gaps (aspects UI missing, HATEOAS not consumed)
- Created comprehensive implementation plan
- Created all dev docs (plan, context, tasks)

### IN PROGRESS
- Nothing - ready for next session to start implementation

### BLOCKERS
- None identified

---

## Key Files Reference

### API Layer (Source of Truth) - ALREADY IMPLEMENTED

**HATEOAS Infrastructure:**
- `Explore.API/Hateoas/RouteNames.cs` - All route name constants (includes aspect routes)
- `Explore.API/Hateoas/Policies/EventLinkPolicy.cs` - Event link generation (includes aspect links)
- `Explore.API/Hateoas/HateoasConstants.cs` - Link relation constants

**Event Aspects API (COMPLETED THIS SESSION):**
- `Explore.API/Controllers/EventController.cs` - 6 aspect endpoints (GET/PUT/DELETE for Islamic and Tech)
- `Explore.Application/Features/EventAspects/` - CQRS handlers
- `Explore.Application/DTOs/EventAspects/` - Aspect DTOs and validators
- `Explore.Application/Contracts/Persistence/IEventIslamicAspectRepository.cs`
- `Explore.Application/Contracts/Persistence/IEventTechAspectRepository.cs`
- `Explore.Persistence/Repositories/EventIslamicAspectRepository.cs`
- `Explore.Persistence/Repositories/EventTechAspectRepository.cs`

**Domain Entities:**
- `Explore.Domain/EventIslamicAspect.cs` - Islamic aspect entity with enums (PrayerTime, GenderSegregationMode)
- `Explore.Domain/EventTechAspect.cs` - Tech aspect entity with enums (SkillLevel)

### Blazor Layer (TO MODIFY)

**Services:**
- `Explore.Blazor.Client/Services/EventService.cs` - Main event service (needs HATEOAS - Phase 5)
- `Explore.Blazor.Client/Services/MadhabService.cs` - Madhab lookup (exists)
- `Explore.Blazor.Client/Services/LanguageService.cs` - Language lookup (exists)
- `Explore.Blazor.Client/Program.cs` - DI registration (add new services here)

**Pages:**
- `Explore.Blazor.Client/Pages/Event/EventDetail.razor` - Show aspects here (Phase 3)
- `Explore.Blazor.Client/Pages/Event/EventEdit.razor` - Edit aspects (Phase 3)
- `Explore.Blazor.Client/Pages/Event/CreateEvent.razor` - Create with aspects (Phase 4)

**Components to Create (Phase 3):**
- `Explore.Blazor.Client/Components/Event/EventIslamicAspectCard.razor` - NEW
- `Explore.Blazor.Client/Components/Event/EventTechAspectCard.razor` - NEW
- `Explore.Blazor.Client/Components/Event/IslamicAspectEditDialog.razor` - NEW
- `Explore.Blazor.Client/Components/Event/TechAspectEditDialog.razor` - NEW

**Generated Client:**
- `Explore.Blazor.Client/Clients/EventApiClient.g.cs` - NSwag generated (DO NOT EDIT DIRECTLY)

---

## Key Decisions Made

### 1. HATEOAS Consumption Strategy

**Decision**: Wrap existing NSwag client with link-aware layer

**Rationale**:
- Preserves type safety from NSwag generation
- Minimal changes to existing code
- Links can be parsed from raw responses

**Alternative Rejected**: Replace NSwag with custom HttpClient (too disruptive)

### 2. Aspect UI Location

**Decision**: Aspects shown as expandable cards in EventDetail

**Rationale**:
- Follows existing section pattern (`.event-detail__section-card`)
- Non-intrusive - collapses when not relevant
- Edit via dialogs matches Organization pattern

**Alternative Rejected**: Separate aspect pages (too fragmented)

### 3. Aspect Edit Pattern

**Decision**: MudDialog pattern for aspect editing

**Rationale**:
- Consistent with existing dialogs (EditSessionDialog, ReviewDialog)
- No page navigation required
- Good for optional data

### 4. Priority Order

**Decision**: Event Aspects (Phases 2-3) before HATEOAS (Phase 1)

**Rationale**:
- Aspects provide immediate user value
- HATEOAS is infrastructure improvement, can defer
- Aspects use existing NSwag client patterns

---

## Technical Constraints

### NSwag Generated Client

The `EventApiClient.g.cs` is auto-generated. Changes to this file will be overwritten.

**Workaround**:
- Create wrapper services that add functionality
- Do NOT modify `*.g.cs` files directly
- If API changes, regenerate client: `dotnet nswag run`

### Enum Synchronization

Enums must match between API and Blazor:
- `SkillLevel` (AllLevels=0, Beginner=1, Intermediate=2, Advanced=3)
- `PrayerTime` (Fajr=1, Sunrise=2, Dhuhr=3, Asr=4, Maghrib=5, Isha=6)
- `GenderSegregationMode` (Mixed=0, MenOnly=1, WomenOnly=2, Segregated=3, Family=4)

**Action**: Verify enums exist in NSwag client or create manual copies in Phase 6

### BFF Pattern

Blazor communicates with API through BFF (Backend-for-Frontend):
- `BffClient.cs` handles token forwarding
- `BrowserCredentialsMessageHandler.cs` adds credentials
- YARP proxies `/api/*` to actual API

**Impact**: HATEOAS links returned from API will have correct BFF paths

---

## Quick Resume Instructions

### To Continue This Work:

1. **Read this file** - You're doing this now
2. **Read the plan**: `blazor-api-sync-plan.md` - Full 7-phase plan
3. **Check tasks file**: `blazor-api-sync-tasks.md` - Checkable task list
4. **Start with Phase 2** - Create EventAspectService

### Priority Order:

1. **Phase 2 & 3**: Event Aspects (service + UI) - Highest priority
2. **Phase 4**: Create flow integration - Medium
3. **Phase 1 & 5**: HATEOAS foundation - Can be deferred
4. **Phase 6 & 7**: Lookups and polish - Low priority

### Key Commands:

```bash
# Build solution
dotnet build

# Run tests
dotnet test

# Regenerate NSwag client (if API changes)
cd Explore.Blazor.Client
dotnet nswag run

# Run Blazor project
cd Explore.Blazor
dotnet run
```

---

## API Endpoints Available for Aspects

The following endpoints are ready to use (implemented this session):

```
GET    /api/v1/Event/{id}/aspects/islamic    - Get Islamic aspect
PUT    /api/v1/Event/{id}/aspects/islamic    - Create/Update Islamic aspect
DELETE /api/v1/Event/{id}/aspects/islamic    - Delete Islamic aspect
GET    /api/v1/Event/{id}/aspects/tech       - Get Tech aspect
PUT    /api/v1/Event/{id}/aspects/tech       - Create/Update Tech aspect
DELETE /api/v1/Event/{id}/aspects/tech       - Delete Tech aspect
```

**NSwag Client Methods** (should be generated):
- `EventGET_IslamicAspectAsync(Guid id)`
- `EventPUT_IslamicAspectAsync(Guid id, CreateUpdateIslamicAspectDto dto)`
- `EventDELETE_IslamicAspectAsync(Guid id)`
- `EventGET_TechAspectAsync(Guid id)`
- `EventPUT_TechAspectAsync(Guid id, CreateUpdateTechAspectDto dto)`
- `EventDELETE_TechAspectAsync(Guid id)`

**Note**: Method names may vary - check `EventApiClient.g.cs` after regeneration.

---

## Dependencies Graph

```
EventDetail.razor
    |
    +-- IEventService (existing)
    |       |
    |       +-- IEventApiClient (NSwag generated)
    |
    +-- IEventAspectService (TO CREATE - Phase 2)
    |       |
    |       +-- IEventApiClient (existing)
    |
    +-- EventIslamicAspectCard (TO CREATE - Phase 3)
    |       |
    |       +-- IslamicAspectEditDialog (TO CREATE - Phase 3)
    |               |
    |               +-- IMadhabService (existing)
    |               +-- ILanguageService (existing)
    |
    +-- EventTechAspectCard (TO CREATE - Phase 3)
            |
            +-- TechAspectEditDialog (TO CREATE - Phase 3)
```

---

## Testing Strategy

1. **Manual Testing**:
   - Navigate to event detail
   - Verify aspects display when present
   - Test edit dialog opens and saves
   - Test delete confirmation

2. **Integration Testing**:
   - EventAspectService can call API endpoints
   - Error handling for 404/401/403

3. **No Unit Tests Required** for initial implementation (UI layer)

---

## What NOT to Do

- **DO NOT** modify `EventApiClient.g.cs` directly - it's generated
- **DO NOT** skip Phase 2 - service layer is required for Phase 3
- **DO NOT** create separate pages for aspects - use cards in EventDetail
- **DO NOT** start with HATEOAS (Phase 1) - aspects are higher priority
## Context Reset Session Update (2026-02-15 21:25 Europe/Brussels)

- Current implementation state: No new implementation changes in this session for this track.
- Key decisions made this session: Priority shifted to analytics implementation completion and verification.
- Files modified and why: None in this track during this session.
- Blockers/issues discovered: None newly discovered for this track.
- Next immediate steps: Continue from highest-priority unchecked items in `blazor-api-sync-tasks.md`.
