# DBML Sync - Context

## Source of Truth (Database Schema)

The canonical DBML schema for this task is:

- `@schema/islamu-event.md`

Rules:
- DBML is authoritative over existing code and EF Core model unless a deviation is explicitly documented in this file under "Decision Log".
- Any change to entities, configurations, DTOs, validators, or endpoints must be traceable back to a DBML table/column/relationship.
- If Modification are needed in the DBML itself, document them here and get approval before proceeding to Update the schama.

## SESSION PROGRESS (2026-01-05)

**Last Updated:** 2026-01-05

### ✅ COMPLETED THIS SESSION

#### Domain Entity Fixes (int instead of long, added missing TenantId)
- [x] **TenantUser.cs** - Changed `Id` from `Guid` to `int`, `UserRoleId` from `long` to `int`
- [x] **TagTypeTags.cs** - Changed `Id` from `Guid` to `int`
- [x] **EventSessionSpeaker.cs** - Changed `Id` from `Guid` to `int`
- [x] **Location.cs** - Added `TenantId` and `Tenant` navigation property
- [x] **ActorKeyStore.cs** - Added `TenantId` and `Tenant` navigation property
- [x] **UserAuthenticationToken.cs** - Added `TenantId` and `Tenant` navigation property
- [x] **UserExternalLogin.cs** - Added `TenantId` and `Tenant` navigation property
- [x] **TenantSettings.cs** - Changed `Id` from `Guid` to `int`
- [x] **Tag.cs** - Removed default values (per CLAUDE.md rules)

#### Configuration Fixes
- [x] **OrganizationConfiguration.cs** - Fixed `Postcode` to be string "1070", added `TenantId` to seed data
- [x] **ApprovalStatusConfiguration.cs** - Added `MasterCode` to all seed data
- [x] **EventTypeConfiguration.cs** - Added `MasterCode` to all seed data, added `ValueGeneratedNever()`
- [x] **AudienceAgeConfiguration.cs** - Added `MasterCode` to all seed data
- [x] **AudienceGenderConfiguration.cs** - Added `MasterCode` to all seed data
- [x] **OrganizationMemberConfiguration.cs** - Removed uuidv7() (entity has int Id now)

#### New Configurations Created (39 total)
**Lookup Tables (ValueGeneratedNever + Seed Data):**
- [x] MadhabConfiguration - 5 madhabs with enums
- [x] LanguageConfiguration - 12 languages
- [x] EventStatusConfiguration - 5 statuses with enums
- [x] EventFormatConfiguration - 3 formats with enums
- [x] VisibilityTypeConfiguration - 4 visibility types with enums
- [x] RegistrationModeConfiguration - 4 modes with enums
- [x] OrganizationRoleConfiguration - 14 roles with enums
- [x] OrganizationPositionConfiguration - 4 positions
- [x] DidCustodyTypeConfiguration - 2 custody types with enums
- [x] ActorTypeConfiguration - 3 actor types with enums
- [x] FileTypeConfiguration - 5 file types with enums
- [x] OwnerTypeConfiguration - 3 owner types
- [x] UserRoleConfiguration

**Entity Configurations:**
- [x] TenantConfiguration, TenantUserConfiguration, TenantSettingsConfiguration
- [x] UserConfiguration, UserAuthenticationTokenConfiguration, UserExternalLoginConfiguration
- [x] ActorConfiguration, ActorKeyStoreConfiguration
- [x] EventSessionConfiguration, EventSessionAgendaItemConfiguration
- [x] EventSessionLanguageConfiguration, EventSessionSpeakerConfiguration
- [x] CategoryConfiguration, TagConfiguration, TagTypeConfiguration, TagTypeTagsConfiguration
- [x] EventCategoriesConfiguration, EventTagsConfiguration
- [x] AtprotoRecordConfiguration, IndexedDidConfiguration, SyncStateConfiguration
- [x] LocationConfiguration

#### DbContext Cleanup
- [x] Removed obsolete commented code from OnModelCreating
- [x] Removed reference to non-existent ProgramRegistrationConfiguration
- [x] Using ApplyConfigurationsFromAssembly for auto-discovery

#### DBML Schema Updated
- [x] Updated `schema/islamu-event.md` - Changed all `bigint` to `int` except for:
  - `storage_object.size` (file sizes can be large)
  - `sync_state.cursor` (ATProto sequence numbers)
- [x] Fixed `organization_members.organization_position` to `organization_position_id`
- [x] Fixed `user_external_login.prover_display_name` to `provider_display_name`
- [x] Added missing tenant refs in tenant table definition

#### Application Layer Updates (Session 2)
**Event DTOs Updated:**
- [x] EventListDto - Updated to use ActorId instead of OrganizationId, added new fields
- [x] EventDto - Updated with all new domain fields (EventStatus, EventFormat, VisibilityType, Madhab, ATProto)
- [x] CreateEventDto - Updated to match new Event entity structure
- [x] UpdateEventDto - Updated to match new Event entity structure

**Event Validators Updated:**
- [x] CreateEventDtoValidator - Fixed to use private fields, replaced IOrganizationRepository with IActorRepository
- [x] UpdateEventDtoValidator - Fixed to use private fields, replaced IOrganizationRepository with IActorRepository

**New Repository Interfaces Created (22 total):**
- [x] IEventStatusRepository, IEventFormatRepository, IVisibilityTypeRepository, IRegistrationModeRepository
- [x] IMadhabRepository, ILanguageRepository
- [x] IOrganizationRoleRepository, IOrganizationPositionRepository
- [x] IActorRepository, IActorTypeRepository, IDidCustodyTypeRepository, IActorKeyStoreRepository
- [x] IFileTypeRepository
- [x] ITenantRepository, ITenantUserRepository, ITenantSettingsRepository, IUserRoleRepository
- [x] IEventSessionRepository, IEventRegistrationRepository, IEventSessionAgendaItemRepository
- [x] IEventSessionLanguageRepository, IEventSessionSpeakerRepository
- [x] ILocationRepository
- [x] IAtprotoRecordRepository, IIndexedDidRepository, ISyncStateRepository
- [x] IUserAuthenticationTokenRepository, IUserExternalLoginRepository

### 🟡 IN PROGRESS
- None currently

### ⏳ NEXT STEPS
2. **Phase 3.3** - Repository implementations in Persistence layer
3. **Phase 3.4** - Migrations

---

## Goal (One Sentence)
Make the codebase match the provided DBML schema across Domain, Application, Persistence, and API layers using your repo documentation conventions.

## Non-Goals
- Implementing moderation system (not in DBML yet).
- Implementing ActivityPub gateway behavior (unless required to compile or existing API endpoints break).
- UI work (Blazor) unless your API layer depends on it.

### 🎯 NEXT IMMEDIATE STEPS (After Context Reset)

## Key Rules Applied This Session

1. **Use `int` instead of `long`** - Per CLAUDE.md rules, unless absolutely necessary (size, cursor)
2. **No default values in Domain entities** - Per CLAUDE.md rules, defaults go in configurations
3. **TenantId required** - All tenant-scoped entities must have TenantId FK

---

## ✅ RESOLVED DECISIONS (from previous session)

1. **atproto_record field types**: ✅ RESOLVED
   - **DECISION**: Use varchar (255/500) for did/record_key/cid
   - **Rationale**: ATProto DIDs and CIDs are strings, not UUIDs per ATProto spec

2. **Geo modeling for location**: ✅ RESOLVED
   - **DECISION**: Use lat/long doubles (PostGIS point can be added later)
   - Primary: `Latitude`/`Longitude` (doubles)
   - **Rationale**: Simple distance calculations, PostGIS can be added later

3. **Tenant enforcement strategy**: ✅ RESOLVED
   - **DECISION**: Multi-layered approach
     1. Persistence Layer: Global query filters on `tenant_id`
     2. Repository Layer: Tenant-scoped method signatures where applicable
     3. Middleware: Tenant resolution from subdomain/header
     4. Application Layer: Commands include tenant context

4. **Join table modeling strategy**: ✅ RESOLVED
   - **DECISION**: Explicit entities for all join tables with tenant_id
   - EventCategories, EventTags, EventSessionLanguage, EventSessionSpeaker, TagTypeTags, OrganizationMember, TenantUser

5. **Delete behaviors**: ✅ RESOLVED
   - Tenant → children: Cascade (TenantUser, TenantSettings)
   - Parent → children: Cascade (Actor → ActorKeyStore, User → tokens/logins)
   - Cross-aggregate: Restrict (Tenant FK on most entities)

6. **Repository return types**: ✅ RESOLVED
   - Query methods: Return DTOs
   - Command methods: Work with entities

---

## Key Files Modified This Session

**Domain Layer:**
- `Explore.Domain/TenantUser.cs`
- `Explore.Domain/TagTypeTags.cs`
- `Explore.Domain/EventSessionSpeaker.cs`
- `Explore.Domain/Location.cs`
- `Explore.Domain/ActorKeyStore.cs`
- `Explore.Domain/UserAuthenticationToken.cs`
- `Explore.Domain/UserExternalLogin.cs`
- `Explore.Domain/TenantSettings.cs`

**Persistence Layer:**
- `Explore.Persistence/Configurations/Entities/OrganizationConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/ApprovalStatusConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/EventTypeConfiguration.cs`
- `Explore.Persistence/Configurations/Entities/LocationConfiguration.cs` (NEW)
- `Explore.Persistence/Configurations/Entities/ActorKeyStoreConfiguration.cs` (NEW)
- `Explore.Persistence/Configurations/Entities/UserAuthenticationTokenConfiguration.cs` (NEW)
- `Explore.Persistence/Configurations/Entities/UserExternalLoginConfiguration.cs` (NEW)
- `Explore.Persistence/Configurations/Entities/TenantUserConfiguration.cs` (NEW)
- `Explore.Persistence/Configurations/Entities/TenantSettingsConfiguration.cs` (NEW)
- `Explore.Persistence/Configurations/Entities/TenantConfiguration.cs` (NEW)

**Schema:**
- `schema/islamu-event.md` - Updated bigint → int

---

## Quick Resume (do this next)
1. Run `dotnet build` to verify compilation
2. Check remaining domain entities for `int` vs `long` consistency
3. Create remaining entity configurations
4. Proceed to Phase 2 (Application layer)

---

## Notes / Guardrails
- No EF Core attributes in Domain (persistence-ignorant entities) - BUT we do use `[ForeignKey]` attribute
- All writes in API go through MediatR commands
- All reads in API go through MediatR queries
- Validate at Application boundary (FluentValidation)
- Keep dev docs updated (SESSION PROGRESS + decisions)
