# Soft Delete & Event Authorization - Implementation Plan

**Created**: 2026-01-19
**Status**: Phase 2 Complete, Phase 3 Ready to Start

---

## Executive Summary

Implementing two critical features for production readiness:

1. **Enhanced Event Deletion Authorization**:
   - Admins can delete any event
   - Organization Creators/CoOwners/Admins can delete organization events
   - Users can delete their own personal events

2. **Soft Delete with Audit Tracking**:
   - All deletions are soft (IsDeleted flag)
   - Track who deleted and when (DeletedAt, DeletedBy)
   - Automatic audit fields on create/update (CreatedAt, CreatedBy, UpdatedAt, UpdatedBy)
   - Use EF Core 10 named query filters

---

## Background & Motivation

### Current State Issues

**Authorization**:
- ❌ System admin endpoints were open or had weak authorization
- ❌ Organization approval endpoint was `[AllowAnonymous]` (critical security flaw)
- ❌ Event deletion only checks if event exists, no ownership validation
- ❌ No distinction between admin, organization member, and personal event ownership

**Data Management**:
- ❌ Hard deletes permanently remove data
- ❌ No audit trail for when/who deleted
- ❌ No way to recover accidentally deleted events
- ❌ No automatic tracking of created/updated timestamps and users

### Desired State

**Authorization** ✅:
- ✅ All system management endpoints require Admin role
- ✅ Event deletion respects organization membership and roles
- ✅ Clear authorization hierarchy: System Admin > Org Creator/CoOwner/Admin > Event Owner

**Data Management**:
- Soft delete preserves data with IsDeleted flag
- Full audit trail (created/updated/deleted by user and timestamp)
- Admin-only hard delete for permanent removal
- Automatic audit field population via EF Core SaveChanges override

---

## Architecture Overview

### Component Interaction

```
HTTP Request (DELETE /api/event/{id})
    ↓
EventController [Authorize]
    ↓
DeleteEventCommand → DeleteEventCommandHandler
    ↓
Authorization Check:
  ┌─ Check UserRole (Admin?) → Allow
  ├─ Check Event.Actor.OrganizationId
  │   └─ OrganizationMember (RoleId 1,2,3?) → Allow
  └─ Check Event.Actor.UserId == CurrentUser → Allow
    ↓
EventRepository.Delete(event)
    ↓
GenericRepository checks ISoftDeletable
    ↓
Sets IsDeleted=true, State=Modified
    ↓
DbContext.SaveChangesAsync()
    ↓
Audit fields auto-populated:
  - UpdatedAt = DateTime.UtcNow
  - UpdatedBy = CurrentUserId
  - DeletedAt = DateTime.UtcNow
  - DeletedBy = CurrentUserId
    ↓
EF Core Query Filter excludes IsDeleted=true
```

### Domain Model

```
┌─────────────────────────────────────────────┐
│           IAuditableEntity                  │
├─────────────────────────────────────────────┤
│ + CreatedAt: DateTime                       │
│ + CreatedBy: Guid?                          │
│ + UpdatedAt: DateTime?                      │
│ + UpdatedBy: Guid?                          │
└─────────────────────────────────────────────┘
                    ▲
                    │ implements
                    │
┌─────────────────────────────────────────────┐
│            ISoftDeletable                   │
├─────────────────────────────────────────────┤
│ + IsDeleted: bool                           │
│ + DeletedAt: DateTime?                      │
│ + DeletedBy: Guid?                          │
└─────────────────────────────────────────────┘
                    ▲
                    │ implements
                    │
┌─────────────────────────────────────────────┐
│               Event Entity                  │
├─────────────────────────────────────────────┤
│ + Id: Guid                                  │
│ + ActorId: Guid (FK)                        │
│ + Title, Description, ...                   │
│ + TenantId: Guid                            │
│ + [Audit Fields]                            │
│ + [Soft Delete Fields]                      │
└─────────────────────────────────────────────┘
                    │
                    │ references
                    ▼
┌─────────────────────────────────────────────┐
│               Actor Entity                  │
├─────────────────────────────────────────────┤
│ + Id: Guid                                  │
│ + UserId: Guid? (personal actor)            │
│ + OrganizationId: Guid? (org actor)         │
│ + [Audit Fields]                            │
│ + [Soft Delete Fields]                      │
└─────────────────────────────────────────────┘
                    │
                    │ references (if org)
                    ▼
┌─────────────────────────────────────────────┐
│          Organization Entity                │
├─────────────────────────────────────────────┤
│ + Id: Guid                                  │
│ + Members: OrganizationMember[]             │
│ + [Audit Fields]                            │
│ + [Soft Delete Fields]                      │
└─────────────────────────────────────────────┘
                    │
                    │ collection
                    ▼
┌─────────────────────────────────────────────┐
│        OrganizationMember Entity            │
├─────────────────────────────────────────────┤
│ + Id: Guid                                  │
│ + OrganizationId: Guid (FK)                 │
│ + UserId: Guid (FK)                         │
│ + OrganizationRoleId: int (1,2,3,4,5,6)     │
│ + [Audit Fields]                            │
│ + [Soft Delete Fields]                      │
└─────────────────────────────────────────────┘
```

---

## Implementation Phases

### ✅ Phase 1: Authorization Hardening (COMPLETE)

**Goal**: Secure all admin-only endpoints

**Changes Made**:
- Added `[Authorize(Roles="Admin")]` to 10 controllers (system, tenant, auth)
- Fixed critical security flaw in OrganizationController.UpdateStatusType

**Impact**: Production-ready authorization on admin endpoints

---

### ✅ Phase 2: Soft Delete Infrastructure (COMPLETE)

**Goal**: Create foundation for soft delete and audit tracking

**Deliverables**:
1. ✅ IAuditableEntity interface
2. ✅ ISoftDeletable interface
3. ✅ Updated 6 core entities (Event, Organization, OrganizationMember, EventSession, Actor, User)

**Impact**: Domain model ready for soft delete implementation

---

### 🟡 Phase 3: DbContext Implementation (IN PROGRESS)

**Goal**: Implement EF Core query filters and SaveChanges override

**Tasks**:
1. Add named "SoftDelete" query filters for all soft-deletable entities
2. Override SaveChangesAsync to auto-populate audit fields
3. Convert EntityState.Deleted to soft delete

**Files**:
- `Explore.Persistence/ExploreDbContext.cs`

**Complexity**: Medium (EF Core override patterns, HTTP context injection)

**Estimated Time**: 2-3 hours

**Risk**: Low (well-documented EF Core patterns)

---

### ⏳ Phase 4: Event Deletion Authorization (NOT STARTED)

**Goal**: Implement organization-aware authorization for event deletion

**Authorization Logic**:
```
IF user.Role == "Admin" THEN
    ALLOW (full system access)
ELSE
    event = GetEvent(eventId)
    actor = event.Actor

    IF actor.OrganizationId IS NOT NULL THEN
        // Organization event
        member = GetOrganizationMember(actor.OrganizationId, userId)
        IF member.RoleId IN (1, 2, 3) THEN  // Creator, CoOwner, Admin
            ALLOW
        ELSE
            FORBID
        END IF
    ELSE IF actor.UserId == userId THEN
        // Personal event owned by user
        ALLOW
    ELSE
        FORBID
    END IF
END IF
```

**Dependencies**:
- IOrganizationMemberRepository
- IUserRoleRepository
- IHttpContextAccessor

**Files**:
- `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`

**Complexity**: Medium (multi-table authorization logic)

**Estimated Time**: 2-3 hours

**Risk**: Medium (complex authorization logic, needs thorough testing)

---

### ⏳ Phase 5: Repository Updates (NOT STARTED)

**Goal**: Update generic repository to handle soft delete

**Changes**:
1. Modify `Delete()` to soft delete for ISoftDeletable entities
2. Add `HardDelete()` for permanent deletion (admin-only)
3. Update IGenericRepository interface

**Files**:
- `Explore.Persistence/Repositories/GenericRepository.cs`
- `Explore.Application/Contracts/Persistence/IGenericRepository.cs`

**Complexity**: Low (straightforward logic)

**Estimated Time**: 1 hour

**Risk**: Low (simple changes)

---

### ⏳ Phase 6: Database Migration (NOT STARTED)

**Goal**: Add audit and soft delete columns to database

**Migration Columns** (per entity):
- `created_at` (timestamp, NOT NULL)
- `created_by` (uuid, NULL)
- `updated_at` (timestamp, NULL)
- `updated_by` (uuid, NULL)
- `is_deleted` (boolean, NOT NULL, DEFAULT false)
- `deleted_at` (timestamp, NULL)
- `deleted_by` (uuid, NULL)

**Special Handling**:
- Organization already has `created_at`, `updated_at` (nullable)
- Need to backfill and make NOT NULL

**Commands**:
```bash
cd Explore.Persistence
dotnet ef migrations add AddAuditAndSoftDeleteFields -s ..\Explore.API\Explore.API.csproj
dotnet ef database update -s ..\Explore.API\Explore.API.csproj
```

**Complexity**: Medium (handle existing Organization columns)

**Estimated Time**: 1-2 hours

**Risk**: Medium (data migration, schema changes)

---

### ⏳ Phase 7: Testing & Verification (NOT STARTED)

**Goal**: Comprehensive testing of all features

**Test Categories**:
1. Soft Delete Functionality
   - IsDeleted flag behavior
   - Query filter exclusion
   - IgnoreQueryFilters behavior

2. Authorization Tests
   - Admin full access
   - Org Creator/CoOwner/Admin access
   - Org Moderator/Member denied
   - Personal event ownership

3. Audit Field Tests
   - CreatedAt/CreatedBy on insert
   - UpdatedAt/UpdatedBy on update
   - DeletedAt/DeletedBy on delete

**Complexity**: High (many test scenarios)

**Estimated Time**: 3-4 hours

**Risk**: High (authorization bugs can cause security issues)

---

### ⏳ Phase 8: Documentation & Cleanup (NOT STARTED)

**Goal**: Document new features and patterns

**Deliverables**:
- Update ARCHITECTURE.md
- Add inline code comments
- Document IgnoreQueryFilters usage
- API endpoint documentation

**Complexity**: Low

**Estimated Time**: 1 hour

**Risk**: Low

---

## Technical Specifications

### EF Core Named Query Filters (EF Core 10+)

**Syntax**:
```csharp
modelBuilder.Entity<Event>().HasQueryFilter(
    name: "SoftDelete",
    predicate: e => !e.IsDeleted);
```

**Benefits**:
- Selective filter disabling
- Better performance than global filters
- Clear intent in code

**Usage**:
```csharp
// Normal query (soft deleted excluded)
var events = await _context.Events.ToListAsync();

// Include soft deleted
var allEvents = await _context.Events
    .IgnoreQueryFilters("SoftDelete")
    .ToListAsync();
```

---

### SaveChanges Override Pattern

```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var userId = GetCurrentUserId();
    var now = DateTime.UtcNow;

    foreach (var entry in ChangeTracker.Entries())
    {
        // Handle IAuditableEntity
        if (entry.Entity is IAuditableEntity auditable)
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    auditable.CreatedAt = now;
                    auditable.CreatedBy = userId;
                    break;

                case EntityState.Modified:
                    auditable.UpdatedAt = now;
                    auditable.UpdatedBy = userId;
                    break;
            }
        }

        // Handle ISoftDeletable (convert hard delete to soft)
        if (entry.Entity is ISoftDeletable deletable &&
            entry.State == EntityState.Deleted)
        {
            entry.State = EntityState.Modified;
            deletable.IsDeleted = true;
            deletable.DeletedAt = now;
            deletable.DeletedBy = userId;
        }
    }

    return await base.SaveChangesAsync(ct);
}

private Guid? GetCurrentUserId()
{
    var userIdClaim = _httpContextAccessor?.HttpContext?.User?
        .FindFirst("sub")?.Value
        ?? _httpContextAccessor?.HttpContext?.User?
            .FindFirst(ClaimTypes.NameIdentifier)?.Value;

    return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
}
```

---

### Organization Role Authorization

**OrganizationRoleEnum** (Explore.Domain/Enums/OrganizationRoleEnum.cs):
```csharp
public enum OrganizationRoleEnum
{
    Creator = 1,      // Can delete events ✓
    CoOwner = 2,      // Can delete events ✓
    Admin = 3,        // Can delete events ✓
    Moderator = 4,    // Cannot delete events ✗
    Member = 5,       // Cannot delete events ✗
    Viewer = 6        // Cannot delete events ✗
}
```

**Delete Permission Check**:
```csharp
private async Task<bool> CanDeleteOrganizationEvent(
    Guid organizationId,
    Guid userId)
{
    var member = await _organizationMemberRepository
        .GetByOrganizationAndUser(organizationId, userId);

    if (member == null) return false;

    // Only Creator, CoOwner, Admin can delete
    return member.OrganizationRoleId <= 3;
}
```

---

## Risk Assessment

### High Risk Items

1. **Authorization Logic Complexity**
   - Multiple paths (Admin, Org member, Personal)
   - Easy to miss edge cases
   - **Mitigation**: Comprehensive test suite

2. **Data Migration**
   - Organization table already has audit fields
   - Risk of data loss during backfill
   - **Mitigation**: Test on dev/staging first, backup production

3. **Performance Impact**
   - Query filters add WHERE clauses
   - Potential N+1 queries in authorization checks
   - **Mitigation**: Add indexes, use eager loading

### Medium Risk Items

1. **IHttpContextAccessor in DbContext**
   - Null during migrations/seeding
   - **Mitigation**: Make nullable, handle gracefully

2. **Tenant Filter + Soft Delete Filter Interaction**
   - Two filters on same entity
   - **Mitigation**: Test thoroughly, document order

### Low Risk Items

1. **Repository Changes**
   - Straightforward logic
   - Well-defined interfaces

2. **Interface Additions**
   - Non-breaking changes
   - Backward compatible

---

## Success Metrics

### Functional Metrics

- ✅ All admin endpoints secured
- 🔲 100% of test scenarios passing
- 🔲 Zero authorization bypass vulnerabilities
- 🔲 Soft delete query filters working correctly
- 🔲 Audit fields auto-populated on all operations

### Performance Metrics

- 🔲 No significant query performance degradation (<10%)
- 🔲 Authorization checks complete in <50ms
- 🔲 Soft delete filter overhead <5ms per query

### Code Quality Metrics

- ✅ All entities implement consistent interfaces
- 🔲 Authorization logic unit tested (>90% coverage)
- 🔲 Integration tests for end-to-end flows
- 🔲 Documentation updated and accurate

---

## Timeline Estimate

| Phase | Estimated Time | Status |
|-------|----------------|--------|
| 1. Authorization Hardening | 2 hours | ✅ Complete |
| 2. Infrastructure | 2 hours | ✅ Complete |
| 3. DbContext | 2-3 hours | 🟡 Next |
| 4. Authorization Logic | 2-3 hours | ⏳ Waiting |
| 5. Repository Updates | 1 hour | ⏳ Waiting |
| 6. Database Migration | 1-2 hours | ⏳ Waiting |
| 7. Testing | 3-4 hours | ⏳ Waiting |
| 8. Documentation | 1 hour | ⏳ Waiting |
| **Total** | **14-18 hours** | **~25% Complete** |

**Current Progress**: 4 hours complete, 10-14 hours remaining

---

## Rollback Plan

If issues arise in production:

1. **Authorization Issues**:
   - Revert controller authorization attributes
   - Deploy previous version
   - Fix issues in dev

2. **Soft Delete Issues**:
   - Remove query filters via migration
   - Set all IsDeleted = false
   - Hard delete truly deleted records

3. **Database Migration Issues**:
   - Revert migration: `dotnet ef migrations remove`
   - Restore database backup
   - Fix migration script

---

## Related Documentation

- [QUICK_REFERENCE.md](../../../docs/QUICK_REFERENCE.md) - Clean Architecture rules
- [GOVERNANCE.md](../../../docs/GOVERNANCE.md) - Coding conventions
- [OrganizationRoleEnum.cs](../../../Explore.Domain/Enums/OrganizationRoleEnum.cs) - Role IDs
- EF Core Named Filters: https://github.com/dotnet/entityframework.docs/blob/main/entity-framework/core/querying/filters.md
