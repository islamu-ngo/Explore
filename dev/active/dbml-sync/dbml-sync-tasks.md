# DBML Sync - Task Checklist

**Last Updated:** 2026-01-05

## ✅ ANALYSIS PHASE (COMPLETED)

### Codebase Analysis (2026-01-04)
- [x] Analyze Organization entity implementation across all layers
- [x] Document Domain layer patterns (entities, enums, navigation)
- [x] Document Application layer patterns (DTOs, CQRS, validators, handlers)
- [x] Document Persistence layer patterns (configurations, repositories)
- [x] Document API layer patterns (controllers, routing, auth)
- [x] Identify actual naming conventions used in codebase
- [x] Identify response patterns (BaseCommandResponse<T>)
- [x] Identify repository return type patterns (DTOs vs entities)
- [x] Create comprehensive refactored plan (60+ pages)

### DBML Schema Analysis
- [x] Compare DBML with existing codebase entities
- [x] Identify type mismatches (atproto_record uuid vs varchar)
- [x] Identify missing columns (tenant_id in 3 tables)
- [x] Identify missing tables (OrganizationReview)
- [x] Identify timestamp inconsistencies
- [x] Document all corrections in dbml-corrections-required.md

### Design Decisions (ALL RESOLVED)
- [x] atproto_record type decision → varchar (255/500) for did/record_key/cid
- [x] location geo representation → PostGIS geometry + lat/long
- [x] tenant enforcement strategy → Multi-layered (filters + repo + middleware)
- [x] join entities modeling → Explicit entities with tenant_id
- [x] delete behaviors → Cascade for children, Restrict for cross-aggregate
- [x] API routing → Standardize to /api/v1/[controller]
- [x] repository return types → DTOs for queries, entities for commands
- [x] user ID extraction → Centralized extension method

### Documentation Created
- [x] Updated dbml-sync-context.md with all findings
- [x] Updated dbml-sync-plan.md with executive summary
- [x] Created dbml-corrections-required.md (CRITICAL)
- [x] Created comprehensive plan at C:\Users\AM5\.claude\plans\purrfect-weaving-sun.md

---

## Phase 0: Discovery & Alignment Spec ⏳ READY TO START (After DBML Corrections)

### 0.1 Entity Mapping (See comprehensive plan for complete table)
- [ ] Create entity mapping table (43 entities total)
  - [ ] Identify aggregates: Tenant, User, Actor, Organization, Event, EventRegistration, Category, Tag, Location, StorageObject, IndexedDid, SyncState
  - [ ] Map sub-entities to aggregates
  - [ ] Map lookup tables to enums or entities
  - [ ] Confirm explicit join entities:
    - [x] event_categories (CONFIRMED)
    - [x] event_tags (CONFIRMED)
    - [x] tag_type_tags (CONFIRMED)
    - [x] event_session_languages (CONFIRMED)
    - [x] event_session_speakers (CONFIRMED)
    - [x] organization_members (CONFIRMED)
    - [x] tenant_user (CONFIRMED)

### 0.2 CQRS Use Case Mapping (See comprehensive plan for complete list)
- [ ] Define Events use cases (create, update, get, list, search with filters)
- [ ] Define EventSessions use cases
- [ ] Define EventRegistrations use cases
- [ ] Define Organizations use cases
- [ ] Define Tags/Categories use cases
- [ ] Define Locations use cases
- [ ] Define Actors use cases (federation)
- [ ] Define ATProto Records use cases (internal)

### 0.3 Verification
- [x] All blocking decisions resolved (8/8 complete)
- [ ] Entity mapping document created
- [ ] CQRS use case list created
- [ ] No unresolved critical questions

Acceptance (Phase 0):
- [x] context.md updated with decisions ✅
- [ ] Concrete entity mapping table created
- [ ] CQRS use case list documented
- [x] No unresolved critical type questions ✅

---

## Phase 1: Domain Layer (DBML → Entities) ✅ COMPLETED
### 1.1 Create/Update Entities
- [x] tenant, tenant_user, tenant_settings
- [x] user, user_role, user_authentication_token, user_external_login
- [x] actor, actor_type, did_custody_type, actor_key_store
- [x] organization, organization_members, organization_role, organization_position, approval_status
- [x] event, event_session, event_registration
- [x] event_type, event_status, visibility_type, event_format, registration_mode
- [x] madhab, audience_age, audience_gender, language
- [x] category (with parent relationship)
- [x] tag, tag_type, tag_type_tags
- [x] join entities:
  - [x] event_categories
  - [x] event_tags
  - [x] event_session_languages
  - [x] event_session_speakers
- [x] event_session_agenda_items
- [x] location
- [x] storage_object, file_type
- [x] indexed_did, sync_state, atproto_record

### 1.2 Relationship & Invariant Review
- [x] Verify required fields align with DBML [not null]
- [x] Changed all `long` to `int` except where necessary (size, cursor)
- [x] Removed default values from Tag.cs (per CLAUDE.md rules)
- [x] Added missing TenantId to Location, ActorKeyStore, UserAuthenticationToken, UserExternalLogin

Acceptance (Phase 1):
- [x] Domain entities match DBML shape and relationships
- [x] All int/long consistency applied
- [x] No default values in Domain entities (moved to configurations)

---

## Phase 2: Application Layer (CQRS + DTOs + Validation + Mapping) ⏳ NOT STARTED

### 2.1 Repository Interfaces
- [ ] Define repository interfaces for aggregates per your conventions
  - [ ] IEventRepository (and session/registration access as needed)
  - [ ] IOrganizationRepository (members/roles as needed)
  - [ ] ITagRepository / ICategoryRepository (or a unified discovery repo)
  - [ ] ILocationRepository
  - [ ] IActorRepository (if required by use cases)
  - [ ] IAtProtoRecordRepository (if required)

- [ ] Ensure interfaces support tenant scoping (explicit parameter or implicit context)

### 2.2 CQRS Use Cases
- [ ] Events
  - [ ] Create event
  - [ ] Update event
  - [ ] Get event by id/slug
  - [ ] List/search events (filters: tenant, visibility, status, gender/age, madhab, tags/categories, date ranges, format)

- [ ] Sessions
  - [ ] Create session
  - [ ] Update session
  - [ ] Get session details
  - [ ] List sessions by event

- [ ] Registrations
  - [ ] Register user to session
  - [ ] Approve/reject registration (if required)
  - [ ] List registrations (scoped)

- [ ] Organizations
  - [ ] Create organization (or application flow)
  - [ ] Update organization
  - [ ] Approve organization (Tier 2 verification hook via approval_status)
  - [ ] Manage organization members + roles/positions

- [ ] Tags/Categories
  - [ ] List/search tags
  - [ ] List categories + tree (parent_id)

- [ ] Locations
  - [ ] Create/update location
  - [ ] Search location by city/country and/or geo (depending on decision)

- [ ] Federation/indexer (only if used by API)
  - [ ] Upsert/resolve atproto_record links (internal)
  - [ ] Sync state read/update (internal tooling)

### 2.3 DTOs
- [ ] Define DTOs for each request/response per your conventions
- [ ] Ensure DTOs align with DBML fields (and API contract decisions)
- [ ] Ensure nested shapes (event with sessions, etc.) follow your documented patterns

### 2.4 FluentValidation
- [ ] Validators for commands/queries per conventions
- [ ] Validate required fields + length constraints
- [ ] Validate FK references strategy (existence checks) per your conventions

### 2.5 AutoMapper
- [ ] Update mapping profiles for all new/changed DTOs and entities
- [ ] Ensure profiles are registered in DI

Acceptance (Phase 2):
- [ ] Application compiles
- [ ] CQRS flows exist for core scenarios
- [ ] Validators and mappings wired correctly

---

## Phase 3: Persistence Layer (DbContext + Configurations + Repositories + Migrations) 🟡 IN PROGRESS

### 3.1 DbContext ✅ COMPLETED
- [x] Add DbSet for all entities needed
- [x] Cleaned up OnModelCreating (removed obsolete commented code)
- [x] Using ApplyConfigurationsFromAssembly for auto-discovery

### 3.2 Entity Configurations ✅ COMPLETED (39 configurations)

**Lookup Tables (ValueGeneratedNever + Seed Data):**
- [x] ApprovalStatusConfiguration
- [x] EventTypeConfiguration
- [x] AudienceAgeConfiguration
- [x] AudienceGenderConfiguration
- [x] MadhabConfiguration (NEW)
- [x] LanguageConfiguration (NEW)
- [x] EventStatusConfiguration (NEW)
- [x] EventFormatConfiguration (NEW)
- [x] VisibilityTypeConfiguration (NEW)
- [x] RegistrationModeConfiguration (NEW)
- [x] OrganizationRoleConfiguration (NEW)
- [x] OrganizationPositionConfiguration (NEW)
- [x] DidCustodyTypeConfiguration (NEW)
- [x] ActorTypeConfiguration (NEW)
- [x] FileTypeConfiguration (NEW)
- [x] OwnerTypeConfiguration (NEW)
- [x] UserRoleConfiguration (NEW)
- [x] TagTypeConfiguration

**Entity Configurations (UUID/relationships):**
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

### 3.3 Repositories
- [ ] Implement repository interfaces
- [ ] Ensure queries used by Application handlers are efficient and correct
- [ ] Confirm includes/projections match mapping approach

### 3.4 Migrations & Schema Validation
- [ ] Generate/adjust migrations to match DBML
- [ ] If existing DB already deployed:
  - [ ] define baseline strategy (documented)
  - [ ] create safe incremental migrations

Acceptance (Phase 3):
- [x] All entity configurations created
- [ ] DB can be migrated/created to match DBML
- [ ] Core queries and commands work end-to-end

---

## Phase 4: API Layer (Controllers + Middleware) ⏳ NOT STARTED

### 4.1 Controllers
- [ ] Events controller endpoints mapped to CQRS
- [ ] Sessions controller endpoints mapped to CQRS
- [ ] Registrations controller endpoints mapped to CQRS
- [ ] Organizations controller endpoints mapped to CQRS
- [ ] Tags/Categories/Locations endpoints as needed

### 4.2 Middleware / Cross-cutting
- [ ] Tenant resolution middleware (if part of your architecture)
- [ ] Exception handling middleware updated for new validation/errors
- [ ] Authentication/Authorization integration remains consistent (Keycloak/Cerbos)
- [ ] Ensure ProblemDetails / error responses follow your conventions

Acceptance (Phase 4):
- [ ] API compiles and runs
- [ ] Endpoints hit handlers correctly
- [ ] Auth and tenant behavior correct

---

## Phase 5: Verification, Cleanup, Documentation ⏳ NOT STARTED
- [ ] Remove/replace obsolete entities/configurations/endpoints
- [ ] Add/update tests (unit/integration) according to your conventions
- [ ] Confirm no lingering schema mismatch references remain
- [ ] Update context.md SESSION PROGRESS and decisions log
- [ ] Archive dev docs when complete (optional per your workflow)

Acceptance (Phase 5):
- [ ] “DBML mismatch” resolved for all in-scope modules
- [ ] Tests pass (or updated consistently)
- [ ] Documentation reflects final state

---

## Quick Resume
1. Complete Phase 0 decisions (atproto_record types, geo, tenant strategy, join modeling).
2. Implement Domain entities first until Domain compiles.
3. Implement Application CQRS + DTOs + validators + mappings.
4. Implement Persistence mappings + repositories + migrations.
5. Update API controllers/middleware.