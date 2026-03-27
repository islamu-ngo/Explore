ABOUTME: Context and key decisions for the background customization feature implementation.
ABOUTME: Tracks session progress, key files, architecture decisions, and quick resume instructions.

# Background Customization — Context

## SESSION PROGRESS (2026-03-26)

### STATUS: IMPLEMENTATION COMPLETE ✅

All 6 phases (24 tasks) implemented, build green, 881 tests pass (0 regressions).
Visual verification partially done (app starts, auth works, but full UI needs Docker infra).

### COMPLETED
- Full codebase exploration (5 parallel agents)
- Architecture analysis — Actor entity identified as the appearance hub for Org/Group/User
- Implementation plan created and approved
- Phase 1: Domain + EF config (Actor.BackgroundImageId, BackgroundImage nav, FK config)
- Phase 2: Application layer (DTOs, validators, nullable-DTO command pattern, mappings)
- Phase 3: API layer (ActorController refactored, serialization, integration test updated)
- Phase 4: Blazor shared components (AppearanceStyleBuilder, AppearanceEditor.razor)
- Phase 5: Blazor page updates (CreateEvent, EventDetail, EventEdit, OrgProfile, GroupProfile, UserProfile, admin pages)
- Phase 6: Unit tests (12 new), all test projects run (86 Blazor failures are pre-existing), NSwag regenerated
- Old helpers deleted by user (EventAppearanceMetadataHelper, OrganizationAppearanceMetadataHelper, GroupBrandingMetadataHelper)

### BLOCKERS
- None. Feature is complete.

### REMAINING (LOW PRIORITY)
- Full visual verification requires Docker stack (PostgreSQL, Keycloak) — not available in this session
- EF migration already exists: `20260325122842_background-field.cs` covers Actor.BackgroundImageId

## Key Architecture Decisions

### Decision 1: Actor is the appearance hub (NOT individual entities)
- Actor already has BackgroundColor, BackgroundEffect, BannerColor, BannerPictureId
- Org/Group/User reference Actor via ActorId
- OrganizationDto and GroupDto already expose Actor appearance fields (e.g., ActorBackgroundColor)
- Only BackgroundImageId was missing on Actor → added it
- Event keeps its OWN appearance fields (separate from Actor)

### Decision 2: Nullable-DTO pattern for targeted updates
- Follow UpdateEventCommand pattern: `UpdateActorDto?` + `UpdateActorAppearanceDto?`
- All fields in AppearanceDto are nullable → handler only updates non-null fields
- Enables "change color only" or "change effect only" without touching other fields
- Handler uses `ApplyAppearanceUpdate()` static method for direct field assignment

### Decision 3: Unified AppearanceStyleBuilder replaces 3 helpers
- EventAppearanceMetadataHelper, OrganizationAppearanceMetadataHelper, GroupBrandingMetadataHelper → DELETED
- Single AppearanceStyleBuilder with static methods: BuildStyle(), BuildHeroStyle(), BuildBannerStyle()
- Single AppearanceSettings class with BackgroundColor, ImageUri, BackgroundEffect properties

### Decision 4: Shared AppearanceEditor component
- MudColorPicker (not MudTextField) for color input — Value/ValueChanged pattern (not @bind-Value)
- Live preview via computed inline style using AppearanceStyleBuilder.BuildStyle()
- Reset button to clear all fields to defaults
- Parameters: BackgroundColor/Changed, BackgroundEffect/Changed, ImageUri/Changed (string two-way binding)
- ThrottleInterval=100, ShowAlpha=false, _lastSyncedColorHex tracking to prevent MudColor recreation

## Files Created

### New Files
- `Explore.Application/DTOs/Actor/UpdateActorAppearanceDto.cs` — all-nullable appearance fields (BackgroundColor?, BackgroundEffect?, BannerColor?, BannerPictureId?, BackgroundImageId?)
- `Explore.Application/DTOs/Actor/Validators/UpdateActorAppearanceDtoValidator.cs` — hex regex, effect enum, FK existence checks
- `Explore.Blazor.Client/Helpers/AppearanceStyleBuilder.cs` — unified builder (AppearanceSettings + BuildStyle/BuildHeroStyle/BuildBannerStyle)
- `Explore.Blazor.Client/Shared/AppearanceEditor.razor` — shared MudColorPicker + live preview + reset component
- `Explore.Blazor.Client/Shared/AppearanceEditor.razor.css` — BEM scoped styles
- `Event.Application.UnitTests/Features/Actors/Commands/UpdateActorCommandHandlerTests.cs` — 12 unit tests

### Files Modified
- `Explore.Domain/Actor.cs` — +BackgroundImageId (Guid?) + BackgroundImage (StorageObject?) nav
- `Explore.Persistence/Configurations/Entities/ActorConfiguration.cs` — +BackgroundImage FK with SetNull
- `Explore.Application/DTOs/Actor/ActorDto.cs` — +BackgroundImageId, +BackgroundImageUri
- `Explore.Application/DTOs/Actor/ActorListDto.cs` — same
- `Explore.Application/DTOs/Actor/UpdateActorDto.cs` — +BackgroundImageId
- `Explore.Application/DTOs/User/UserDto.cs` — +7 Actor appearance fields
- `Explore.Application/DTOs/Group/GroupDto.cs` — +ActorBackgroundImageId/Uri
- `Explore.Application/DTOs/Group/GroupListDto.cs` — same
- `Explore.Application/DTOs/Organization/OrganizationDto.cs` — +ActorBackgroundImageId/Uri
- `Explore.Application/DTOs/Organization/OrganizationListDto.cs` — same
- `Explore.Application/Features/Actors/Requests/Commands/UpdateActorCommand.cs` — nullable-DTO pattern: Guid Id, UpdateActorDto?, UpdateActorAppearanceDto?
- `Explore.Application/Features/Actors/Handlers/Commands/UpdateActorCommandHandler.cs` — full rewrite with if-null-check branches, ApplyAppearanceUpdate, HybridCache
- `Explore.Application/Profiles/MappingProfile.cs` — Actor/User/Group/Org BackgroundImage URI mappings
- `Explore.Application/Serialization/ExploreJsonContext.cs` — +UpdateActorAppearanceDto in all 6 sections
- `Explore.API/Controllers/ActorController.cs` — Update endpoint accepts UpdateActorCommand with command.Id = id
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor` — AppearanceEditor replaces old MudTextField inputs
- `Explore.Blazor.Client/Pages/Events/CreateEvent.razor.cs` — _bgColor/_bgEffect/_bgImageUri strings replace EventAppearanceSettings
- `Explore.Blazor.Client/Pages/Events/EventDetail.razor` — ImageUri property name fix
- `Explore.Blazor.Client/Pages/Events/EventDetail.razor.cs` — AppearanceSettings + AppearanceStyleBuilder
- `Explore.Blazor.Client/Pages/Events/EventEdit.razor` — ImageUri property name fix
- `Explore.Blazor.Client/Pages/Events/EventEdit.razor.cs` — AppearanceSettings + AppearanceStyleBuilder
- `Explore.Blazor.Client/Pages/Organizations/OrganizationProfile.razor.cs` — AppearanceSettings + AppearanceStyleBuilder
- `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor` — ImageUri fix
- `Explore.Blazor.Client/Pages/Organizations/OrganizationDetails.razor.cs` — AppearanceSettings + AppearanceStyleBuilder
- `Explore.Blazor.Client/Pages/Organizations/CreateOrganization.razor.cs` — AppearanceSettings type
- `Explore.Blazor.Client/Pages/Groups/GroupProfile.razor.cs` — AppearanceSettings + AppearanceStyleBuilder
- `Explore.Blazor.Client/Pages/User/UserProfile.razor` — @_bannerStyle replaces hardcoded gradient
- `Explore.Blazor.Client/Pages/User/UserProfile.razor.cs` — AppearanceStyleBuilder + typed Actor appearance fields
- `Explore.Blazor.Client/Pages/Admin/Organizations/OrganizationProfileSection.razor` — AppearanceSettings + AppearanceStyleBuilder
- `Explore.Blazor.Client/Pages/Admin/Groups/GroupProfileSection.razor` — AppearanceSettings + AppearanceStyleBuilder
- `Explore.Blazor.Client/Pages/Admin/Groups/GroupAdminSettingsLayout.razor` — AppearanceSettings + AppearanceStyleBuilder
- `Event.API.IntegrationTests/Features/ActorControllerTests.cs` — Updated test payload to command shape

### Files Deleted (by user)
- `Explore.Blazor.Client/Helpers/EventAppearanceMetadataHelper.cs`
- `Explore.Blazor.Client/Helpers/OrganizationAppearanceMetadataHelper.cs`
- `Explore.Blazor.Client/Helpers/GroupBrandingMetadataHelper.cs`

## MudBlazor Color Picker Notes
- Use `Value` (MudColor?) + `ValueChanged` (not @bind-Value) for live preview interception
- `ThrottleInterval=100` to rate-limit drag updates
- `MudColorOutputFormats.Hex` for CSS-compatible output (default .ToString() gives RGBA)
- `ColorPickerView.Spectrum` for best UX
- `ShowAlpha=false` unless transparency is needed
- Track `_lastSyncedColorHex` to avoid re-creating MudColor on every re-render

## Quick Resume
1. **Feature is COMPLETE** — all 24 tasks done, build green, tests pass
2. Only remaining: full visual verification when Docker infrastructure is available
3. NSwag client has been regenerated with all new types/fields
4. EF migration `20260325122842_background-field` already covers the schema change
5. All changes are UNCOMMITTED — need git add + commit
