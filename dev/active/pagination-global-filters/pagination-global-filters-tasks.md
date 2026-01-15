# Pagination and Global Query Filters - Task Checklist

**Last Updated**: 2026-01-15

## Phase 1: Infrastructure Foundation 🟡 IN PROGRESS

### 1.1 Domain Layer
- [ ] Create `Explore.Domain/Interfaces/ITenantEntity.cs`
- [ ] Add ITenantEntity interface to 22 tenant-scoped entities

### 1.2 Application Layer
- [ ] Create `Explore.Application/Responses/PaginatedResult.cs`
- [ ] Create `Explore.Application/Requests/PaginationParams.cs`
- [ ] Update `IGenericRepository.cs` - Add GetAllPaged method signature

### 1.3 Persistence Layer
- [ ] Update `GenericRepository.cs` - Implement GetAllPaged with Skip/Take

## Phase 2: Global Query Filters ⏳ NOT STARTED

- [ ] Update `ExploreDbContext.cs` - Add ITenantContext injection
- [ ] Add Global Query Filters in OnModelCreating
- [ ] Test that migrations still work (null tenant context)

## Phase 3: Repository Updates ⏳ NOT STARTED

### Tenant-Scoped Entities (~22 repos)
- [ ] EventRepository - Add GetEventsWithDetailsPaged
- [ ] EventSessionRepository - Add paged method
- [ ] EventRegistrationRepository - Add paged method
- [ ] OrganizationRepository - Add paged method
- [ ] ActorRepository - Add paged method
- [ ] LocationRepository - Add paged method
- [ ] CategoryRepository - Add paged method
- [ ] TagRepository - Add paged method
- [ ] StorageObjectRepository - Add paged method
- [ ] OrganizationMemberRepository - Add paged method
- [ ] OrganizationReviewRepository - Add paged method
- [ ] ActorKeyStoreRepository - Add paged method
- [ ] EventCategoriesRepository - Add paged method
- [ ] EventTagsRepository - Add paged method
- [ ] EventSessionLanguageRepository - Add paged method
- [ ] EventSessionSpeakerRepository - Add paged method
- [ ] EventSessionAgendaItemRepository - Add paged method
- [ ] TagTypeTagsRepository - Add paged method
- [ ] UserAuthenticationTokenRepository - Add paged method
- [ ] UserExternalLoginRepository - Add paged method
- [ ] TenantUserRepository - Add paged method
- [ ] TenantSettingsRepository - Add paged method

## Phase 4: Request/Handler Updates ⏳ NOT STARTED

### Event Feature
- [ ] GetEventListRequest - Change to PaginatedResult<EventListDto>
- [ ] GetEventListRequestHandler - Implement pagination

### EventSession Feature
- [ ] GetEventSessionListRequest - Change to PaginatedResult
- [ ] GetEventSessionListRequestHandler - Implement pagination

### Organization Feature
- [ ] GetOrganizationListRequest - Change to PaginatedResult
- [ ] GetOrganizationListRequestHandler - Implement pagination

### Actor Feature
- [ ] GetActorListRequest - Change to PaginatedResult
- [ ] GetActorListRequestHandler - Implement pagination

### Location Feature
- [ ] GetLocationListRequest - Change to PaginatedResult
- [ ] GetLocationListRequestHandler - Implement pagination

### Category Feature
- [ ] GetCategoryListRequest - Change to PaginatedResult
- [ ] GetCategoryListRequestHandler - Implement pagination

### Tag Feature
- [ ] GetTagListRequest - Change to PaginatedResult
- [ ] GetTagListRequestHandler - Implement pagination

### StorageObject Feature
- [ ] GetStorageObjectListRequest - Change to PaginatedResult
- [ ] GetStorageObjectListRequestHandler - Implement pagination

### (Remaining ~17 features)

## Phase 5: Controller Updates ⏳ NOT STARTED

- [ ] EventController - Add pageNumber, pageSize parameters
- [ ] EventSessionController - Add pagination parameters
- [ ] OrganizationController - Add pagination parameters
- [ ] ActorController - Add pagination parameters
- [ ] LocationController - Add pagination parameters
- [ ] CategoryController - Add pagination parameters
- [ ] TagController - Add pagination parameters
- [ ] StorageObjectController - Add pagination parameters
- [ ] (remaining ~17 controllers)

## Phase 6: Testing & Validation ⏳ NOT STARTED

- [ ] Build solution: `dotnet build Explore.sln` - 0 errors
- [ ] Test Event pagination via Swagger/Scalar
- [ ] Test Organization pagination
- [ ] Verify Global Query Filters work (tenant isolation)
- [ ] Verify lookup tables still return full lists
- [ ] Test migrations still work

## Lookup Tables (NO CHANGES NEEDED) ✅

These remain unchanged:
- EventType, EventStatus, EventFormat, VisibilityType, RegistrationMode
- AudienceAge, AudienceGender, Madhab, Language, ApprovalStatus
- OrganizationRole, OrganizationPosition, ActorType, DidCustodyType
- FileType, TagType, OwnerType
