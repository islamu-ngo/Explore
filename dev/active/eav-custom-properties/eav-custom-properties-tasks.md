ABOUTME: Checklist for tracking EAV custom properties implementation progress.
ABOUTME: Each task maps to a specific file change with clear acceptance criteria.

# EAV Custom Properties — Task Checklist

**Last Updated: 2026-03-04**

---

## Phase 1: Domain Layer ⏳ NOT STARTED

- [ ] **1.1** Create `PropertyType` enum (`Explore.Domain/Enums/PropertyType.cs`)
  - Text=1, Number=2, Option=3, Boolean=4, DateTime=5, Url=6
- [ ] **1.2** Create `EntityTypeName` enum (`Explore.Domain/Enums/EntityTypeName.cs`)
  - Event=1, Organization=2, Group=3
- [ ] **1.3** Create `CustomPropertyDefinition` entity (`Explore.Domain/CustomPropertyDefinition.cs`)
  - ITenantEntity, IAuditableEntity, ISoftDeletable
  - All properties per plan, IReadOnlyCollection navigations
- [ ] **1.4** Create `CustomPropertyOption` entity (`Explore.Domain/CustomPropertyOption.cs`)
  - IAuditableEntity, ISoftDeletable
  - Self-referencing ParentOptionId
- [ ] **1.5** Create `CustomPropertyValue` entity (`Explore.Domain/CustomPropertyValue.cs`)
  - ITenantEntity, IAuditableEntity, ISoftDeletable
  - Typed value columns, polymorphic EntityId
- [ ] **1.6** Add appearance columns to `Event.cs`, remove MetadataJson
  - BackgroundColor, BackgroundMediaUrl, BackgroundEffect (all string?)
- [ ] **1.7** Add appearance columns to `Organization.cs`, remove MetadataJson
  - ProfileImageUrl, BackgroundColor, BackgroundMediaUrl, BackgroundEffect
- [ ] **1.8** Add branding columns to `Group.cs`, remove MetadataJson
  - PictureUrl, BannerColor, BannerMediaUrl, BannerEffect

---

## Phase 2: Persistence — EF Configurations ⏳ NOT STARTED

- [ ] **2.1** Create `CustomPropertyDefinitionConfiguration.cs`
  - Table: custom_property_definitions
  - Unique index: (TenantId, EntityTypeName, EventTypeId, Name)
  - Listing index: (TenantId, EntityTypeName, IsActive)
  - SoftDelete query filter
- [ ] **2.2** Create `CustomPropertyOptionConfiguration.cs`
  - Table: custom_property_options
  - Index: (DefinitionId, SortOrder)
  - Self-ref FK for ParentOptionId
- [ ] **2.3** Create `CustomPropertyValueConfiguration.cs`
  - Table: custom_property_values
  - Index: (DefinitionId, EntityId)
  - Index: (EntityId)
  - Index: (TenantId, DefinitionId)
  - FK cascade from Definition
- [ ] **2.4** Update `EventConfiguration.cs` — remove jsonb, add appearance columns
- [ ] **2.5** Update `OrganizationConfiguration.cs` — remove jsonb, add appearance columns
- [ ] **2.6** Update `GroupConfiguration.cs` — remove jsonb, add branding columns
- [ ] **2.7** Add DbSets + query filters to `ExploreDbContext.cs`
- [ ] **2.8** Create EF migration

---

## Phase 3: Persistence — Repositories ⏳ NOT STARTED

- [ ] **3.1** Create `ICustomPropertyDefinitionRepository.cs`
  - GetByEntityType, GetWithOptions, NameExists
- [ ] **3.2** Create `ICustomPropertyValueRepository.cs`
  - GetByEntity, GetByDefinitionAndEntity, GetByDefinition, DeleteByEntity
- [ ] **3.3** Implement `CustomPropertyDefinitionRepository.cs`
- [ ] **3.4** Implement `CustomPropertyValueRepository.cs`
- [ ] **3.5** Register repos in `PersistenceServicesRegistration.cs`

---

## Phase 4: Application — DTOs ⏳ NOT STARTED

- [ ] **4.1** Create custom property definition DTOs (4 files)
- [ ] **4.2** Create custom property option DTOs (3 files)
- [ ] **4.3** Create custom property value DTOs (2 files)
- [ ] **4.4** Remove MetadataJson from Event DTOs, add appearance (3 files)
- [ ] **4.5** Remove MetadataJson from Organization DTOs, add appearance (4 files)
- [ ] **4.6** Remove MetadataJson from Group DTOs, add branding (4 files)
- [ ] **4.7** Update `MappingProfile.cs` for new entities + appearance columns

---

## Phase 5: Application — CQRS Commands & Queries ⏳ NOT STARTED

- [ ] **5.1** CreateCustomPropertyDefinition command + handler + validator
- [ ] **5.2** UpdateCustomPropertyDefinition command + handler + validator
- [ ] **5.3** DeleteCustomPropertyDefinition command + handler
- [ ] **5.4** CreateCustomPropertyOption command + handler + validator
- [ ] **5.5** UpdateCustomPropertyOption command + handler + validator
- [ ] **5.6** DeleteCustomPropertyOption command + handler
- [ ] **5.7** SetCustomPropertyValue command + handler + validator (upsert, type validation)
- [ ] **5.8** RemoveCustomPropertyValue command + handler
- [ ] **5.9** GetCustomPropertyDefinitions query + handler (list by entity type)
- [ ] **5.10** GetCustomPropertyDefinitionDetail query + handler (single with options)
- [ ] **5.11** GetCustomPropertyValues query + handler (values for entity)

---

## Phase 6: Remove MetadataJson from Application Layer ⏳ NOT STARTED

- [ ] **6.1** Remove JSONB filters from `EventSubqueryFilter.cs` (lines 132-204)
- [ ] **6.2** Remove JSONB filtering from `EventRepository.cs` (lines 279-287)
- [ ] **6.3** Remove MetadataJson from `GetEventListRequest.cs` + handler
- [ ] **6.4** Update Event create/update handlers for appearance columns
- [ ] **6.5** Update `UpdateOrganizationDetailsCommandHandler.cs` (line 76)
- [ ] **6.6** Update `UpdateGroupCommandHandler.cs` (line 77)

---

## Phase 7: API Layer ⏳ NOT STARTED

- [ ] **7.1** Create `CustomPropertyDefinitionController.cs` (CRUD, 5 endpoints)
- [ ] **7.2** Create `CustomPropertyOptionController.cs` (nested CRUD, 3 endpoints)
- [ ] **7.3** Create `CustomPropertyValueController.cs` (get/set/remove, 3 endpoints)
- [ ] **7.4** Update `EventController.cs` — remove MetadataJson query params

---

## Phase 8: Blazor Client ⏳ NOT STARTED

- [ ] **8.1** Refactor `EventAppearanceMetadataHelper.cs` — use dedicated columns
- [ ] **8.2** Refactor `OrganizationAppearanceMetadataHelper.cs` — use dedicated columns
- [ ] **8.3** Refactor `GroupBrandingMetadataHelper.cs` — use dedicated columns
- [ ] **8.4** Update Event pages (CreateEvent, EventEdit, EventDetail)
- [ ] **8.5** Update Organization pages (CreateOrg, OrgDetails, OrgProfile, OrgProfileSection)
- [ ] **8.6** Update Group pages (GroupAdminSettingsLayout, GroupService)
- [ ] **8.7** Regenerate `EventApiClient.g.cs` from OpenAPI spec

---

## Phase 9: Testing & Documentation ⏳ NOT STARTED

- [ ] **9.1** Architecture tests — new entities follow layer rules
- [ ] **9.2** Unit tests — command handlers (create/update/delete definition, option, value)
- [ ] **9.3** Unit tests — query handlers (list definitions, detail, values)
- [ ] **9.4** Integration tests — repositories (CRUD, constraints, cascade, tenant isolation)
- [ ] **9.5** Integration tests — API endpoints (full roundtrip)
- [ ] **9.6** Update docs: DOMAIN.md, ARCHITECTURE.md, EXTENSIBILITY.md

---

## Summary

| Phase | Tasks | Status |
|-------|-------|--------|
| 1. Domain | 8 | ⏳ |
| 2. EF Configs | 8 | ⏳ |
| 3. Repositories | 5 | ⏳ |
| 4. DTOs | 7 | ⏳ |
| 5. CQRS | 11 | ⏳ |
| 6. Remove MetadataJson | 6 | ⏳ |
| 7. API | 4 | ⏳ |
| 8. Blazor | 7 | ⏳ |
| 9. Tests & Docs | 6 | ⏳ |
| **Total** | **62** | |
