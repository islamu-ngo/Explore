ABOUTME: Strategic plan for replacing JSONB MetadataJson with normalized EAV custom properties.
ABOUTME: Covers all Clean Architecture layers, from domain to Blazor, with Plane-inspired design.

# EAV Custom Properties — Implementation Plan

**Last Updated: 2026-03-04**

---

## Executive Summary

Replace the untyped JSONB `MetadataJson` column on `Event`, `Organization`, and `Group` entities
with a **fully normalized Entity-Attribute-Value (EAV)** system inspired by
[Plane's custom properties architecture](https://developers.plane.so/api-reference/custom-properties).

Plane uses 4 tables: type definitions → property definitions → property options → property values.
We adapt this to our Clean Architecture + CQRS stack with 3 new domain entities, 2 new enums,
and dedicated appearance columns to replace the current MetadataJson-embedded visual settings.

**Constraints:**
- No backward compatibility — project is in active development
- No data migration — MetadataJson is dropped, EAV is the replacement
- No feature toggle — EAV is always active
- Appearance settings move to dedicated columns (not EAV)

---

## Current State Analysis

### MetadataJson Usage (Verified)

| Entity | Property | Line | Type |
|--------|----------|------|------|
| `Explore.Domain/Event.cs` | `MetadataJson` | 109 | `string?` → `jsonb` |
| `Explore.Domain/Organization.cs` | `MetadataJson` | 60 | `string?` → `jsonb` |
| `Explore.Domain/Group.cs` | `MetadataJson` | 33 | `string?` → `jsonb` |

### EF Configurations (Verified)

| File | MetadataJson Config |
|------|---------------------|
| `Explore.Persistence/Configurations/Entities/EventConfiguration.cs` | Lines 99–101: `HasColumnType("jsonb")` |
| `Explore.Persistence/Configurations/Entities/OrganizationConfiguration.cs` | Line 31: `HasColumnType("jsonb")` |
| `Explore.Persistence/Configurations/Entities/GroupConfiguration.cs` | Line 30: `HasColumnType("jsonb")` |

### DTOs with MetadataJson (Verified — 11 files)

**Event:**
- `CreateEventDto.cs` (line 63)
- `UpdateEventDto.cs` (line 51)
- `EventDto.cs` (line 100)
- `EventListDto.cs` — comment only (line 68), no property

**Organization:**
- `CreateOrganizationDto.cs` (line 28)
- `UpdateOrganizationDto.cs` (line 14)
- `OrganizationDto.cs` (line 15)
- `OrganizationListDto.cs` (line 20)

**Group:**
- `CreateGroupDto.cs` (line 10)
- `UpdateGroupDto.cs` (line 9)
- `GroupDto.cs` (line 10)
- `GroupListDto.cs` (line 11)

### Handlers with MetadataJson (Verified — 2 files)

| File | Usage |
|------|-------|
| `UpdateOrganizationDetailsCommandHandler.cs` | Line 76: assigns MetadataJson |
| `UpdateGroupCommandHandler.cs` | Line 77: assigns MetadataJson |

### Query Filters (Verified — 3 files)

| File | Usage |
|------|-------|
| `EventSubqueryFilter.cs` | Lines 132–204: `JsonContains` and `JsonKeyExists` filter types |
| `EventRepository.cs` | Lines 279–287: JSONB `@>` and `?` operator expressions |
| `GetEventListRequestHandler.cs` | Lines 157–161: dispatches MetadataJsonContains/KeyExists |
| `GetEventListRequest.cs` | Lines 225–234: query properties |

### API Controllers (Verified — 1 file)

| File | Usage |
|------|-------|
| `EventController.cs` | Lines 151–152: `metadataJsonContains`, `metadataJsonKeyExists` params |

### Blazor Helpers (Verified — 3 files)

| File | Settings Parsed from MetadataJson |
|------|-----------------------------------|
| `EventAppearanceMetadataHelper.cs` | BackgroundColor, BackgroundMediaUrl, BackgroundEffect |
| `OrganizationAppearanceMetadataHelper.cs` | ProfileImageUrl, BackgroundColor, BackgroundMediaUrl, BackgroundEffect |
| `GroupBrandingMetadataHelper.cs` | PictureUrl, BannerColor, BannerMediaUrl, BannerEffect |

### Blazor Pages (Verified — 9 files)

- `CreateEvent.razor.cs` (line 557)
- `EventEdit.razor.cs` (lines 185, 188, 432)
- `EventDetail.razor.cs` (line 96)
- `CreateOrganization.razor.cs` (line 51)
- `OrganizationDetails.razor.cs` (lines 78, 81, 162)
- `OrganizationProfile.razor.cs` (lines 57, 111)
- `OrganizationProfileSection.razor` (lines 186, 188, 217)
- `GroupAdminSettingsLayout.razor` (lines 119, 121, 146)
- `GroupService.cs` (lines 129, 275)

### Generated Client (Auto-Generated — no manual changes)

- `EventApiClient.g.cs` — 11 occurrences (will regenerate after API changes)

---

## Proposed Future State

### New Domain Model

```
PropertyType enum: Text, Number, Option, Boolean, DateTime, Url

EntityTypeName enum: Event, Organization, Group

CustomPropertyDefinition (Guid PK) ─── ITenantEntity, IAuditableEntity, ISoftDeletable
├── EntityTypeName          enum    — which entity this property applies to
├── EventTypeId (int?)      FK      — optional scoping for Events (like Plane's IssueType)
├── TenantId (Guid)         FK      — tenant-scoped
├── Name (string)                   — internal key (unique per entity-type + tenant + event-type)
├── DisplayName (string)            — user-facing label
├── Description (string?)           — help text
├── PropertyType            enum    — data type
├── IsRequired (bool)
├── IsMulti (bool)                  — allows multiple values per entity
├── IsActive (bool)
├── SortOrder (int)
├── DefaultValue (string?)          — string, interpreted by PropertyType
├── ValidationRules (string?)       — JSON (min/max/regex/etc.)
├── Audit + SoftDelete fields
│
└── Options: IReadOnlyCollection<CustomPropertyOption>

CustomPropertyOption (Guid PK) ─── IAuditableEntity, ISoftDeletable
├── CustomPropertyDefinitionId (Guid, FK)
├── Name (string)                   — display name
├── Description (string?)
├── Value (string)                  — stored value
├── IsDefault (bool)
├── IsActive (bool)
├── SortOrder (int)
├── ParentOptionId (Guid?, FK)      — hierarchical options
├── Audit + SoftDelete fields

CustomPropertyValue (Guid PK) ─── ITenantEntity, IAuditableEntity, ISoftDeletable
├── CustomPropertyDefinitionId (Guid, FK)
├── EntityId (Guid)                 — polymorphic (Event/Org/Group ID, NO DB FK)
├── TextValue (string?)             — TEXT, URL
├── NumberValue (decimal?)          — NUMBER
├── BooleanValue (bool?)            — BOOLEAN
├── DateTimeValue (DateTimeOffset?) — DATETIME
├── OptionId (Guid?, FK)            — OPTION type → CustomPropertyOption
├── Audit + SoftDelete fields
```

### Appearance Columns (Replacing MetadataJson-Embedded Settings)

**Event** — new columns:
- `BackgroundColor` (string?, max 50)
- `BackgroundMediaUrl` (string?, max 500)
- `BackgroundEffect` (string?, max 50)

**Organization** — new columns:
- `ProfileImageUrl` (string?, max 500)
- `BackgroundColor` (string?, max 50)
- `BackgroundMediaUrl` (string?, max 500)
- `BackgroundEffect` (string?, max 50)

**Group** — new columns:
- `PictureUrl` (string?, max 500)
- `BannerColor` (string?, max 50)
- `BannerMediaUrl` (string?, max 500)
- `BannerEffect` (string?, max 50)

### Key Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Property scope | EntityTypeName + optional EventTypeId | Like Plane: properties per type, but extended to Org/Group via EntityTypeName discriminator |
| Value storage | Typed columns (TextValue/NumberValue/BooleanValue/DateTimeValue/OptionId) | Better querying, type safety, indexing vs single-string approach |
| EntityId FK | Polymorphic Guid, no DB FK constraint | Entity could be Event, Org, or Group — discriminated by Definition.EntityTypeName |
| Appearance fields | Dedicated columns on entities | They're system-defined visual settings, not user-defined custom fields |
| Option hierarchy | ParentOptionId self-reference | Like Plane — supports cascading dropdowns |
| Multi-value | IsMulti flag on definition | Single unique constraint when IsMulti=false; multiple rows when true |
| Tenant isolation | TenantId on Definition + Value | Definitions are tenant-scoped; Options inherit through Definition |

---

## Implementation Phases

### Phase 1: Domain Layer
**Effort: M** | **Related Skills:** `clean-architecture-rules`

#### Task 1.1: Create PropertyType Enum
- **File:** `Explore.Domain/Enums/PropertyType.cs`
- **Acceptance Criteria:**
  - Enum with values: Text=1, Number=2, Option=3, Boolean=4, DateTime=5, Url=6
  - File-scoped namespace
  - ABOUTME header
- **Effort:** S

#### Task 1.2: Create EntityTypeName Enum
- **File:** `Explore.Domain/Enums/EntityTypeName.cs`
- **Acceptance Criteria:**
  - Enum with values: Event=1, Organization=2, Group=3
  - File-scoped namespace
  - ABOUTME header
- **Effort:** S

#### Task 1.3: Create CustomPropertyDefinition Entity
- **File:** `Explore.Domain/CustomPropertyDefinition.cs`
- **Acceptance Criteria:**
  - Implements `ITenantEntity`, `IAuditableEntity`, `ISoftDeletable`
  - All properties as specified in model above
  - `IReadOnlyCollection<CustomPropertyOption>` navigation (backed by private list)
  - `IReadOnlyCollection<CustomPropertyValue>` navigation (backed by private list)
  - Guid PK, file-scoped namespace, ABOUTME header
  - No default values in properties (set in handlers/EF config per CLAUDE.md rule)
- **Effort:** S

#### Task 1.4: Create CustomPropertyOption Entity
- **File:** `Explore.Domain/CustomPropertyOption.cs`
- **Acceptance Criteria:**
  - Implements `IAuditableEntity`, `ISoftDeletable`
  - All properties as specified: DefinitionId FK, Name, Description, Value, IsDefault, IsActive, SortOrder, ParentOptionId
  - Navigation: `CustomPropertyDefinition` (parent), `CustomPropertyOption?` (parent option), `IReadOnlyCollection<CustomPropertyOption>` (children)
  - Guid PK, file-scoped namespace, ABOUTME header
- **Effort:** S

#### Task 1.5: Create CustomPropertyValue Entity
- **File:** `Explore.Domain/CustomPropertyValue.cs`
- **Acceptance Criteria:**
  - Implements `ITenantEntity`, `IAuditableEntity`, `ISoftDeletable`
  - All typed value columns: TextValue, NumberValue, BooleanValue, DateTimeValue, OptionId
  - EntityId (Guid, polymorphic — no nav prop to Event/Org/Group)
  - Navigation: `CustomPropertyDefinition`, `CustomPropertyOption?`
  - Guid PK, file-scoped namespace, ABOUTME header
- **Effort:** S

#### Task 1.6: Add Appearance Columns to Event
- **File:** `Explore.Domain/Event.cs`
- **Changes:** Add `BackgroundColor`, `BackgroundMediaUrl`, `BackgroundEffect` (all `string?`). Remove `MetadataJson`.
- **Effort:** S

#### Task 1.7: Add Appearance Columns to Organization
- **File:** `Explore.Domain/Organization.cs`
- **Changes:** Add `ProfileImageUrl`, `BackgroundColor`, `BackgroundMediaUrl`, `BackgroundEffect`. Remove `MetadataJson`.
- **Effort:** S

#### Task 1.8: Add Appearance Columns to Group
- **File:** `Explore.Domain/Group.cs`
- **Changes:** Add `PictureUrl`, `BannerColor`, `BannerMediaUrl`, `BannerEffect`. Remove `MetadataJson`.
- **Effort:** S

---

### Phase 2: Persistence Layer — EF Configurations
**Effort: L** | **Related Skills:** `dotnet-efcore-guidelines`

#### Task 2.1: Create CustomPropertyDefinitionConfiguration
- **File:** `Explore.Persistence/Configurations/Entities/CustomPropertyDefinitionConfiguration.cs`
- **Acceptance Criteria:**
  - Table name: `custom_property_definitions`
  - Name: required, max 100
  - DisplayName: required, max 200
  - Description: max 500
  - DefaultValue: max 1000
  - ValidationRules: max 2000
  - PropertyType + EntityTypeName stored as string (or int, consistent with codebase)
  - Unique index: (TenantId, EntityTypeName, EventTypeId, Name) — `ix_cpd_tenant_entity_type_name`
  - Index: (TenantId, EntityTypeName, IsActive) — `ix_cpd_tenant_entity_active`
  - SoftDelete named query filter
  - Relationship: HasMany(Options), HasMany(Values)
  - EventType FK config (optional)
- **Effort:** M

#### Task 2.2: Create CustomPropertyOptionConfiguration
- **File:** `Explore.Persistence/Configurations/Entities/CustomPropertyOptionConfiguration.cs`
- **Acceptance Criteria:**
  - Table name: `custom_property_options`
  - Name: required, max 200
  - Value: required, max 500
  - Description: max 500
  - Index: (DefinitionId, SortOrder) — `ix_cpo_definition_sort`
  - Self-referencing FK for ParentOptionId (SetNull on delete)
  - SoftDelete named query filter
- **Effort:** S

#### Task 2.3: Create CustomPropertyValueConfiguration
- **File:** `Explore.Persistence/Configurations/Entities/CustomPropertyValueConfiguration.cs`
- **Acceptance Criteria:**
  - Table name: `custom_property_values`
  - TextValue: max 4000
  - NumberValue: precision (19,4)
  - Unique index: (DefinitionId, EntityId) when IsMulti=false — `ix_cpv_definition_entity` (non-unique to handle both cases; uniqueness enforced in handler)
  - Index: (EntityId) — `ix_cpv_entity` (for "get all values for entity X")
  - Index: (TenantId, DefinitionId) — `ix_cpv_tenant_definition` (for "all values of property Y")
  - FK to CustomPropertyDefinition (Cascade delete)
  - FK to CustomPropertyOption (SetNull on delete)
  - SoftDelete named query filter
- **Effort:** M

#### Task 2.4: Update EventConfiguration
- **File:** `Explore.Persistence/Configurations/Entities/EventConfiguration.cs`
- **Changes:**
  - Remove lines 99–101 (MetadataJson jsonb config)
  - Add appearance column configs: BackgroundColor (max 50), BackgroundMediaUrl (max 500), BackgroundEffect (max 50)
- **Effort:** S

#### Task 2.5: Update OrganizationConfiguration
- **File:** `Explore.Persistence/Configurations/Entities/OrganizationConfiguration.cs`
- **Changes:**
  - Remove MetadataJson jsonb config (line 31)
  - Add appearance column configs
- **Effort:** S

#### Task 2.6: Update GroupConfiguration
- **File:** `Explore.Persistence/Configurations/Entities/GroupConfiguration.cs`
- **Changes:**
  - Remove MetadataJson jsonb config (line 30)
  - Add appearance column configs
- **Effort:** S

#### Task 2.7: Add DbSets to ExploreDbContext
- **File:** `Explore.Persistence/ExploreDbContext.cs`
- **Changes:**
  - Add `DbSet<CustomPropertyDefinition>`
  - Add `DbSet<CustomPropertyOption>`
  - Add `DbSet<CustomPropertyValue>`
  - Add query filter registrations for new entities in `ApplyGlobalQueryFilters`
- **Effort:** S

#### Task 2.8: Create EF Migration
- **Command:** `dotnet ef migrations add AddEavCustomProperties`
- **Acceptance:** Migration compiles, applies cleanly to empty DB
- **Effort:** S

---

### Phase 3: Persistence Layer — Repositories
**Effort: M** | **Related Skills:** `dotnet-efcore-guidelines`, `clean-architecture-rules`

#### Task 3.1: Create ICustomPropertyDefinitionRepository
- **File:** `Explore.Application/Contracts/Persistence/ICustomPropertyDefinitionRepository.cs`
- **Interface extends:** `IGenericRepository<CustomPropertyDefinition, Guid>`
- **Custom methods:**
  - `Task<List<CustomPropertyDefinition>> GetByEntityType(EntityTypeName entityType, int? eventTypeId = null)`
  - `Task<CustomPropertyDefinition?> GetWithOptions(Guid id)`
  - `Task<bool> NameExists(Guid tenantId, EntityTypeName entityType, int? eventTypeId, string name, Guid? excludeId = null)`
- **Effort:** S

#### Task 3.2: Create ICustomPropertyValueRepository
- **File:** `Explore.Application/Contracts/Persistence/ICustomPropertyValueRepository.cs`
- **Interface extends:** `IGenericRepository<CustomPropertyValue, Guid>`
- **Custom methods:**
  - `Task<List<CustomPropertyValue>> GetByEntity(Guid entityId)`
  - `Task<CustomPropertyValue?> GetByDefinitionAndEntity(Guid definitionId, Guid entityId)`
  - `Task<List<CustomPropertyValue>> GetByDefinition(Guid definitionId)`
  - `Task DeleteByEntity(Guid entityId)` — bulk cleanup
- **Effort:** S

#### Task 3.3: Implement CustomPropertyDefinitionRepository
- **File:** `Explore.Persistence/Repositories/CustomPropertyDefinitionRepository.cs`
- **Acceptance:** Include eager loading of Options in GetWithOptions, apply active filter in GetByEntityType
- **Effort:** M

#### Task 3.4: Implement CustomPropertyValueRepository
- **File:** `Explore.Persistence/Repositories/CustomPropertyValueRepository.cs`
- **Acceptance:** Include eager loading of Definition + Option in GetByEntity
- **Effort:** M

#### Task 3.5: Register Repositories in DI
- **File:** `Explore.Persistence/PersistenceServicesRegistration.cs`
- **Changes:** Add `AddScoped<ICustomPropertyDefinitionRepository, ...>` and value repo
- **Effort:** S

---

### Phase 4: Application Layer — DTOs
**Effort: M** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 4.1: Create Custom Property Definition DTOs
- **Files:**
  - `Explore.Application/DTOs/CustomProperty/CustomPropertyDefinitionDto.cs`
  - `Explore.Application/DTOs/CustomProperty/CustomPropertyDefinitionListDto.cs`
  - `Explore.Application/DTOs/CustomProperty/CreateCustomPropertyDefinitionDto.cs`
  - `Explore.Application/DTOs/CustomProperty/UpdateCustomPropertyDefinitionDto.cs`
- **Effort:** S

#### Task 4.2: Create Custom Property Option DTOs
- **Files:**
  - `Explore.Application/DTOs/CustomProperty/CustomPropertyOptionDto.cs`
  - `Explore.Application/DTOs/CustomProperty/CreateCustomPropertyOptionDto.cs`
  - `Explore.Application/DTOs/CustomProperty/UpdateCustomPropertyOptionDto.cs`
- **Effort:** S

#### Task 4.3: Create Custom Property Value DTOs
- **Files:**
  - `Explore.Application/DTOs/CustomProperty/CustomPropertyValueDto.cs`
  - `Explore.Application/DTOs/CustomProperty/SetCustomPropertyValueDto.cs`
- **Effort:** S

#### Task 4.4: Remove MetadataJson from Event DTOs + Add Appearance
- **Files:** `CreateEventDto.cs`, `UpdateEventDto.cs`, `EventDto.cs`
- **Changes:** Remove `MetadataJson` property; add `BackgroundColor`, `BackgroundMediaUrl`, `BackgroundEffect`
- **Effort:** S

#### Task 4.5: Remove MetadataJson from Organization DTOs + Add Appearance
- **Files:** `CreateOrganizationDto.cs`, `UpdateOrganizationDto.cs`, `OrganizationDto.cs`, `OrganizationListDto.cs`
- **Changes:** Remove `MetadataJson`; add appearance properties
- **Effort:** S

#### Task 4.6: Remove MetadataJson from Group DTOs + Add Appearance
- **Files:** `CreateGroupDto.cs`, `UpdateGroupDto.cs`, `GroupDto.cs`, `GroupListDto.cs`
- **Changes:** Remove `MetadataJson`; add branding properties
- **Effort:** S

#### Task 4.7: Update AutoMapper Profile
- **File:** `Explore.Application/Profiles/MappingProfile.cs`
- **Changes:** Add mappings for new EAV DTOs; update Event/Org/Group mappings for appearance columns
- **Effort:** S

---

### Phase 5: Application Layer — CQRS Commands & Queries
**Effort: XL** | **Related Skills:** `cqrs-mediatr-guidelines`

#### Task 5.1: CreateCustomPropertyDefinition Command + Handler + Validator
- **Files:**
  - `Features/CustomProperties/Requests/Commands/CreateCustomPropertyDefinitionCommand.cs`
  - `Features/CustomProperties/Handlers/Commands/CreateCustomPropertyDefinitionCommandHandler.cs`
  - `DTOs/CustomProperty/Validators/CreateCustomPropertyDefinitionDtoValidator.cs`
- **Acceptance:** Returns `BaseCommandResponse<Guid>`, validates name uniqueness, validates PropertyType
- **Effort:** M

#### Task 5.2: UpdateCustomPropertyDefinition Command + Handler + Validator
- **Files:** Same pattern as 5.1 but for Update
- **Acceptance:** Cannot change PropertyType after values exist, validates name uniqueness excluding self
- **Effort:** M

#### Task 5.3: DeleteCustomPropertyDefinition Command + Handler
- **Files:** Request + Handler
- **Acceptance:** Soft-deletes definition + cascades to options/values
- **Effort:** S

#### Task 5.4: CreateCustomPropertyOption Command + Handler + Validator
- **Files:** Request + Handler + Validator
- **Acceptance:** Validates definition exists and is Option type, validates unique value within definition
- **Effort:** M

#### Task 5.5: UpdateCustomPropertyOption Command + Handler + Validator
- **Effort:** S

#### Task 5.6: DeleteCustomPropertyOption Command + Handler
- **Effort:** S

#### Task 5.7: SetCustomPropertyValue Command + Handler + Validator
- **Files:** Request + Handler + Validator
- **Acceptance:** Upsert behavior (create or update), validates value type matches property type, enforces IsRequired, enforces single-value uniqueness when IsMulti=false
- **Effort:** L

#### Task 5.8: RemoveCustomPropertyValue Command + Handler
- **Effort:** S

#### Task 5.9: GetCustomPropertyDefinitions Query + Handler
- **Files:** Request + Handler
- **Acceptance:** Filter by EntityTypeName, optional EventTypeId, include Options, paginated
- **Effort:** M

#### Task 5.10: GetCustomPropertyDefinitionDetail Query + Handler
- **Files:** Request + Handler
- **Acceptance:** Single definition with all options, used for definition editing
- **Effort:** S

#### Task 5.11: GetCustomPropertyValues Query + Handler
- **Files:** Request + Handler
- **Acceptance:** Get all custom property values for a specific entity (by EntityId), includes definition display info
- **Effort:** M

---

### Phase 6: Remove MetadataJson from Existing Application Layer
**Effort: M**

#### Task 6.1: Remove JSONB Filters from EventSubqueryFilter
- **File:** `Explore.Application/Specifications/Events/EventSubqueryFilter.cs`
- **Changes:** Remove `JsonContains` and `JsonKeyExists` filter types and factory methods (lines 132–204)
- **Effort:** S

#### Task 6.2: Remove JSONB Filtering from EventRepository
- **File:** `Explore.Persistence/Repositories/EventRepository.cs`
- **Changes:** Remove JSONB case handlers (lines 279–287)
- **Effort:** S

#### Task 6.3: Remove MetadataJson from GetEventListRequest + Handler
- **File:** `GetEventListRequest.cs` — remove MetadataJsonContains/MetadataJsonKeyExists (lines 225–234)
- **File:** `GetEventListRequestHandler.cs` — remove MetadataJson filter dispatch (lines 157–161)
- **Changes:** Also update handler mappings for new appearance columns
- **Effort:** S

#### Task 6.4: Update Event Create/Update Handlers for Appearance
- **Files:** `CreateEventCommandHandler.cs`, `UpdateEventCommandHandler.cs`
- **Changes:** Map appearance columns from DTO to entity (instead of MetadataJson)
- **Effort:** S

#### Task 6.5: Update Organization Handler for Appearance
- **File:** `UpdateOrganizationDetailsCommandHandler.cs`
- **Changes:** Replace `organization.MetadataJson = request.OrganizationDto.MetadataJson` (line 76) with appearance column mapping
- **Effort:** S

#### Task 6.6: Update Group Handler for Appearance
- **File:** `UpdateGroupCommandHandler.cs`
- **Changes:** Replace `group.MetadataJson = request.GroupDto.MetadataJson` (line 77) with branding column mapping
- **Effort:** S

---

### Phase 7: API Layer
**Effort: L** | **Related Skills:** `auth-patterns`

#### Task 7.1: Create CustomPropertyDefinitionController
- **File:** `Explore.API/Controllers/CustomPropertyDefinitionController.cs`
- **Endpoints:**
  - `GET /api/custom-property-definitions?entityType={}&eventTypeId={}` — list definitions
  - `GET /api/custom-property-definitions/{id}` — detail with options
  - `POST /api/custom-property-definitions` — create (Authorize)
  - `PUT /api/custom-property-definitions/{id}` — update (Authorize)
  - `DELETE /api/custom-property-definitions/{id}` — delete (Authorize)
- **Acceptance:** HAL wrapping, proper OpenAPI attributes, authorization
- **Effort:** M

#### Task 7.2: Create CustomPropertyOptionController
- **File:** `Explore.API/Controllers/CustomPropertyOptionController.cs`
- **Endpoints:**
  - `POST /api/custom-property-definitions/{definitionId}/options` — create
  - `PUT /api/custom-property-definitions/{definitionId}/options/{id}` — update
  - `DELETE /api/custom-property-definitions/{definitionId}/options/{id}` — delete
- **Effort:** M

#### Task 7.3: Create CustomPropertyValueController
- **File:** `Explore.API/Controllers/CustomPropertyValueController.cs`
- **Endpoints:**
  - `GET /api/custom-property-values?entityId={}` — get all values for entity
  - `PUT /api/custom-property-values` — set value (upsert)
  - `DELETE /api/custom-property-values/{id}` — remove value
- **Effort:** M

#### Task 7.4: Update EventController
- **File:** `Explore.API/Controllers/EventController.cs`
- **Changes:** Remove `metadataJsonContains` and `metadataJsonKeyExists` query parameters (lines 151–152)
- **Effort:** S

---

### Phase 8: Blazor Client Updates
**Effort: L** | **Related Skills:** `blazor-ui-conventions`, `blazor-css-isolation`

#### Task 8.1: Refactor EventAppearanceMetadataHelper
- **File:** `Explore.Blazor.Client/Helpers/EventAppearanceMetadataHelper.cs`
- **Changes:** Remove MetadataJson parsing/upsert; read/write from dedicated DTO properties. Keep `BuildHeroStyle()` method.
- **Effort:** M

#### Task 8.2: Refactor OrganizationAppearanceMetadataHelper
- **File:** `Explore.Blazor.Client/Helpers/OrganizationAppearanceMetadataHelper.cs`
- **Changes:** Same pattern — remove JSON parsing, use dedicated columns
- **Effort:** M

#### Task 8.3: Refactor GroupBrandingMetadataHelper
- **File:** `Explore.Blazor.Client/Helpers/GroupBrandingMetadataHelper.cs`
- **Changes:** Same pattern
- **Effort:** M

#### Task 8.4: Update Event Pages
- **Files:** `CreateEvent.razor.cs`, `EventEdit.razor.cs`, `EventDetail.razor.cs`
- **Changes:** Use appearance columns instead of MetadataJson + helper parse/upsert
- **Effort:** M

#### Task 8.5: Update Organization Pages
- **Files:** `CreateOrganization.razor.cs`, `OrganizationDetails.razor.cs`, `OrganizationProfile.razor.cs`, `OrganizationProfileSection.razor`
- **Changes:** Use appearance columns instead of MetadataJson
- **Effort:** M

#### Task 8.6: Update Group Pages
- **Files:** `GroupAdminSettingsLayout.razor`, `GroupService.cs`
- **Changes:** Use branding columns instead of MetadataJson
- **Effort:** M

#### Task 8.7: Regenerate API Client
- **Command:** Regenerate `EventApiClient.g.cs` from OpenAPI spec after API changes
- **Effort:** S

---

### Phase 9: Testing & Documentation
**Effort: L**

#### Task 9.1: Architecture Tests
- **File:** `Event.Architecture.Tests/`
- **Acceptance:** New entities follow layer dependency rules, implement correct interfaces
- **Effort:** S

#### Task 9.2: Unit Tests — Command Handlers
- **File:** `Event.Application.UnitTests/`
- **Acceptance:** Tests for all create/update/delete handlers, validation scenarios, edge cases (duplicate name, type mismatch, IsMulti)
- **Effort:** L

#### Task 9.3: Unit Tests — Query Handlers
- **Acceptance:** Tests for list/detail/values queries
- **Effort:** M

#### Task 9.4: Integration Tests — Repositories
- **File:** `Event.Persistence.IntegrationTests/`
- **Acceptance:** CRUD operations, unique constraint enforcement, cascade behavior, tenant isolation
- **Effort:** M

#### Task 9.5: Integration Tests — API Endpoints
- **File:** `Event.API.IntegrationTests/`
- **Acceptance:** Full roundtrip: create definition → add options → set value → query values
- **Effort:** M

#### Task 9.6: Update Documentation
- **Files:** `docs/DOMAIN.md`, `docs/ARCHITECTURE.md`, `docs/EXTENSIBILITY.md`
- **Changes:** Document new EAV entities, remove MetadataJson references, update diagram
- **Effort:** M

---

## Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| Polymorphic EntityId queries slow without proper indexes | Medium | High | Add composite indexes on (EntityId), (TenantId, DefinitionId); monitor query plans |
| N+1 queries when fetching entity + all custom values | Medium | Medium | Repository includes eager loading; consider batched value retrieval |
| PropertyType change after values exist | Low | High | Block type changes when values exist (handler validation) |
| Blazor client regeneration breaks due to MetadataJson removal | Medium | Low | Regenerate client after all API changes complete |
| Cascading soft-delete across definition → options → values | Medium | Medium | Test cascade behavior thoroughly in integration tests |

---

## Success Metrics

1. **All MetadataJson references removed** from Domain, Application, Persistence, API, Blazor layers
2. **New EAV tables** created with proper indexes and constraints
3. **CRUD operations** for definitions, options, values work end-to-end
4. **Appearance settings** function via dedicated columns (no regression)
5. **All existing tests pass** after changes
6. **New tests cover** all EAV handlers, validators, and repositories

---

## Potential Risks & Unknowns

The **most likely complexity point** is Task 5.7 (SetCustomPropertyValue) — upsert behavior with type validation, IsMulti enforcement, and value type coercion across 5 different typed columns. This handler needs the most careful testing. The polymorphic EntityId design also means we cannot use database-level FK constraints for referential integrity, so orphaned values could accumulate if entity deletion doesn't trigger value cleanup — this must be handled explicitly in Event/Org/Group delete handlers or via a background cleanup job. Finally, the EAV filtering replacement for EventSubqueryFilter's JSONB queries will require careful SQL generation testing with EF Core to ensure the JOIN-based filters produce efficient query plans comparable to the current JSONB operators.
