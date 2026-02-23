# Enterprise Cleanup - Task Checklist

## Phase 1: Namespace & Style Cleanup — All Layers ✅ COMPLETE

### 1.1 Convert ALL files to file-scoped namespaces

**Domain (~35 files)**
- [ ] `Organization.cs` — block-scoped → file-scoped
- [ ] `Event.cs` — block-scoped → file-scoped
- [ ] `Actor.cs` — block-scoped → file-scoped
- [ ] `ActorKeyStore.cs` — block-scoped → file-scoped
- [ ] `ActorType.cs` — block-scoped → file-scoped
- [ ] `ApprovalStatus.cs` — block-scoped → file-scoped
- [ ] `AtprotoRecord.cs` — block-scoped → file-scoped
- [ ] `AudienceAge.cs` — block-scoped → file-scoped
- [ ] `AudienceGender.cs` — block-scoped → file-scoped
- [ ] `Category.cs` — block-scoped → file-scoped
- [ ] `DidCustodyType.cs` — block-scoped → file-scoped
- [ ] `EventCategories.cs` — block-scoped → file-scoped
- [ ] `EventFormat.cs` — block-scoped → file-scoped
- [ ] `EventRegistration.cs` — block-scoped → file-scoped
- [ ] `EventSession.cs` — block-scoped → file-scoped
- [ ] `EventSessionAgendaItem.cs` — block-scoped → file-scoped
- [ ] `EventSessionLanguage.cs` — block-scoped → file-scoped
- [ ] `EventSessionSpeaker.cs` — block-scoped → file-scoped
- [ ] `EventStatus.cs` — block-scoped → file-scoped
- [ ] `EventTags.cs` — block-scoped → file-scoped
- [ ] `EventType.cs` — block-scoped → file-scoped
- [ ] `FileType.cs` — block-scoped → file-scoped
- [ ] `IndexedDid.cs` — block-scoped → file-scoped
- [ ] `Language.cs` — block-scoped → file-scoped
- [ ] `Location.cs` — block-scoped → file-scoped
- [ ] `Madhab.cs` — block-scoped → file-scoped
- [ ] `OrganizationMember.cs` — block-scoped → file-scoped
- [ ] `OrganizationPosition.cs` — block-scoped → file-scoped
- [ ] `OrganizationReview.cs` — block-scoped → file-scoped
- [ ] `OrganizationRole.cs` — block-scoped → file-scoped
- [ ] `OwnerType.cs` — block-scoped → file-scoped
- [ ] `RegistrationMode.cs` — block-scoped → file-scoped
- [ ] `Role.cs` — block-scoped → file-scoped
- [ ] `StorageObject.cs` — block-scoped → file-scoped
- [ ] `SyncState.cs` — block-scoped → file-scoped
- [ ] `Tag.cs` — block-scoped → file-scoped
- [ ] `TagType.cs` — block-scoped → file-scoped
- [ ] `TagTypeTags.cs` — block-scoped → file-scoped
- [ ] `Tenant.cs` — block-scoped → file-scoped
- [ ] `TenantSettings.cs` — block-scoped → file-scoped
- [ ] `TenantUser.cs` — block-scoped → file-scoped
- [ ] `User.cs` — block-scoped → file-scoped
- [ ] `UserAuthenticationToken.cs` — block-scoped → file-scoped (verify — may already be file-scoped)
- [ ] `UserExternalLogin.cs` — block-scoped → file-scoped (verify — may already be file-scoped)
- [ ] `UserRole.cs` — block-scoped → file-scoped (verify — may already be file-scoped)
- [ ] Build and verify domain compilation

**API Controllers (~31 files)**
- [ ] All controllers in `Explore.API/Controllers/` using block-scoped → file-scoped
- [ ] Build and verify API compilation

**Application Layer**
- [ ] `Responses/BaseCommandResponse.cs` — block-scoped → file-scoped
- [ ] `Exceptions/NotFoundException.cs` — block-scoped → file-scoped
- [ ] All DTOs with block-scoped namespaces → file-scoped
- [ ] All Feature files with block-scoped namespaces → file-scoped
- [ ] Build and verify Application compilation

**Persistence Layer**
- [ ] `ExploreDbContext.cs` — block-scoped → file-scoped
- [ ] `Repositories/EventRepository.cs` — block-scoped → file-scoped
- [ ] `Repositories/GenericRepository.cs` — block-scoped → file-scoped
- [ ] All Configuration files with block-scoped namespaces → file-scoped
- [ ] Build and verify Persistence compilation

- [ ] **Final: Full solution build passes with zero block-scoped namespaces**

### 1.2 Remove default values from domain entities
- [ ] `AppSetting.cs` — remove `= string.Empty` from ConfigKey, EncryptedValue
- [ ] `SystemSetting.cs` — remove `= string.Empty` from SettingKey, Value
- [ ] `TenantSetting.cs` — remove `= string.Empty` from SettingKey, Value
- [ ] `TenantAdministratorRole.cs` — remove `= string.Empty` from FullName, MasterCode
- [ ] `Federation/PdsSyncOutbox.cs` — remove `= string.Empty` from Did, Collection, RecordKey
- [ ] `Modules/ModuleDefinition.cs` — remove `= string.Empty` from ModuleKey, Name
- [ ] Verify handlers/mappers properly set these values (check Create/Update handlers)
- [ ] Build and verify no nullable warnings introduced

### 1.3 Standardize navigation property patterns
- [ ] `Actor.cs` — remove `= null!` from ActorType, Tenant
- [ ] `ActorKeyStore.cs` — remove `= null!` from Actor, Tenant
- [ ] `InstanceAdministrator.cs` — remove `= null!` from User
- [ ] `OrganizationReview.cs` — remove `= null!` from Event, User
- [ ] `TenantAdministrator.cs` — remove `= null!` from User, Tenant, TenantAdministratorRole
- [ ] `TenantOnboardingState.cs` — remove `= null!` from Tenant
- [ ] `UserAuthenticationToken.cs` — remove `= null!` from User, Tenant
- [ ] `UserRole.cs` — remove `= null!` from Tenant
- [ ] Make navigation properties nullable (`?`) where not already
- [ ] Build and verify

### 1.4 Fix Organization entity
- [ ] Convert to file-scoped namespace (covered in 1.1)
- [ ] Add proper nullable annotations to FullName, Email, Country, City, Address, Postcode
- [ ] Verify Organization-related handlers/DTOs handle nullable correctly
- [ ] Build and verify

### 1.5 Add missing IAuditableEntity / ISoftDeletable interfaces
**Entities needing IAuditableEntity (missing interface entirely):**
- [ ] `EventRegistration.cs` — add IAuditableEntity + audit properties if missing
- [ ] `EventCategories.cs` — add IAuditableEntity + audit properties if missing
- [ ] `EventTags.cs` — add IAuditableEntity + audit properties if missing
- [ ] `StorageObject.cs` — add IAuditableEntity + audit properties if missing
- [ ] `UserAuthenticationToken.cs` — add IAuditableEntity + audit properties if missing
- [ ] `UserExternalLogin.cs` — add IAuditableEntity + audit properties if missing
- [ ] `SystemSetting.cs` — has CreatedAt/UpdatedAt, add IAuditableEntity interface
- [ ] `TenantSetting.cs` — add IAuditableEntity interface
- [ ] `Modules/TenantCapability.cs` — add IAuditableEntity + audit properties if missing

**Entities with audit fields but missing interface declaration:**
- [ ] `OrganizationReview.cs` — has CreatedAt/UpdatedAt, add IAuditableEntity interface
- [ ] `TenantAdministrator.cs` — has GrantedAt/GrantedBy, decide if IAuditableEntity applies
- [ ] `InstanceAdministrator.cs` — has GrantedAt/GrantedBy, decide if IAuditableEntity applies
- [ ] `TenantOnboardingState.cs` — has CreatedAt only, decide completeness

- [ ] **NOTE**: Check if adding properties requires a new EF migration
- [ ] Build and verify

---

## Phase 2: CQRS Pattern Standardization ⏳ NOT STARTED

### 2.1 Restructure OrganizationReviews to standard pattern
- [ ] Create `Features/OrganizationReviews/Requests/Commands/` directory
- [ ] Create `Features/OrganizationReviews/Requests/Queries/` directory
- [ ] Create `Features/OrganizationReviews/Handlers/Commands/` directory
- [ ] Create `Features/OrganizationReviews/Handlers/Queries/` directory
- [ ] Move `CreateOrganizationReviewCommand.cs` → `Requests/Commands/`
- [ ] Move `CreateOrganizationReviewCommandHandler.cs` → `Handlers/Commands/`
- [ ] Move query files → `Requests/Queries/` and `Handlers/Queries/`
- [ ] Update namespaces in all moved files
- [ ] Delete old empty directories
- [ ] Build and verify MediatR still resolves handlers

### 2.2 Rename OrganizationReview query classes
- [ ] `GetMyReviewsQuery` → `GetMyReviewsRequest`
- [ ] `GetMyReviewsQueryHandler` → `GetMyReviewsRequestHandler`
- [ ] `GetOrganizationReviewsQuery` → `GetOrganizationReviewsRequest`
- [ ] `GetOrganizationReviewsQueryHandler` → `GetOrganizationReviewsRequestHandler`
- [ ] Update OrganizationReviewController references
- [ ] Update any test references
- [ ] Build and verify

### 2.3 Convert delete commands to BaseCommandResponse
**Batch A: Core entities**
- [ ] `DeleteEventCommand` + handler + controller
- [ ] `DeleteCategoryCommand` + handler + controller
- [ ] `DeleteTagCommand` + handler + controller
- [ ] `DeleteLocationCommand` + handler + controller
- [ ] `DeleteStorageObjectCommand` + handler + controller
- [ ] `DeleteActorCommand` + handler + controller

**Batch B: Event sub-entities**
- [ ] `DeleteEventSessionCommand` + handler + controller
- [ ] `DeleteEventSessionAgendaItemCommand` + handler + controller
- [ ] `DeleteEventRegistrationCommand` + handler + controller
- [ ] `DeleteEventCategoriesCommand` + handler + controller
- [ ] `DeleteEventTagsCommand` + handler + controller
- [ ] `DeleteEventSessionLanguageCommand` + handler + controller
- [ ] `DeleteEventSessionSpeakerCommand` + handler + controller

**Batch C: User/Tenant entities**
- [ ] `DeleteTenantCommand` + handler + controller
- [ ] `DeleteTenantUserCommand` + handler + controller
- [ ] `DeleteTenantSettingsCommand` + handler + controller
- [ ] `DeleteUserExternalLoginCommand` + handler + controller
- [ ] `DeleteUserAuthenticationTokenCommand` + handler + controller
- [ ] `DeleteUserRoleCommand` (if exists)

**Batch D: Remaining entities**
- [ ] `DeleteActorKeyStoreCommand` + handler + controller
- [ ] `DeleteAtprotoRecordCommand` + handler + controller
- [ ] `DeleteIndexedDidCommand` + handler + controller
- [ ] `DeleteSyncStateCommand` + handler + controller
- [ ] `DeleteTagTypeTagsCommand` + handler + controller
- [ ] `DeleteEventIslamicAspectCommand` + handler + controller
- [ ] `DeleteEventTechAspectCommand` + handler + controller

- [ ] Build and test after each batch

### 2.4 Fix validators injected via DI (convert to manual instantiation)
- [ ] `UpdateActorKeyStoreCommandHandler.cs` — remove IValidator<> from constructor, use `new UpdateActorKeyStoreDtoValidator(...)` in Handle()
- [ ] `CreateTenantCommandHandler.cs` — remove IValidator<> from constructor, use `new CreateTenantDtoValidator(...)` in Handle()
- [ ] `UpdateTenantCommandHandler.cs` — remove IValidator<> from constructor, use `new UpdateTenantDtoValidator(...)` in Handle()
- [ ] `UpdateStorageObjectCommandHandler.cs` — remove IValidator<> from constructor, use `new UpdateStorageObjectDtoValidator(...)` in Handle()
- [ ] Remove any FluentValidation DI registration for these validators if exists
- [ ] Build and verify

### 2.5 Add missing validation to command handlers
- [ ] `CreateOrganizationCommandHandler.cs` — create `CreateOrganizationDtoValidator` if missing, add validation before entity creation
- [ ] `UpdateOrganizationDetailsCommandHandler.cs` — create `UpdateOrganizationDtoValidator` if missing, add validation
- [ ] `UpdateUserCommandHandler.cs` — create `UpdateUserDtoValidator` if missing, add validation
- [ ] `CreateOrganizationReviewCommandHandler.cs` — create validator, add validation
- [ ] Build and test

---

## Phase 3: API Controller Cleanup ⏳ NOT STARTED

### 3.1 Add missing auth attributes
- [ ] Audit all 43 controllers for auth attribute completeness
- [ ] Add `[AllowAnonymous]` to GET endpoints missing it
- [ ] Add `[Authorize]` to POST/PUT/DELETE endpoints missing it
- [ ] Verify lookup-only controllers (no write endpoints) only need `[AllowAnonymous]`
- [ ] Build and verify

### 3.2 Add missing OpenAPI metadata
- [ ] Add `[Consumes("application/json")]` to all POST/PUT endpoints (~30+ controllers)
- [ ] Add `[EndpointDescription]` to endpoints missing it
- [ ] Verify `[ProducesResponseType]` coverage on all endpoints
- [ ] Regenerate swagger.json
- [ ] Verify Scalar UI shows complete descriptions

### 3.3 Standardize controller response patterns
- [ ] Verify all POST endpoints: `response.Success ? Ok(response) : BadRequest(response)`
- [ ] Verify all PUT endpoints: check ID mismatch pattern
- [ ] Verify all DELETE endpoints: `NoContent()` / `NotFound()` after Phase 2.3
- [ ] Build and verify

### 3.4 Fix userId extraction fallback pattern
- [ ] `UserController.cs` — update 5 locations to use 3-claim fallback (sub → nameidentifier → sid)
- [ ] `OrganizationMemberController.cs` — update 5 locations to use 3-claim fallback
- [ ] Search for any other controllers with incomplete userId extraction
- [ ] Build and verify

### 3.5 Replace Console.WriteLine with ILogger
- [ ] `UserController.cs` — replace 4 Console.WriteLine calls with ILogger
- [ ] Inject `ILogger<UserController>` if not already present
- [ ] Search for Console.WriteLine in any other controllers
- [ ] Build and verify

### 3.6 Fix ApprovalStatusController dead stub code
- [ ] Decide: implement `GetById()`, `Post()`, `Put()` properly OR remove stub methods
- [ ] If implementing: use MediatR pattern consistent with other lookup controllers
- [ ] If removing: verify no routes depend on these endpoints
- [ ] Build and verify

---

## Phase 4: Persistence & Configuration Cleanup ⏳ NOT STARTED

### 4.1 Fix CongfigurePersistenceServices typo
- [ ] Rename method in `PersistenceServicesRegistration.cs`
- [ ] Update call in `Explore.API/Program.cs`
- [ ] Search for any other references (docs, comments)
- [ ] Update docs/NAMING_CONVENTIONS.md, docs/CODEBASE_INSIGHTS.md references
- [ ] Build and verify

### 4.2 Add missing DbSets to ExploreDbContext
- [ ] Add `DbSet<ModuleDefinition>` to ExploreDbContext
- [ ] Add `DbSet<OwnerType>` to ExploreDbContext
- [ ] Add `DbSet<Role>` to ExploreDbContext
- [ ] Add `DbSet<TenantCapability>` to ExploreDbContext
- [ ] Build and verify

### 4.3 Create missing entity configurations
- [ ] Create `ModuleDefinitionConfiguration.cs` (check if one exists already in Configurations/Entities/)
- [ ] Create `OwnerTypeConfiguration.cs`
- [ ] Create `RoleConfiguration.cs`
- [ ] Add proper query filters (Tenant + SoftDelete) where applicable
- [ ] Build and verify

### 4.4 Add missing query filters
- [ ] Add tenant + soft delete filter for `EventSessionSpeaker` in ExploreDbContext
- [ ] Audit all ITenantEntity implementations have tenant filter
- [ ] Audit all ISoftDeletable implementations have soft delete filter
- [ ] Build and verify

### 4.5 Standardize EF configuration patterns
- [ ] Audit all entity configurations for consistent structure
- [ ] Verify named query filters ("Tenant", "SoftDelete") on all tenant entities
- [ ] Check for duplicate seeding (HasData + DatabaseSeeder)
- [ ] Document standard configuration template in docs
- [ ] Build and verify

### 4.6 Standardize ForeignKey usage
- [ ] Audit all entities for redundant `[ForeignKey]` attributes (already defined in Fluent API)
- [ ] Apply consistent convention (Fluent API in configurations, remove entity-level attributes)
- [ ] Build and verify no FK changes in migration diff

---

## Phase 5: Blazor Client Service Standardization ⏳ NOT STARTED

### 5.1 Consolidate service interface locations
- [ ] Decide: ALL interfaces in `Services/Contracts/` folder (recommended)
- [ ] Move inline interfaces from: EventService, OrganizationService, UserService, CategoryService, LocationService, TagService, and others
- [ ] Verify all DI registrations still resolve correctly
- [ ] Build and verify

### 5.2 Remove BaseCommandResponse duplication
- [ ] Check if NSwag-generated client provides BaseCommandResponse type
- [ ] If yes: remove `Blazor.Client/Models/Responses/BaseCommandResponse.cs`, use generated type
- [ ] If no: keep but document why duplication is necessary
- [ ] Update all service references to use the canonical type
- [ ] Build and verify

### 5.3 Standardize service error handling
- [ ] Define standard pattern: `ServiceResult<T>` for writes, `T?` for reads
- [ ] Migrate EventService (swallows exceptions → ServiceResult)
- [ ] Migrate OrganizationService (re-throws ApiException → ServiceResult)
- [ ] Migrate EventRegistrationService (already returns response objects — align)
- [ ] Migrate AdminService (returns bool → ServiceResult)
- [ ] Migrate LandingPageService (hardcoded fallbacks → ServiceResult)
- [ ] Migrate remaining ~15 services
- [ ] Update all page code-behind files to handle ServiceResult
- [ ] Build and verify

### 5.4 Ensure all pages have code-behind files
- [ ] `EventCreated.razor` — extract inline @code to .razor.cs if needed
- [ ] `EventEdit.razor` — extract inline @code to .razor.cs if needed
- [ ] Audit all other pages for inline @code blocks
- [ ] Build and verify

### 5.5 Standardize page loading/error patterns
- [ ] Define standard: isLoading flag + skeleton, MudAlert for errors, empty state component
- [ ] Audit all pages for loading state consistency
- [ ] Audit all pages for error display consistency
- [ ] Audit all pages for empty state handling
- [ ] Build and verify

---

## Phase 6: ABOUTME Comments & Documentation ⏳ NOT STARTED

### 6.1 Add ABOUTME comments
- [ ] Domain layer entities (~35 files missing ABOUTME)
- [ ] Domain interfaces, enums, constants
- [ ] Application DTOs
- [ ] Application Features (Requests + Handlers)
- [ ] Application Contracts
- [ ] Persistence Repositories
- [ ] Persistence Configurations
- [ ] API Controllers
- [ ] Blazor Client Services
- [ ] Blazor Client Pages
- [ ] Test files
- [ ] Verify 100% coverage

### 6.2 Update documentation after cleanup
- [ ] Update CODEBASE_STRUCTURE.md if any files moved/renamed
- [ ] Update NAMING_CONVENTIONS.md if conventions clarified
- [ ] Update CODEBASE_INSIGHTS.md for resolved inconsistencies
- [ ] Update TROUBLESHOOTING.md with any new gotchas

---

## Phase 7: Test Coverage & Validation ⏳ NOT STARTED

### 7.1 Run full test suite
- [ ] `Event.Application.UnitTests` — passes
- [ ] `Event.Domain.UnitTests` — passes
- [ ] `Event.Architecture.Tests` — passes
- [ ] `Explore.Secrets.UnitTests` — passes
- [ ] `Event.Persistence.IntegrationTests` — passes
- [ ] `Event.API.IntegrationTests` — passes
- [ ] `Explore.Blazor.Client.Tests` — passes
- [ ] Fix any regressions from cleanup changes

### 7.2 Architecture test validation
- [ ] All clean architecture dependency rules pass
- [ ] CQRS naming convention tests pass
- [ ] Verify no new architecture violations introduced

### 7.3 Format and finalize
- [ ] Run `dotnet format` across solution
- [ ] Verify no formatting diff remains
- [ ] Final build in Release mode
- [ ] Commit all changes with clear commit messages

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 1. Namespace, Style & Interface Cleanup | 5 tasks, ~100+ files (all layers) | ⏳ Not Started |
| 2. CQRS Standardization | 5 tasks, ~70 files | ⏳ Not Started |
| 3. API Controllers | 6 tasks, ~43 files | ⏳ Not Started |
| 4. Persistence & Config | 6 tasks, ~25 files | ⏳ Not Started |
| 5. Blazor Services & Pages | 5 tasks, ~50 files | ⏳ Not Started |
| 6. Documentation | 2 tasks, ~300 files | ⏳ Not Started |
| 7. Validation | 3 tasks | ⏳ Not Started |
| **Total** | **32 tasks** | **⏳ Not Started** |
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
