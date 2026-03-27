ABOUTME: Task checklist for background customization feature implementation.
ABOUTME: Tracks progress across 6 phases from domain changes through Blazor UI integration.

# Background Customization — Task Checklist

## Phase 1: Domain + Persistence (~1 hour)
- [ ] 1.1 Add `BackgroundImageId` + `BackgroundImage` nav property to `Actor.cs`
- [ ] 1.2 Update `ActorConfiguration.cs` with BackgroundImage FK (SetNull delete)
- [ ] 1.3 Generate EF migration for Actor.BackgroundImageId

## Phase 2: Application Layer — DTOs + Mapping (~1.5 hours)
- [ ] 2.1 Create `UpdateActorAppearanceDto` in `DTOs/Actor/`
- [ ] 2.2 Create `UpdateActorAppearanceDtoValidator` in `DTOs/Actor/Validators/`
- [ ] 2.3 Refactor `UpdateActorCommand` to nullable-DTO pattern (+ `Guid Id`)
- [ ] 2.4 Refactor `UpdateActorCommandHandler` for nullable-DTO branches
- [ ] 2.5 Add Actor appearance fields to `UserDto`
- [ ] 2.6 Update `MappingProfile.cs` for UserDto + Actor BackgroundImage mapping
- [ ] 2.7 Add `BackgroundImageId`/`BackgroundImageUri` to `ActorDto` + `UpdateActorDto`

## Phase 3: API Layer (~30 min)
- [ ] 3.1 Update `ActorController.Update` to accept `UpdateActorCommand`
- [ ] 3.2 Build + run tests (no regression)

## Phase 4: Blazor — Shared Infrastructure (~2 hours)
- [ ] 4.1 Create unified `AppearanceStyleBuilder` helper
- [ ] 4.2 Create `AppearanceEditor.razor` shared component (MudColorPicker + live preview + reset)
- [ ] 4.3 Create `AppearanceEditor.razor.css` with BEM scoped styles

## Phase 5: Blazor — Page Integration (~2 hours)
- [ ] 5.1 Update `CreateEvent.razor` to use AppearanceEditor with live preview
- [ ] 5.2 Update `CreateEvent.razor.cs` to use AppearanceStyleBuilder
- [ ] 5.3 Update OrganizationProfile pages for appearance editing
- [ ] 5.4 Update GroupProfile pages for appearance editing
- [ ] 5.5 Update UserProfile page for appearance editing
- [ ] 5.6 Replace old helpers with AppearanceStyleBuilder, delete old files

## Phase 6: Testing + Verification (~1 hour)
- [ ] 6.1 Unit tests for UpdateActorCommandHandler (appearance branch)
- [ ] 6.2 Run all test projects — all green
- [ ] 6.3 NSwag client regeneration
- [ ] 6.4 Visual verification via Playwriter (CreateEvent + profiles)
