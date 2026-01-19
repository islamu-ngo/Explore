# Pagination and Global Query Filters Implementation Plan

**Last Updated**: 2026-01-15

## Executive Summary

Add **offset pagination** to all GetAll endpoints (~25 entities) and implement **EF Core Global Query Filters** for automatic multi-tenant filtering (22 tenant-scoped entities). Follows existing Clean Architecture and CQRS patterns.

## Current State Analysis

### Problems Identified:
1. **No Pagination**: All 43 controllers return `List<EntityListDto>` loading ALL data
2. **No Automatic Tenant Filtering**: Queries don't automatically filter by TenantId
3. **Performance Risk**: Large datasets will cause slow responses and memory issues

### Current Implementation:
- `IGenericRepository.GetAll()` returns `IReadOnlyList<T>` via `ToListAsync()`
- Handlers map entities to DTOs using AutoMapper
- Controllers return full lists without pagination parameters
- ITenantContext exists but not used in DbContext

## Proposed Future State

### Target Architecture:
1. **PaginatedResult<T>** wrapper for all list responses
2. **Global Query Filters** for automatic TenantId filtering
3. **ITenantEntity** interface for tenant-scoped entities
4. **Lookup tables unchanged** (small datasets, no TenantId)

### API Response Format:
```json
{
  "items": [...],
  "pageNumber": 1,
  "pageSize": 20,
  "totalCount": 150,
  "totalPages": 8,
  "hasPreviousPage": false,
  "hasNextPage": true
}
```

## Design Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| Pagination Type | Offset (Skip/Take) | Simpler, supports random access, acceptable for event data scale |
| Default Page Size | 20 | Balance between UX and performance |
| Max Page Size | 100 | Prevent excessive queries |
| Tenant Filtering | Global Query Filters | Automatic, consistent, EF Core best practice |
| Lookup Tables | NO pagination | Small datasets (<20 items), no TenantId |

## Implementation Phases

### Phase 1: Infrastructure Foundation (2-3 hours)

**1.1 Create ITenantEntity Interface**
- File: `Explore.Domain/Interfaces/ITenantEntity.cs`
- Effort: S
- Skills: `clean-architecture-rules`

**1.2 Create PaginatedResult<T> Class**
- File: `Explore.Application/Responses/PaginatedResult.cs`
- Effort: S
- Skills: `cqrs-mediatr-guidelines`

**1.3 Create PaginationParams Class**
- File: `Explore.Application/Requests/PaginationParams.cs`
- Effort: S

**1.4 Update IGenericRepository Interface**
- File: `Explore.Application/Contracts/Persistence/IGenericRepository.cs`
- Add: `Task<(IReadOnlyList<T> Items, int TotalCount)> GetAllPaged(int pageNumber, int pageSize)`
- Effort: S

**1.5 Update GenericRepository Implementation**
- File: `Explore.Persistence/Repositories/GenericRepository.cs`
- Implement Skip/Take pagination
- Effort: S

### Phase 2: Global Query Filters (2-3 hours)

**2.1 Add ITenantEntity to Domain Entities**
- Files: 22 domain entity files
- Pattern: `public class Event : ITenantEntity`
- Effort: M

**2.2 Update ExploreDbContext with Global Query Filters**
- File: `Explore.Persistence/ExploreDbContext.cs`
- Inject ITenantContext (nullable for migrations)
- Apply HasQueryFilter for all ITenantEntity types
- Effort: M
- Skills: `dotnet-efcore-guidelines`

### Phase 3: Repository Updates (~25 entities) (3-4 hours)

**3.1 Update Repository Interfaces**
- Add paged method signatures to each repository interface
- Pattern: `Task<(List<Entity>, int)> GetEntitiesWithDetailsPaged(int pageNumber, int pageSize)`
- Effort: M

**3.2 Update Repository Implementations**
- Implement paged queries with Skip/Take
- Include proper ordering for consistent pagination
- Effort: L

### Phase 4: Request/Handler Updates (~25 entities) (4-6 hours)

**4.1 Update Request Classes**
- Change: `IRequest<List<EntityListDto>>` → `IRequest<PaginatedResult<EntityListDto>>`
- Add: PageNumber, PageSize properties
- Effort: L
- Skills: `cqrs-mediatr-guidelines`

**4.2 Update Handler Classes**
- Call paged repository methods
- Create PaginatedResult with metadata
- Effort: L

### Phase 5: Controller Updates (~25 controllers) (3-4 hours)

**5.1 Update GetAll Endpoints**
- Add: `[FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20`
- Change return type: `ActionResult<PaginatedResult<EntityListDto>>`
- Update endpoint descriptions
- Effort: L

### Phase 6: Testing & Validation (2-3 hours)

**6.1 Build and Fix Errors**
- Run: `dotnet build Explore.sln`
- Fix any compilation errors
- Effort: M

**6.2 Manual API Testing**
- Test pagination via Scalar/Swagger
- Verify Global Query Filters work
- Test lookup tables still work
- Effort: M

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Global Query Filter breaks migrations | High | Null check: `_tenantContext == null` |
| Existing tests fail | Medium | Update tests to expect PaginatedResult |
| Performance regression | Low | Skip/Take is efficient with proper indexing |

## Success Metrics

1. All GetAll endpoints return `PaginatedResult<T>` with correct metadata
2. Global Query Filters automatically filter by TenantId
3. Lookup tables continue to work without pagination
4. Build succeeds with 0 errors
5. API documentation shows pagination parameters

## Entities Classification

### 22 Tenant-Scoped Entities (Get Global Query Filter + Pagination):
Event, EventSession, EventRegistration, EventCategories, EventTags,
EventSessionLanguage, EventSessionSpeaker, EventSessionAgendaItem,
Organization, OrganizationReview, OrganizationMember,
Actor, ActorKeyStore, Location, StorageObject,
Category, Tag, TagTypeTags,
UserAuthenticationToken, UserExternalLogin, UserRole,
TenantUser, TenantSettings

### 21 Lookup Tables (NO TenantId, NO Pagination):
EventType, EventStatus, EventFormat, VisibilityType, RegistrationMode,
AudienceAge, AudienceGender, Madhab, Language, ApprovalStatus,
OrganizationRole, OrganizationPosition, ActorType, DidCustodyType,
FileType, TagType, OwnerType

### 3 System Tables (May need pagination, NO tenant filter):
User, Tenant, IndexedDid, SyncState, AtprotoRecord
