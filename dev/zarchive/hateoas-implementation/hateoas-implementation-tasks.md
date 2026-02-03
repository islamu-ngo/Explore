# HATEOAS Implementation - Task Checklist

> **Track progress through implementation phases**

**Last Updated**: 2026-01-23

---

## Design Approach

**HATEOAS is ON by default** - All responses include `_links` and `_embedded`.

**Opt-Out via RFC 7240** - Clients send `Prefer: return=minimal` to strip links.

**Single response shape** - No content negotiation, no fragmented API surface.

---

## Overview

| Phase | Status | Tasks | Completed |
|-------|--------|-------|-----------|
| Phase 1: Core Infrastructure | ✅ COMPLETE | 6 | 6/6 |
| Phase 2: Entity Assemblers | ✅ COMPLETE | 7 | 7/7 |
| Phase 3: Controller Integration | ✅ COMPLETE (7 core entities) | 5 | 5/5 |
| Phase 4: Pagination Enhancement | ✅ BUILT-IN | 3 | 3/3 |
| Phase 5: Documentation | ⏭️ SKIPPED BY USER | 3 | 0/3 |
| Phase 6: Testing (Core) | ✅ COMPLETE | 4 | 3/4 |
| Phase 7: Remaining Controllers | ✅ POLICIES & ASSEMBLERS DONE | 11 | 11/11 |
| Phase 8: Additional Entity Tests | ✅ COMPLETE | 11 | 11/11 |
| **Total** | | **50** | **46/50** |

### HATEOAS Coverage Summary (Enterprise Design)

| Category | Controllers | HATEOAS? | Reason |
|----------|-------------|----------|--------|
| ✅ Core Business Entities | 7 | YES ✅ | Event, Organization, EventSession, Actor, Location, Category, Tag |
| ✅ Relationship with Payload | 3 | YES ⏳ | OrganizationMember, EventRegistration, TenantUser |
| ✅ Core Entities | 5 | YES ⏳ | User, Tenant, TenantSettings, StorageObject, EventSessionAgendaItem |
| ✅ Organization Review | 1 | YES ⏳ | Has ratings/comments payload |
| ✅ ATProto (Recommended) | 2 | YES ⏳ | AtprotoRecord, IndexedDid |
| 🚫 Pure Join Tables | 5 | NO | EventCategories, EventTags, EventSessionLanguage, EventSessionSpeaker, TagTypeTags → Embed in parent |
| ⚪ Lookup/Reference Tables | 17 | OPTIONAL | Static enum-backed data, low ROI |
| 🚫 ATProto Security | 4 | NO | ActorKeyStore, SyncState, UserAuthToken, UserExternalLogin → Security-sensitive |
| **TOTAL** | **44** | **18 YES** | 7 complete + 11 remaining |

---

## Phase 1: Core Infrastructure ✅ COMPLETE

**Objective**: Establish base HATEOAS infrastructure without modifying existing behavior

### Task 1.1: Create HAL Resource Models ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.Application/Hateoas/HalResource.cs`
  - [x] `Explore.Application/Hateoas/HalLink.cs`
  - [x] `Explore.Application/Hateoas/HalCollectionResource.cs`
  - [x] `Explore.Application/Hateoas/HalResourceJsonConverter.cs` (custom converter for flattening)
- **Acceptance Criteria**:
  - [x] Generic `HalResource<T>` with `_links` and `_embedded`
  - [x] `HalLink` with `Href`, `Method`, `Title`, `Templated`
  - [x] `HalCollectionResource<T>` for collections
  - [x] JSON serialization with correct property names
- **Effort**: S

### Task 1.2: Create Link Definition Models ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.Application/Hateoas/LinkDefinition.cs`
  - [x] `Explore.Application/Hateoas/LinkRelations.cs`
- **Acceptance Criteria**:
  - [x] `LinkDefinition` record with Rel, RouteName, RouteValues
  - [x] IANA link relation constants (self, collection, next, prev, etc.)
  - [x] Custom relation constants (events, sessions, members)
- **Effort**: S

### Task 1.3: Create Resource Assembler Interfaces ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/IResourceAssembler.cs` (moved to API layer for Clean Architecture)
  - [x] `Explore.Application/Contracts/Hateoas/ILinkPolicy.cs`
  - [x] `Explore.API/Hateoas/IHateoasLinkGenerator.cs` (moved to API layer for Clean Architecture)
- **Acceptance Criteria**:
  - [x] `IResourceAssembler<TDto, TResource>` interface
  - [x] `ILinkPolicy<TDto>` for link determination logic
  - [x] `IHateoasLinkGenerator` abstraction over ASP.NET LinkGenerator
- **Note**: Interfaces moved to API layer because they depend on HttpContext
- **Effort**: M

### Task 1.4: Create Base Resource Assembler Implementation ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/ResourceAssemblerBase.cs`
  - [x] `Explore.API/Hateoas/HateoasLinkGenerator.cs`
  - [x] `Explore.API/Hateoas/RouteNames.cs`
  - [x] `Explore.API/Hateoas/HateoasConstants.cs`
- **Acceptance Criteria**:
  - [x] Abstract base with common link generation logic
  - [x] Integration with ASP.NET Core `LinkGenerator`
  - [x] Authorization-aware link filtering
  - [x] Support for conditional links based on resource state
- **Effort**: M

### Task 1.5: Create Prefer Header Middleware (RFC 7240) ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Middleware/PreferHeaderMiddleware.cs`
- **Acceptance Criteria**:
  - [x] Parse `Prefer: return=minimal` header
  - [x] Store preference in `HttpContext.Items["HateoasMinimal"]`
  - [x] Add `Preference-Applied: return=minimal` response header when honored
  - [x] Default behavior: full HAL response with links
- **Effort**: M

### Task 1.6: Register HATEOAS Services ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Extensions/HateoasServiceExtensions.cs`
  - [x] `Explore.API/Extensions/HateoasAssemblerRegistration.cs`
  - [x] `Explore.API/Program.cs` (updated)
- **Acceptance Criteria**:
  - [x] Extension method `AddHateoas()` for service registration
  - [x] Extension method `UseHateoas()` for middleware pipeline
  - [x] PreferHeaderMiddleware registered in pipeline
  - [x] Resource assemblers registered with DI (scoped lifetime)
  - [x] HAL is default response format for all endpoints
- **Effort**: S

---

## Phase 2: Entity-Specific Assemblers ✅ COMPLETE

**Objective**: Implement resource assemblers for primary business entities

### Task 2.1: Organization Resource Assembler ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/Assemblers/OrganizationResourceAssembler.cs`
  - [x] `Explore.API/Hateoas/Policies/OrganizationLinkPolicy.cs`
- **Acceptance Criteria**:
  - [x] Links: self, collection, events, members
  - [x] Links: update (if member), delete (if admin)
  - [x] Authorization-aware link filtering
  - [x] Supports `OrganizationDto` and `OrganizationListDto`
- **Effort**: M

### Task 2.2: Event Resource Assembler ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/Assemblers/EventResourceAssembler.cs`
  - [x] `Explore.API/Hateoas/Policies/EventLinkPolicy.cs`
- **Acceptance Criteria**:
  - [x] Links: self, collection, sessions, categories, tags, actor
  - [x] Links: registration (if open), update/delete (if owner)
  - [x] Authorization-aware link filtering
  - [x] Supports `EventDto` and `EventListDto`
- **Effort**: L

### Task 2.3: Event Session Resource Assembler ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/Assemblers/EventSessionResourceAssembler.cs`
  - [x] `Explore.API/Hateoas/Policies/EventSessionLinkPolicy.cs`
- **Acceptance Criteria**:
  - [x] Links: self, event (parent), speakers, agenda-items, location
  - [x] Links: update, delete (if authorized)
  - [x] Supports `EventSessionDto` and `EventSessionListDto`
- **Effort**: M

### Task 2.4: Actor Resource Assembler ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/Assemblers/ActorResourceAssembler.cs`
  - [x] `Explore.API/Hateoas/Policies/ActorLinkPolicy.cs`
- **Acceptance Criteria**:
  - [x] Links: self, collection, events (as organizer)
  - [x] Links based on actor type (user, organization)
  - [x] Supports `ActorDto` and `ActorListDto`
- **Effort**: M

### Task 2.5: Location Resource Assembler ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/Assemblers/LocationResourceAssembler.cs`
  - [x] `Explore.API/Hateoas/Policies/LocationLinkPolicy.cs`
- **Acceptance Criteria**:
  - [x] Links: self, collection, edit, delete
  - [x] Authorization-aware link filtering
  - [x] Supports `LocationDto` and `LocationListDto`
- **Effort**: S

### Task 2.6: Category Resource Assembler ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/Assemblers/CategoryResourceAssembler.cs`
  - [x] `Explore.API/Hateoas/Policies/CategoryLinkPolicy.cs`
- **Acceptance Criteria**:
  - [x] Links: self, collection, parent (if subcategory), children, events
  - [x] Supports `CategoryDto` and `CategoryListDto`
- **Effort**: S

### Task 2.7: Tag Resource Assembler ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Hateoas/Assemblers/TagResourceAssembler.cs`
  - [x] `Explore.API/Hateoas/Policies/TagLinkPolicy.cs`
- **Acceptance Criteria**:
  - [x] Links: self, collection, events (with this tag), edit, delete
  - [x] Supports `TagDto` and `TagListDto`
- **Effort**: S

---

## Phase 3: Controller Integration ✅ COMPLETE

**Objective**: Update controllers to return HAL resources while maintaining backward compatibility

### Task 3.1: Create HateoasControllerBase ⏭️ SKIPPED
- **Status**: ⏭️ SKIPPED (Not needed - using composition over inheritance)
- **Files**:
  - N/A - Resource assemblers handle all HAL logic
- **Notes**: Controllers inject IResourceAssembler instead of inheriting from base class
- **Effort**: N/A

### Task 3.2: Update OrganizationController ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Controllers/OrganizationController.cs`
- **Acceptance Criteria**:
  - [x] Inject `IResourceAssembler<OrganizationDto, OrganizationListDto>`
  - [x] Return HAL resources by default (with `_links`)
  - [x] Links stripped when `Prefer: return=minimal` sent
  - [x] All endpoints return HAL resources
- **Effort**: M

### Task 3.3: Update EventController ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Controllers/EventController.cs`
- **Acceptance Criteria**:
  - [x] HAL resources with session links
  - [x] Paginated collection with navigation links
  - [x] State-driven links (draft vs published)
- **Effort**: M

### Task 3.4: Update EventSessionController ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Controllers/EventSessionController.cs`
- **Acceptance Criteria**:
  - [x] HAL resources with parent event link
  - [x] Speaker and agenda item links
- **Effort**: M

### Task 3.5: Update Remaining Controllers ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Explore.API/Controllers/ActorController.cs`
  - [x] `Explore.API/Controllers/LocationController.cs`
  - [x] `Explore.API/Controllers/CategoryController.cs`
  - [x] `Explore.API/Controllers/TagController.cs`
- **Acceptance Criteria**:
  - [x] All controllers support HATEOAS
  - [x] Consistent pattern across controllers
  - [x] Named routes for all endpoints
  - [x] HAL+JSON Produces attribute
- **Effort**: L

---

## Phase 4: Pagination Enhancement ✅ BUILT-IN

**Objective**: Standardize pagination with HATEOAS navigation links

### Task 4.1: Create PaginatedHalResource ✅ BUILT-IN
- **Status**: ✅ BUILT-IN (HalCollectionResource already includes pagination)
- **Files**:
  - [x] `Explore.Application/Hateoas/HalCollectionResource.cs` - Has PageNumber, PageSize, TotalPages, TotalCount
- **Acceptance Criteria**:
  - [x] Extends `HalCollectionResource` with pagination metadata
  - [x] Auto-generates first, prev, next, last links via ResourceAssemblerBase
  - [x] Handles edge cases (first page, last page)
- **Effort**: N/A

### Task 4.2: Create Pagination Link Generator ✅ BUILT-IN
- **Status**: ✅ BUILT-IN (Implemented in HateoasLinkGenerator)
- **Files**:
  - [x] `Explore.API/Hateoas/HateoasLinkGenerator.cs` - GeneratePaginationLinks method
- **Acceptance Criteria**:
  - [x] Generates pagination links from `PaginatedResult`
  - [x] Preserves query parameters (filters, sorting)
  - [x] RFC 8288 compliant link relations (first, prev, next, last)
- **Effort**: N/A

### Task 4.3: Update PaginatedResult<T> ✅ COMPATIBLE
- **Status**: ✅ COMPATIBLE (Existing PaginatedResult works with HATEOAS)
- **Files**:
  - [x] `Explore.Application/Responses/PaginatedResult.cs` - Already has all needed properties
- **Acceptance Criteria**:
  - [x] Has PageNumber, PageSize, TotalPages, TotalCount
  - [x] ResourceAssemblerBase.ToCollectionResource uses these for pagination links
  - [x] No breaking changes to existing usage
- **Effort**: N/A

---

## Phase 5: Documentation & OpenAPI NOT STARTED

**Objective**: Document HATEOAS responses in OpenAPI/Scalar

### Task 5.1: Create HAL Schema Examples
- **Status**: NOT STARTED
- **Files**:
  - [ ] `Explore.API/OpenApi/HalSchemaFilter.cs`
- **Acceptance Criteria**:
  - [ ] OpenAPI schema filter for HAL resources
  - [ ] Example responses with `_links` and `_embedded`
  - [ ] Document custom media type
- **Effort**: M

### Task 5.2: Update API Documentation
- **Status**: NOT STARTED
- **Files**:
  - [ ] `docs/API.md`
- **Acceptance Criteria**:
  - [ ] Document HATEOAS usage
  - [ ] Link relation reference
  - [ ] Client integration guide
- **Effort**: S

### Task 5.3: Add Response Examples to Controllers
- **Status**: NOT STARTED
- **Files**:
  - [ ] All controllers
- **Acceptance Criteria**:
  - [ ] `[ProducesResponseType]` with HAL examples
  - [ ] Document both JSON and HAL+JSON responses
- **Effort**: M

---

## Phase 6: Testing & Validation ✅ COMPLETE

**Objective**: Comprehensive testing of HATEOAS implementation

### Task 6.1: Unit Tests for HATEOAS Models ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.Application.UnitTests/Hateoas/HalLinkTests.cs`
  - [x] `Event.Application.UnitTests/Hateoas/HalResourceTests.cs`
  - [x] `Event.Application.UnitTests/Hateoas/HalCollectionResourceTests.cs`
  - [x] `Event.Application.UnitTests/Hateoas/LinkDefinitionTests.cs`
  - [x] `Event.Application.UnitTests/Hateoas/LinkRelationsTests.cs`
- **Acceptance Criteria**:
  - [x] Test HalLink creation (simple, action, templated)
  - [x] Test HalResource with data, links, and embedded
  - [x] Test HalCollectionResource pagination metadata
  - [x] Test LinkDefinition factory methods and modifiers
  - [x] Test LinkRelations constants are correct
- **Effort**: M

### Task 6.2: Integration Tests for HATEOAS Functionality ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/HateoasIntegrationTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/PreferHeaderMiddlewareTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/HateoasLinkGeneratorTests.cs`
- **Acceptance Criteria**:
  - [x] Test default response includes `_links`
  - [x] Test `Prefer: return=minimal` strips links
  - [x] Test `Preference-Applied` response header
  - [x] Test pagination links (first, prev, next, last)
  - [x] Verify RFC 8288 link relation compliance
  - [x] Test HAL+JSON structure
  - [x] Test content-type handling
- **Effort**: L

### Task 6.3: Entity-Specific HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/OrganizationHateoasTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/EventHateoasTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/EventSessionHateoasTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/ActorHateoasTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/LocationHateoasTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/CategoryHateoasTests.cs`
  - [x] `Event.API.IntegrationTests/Features/Hateoas/TagHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test Organization-specific links (events, members)
  - [x] Test Event-specific links (sessions, actor, categories, tags)
  - [x] Test EventSession-specific links (event, speakers, agenda-items)
  - [x] Test Actor-specific links (events)
  - [x] Test Location-specific links
  - [x] Test Category-specific links (parent, children, events)
  - [x] Test Tag-specific links (events)
- **Effort**: L

### Task 6.4: Manual API Testing
- **Status**: ⏳ NOT STARTED
- **Tools**: Scalar, Postman
- **Acceptance Criteria**:
  - [ ] Test all endpoints with HAL Accept header
  - [ ] Verify link URLs are correct and functional
  - [ ] Test navigation through links only
- **Effort**: M

---

## Quick Reference

### Starting a Task
1. Find the task in the checklist above
2. Update status to "IN PROGRESS"
3. Check off individual items as completed
4. Update context file with progress

### Completing a Task
1. Verify all acceptance criteria are met
2. Check off all items
3. Update status to "COMPLETE"
4. Update context file

### Adding New Tasks
If new tasks are discovered during implementation:
1. Add to appropriate phase
2. Assign effort (S/M/L/XL)
3. Define acceptance criteria
4. Update phase totals

---

## Legend

| Symbol | Meaning |
|--------|---------|
| NOT STARTED | Work not yet begun |
| IN PROGRESS | Currently being worked on |
| COMPLETE | All criteria met |
| [ ] | Uncompleted item |
| [x] | Completed item |
| S/M/L/XL | Effort: Small/Medium/Large/Extra Large |

---

## Phase 7: Remaining Controllers - Enterprise Design ⏳ NOT STARTED

**Objective**: Apply enterprise-grade HATEOAS patterns based on REST resource design principles

---

### Enterprise REST API Design Principles

> **Key Insight**: Not all database tables should be exposed as REST resources.
>
> - **Join Tables** (pure ID mappings) should be **embedded** in parent resources via `_links` or `_embedded`
> - **Resources with Payload** (business data beyond IDs) should have standalone HATEOAS
> - **Lookup/Reference Tables** are low-priority static data

---

### Controllers WITH HATEOAS Support (7 controllers) ✅ COMPLETE

| Controller | Assembler | Tests | Status |
|------------|-----------|-------|--------|
| OrganizationController | ✅ OrganizationResourceAssembler | ✅ OrganizationHateoasTests | ✅ COMPLETE |
| EventController | ✅ EventResourceAssembler | ✅ EventHateoasTests | ✅ COMPLETE |
| EventSessionController | ✅ EventSessionResourceAssembler | ✅ EventSessionHateoasTests | ✅ COMPLETE |
| ActorController | ✅ ActorResourceAssembler | ✅ ActorHateoasTests | ✅ COMPLETE |
| LocationController | ✅ LocationResourceAssembler | ✅ LocationHateoasTests | ✅ COMPLETE |
| CategoryController | ✅ CategoryResourceAssembler | ✅ CategoryHateoasTests | ✅ COMPLETE |
| TagController | ✅ TagResourceAssembler | ✅ TagHateoasTests | ✅ COMPLETE |

---

### 🚫 PURE JOIN TABLES - DO NOT IMPLEMENT HATEOAS (5 controllers)

> **These are implementation details, not REST resources.**
> Relationships should be accessed via parent resource links.

| Controller | Entity Analysis | Enterprise Approach |
|------------|-----------------|---------------------|
| EventCategoriesController | `EventId + CategoryId` only | Embed via `GET /events/{id}/categories` or `Event._embedded.categories` |
| EventTagsController | `EventId + TagId` only | Embed via `GET /events/{id}/tags` or `Event._embedded.tags` |
| EventSessionLanguageController | `EventSessionId + LanguageId` only | Embed via `GET /event-sessions/{id}/languages` or `EventSession._embedded.languages` |
| EventSessionSpeakerController | `ActorId + EventSessionId` only | Embed via `GET /event-sessions/{id}/speakers` or `EventSession._embedded.speakers` |
| TagTypeTagsController | `TagId + TagTypeId` only | Embed via `GET /tags/{id}/tag-types` or `Tag._embedded.tagTypes` |

**Action Required**: Update parent controllers to include relationship links:
- [ ] EventController: Add `categories`, `tags` links to `_links`
- [ ] EventSessionController: Add `languages`, `speakers` links to `_links`
- [ ] TagController: Add `tagTypes` link to `_links`

---

### ✅ RELATIONSHIP WITH PAYLOAD - SHOULD HAVE HATEOAS (3 controllers)

> **These have business data beyond IDs, making them meaningful resources.**

| Controller | Business Payload | Priority | Status |
|------------|------------------|----------|--------|
| OrganizationMemberController | `OrganizationRoleId`, `OrganizationPositionId`, Audit fields | HIGH | ✅ POLICIES & ASSEMBLERS DONE |
| EventRegistrationController | `ApprovalStatusId`, `AtprotoRecordId` | HIGH | ✅ POLICIES & ASSEMBLERS DONE |
| TenantUserController | `UserRoleId` | HIGH | ✅ POLICIES & ASSEMBLERS DONE |

**Implementation Completed**:
- [x] OrganizationMemberResourceAssembler + OrganizationMemberLinkPolicy
- [x] EventRegistrationResourceAssembler + EventRegistrationLinkPolicy
- [x] TenantUserResourceAssembler + TenantUserLinkPolicy

---

### ✅ CORE BUSINESS ENTITIES - SHOULD HAVE HATEOAS (5 controllers)

| Controller | Priority | Status |
|------------|----------|--------|
| UserController | HIGH | ✅ POLICIES & ASSEMBLERS DONE |
| TenantController | HIGH | ✅ POLICIES & ASSEMBLERS DONE |
| TenantSettingsController | MEDIUM | ✅ POLICIES & ASSEMBLERS DONE |
| StorageObjectController | MEDIUM | ✅ POLICIES & ASSEMBLERS DONE |
| EventSessionAgendaItemController | MEDIUM | ✅ POLICIES & ASSEMBLERS DONE |

**Implementation Completed**:
- [x] UserResourceAssembler + UserLinkPolicy
- [x] TenantResourceAssembler + TenantLinkPolicy
- [x] TenantSettingsResourceAssembler + TenantSettingsLinkPolicy
- [x] StorageObjectResourceAssembler + StorageObjectLinkPolicy
- [x] EventSessionAgendaItemResourceAssembler + EventSessionAgendaItemLinkPolicy

---

### ⚪ LOOKUP/REFERENCE TABLES - OPTIONAL HATEOAS (17 controllers)

> **Static reference data. HATEOAS is optional but low priority.**
> Simple `self` and `collection` links are sufficient if implemented.

| Controller | Notes |
|------------|-------|
| ActorTypeController | Enum-backed lookup |
| ApprovalStatusController | Enum-backed lookup |
| AudienceAgeController | Enum-backed lookup |
| AudienceGenderController | Enum-backed lookup |
| DidCustodyTypeController | Enum-backed lookup |
| EventFormatController | Enum-backed lookup |
| EventStatusController | Enum-backed lookup |
| EventTypeController | Enum-backed lookup |
| FileTypeController | Enum-backed lookup |
| LanguageController | Reference data |
| MadhabController | Enum-backed lookup |
| OrganizationPositionController | Enum-backed lookup |
| OrganizationRoleController | Enum-backed lookup |
| RegistrationModeController | Enum-backed lookup |
| TagTypeController | Reference data |
| UserRoleController | Reference data |
| VisibilityTypeController | Enum-backed lookup |

**Decision**: Skip HATEOAS for lookup tables (low ROI, high effort)

---

### 🔷 ATPROTO/FEDERATION - SPECIALIZED (6 controllers)

> **Domain-specific federation resources. Consider carefully.**

| Controller | Notes | Recommendation |
|------------|-------|----------------|
| ActorKeyStoreController | Cryptographic key management | ⚠️ Security-sensitive, minimal exposure |
| AtprotoRecordController | ATProto record references | ✅ HATEOAS useful for federation links |
| IndexedDidController | DID indexing | ✅ HATEOAS useful for DID resolution |
| SyncStateController | Sync cursor management | ⚠️ Internal, minimal exposure |
| UserAuthenticationTokenController | Auth tokens | 🚫 Security-sensitive, NO public HATEOAS |
| UserExternalLoginController | External login providers | ⚠️ Limited exposure |

**Implemented for HATEOAS**:
- [x] AtprotoRecordResourceAssembler + AtprotoRecordLinkPolicy
- [x] IndexedDidResourceAssembler + IndexedDidLinkPolicy

**Skip HATEOAS** (security/internal):
- ActorKeyStoreController
- SyncStateController
- UserAuthenticationTokenController
- UserExternalLoginController

---

### ✅ ORGANIZATION REVIEW - SHOULD HAVE HATEOAS (1 controller)

| Controller | Notes | Priority |
|------------|-------|----------|
| OrganizationReviewController | User reviews with ratings/comments | MEDIUM | ✅ POLICIES & ASSEMBLERS DONE |

**Implementation Completed**:
- [x] OrganizationReviewResourceAssembler + OrganizationReviewLinkPolicy

---

### Summary: Implementation Status

| Category | Count | Status |
|----------|-------|--------|
| ✅ Core Business Entities (Phase 1-3) | 7 | ✅ COMPLETE |
| 🚫 Pure Join Tables | 5 | Skip - embedded via parent `_links` |
| ✅ Relationship with Payload | 3 | ✅ POLICIES & ASSEMBLERS DONE |
| ✅ Core Business Entities | 5 | ✅ POLICIES & ASSEMBLERS DONE |
| ✅ Organization Review | 1 | ✅ POLICIES & ASSEMBLERS DONE |
| ✅ ATProto (Recommended) | 2 | ✅ POLICIES & ASSEMBLERS DONE |
| ⚪ Lookup Tables | 17 | Skip (optional, low ROI) |
| 🚫 ATProto Security | 4 | Skip (security-sensitive) |
| **TOTAL IMPLEMENTED** | **18** | Link policies + Resource assemblers + DI registration

---

### Implementation Pattern for Phase 7

**For each resource needing HATEOAS:**
1. **Link Policy** (`Explore.API/Hateoas/Policies/{Entity}LinkPolicy.cs`)
2. **Resource Assembler** (`Explore.API/Hateoas/Assemblers/{Entity}ResourceAssembler.cs`)
3. **Register in DI** (`Explore.API/Extensions/HateoasAssemblerRegistration.cs`)
4. **Add Route Names** to `RouteNames.cs`
5. **Update Controller** to use assembler
6. **Integration Tests** (`Event.API.IntegrationTests/Features/Hateoas/{Entity}HateoasTests.cs`)

**For pure join tables (update parent controllers):**
1. Add relationship links to existing assemblers (e.g., EventResourceAssembler)
2. Ensure `_links` includes navigation to related collections
3. Optionally add `_embedded` for eager loading scenarios

---

## Phase 8: Additional Entity Tests ✅ COMPLETE

**Objective**: Integration tests for all 11 additional HATEOAS entities using TUnit

### Task 8.1: User HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/UserHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test auth-required create link
  - [x] Test item links
  - [x] Test tenant/actor links

### Task 8.2: Tenant HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/TenantHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test settings/users links

### Task 8.3: TenantUser HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/TenantUserHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test tenant/user links

### Task 8.4: TenantSettings HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/TenantSettingsHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test tenant link

### Task 8.5: OrganizationMember HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/OrganizationMemberHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test organization/user links

### Task 8.6: EventRegistration HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/EventRegistrationHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test user/event-session links

### Task 8.7: EventSessionAgendaItem HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/EventSessionAgendaItemHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test event-session/location links

### Task 8.8: StorageObject HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/StorageObjectHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test collection link

### Task 8.9: OrganizationReview HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/OrganizationReviewHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test organization/reviewer links

### Task 8.10: AtprotoRecord HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/AtprotoRecordHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test DID/by-uri links

### Task 8.11: IndexedDid HATEOAS Tests ✅
- **Status**: ✅ COMPLETE
- **Files**:
  - [x] `Event.API.IntegrationTests/Features/Hateoas/IndexedDidHateoasTests.cs`
- **Acceptance Criteria**:
  - [x] Test HAL structure
  - [x] Test self link
  - [x] Test actor/collection links
