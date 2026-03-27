ABOUTME: Implementation plan for unified background customization across Event, Organization, Group, and User entities.
ABOUTME: Covers domain changes, CQRS nullable-DTO pattern, shared Blazor appearance editor with live preview.

# Background Customization — Implementation Plan

## Executive Summary

Add a unified background customization system (BackgroundColor, BackgroundImage, BackgroundEffect) that works consistently across Events, Organizations, Groups, and Users. The system uses a shared Blazor appearance editor component with live preview via MudColorPicker, replaces the current text-field-based input on CreateEvent, and enables profile page background customization through the Actor entity.

## Key Architecture Decision

**Background appearance for Org/Group/User goes through the Actor entity** — NOT by adding fields to each entity separately.

- Actor already has: `BackgroundColor`, `BackgroundEffect`, `BannerColor`, `BannerPictureId`
- Actor is MISSING: `BackgroundImageId` (Guid? FK→StorageObject)
- Event has its OWN appearance fields (separate from Actor) — this stays as-is
- Org/Group/User all reference Actor via ActorId — appearance updates go through Actor
- OrganizationDto and GroupDto already expose `ActorBackgroundColor`, `ActorBackgroundEffect`, etc.
- **UserDto does NOT expose Actor appearance fields** → gap to fix

## Current State

### Domain Layer
- **Event** (`Explore.Domain/Event.cs` lines 117-122): Has `BackgroundColor` (string?), `BackgroundEffect` (string?), `BackgroundImageId` (Guid? FK→StorageObject) — COMPLETE
- **Actor** (`Explore.Domain/Actor.cs` lines 57-60): Has `BackgroundColor`, `BackgroundEffect`, `BannerColor`, `BannerPictureId` — MISSING `BackgroundImageId`

### Application Layer
- **UpdateEventCommand**: Uses nullable-DTO pattern (`UpdateEventDto?`, `UpdateEventStatusDto?`) — REFERENCE PATTERN
- **UpdateActorCommand**: Simple required `UpdateActorDto` — NO nullable-DTO pattern, NO `ISecureRequest`
- **UpdateActorDto**: Already has `BackgroundColor`, `BackgroundEffect`, `BannerColor`, `BannerPictureId`
- **ActorDto**: Has all appearance fields + `BannerPictureUri`
- **OrganizationDto/GroupDto**: Expose `ActorBackgroundColor/Effect/BannerColor/BannerPictureId/Uri` via mapping
- **UserDto**: Missing ALL Actor appearance fields

### Blazor Layer
- **CreateEvent.razor** (lines 86-119): MudTextField for hex color, MudTextField for image URL, MudSelect for effect — NO MudColorPicker, NO live preview
- **Three near-identical appearance helpers**: `EventAppearanceMetadataHelper`, `OrganizationAppearanceMetadataHelper`, `GroupBrandingMetadataHelper` — each with own Settings class and BuildStyle method
- **Profile pages** (Org/Group/User): Each has a banner area with inline styles, but no editing capability

### Validators
- `UpdateActorDtoValidator`: Exists, validates full UpdateActorDto
- No appearance-specific validator

## Implementation Phases

### Phase 1: Domain + Persistence (1 hour)

**Goal**: Add `BackgroundImageId` to Actor entity, configure EF, generate migration.

- **Task 1.1**: Add `BackgroundImageId` (Guid?) and `BackgroundImage` (StorageObject?) navigation property to `Explore.Domain/Actor.cs`
  - Follow Event entity's pattern (lines 119-121)
  - FK attribute: `[ForeignKey("BackgroundImageStorage")]`
  - Acceptance: Compiles, matches Event's field pattern

- **Task 1.2**: Update `ActorConfiguration.cs` to add BackgroundImage FK
  - Add `builder.HasOne(e => e.BackgroundImage).WithMany().HasForeignKey(e => e.BackgroundImageId).OnDelete(DeleteBehavior.SetNull);`
  - Follows Event's configuration pattern
  - Acceptance: EF config compiles, SetNull delete behavior

- **Task 1.3**: Generate EF migration
  - `dotnet ef migrations add AddActorBackgroundImageId --project Explore.Persistence --startup-project Explore.API`
  - Acceptance: Migration generated, only adds single FK column

### Phase 2: Application Layer — DTOs + Mapping (1.5 hours)

**Goal**: Create appearance-specific DTO, refactor Actor command to nullable-DTO pattern, update UserDto.

- **Task 2.1**: Create `UpdateActorAppearanceDto` in `Explore.Application/DTOs/Actor/`
  - Fields (all nullable): `BackgroundColor?`, `BackgroundEffect?`, `BackgroundImageId?`, `BannerColor?`, `BannerPictureId?`
  - All-nullable design: handler updates only non-null fields (targeted partial update)
  - Acceptance: DTO compiles, follows namespace convention

- **Task 2.2**: Create `UpdateActorAppearanceDtoValidator` in `Explore.Application/DTOs/Actor/Validators/`
  - BackgroundColor: MaxLength(50), optional hex format validation (When not null/empty)
  - BackgroundEffect: MaxLength(50), must be one of: None, SoftOverlay, StrongOverlay, Blur (When not null/empty)
  - BannerColor: MaxLength(50) (When not null/empty)
  - BackgroundImageId/BannerPictureId: MustAsync verify exists in StorageObjectRepository (When HasValue)
  - Acceptance: Validator compiles, follows existing validator patterns (manual instantiation with repo deps)

- **Task 2.3**: Refactor `UpdateActorCommand` to nullable-DTO pattern
  - Change from `required UpdateActorDto ActorDto` to `UpdateActorDto? ActorDto` + `UpdateActorAppearanceDto? AppearanceDto`
  - Add `Guid Id` property (set from route like UpdateEventCommand)
  - Consider adding `ISecureRequest` for authorization (if Actor update should be auth-checked)
  - Acceptance: Compiles, matches UpdateEventCommand pattern

- **Task 2.4**: Refactor `UpdateActorCommandHandler` to handle nullable DTOs
  - Get actor by `request.Id` (not from DTO)
  - `if (request.ActorDto is not null)` → validate with existing validator → map
  - `if (request.AppearanceDto is not null)` → validate with new validator → apply targeted field updates (only non-null fields)
  - Add cache invalidation (follow Event handler pattern)
  - Acceptance: Handler compiles, both branches work independently

- **Task 2.5**: Add Actor appearance fields to `UserDto`
  - Add: `ActorBackgroundColor?`, `ActorBackgroundEffect?`, `ActorBannerColor?`, `ActorBannerPictureId?`, `ActorBannerPictureUri?`, `ActorBackgroundImageId?`, `ActorBackgroundImageUri?`
  - Follows OrganizationDto/GroupDto naming convention
  - Acceptance: DTO compiles, matches Org/Group field naming

- **Task 2.6**: Update `MappingProfile.cs` for UserDto and Actor
  - Add User→UserDto mapping for Actor appearance fields (same pattern as Org/Group)
  - Add `BackgroundImageUri` mapping from `BackgroundImage?.Uri` on ActorDto
  - Acceptance: Mapping compiles, AutoMapper doesn't throw

- **Task 2.7**: Add `BackgroundImageId` and `BackgroundImageUri` to `ActorDto` and `UpdateActorDto`
  - ActorDto: `BackgroundImageId` (Guid?) + `BackgroundImageUri` (string?)
  - UpdateActorDto: `BackgroundImageId` (Guid?)
  - Acceptance: DTOs compile

### Phase 3: API Layer (30 min)

**Goal**: Update ActorController to accept the new command shape.

- **Task 3.1**: Update `ActorController.Update` endpoint
  - Accept `UpdateActorCommand` (with nullable DTOs) instead of raw `UpdateActorDto`
  - Set `command.Id = id` from route parameter (like EventController)
  - Acceptance: Endpoint compiles, Swagger shows correct schema

- **Task 3.2**: Verify Event endpoint still works (no regression)
  - Build and run tests
  - Acceptance: All existing tests pass

### Phase 4: Blazor — Shared Infrastructure (2 hours)

**Goal**: Unify appearance helpers, create shared AppearanceEditor component.

- **Task 4.1**: Create unified `AppearanceStyleBuilder` in `Explore.Blazor.Client/Helpers/`
  - Single `AppearanceSettings` record: `BackgroundColor`, `BackgroundImageUri`, `BackgroundEffect`
  - Single `BuildStyle(AppearanceSettings settings, string fallbackColorHex, string? extraCss = null)` method
  - `FromActor(dto)` factory method for Actor-based entities
  - `FromEvent(dto)` factory method for Event
  - Replace the 3 helpers in a subsequent step
  - Acceptance: Compiles, produces identical CSS output for each entity type

- **Task 4.2**: Create `AppearanceEditor.razor` shared component in `Explore.Blazor.Client/Components/`
  - Parameters: `AppearanceSettings Value`, `EventCallback<AppearanceSettings> ValueChanged` (two-way binding)
  - MudColorPicker for BackgroundColor (Spectrum view, ThrottleInterval=100, explicit HEX output)
  - MudTextField for BackgroundImageUri (URL input, optional)
  - MudSelect for BackgroundEffect (None/SoftOverlay/StrongOverlay/Blur)
  - Reset button (MudIconButton with refresh icon) that clears all fields to defaults
  - Live preview panel: div with computed inline style from `AppearanceStyleBuilder.BuildStyle()`
  - On any field change → update settings → fire ValueChanged → Blazor re-renders preview automatically
  - Acceptance: Component renders, color picker works, preview updates live

- **Task 4.3**: Create `AppearanceEditor.razor.css` with BEM scoped styles
  - `.appearance-editor__preview` — preview container with aspect-ratio, rounded corners
  - `.appearance-editor__controls` — control layout
  - `.appearance-editor__reset` — reset button styling
  - Follow repo's BEM + CSS isolation conventions
  - Acceptance: Styles scoped correctly, render correctly

### Phase 5: Blazor — Page Integration (2 hours)

**Goal**: Integrate AppearanceEditor into CreateEvent and profile pages.

- **Task 5.1**: Update CreateEvent.razor to use AppearanceEditor
  - Replace MudExpansionPanel appearance section (lines 86-119) with `<AppearanceEditor @bind-Value="_appearance" />`
  - Wire up live preview: the hero section at top should reflect _appearance changes in real-time
  - Remove old MudTextField/MudSelect appearance inputs
  - Acceptance: CreateEvent page shows color picker, live preview works, form submission still maps correctly

- **Task 5.2**: Update CreateEvent.razor.cs to use AppearanceStyleBuilder
  - Replace `EventAppearanceSettings` with `AppearanceSettings` (from unified builder)
  - Compute hero style from `AppearanceStyleBuilder.BuildStyle(_appearance, fallbackHex)`
  - Acceptance: Code-behind compiles, preview renders, submit still works

- **Task 5.3**: Update Organization profile pages to add appearance editing
  - Add AppearanceEditor to OrganizationProfile (for org admins)
  - Wire save to Actor update endpoint via NSwag client
  - Use existing `ActorBackgroundColor/Effect` from OrganizationDto to populate initial values
  - Acceptance: Org admins can edit and save appearance

- **Task 5.4**: Update Group profile pages to add appearance editing
  - Same pattern as Org
  - Acceptance: Group admins can edit and save appearance

- **Task 5.5**: Update User profile page to add appearance editing
  - Uses newly-added UserDto Actor appearance fields
  - Wire save to Actor update endpoint
  - Acceptance: Users can customize their profile background

- **Task 5.6**: Deprecate old helpers — update all usages to AppearanceStyleBuilder
  - Replace EventAppearanceMetadataHelper usage → AppearanceStyleBuilder.FromEvent()
  - Replace OrganizationAppearanceMetadataHelper usage → AppearanceStyleBuilder.FromActor()
  - Replace GroupBrandingMetadataHelper usage → AppearanceStyleBuilder.FromActor()
  - Delete the 3 old helper files
  - Acceptance: Build passes, no references to old helpers remain

### Phase 6: Testing + Verification (1 hour)

- **Task 6.1**: Update/create unit tests for UpdateActorCommandHandler
  - Test: AppearanceDto branch updates only non-null fields
  - Test: ActorDto branch still works as before
  - Test: Both null → no-op (or appropriate response)
  - Acceptance: Tests pass

- **Task 6.2**: Architecture tests pass
  - Run all test projects per CLAUDE.md
  - Acceptance: All green

- **Task 6.3**: NSwag client regeneration
  - Regenerate swagger.json and NSwag client after API changes
  - Acceptance: Client reflects new endpoint shape

- **Task 6.4**: Visual verification of Blazor UI
  - Build and run Aspire AppHost
  - Verify CreateEvent: color picker, live preview, reset button
  - Verify profile pages: appearance editor present, save works
  - Acceptance: Visual confirmation via Playwriter

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Actor migration breaks existing data | Low | High | Migration only ADDs nullable column |
| UpdateActorCommand refactor breaks existing callers | Medium | High | Ensure backward compat — if only ActorDto sent, works as before |
| MudColorPicker WASM issues | Low | Medium | ThrottleInterval=100ms, test in both server/WASM modes |
| Old helper removal misses usages | Low | Medium | Grep for all references before deleting |
| NSwag client out of sync | Medium | Medium | Regenerate immediately after API changes |

## Success Metrics

1. Actor entity has `BackgroundImageId` FK → StorageObject
2. `PUT /api/actor/{id}` accepts targeted appearance-only updates (nullable-DTO pattern)
3. CreateEvent page uses MudColorPicker with live background preview
4. Reset button returns to theme-default background
5. Org/Group/User profile pages have appearance editing capability
6. All 3 old helpers replaced by single `AppearanceStyleBuilder`
7. All tests pass
8. No type errors, clean diagnostics

## Estimated Total: ~8 hours across phases
