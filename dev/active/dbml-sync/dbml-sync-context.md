# DBML Sync - Context

## Source of Truth (Database Schema)

The canonical DBML schema for this task is:

- `@schema/islamu-event.md`

Rules:
- DBML is authoritative over existing code and EF Core model unless a deviation is explicitly documented in this file under "Decision Log".
- Any change to entities, configurations, DTOs, validators, or endpoints must be traceable back to a DBML table/column/relationship.
- If Modification are needed in the DBML itself, document them here and get approval before proceeding to Update the schema.

---

## SESSION PROGRESS (2026-01-08)

**Last Updated:** 2026-01-08 (Current Session)

### ✅ COMPLETED THIS SESSION (2026-01-08)

#### Repository Pattern Enforcement (CRITICAL DECISION)
**ALL repositories now return ENTITIES, not DTOs. DTO mapping happens in Application layer handlers.**

**Interfaces Fixed:**
- [x] `IEventRepository.cs` - Changed to return `Event` entities, removed DTO imports
- [x] `IOrganizationRepository.cs` - Changed to return `Organization` entities, changed `GetMyOrganizations(string)` to `GetMyOrganizations(Guid)`
- [x] `IUserRepository.cs` - Changed `GetByIdDto` to `GetUserWithDetails` returning `User?`
- [x] `IOrganizationMemberRepository.cs` - Fixed nullable return type on `GetOrganizationMemberWithDetails`
- [x] `IStorageObjectRepository.cs` - Fixed import to use `Explore.Domain`, nullable return type
- [x] `ITagTypeTagsRepository.cs` - Fixed generic type from `<TagType, Guid>` to `<TagTypeTags, Guid>`
- [x] `IEventTagsRepository.cs` - Fixed to use `Event` instead of obsolete `Program`
- [x] `IEventCategoriesRepository.cs` - Fixed to use `Event` instead of obsolete `Program`
- [x] `ITagTypeRepository.cs` - Fixed key type from `Guid` to `int`

**Implementations Updated:**
- [x] `EventRepository.cs` - Returns entities with proper includes for new Event structure (Actor, EventStatus, EventFormat, VisibilityType, Madhab, AtprotoRecord)
- [x] `OrganizationRepository.cs` - Returns entities, uses `Members` navigation
- [x] `UserRepository.cs` - Method renamed, includes `Actor`
- [x] `OrganizationMemberRepository.cs` - Fixed missing interface methods, removed invalid `Email` property reference, added all includes
- [x] `StorageObjectRepository.cs` - Fixed to use `StorageObjects` DbSet (not `Files`), added `FileType` and `Tenant` includes

**New Implementations Created:**
- [x] `TagRepository.cs`
- [x] `TagTypeRepository.cs`
- [x] `TagTypeTagsRepository.cs`
- [x] `CategoryRepository.cs`
- [x] `EventTagsRepository.cs`
- [x] `EventCategoriesRepository.cs`

#### Domain Entity Fixes
- [x] `Organization.cs` - Added `Members` navigation property (`ICollection<OrganizationMember>`)
  - **IMPORTANT**: This is readonly for queries only. Never add members through this navigation. Use `OrganizationMemberRepository` for writes.

#### DI Registration
- [x] `PersistenceServicesRegistration.cs` - Added 6 new Tag & Category repositories

---

### ⚠️ OBSOLETE CODE TO DELETE (User Task)

The following files reference obsolete `Program`/`Education` entities (TPH inheritance removed):

**Persistence Layer:**
- `Explore.Persistence/Repositories/ProgramRepository.cs`
- `Explore.Persistence/Repositories/EducationRepository.cs`
- `Explore.Persistence/Repositories/EducationTypeRepository.cs`

**Application Layer:**
- `Explore.Application/Contracts/Persistence/IProgramRepository.cs`
- `Explore.Application/Contracts/Persistence/IProgramRegistrationRepository.cs`
- `Explore.Application/Features/Programs/` (entire folder)
- `Explore.Application/Features/ProgramRegistration/` (entire folder)
- `Explore.Application/DTOs/Program/` (entire folder)
- `Explore.Application/DTOs/Education/` (entire folder)

---

### ✅ COMPLETED PREVIOUS SESSIONS

#### Phase 1: Domain Layer ✅ COMPLETE
- All entities created/updated to match DBML
- `int` used instead of `long` (per CLAUDE.md)
- No default values in entities (per CLAUDE.md)
- TenantId added to all tenant-scoped entities

#### Phase 3.1-3.2: Persistence Layer ✅ COMPLETE
- DbContext with all DbSets
- 39 entity configurations with seed data
- All lookup tables have `ValueGeneratedNever()` and seed data

#### Phase 3.3: Repositories ✅ COMPLETE
- All interfaces return entities
- All implementations use proper includes
- DI registration complete

---

## 🎯 NEXT IMMEDIATE STEPS (After Context Reset)

1. **User: Delete obsolete files** (listed above)
2. **Run `dotnet build`** to verify compilation
3. **Phase 3.4: Migrations** - Generate/adjust migrations to match DBML
4. **Phase 4: API Layer** - Update controllers to use CQRS properly

---

## ✅ RESOLVED DESIGN DECISIONS

| # | Decision | Resolution | Rationale |
|---|----------|------------|-----------|
| 1 | atproto_record field types | `varchar(255/500)` for did/record_key/cid | ATProto spec uses strings |
| 2 | Location geo modeling | `Latitude`/`Longitude` doubles | Simple; PostGIS later |
| 3 | Tenant enforcement | Multi-layered (filters + repo + middleware) | Defense in depth |
| 4 | Join table modeling | Explicit entities with tenant_id | Required for tenant isolation |
| 5 | Delete behaviors | Cascade children, Restrict cross-aggregate | Referential integrity |
| 6 | Repository returns | **ENTITIES only, never DTOs** | Clean Architecture |
| 7 | Navigation properties on link tables | **Readonly for queries only** | Writes via link table repo |

---

## Key Files Reference

### Repository Interfaces (Application Layer)
```
Explore.Application/Contracts/Persistence/
├── IGenericRepository.cs          # Base: GetById, GetAll, Create, Update, Delete
├── IEventRepository.cs            # GetEventWithDetails, GetEventsWithDetails, GetMyEventsWithDetails
├── IOrganizationRepository.cs     # GetOrganizationWithDetails, GetOrganizationsWithDetails, GetMyOrganizations
├── IUserRepository.cs             # GetUserWithDetails, ExistsByEmail, GetUsersByIdsAsync
├── IOrganizationMemberRepository.cs # GetUsersByOrganization, GetOrganizationsByUser, Exists, etc.
├── IActorRepository.cs            # GetActorWithDetails, GetActorByDid, GetActorByHandle
├── IEventSessionRepository.cs     # GetSessionWithDetails, GetSessionsByEvent, GetSessionsByLocation
├── IEventRegistrationRepository.cs # GetRegistrationByUserAndSession, IsUserRegisteredForSession
├── ITagRepository.cs              # GetTagWithDetails, GetTagsWithDetails
├── ICategoryRepository.cs         # GetCategoryWithDetails, GetCategoriesWithDetails
├── ILocationRepository.cs         # GetLocationsByTenant, GetLocationsByCity, GetLocationsByCountry
└── ... (lookup tables just extend IGenericRepository)
```

### Repository Implementations (Persistence Layer)
```
Explore.Persistence/Repositories/
├── GenericRepository.cs           # Base implementation
├── EventRepository.cs             # Includes: EventType, AudienceGender/Age, Actor, FeaturedImage, etc.
├── OrganizationRepository.cs      # Includes: ApprovalStatus, Actor, Tenant, Members
├── UserRepository.cs              # Includes: Actor
├── OrganizationMemberRepository.cs # Includes: User, Organization, Role, Position
├── ActorRepository.cs             # Includes: ActorType, DidCustodyType, ProfilePicture
├── EventSessionRepository.cs      # Includes: Event, Location, RegistrationMode
├── StorageObjectRepository.cs     # Uses: _dbContext.StorageObjects (not Files!)
└── ... (45+ repository files)
```

### DI Registration
```csharp
// Explore.Persistence/PersistenceServicesRegistration.cs
services.AddScoped(typeof(IGenericRepository<,>), typeof(GenericRepository<,>));
services.AddScoped<IEventRepository, EventRepository>();
services.AddScoped<IOrganizationRepository, OrganizationRepository>();
// ... all 45+ repositories registered
```

---

## Notes / Guardrails

1. **Repository Pattern**: Repositories return ENTITIES. Handlers map to DTOs using AutoMapper.
2. **Navigation Properties**: Link table navigations (e.g., `Organization.Members`) are readonly.
3. **No EF attributes in Domain**: Except `[ForeignKey]` which is allowed.
4. **CQRS**: All writes via MediatR commands, all reads via MediatR queries.
5. **Validation**: FluentValidation at Application boundary.
6. **Tenant Isolation**: Query filters in DbContext + repository scoping.

---

## Quick Resume Commands

```bash
# Check compilation
cd C:\ISLAMU\GitHub\Explore
dotnet build

# If migrations needed
cd Explore.Persistence
dotnet ef migrations add SyncWithDbml --startup-project ../Explore.API
```
