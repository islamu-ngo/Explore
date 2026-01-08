# DBML Sync - Task Checklist

**Last Updated:** 2026-01-08

---

## ✅ PHASE 0: ANALYSIS (COMPLETE)

- [x] Analyze codebase patterns (Organization entity across all layers)
- [x] Document naming conventions, file structures
- [x] Identify DBML schema corrections needed
- [x] Resolve all blocking design decisions (8/8 complete)

---

## ✅ PHASE 1: DOMAIN LAYER (COMPLETE)

### Entity Creation/Updates
- [x] Tenant, TenantUser, TenantSettings
- [x] User, UserRole, UserAuthenticationToken, UserExternalLogin
- [x] Actor, ActorType, DidCustodyType, ActorKeyStore
- [x] Organization, OrganizationMember, OrganizationRole, OrganizationPosition
- [x] Event, EventSession, EventRegistration
- [x] EventType, EventStatus, VisibilityType, EventFormat, RegistrationMode
- [x] Madhab, AudienceAge, AudienceGender, Language
- [x] Category (with parent), Tag, TagType, TagTypeTags
- [x] EventCategories, EventTags, EventSessionLanguages, EventSessionSpeakers
- [x] EventSessionAgendaItem
- [x] Location
- [x] StorageObject, FileType
- [x] IndexedDid, SyncState, AtprotoRecord
- [x] OrganizationReview

### Entity Fixes Applied
- [x] Changed `long` to `int` (per CLAUDE.md) except size/cursor
- [x] Removed default values from entities
- [x] Added missing TenantId to Location, ActorKeyStore, UserAuthenticationToken, UserExternalLogin
- [x] Added `Members` navigation to Organization (readonly)

---

## ✅ PHASE 2: APPLICATION LAYER (PARTIAL - DTOs & Validators)

### DTOs Updated
- [x] EventDto, EventListDto, CreateEventDto, UpdateEventDto
- [x] DTOs now reference ActorId instead of OrganizationId

### Validators Updated
- [x] CreateEventDtoValidator - uses IActorRepository
- [x] UpdateEventDtoValidator - uses IActorRepository

### Repository Interfaces (45+ total)
- [x] All interfaces return ENTITIES (not DTOs)
- [x] Fixed ITagTypeTagsRepository generic type
- [x] Fixed IEventTagsRepository/IEventCategoriesRepository (Event not Program)
- [x] Fixed ITagTypeRepository key type (int not Guid)
- [x] Fixed IUserRepository method names
- [x] Fixed IOrganizationRepository parameter types
- [x] Fixed IStorageObjectRepository imports

### CQRS Handlers
- [ ] Update Event handlers to use new DTOs (if needed)
- [ ] Update Organization handlers (if needed)
- [ ] Verify AutoMapper profiles exist for new DTOs

---

## ✅ PHASE 3: PERSISTENCE LAYER (COMPLETE except Migrations)

### 3.1 DbContext ✅
- [x] All DbSets defined (45+ entities)
- [x] ApplyConfigurationsFromAssembly for auto-discovery
- [x] Removed obsolete Program/Education DbSets

### 3.2 Entity Configurations ✅ (39 configurations)

**Lookup Tables (with seed data):**
- [x] ApprovalStatusConfiguration
- [x] EventTypeConfiguration
- [x] AudienceAgeConfiguration
- [x] AudienceGenderConfiguration
- [x] MadhabConfiguration
- [x] LanguageConfiguration
- [x] EventStatusConfiguration
- [x] EventFormatConfiguration
- [x] VisibilityTypeConfiguration
- [x] RegistrationModeConfiguration
- [x] OrganizationRoleConfiguration
- [x] OrganizationPositionConfiguration
- [x] DidCustodyTypeConfiguration
- [x] ActorTypeConfiguration
- [x] FileTypeConfiguration
- [x] OwnerTypeConfiguration
- [x] UserRoleConfiguration
- [x] TagTypeConfiguration

**Entity Configurations:**
- [x] TenantConfiguration
- [x] TenantUserConfiguration
- [x] TenantSettingsConfiguration
- [x] UserConfiguration
- [x] UserAuthenticationTokenConfiguration
- [x] UserExternalLoginConfiguration
- [x] ActorConfiguration
- [x] ActorKeyStoreConfiguration
- [x] OrganizationConfiguration
- [x] OrganizationMemberConfiguration
- [x] EventConfiguration
- [x] EventSessionConfiguration
- [x] EventSessionAgendaItemConfiguration
- [x] EventSessionLanguageConfiguration
- [x] EventSessionSpeakerConfiguration
- [x] EventRegistrationConfiguration
- [x] CategoryConfiguration
- [x] TagConfiguration
- [x] TagTypeTagsConfiguration
- [x] EventCategoriesConfiguration
- [x] EventTagsConfiguration
- [x] LocationConfiguration
- [x] StorageObjectConfiguration
- [x] OrganizationReviewConfiguration
- [x] IndexedDidConfiguration
- [x] SyncStateConfiguration
- [x] AtprotoRecordConfiguration

### 3.3 Repositories ✅

**Fixed Interfaces:**
- [x] IEventRepository - returns Event entities
- [x] IOrganizationRepository - returns Organization entities
- [x] IUserRepository - GetUserWithDetails returns User?
- [x] IOrganizationMemberRepository - nullable returns
- [x] IStorageObjectRepository - proper imports
- [x] ITagTypeTagsRepository - correct generic type
- [x] IEventTagsRepository - Event not Program
- [x] IEventCategoriesRepository - Event not Program
- [x] ITagTypeRepository - int key not Guid

**New Implementations:**
- [x] TagRepository
- [x] TagTypeRepository
- [x] TagTypeTagsRepository
- [x] CategoryRepository
- [x] EventTagsRepository
- [x] EventCategoriesRepository

**Updated Implementations:**
- [x] EventRepository - proper includes for new entity
- [x] OrganizationRepository - returns entities
- [x] UserRepository - renamed methods
- [x] OrganizationMemberRepository - all interface methods
- [x] StorageObjectRepository - correct DbSet name

**DI Registration:**
- [x] All repositories registered in PersistenceServicesRegistration.cs

### 3.4 Migrations ⏳ NOT STARTED
- [ ] Generate migration to sync with DBML
- [ ] Verify migration is safe for existing data
- [ ] Test migration on local DB

---

## ⏳ PHASE 4: API LAYER (NOT STARTED)

### Controllers
- [ ] EventsController - verify CQRS mapping
- [ ] EventSessionsController
- [ ] EventRegistrationsController
- [ ] OrganizationsController
- [ ] TagsController / CategoriesController
- [ ] LocationsController

### Middleware
- [ ] Tenant resolution middleware
- [ ] Exception handling
- [ ] Auth/Authz integration

---

## ⏳ PHASE 5: CLEANUP (NOT STARTED)

### Obsolete Files to Delete (USER TASK)
```
Explore.Persistence/Repositories/
  - ProgramRepository.cs
  - EducationRepository.cs
  - EducationTypeRepository.cs

Explore.Application/Contracts/Persistence/
  - IProgramRepository.cs
  - IProgramRegistrationRepository.cs

Explore.Application/Features/
  - Programs/ (entire folder)
  - ProgramRegistration/ (entire folder)

Explore.Application/DTOs/
  - Program/ (entire folder)
  - Education/ (entire folder)
```

### Verification
- [ ] `dotnet build` succeeds
- [ ] All tests pass
- [ ] No schema mismatch warnings
- [ ] API endpoints work

---

## 🎯 QUICK RESUME CHECKLIST

When resuming after context reset:

1. **Read these files first:**
   - `dev/active/dbml-sync/dbml-sync-context.md`
   - `dev/active/dbml-sync/dbml-sync-tasks.md`

2. **User action required:**
   - Delete obsolete files listed above

3. **Next implementation steps:**
   - Run `dotnet build` to verify state
   - Generate migrations (Phase 3.4)
   - Update API controllers (Phase 4)

4. **Key decision to remember:**
   - Repositories return ENTITIES only
   - DTO mapping in Application handlers
   - Navigation properties on link tables are readonly
