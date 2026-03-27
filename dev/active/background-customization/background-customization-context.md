ABOUTME: Context and key decisions for the background customization feature implementation.
ABOUTME: Tracks session progress, key files, architecture decisions, and quick resume instructions.

# Background Customization — Context

## SESSION PROGRESS (2026-03-25)

### COMPLETED
- Full codebase exploration (5 parallel agents)
- Architecture analysis — Actor entity identified as the appearance hub for Org/Group/User
- Implementation plan created (`background-customization-plan.md`)
- MudBlazor Color Picker API researched (MudColorPicker, DragEffect, ThrottleInterval, MudColor)

### IN PROGRESS
- Plan review and user approval before implementation

### BLOCKERS
- None

## Key Architecture Decisions

### Decision 1: Actor is the appearance hub (NOT individual entities)
- Actor already has BackgroundColor, BackgroundEffect, BannerColor, BannerPictureId
- Org/Group/User reference Actor via ActorId
- OrganizationDto and GroupDto already expose Actor appearance fields (e.g., ActorBackgroundColor)
- Only BackgroundImageId is missing on Actor → add it
- Event keeps its OWN appearance fields (separate from Actor)

### Decision 2: Nullable-DTO pattern for targeted updates
- Follow UpdateEventCommand pattern: `UpdateActorDto?` + `UpdateActorAppearanceDto?`
- All fields in AppearanceDto are nullable → handler only updates non-null fields
- Enables "change color only" or "change effect only" without touching other fields

### Decision 3: Unified AppearanceStyleBuilder replaces 3 helpers
- EventAppearanceMetadataHelper, OrganizationAppearanceMetadataHelper, GroupBrandingMetadataHelper are near-identical
- Single AppearanceStyleBuilder with factory methods: FromEvent(), FromActor()
- Single AppearanceSettings record type

### Decision 4: Shared AppearanceEditor component
- MudColorPicker (not MudTextField) for color input
- Live preview via computed inline style
- Reset button to clear to defaults
- Used in CreateEvent AND profile pages

## Key Files

### Domain
- `Explore.Domain/Actor.cs` — Actor entity (lines 57-60: appearance fields, MISSING BackgroundImageId)
- `Explore.Domain/Event.cs` — Event entity (lines 117-122: has all 3 appearance fields)

### EF Config
- `Explore.Persistence/Configurations/Entities/ActorConfiguration.cs` — Actor EF config (lines 46-48: appearance max lengths)

### Application — DTOs
- `Explore.Application/DTOs/Actor/UpdateActorDto.cs` — Current update DTO (has appearance fields)
- `Explore.Application/DTOs/Actor/ActorDto.cs` — Read DTO (has appearance + BannerPictureUri)
- `Explore.Application/DTOs/Actor/Validators/UpdateActorDtoValidator.cs` — Existing validator
- `Explore.Application/DTOs/User/UserDto.cs` — MISSING Actor appearance fields
- `Explore.Application/DTOs/Organization/OrganizationDto.cs` — Has ActorBackgroundColor/Effect/BannerColor
- `Explore.Application/DTOs/Group/GroupDto.cs` — Has ActorBackgroundColor/Effect/BannerColor

### Application — CQRS (Reference pattern)
- `Explore.Application/Features/Events/Requests/Commands/UpdateEventCommand.cs` — Nullable-DTO pattern reference
- `Explore.Application/Features/Events/Handlers/Commands/UpdateEventCommandHandler.cs` — Handler reference
- `Explore.Application/Features/Actors/Requests/Commands/UpdateActorCommand.cs` — TO REFACTOR
- `Explore.Application/Features/Actors/Handlers/Commands/UpdateActorCommandHandler.cs` — TO REFACTOR

### Blazor — Helpers (TO UNIFY)
- `Explore.Blazor.Client/Helpers/EventAppearanceMetadataHelper.cs` — Event appearance helper
- `Explore.Blazor.Client/Helpers/OrganizationAppearanceMetadataHelper.cs` — Org appearance helper
- `Explore.Blazor.Client/Helpers/GroupBrandingMetadataHelper.cs` — Group branding helper

### Blazor — Pages (TO UPDATE)
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` (lines 86-119: appearance section)
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` (line 87: _appearance field)
- `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor`
- `Explore.Blazor.Client/Pages/Groups/GroupProfile.razor`
- `Explore.Blazor.Client/Pages/User/UserProfile.razor`

### Mapping
- `Explore.Application/Profiles/MappingProfile.cs` — AutoMapper config (Org/Group map Actor fields, User doesn't)

## MudBlazor Color Picker Notes
- Use `Value` (MudColor?) + `ValueChanged` (not @bind-Value) for live preview interception
- `ThrottleInterval=100` to rate-limit drag updates
- `MudColorOutputFormats.Hex` for CSS-compatible output (default .ToString() gives RGBA)
- `ColorPickerView.Spectrum` for best UX
- `ShowAlpha=false` unless transparency is needed

## Quick Resume
1. Read this file and `background-customization-plan.md`
2. Check `background-customization-tasks.md` for current progress
3. Implementation starts at Phase 1 (Domain + Persistence)
4. Follow plan phases in order — each phase builds on the previous
