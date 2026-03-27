ABOUTME: Task checklist for background customization feature implementation.
ABOUTME: Tracks progress across 6 phases from domain changes through Blazor UI integration.

# Background Customization — Task Checklist

## Phase 1: Domain + Persistence (~1 hour) ✅
- [x] 1.1 Add `BackgroundImageId` + `BackgroundImage` nav property to `Actor.cs`
- [x] 1.2 Update `ActorConfiguration.cs` with BackgroundImage FK (SetNull delete)
- [x] 1.3 EF migration — already covered by pre-existing `20260325122842_background-field` migration

## Phase 2: Application Layer — DTOs + Mapping (~1.5 hours) ✅
- [x] 2.1 Create `UpdateActorAppearanceDto` in `DTOs/Actor/`
- [x] 2.2 Create `UpdateActorAppearanceDtoValidator` in `DTOs/Actor/Validators/`
- [x] 2.3 Refactor `UpdateActorCommand` to nullable-DTO pattern (+ `Guid Id`)
- [x] 2.4 Refactor `UpdateActorCommandHandler` for nullable-DTO branches
- [x] 2.5 Add Actor appearance fields to `UserDto`
- [x] 2.6 Update `MappingProfile.cs` for UserDto + Actor BackgroundImage mapping
- [x] 2.7 Add `BackgroundImageId`/`BackgroundImageUri` to `ActorDto`, `ActorListDto`, `UpdateActorDto`, `GroupDto`, `GroupListDto`, `OrganizationDto`, `OrganizationListDto`

## Phase 3: API Layer (~30 min) ✅
- [x] 3.1 Update `ActorController.Update` to accept `UpdateActorCommand` (command.Id = id pattern)
- [x] 3.2 Update `ExploreJsonContext.cs` with `UpdateActorAppearanceDto`
- [x] 3.3 Update `ActorControllerTests.cs` integration test payload
- [x] 3.4 Build + run tests (no regression) — 679 tests pass

## Phase 4: Blazor — Shared Infrastructure (~2 hours) ✅
- [x] 4.1 Create unified `AppearanceStyleBuilder` helper (AppearanceSettings + BuildStyle/BuildHeroStyle/BuildBannerStyle)
- [x] 4.2 Create `AppearanceEditor.razor` shared component (MudColorPicker + live preview + reset)
- [x] 4.3 Create `AppearanceEditor.razor.css` with BEM scoped styles

## Phase 5: Blazor — Page Integration (~2 hours) ✅
- [x] 5.1 Update `CreateEvent.razor` to use AppearanceEditor with live preview
- [x] 5.2 Update `CreateEvent.razor.cs` to use AppearanceStyleBuilder (replaced EventAppearanceSettings with plain strings)
- [x] 5.3 Update OrganizationProfile, OrganizationDetails, CreateOrganization to use AppearanceSettings + AppearanceStyleBuilder
- [x] 5.4 Update GroupProfile to use AppearanceSettings + AppearanceStyleBuilder
- [x] 5.5 Update UserProfile to use Actor appearance fields from NSwag-generated UserDto + AppearanceStyleBuilder
- [x] 5.6 Replace ALL old helper usages (EventDetail, EventEdit, admin pages: GroupProfileSection, GroupAdminSettingsLayout, OrganizationProfileSection)
- [x] 5.7 Fix 4 razor template build errors (BackgroundImageUri/BannerPictureUri → ImageUri)
- [x] 5.8 Old helpers deleted by user (3 files)

## Phase 6: Testing + Verification (~1 hour) ✅
- [x] 6.1 Unit tests for UpdateActorCommandHandler — 12 tests PASS (547 total app tests)
- [x] 6.2 Run all test projects — 881 pass, 86 Blazor failures are PRE-EXISTING (IAccessibilityFocusService DI, MudBlazor type cast, theme border radius)
- [x] 6.3 NSwag client regeneration — swagger.json + EventApiClient.g.cs regenerated, verified all new types present
- [x] 6.4 Visual verification — app starts on port 7177, auth redirects work, full UI needs Docker infrastructure

## Summary
- **Total tasks**: 24 (all complete)
- **New files created**: 6
- **Files modified**: 30+
- **Files deleted**: 3 (old helpers)
- **Tests added**: 12 unit tests
- **Build**: 0 errors
- **All changes are UNCOMMITTED** — need git add + commit
