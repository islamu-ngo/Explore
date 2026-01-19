# Soft Delete & Event Authorization - Context

**Last Updated**: 2026-01-19
**Status**: 🟡 IN PROGRESS - Infrastructure complete, DbContext updates needed

---

## SESSION PROGRESS

### ✅ COMPLETED

#### 1. Authorization Updates (Production-Ready)
- **Added `[Authorize(Roles="Admin")]` to ALL admin-only endpoints**:
  - System Management: TenantController, TenantSettingsController, TenantUserController, SyncStateController, IndexedDidController, AtprotoRecordController, ActorKeyStoreController (ALL operations)
  - Auth/Security: UserAuthenticationTokenController, UserExternalLoginController (ALL operations)
  - Organization: OrganizationController.UpdateStatusType (approve/reject organizations)

- **Security Fix**: OrganizationController.UpdateStatusType was `[AllowAnonymous]` - now `[Authorize(Roles="Admin")]`

#### 2. Soft Delete Infrastructure
- **Created domain interfaces**:
  - `Explore.Domain/Interfaces/IAuditableEntity.cs` - CreatedAt, UpdatedAt, CreatedBy, UpdatedBy
  - `Explore.Domain/Interfaces/ISoftDeletable.cs` - IsDeleted, DeletedAt, DeletedBy

- **Updated domain entities with audit + soft delete fields**:
  - ✅ Event (Explore.Domain/Event.cs)
  - ✅ Organization (Explore.Domain/Organization.cs)
  - ✅ OrganizationMember (Explore.Domain/OrganizationMember.cs)
  - ✅ EventSession (Explore.Domain/EventSession.cs)
  - ✅ Actor (Explore.Domain/Actor.cs)
  - ✅ User (Explore.Domain/User.cs)

#### 3. Research Complete
- Studied EF Core 10 named query filters documentation
- Analyzed organization member authorization pattern (OrganizationRoleEnum: Creator=1, CoOwner=2, Admin=3, Moderator=4, Member=5, Viewer=6)
- Identified current event deletion flow: DeleteEventCommandHandler → EventRepository → Currently hard delete

---

### 🟡 IN PROGRESS
**None** - Ready to start DbContext updates

---

### ⚠️ BLOCKERS
**None**

---

## Key Files Modified

### Domain Interfaces (NEW)
1. **Explore.Domain/Interfaces/IAuditableEntity.cs**
   - Purpose: Standardize audit tracking across entities
   - Properties: CreatedAt, CreatedBy, UpdatedAt, UpdatedBy
   - Used by: Event, Organization, OrganizationMember, EventSession, Actor, User

2. **Explore.Domain/Interfaces/ISoftDeletable.cs**
   - Purpose: Enable soft delete with EF Core query filters
   - Properties: IsDeleted, DeletedAt, DeletedBy
   - Used by: Same entities as IAuditableEntity

### Domain Entities Updated
All entities now implement `IAuditableEntity, ISoftDeletable`:
- **Event**: Main entity for events
- **Organization**: Organization management with existing CreatedAt/UpdatedAt (enhanced)
- **OrganizationMember**: Links users to organizations with roles
- **EventSession**: Individual sessions within events
- **Actor**: Federation identity (users, orgs, services)
- **User**: User accounts

### Controllers (Authorization Updates)
- **TenantController** - All ops now admin-only
- **OrganizationController** - UpdateStatusType fixed (was AllowAnonymous!)
- 8 other system/auth controllers - All admin-only

---

## Important Decisions Made

### 1. Audit Field Design
- **CreatedAt is NOT nullable**: Every entity must have creation timestamp
- **UpdatedAt IS nullable**: Only set on actual updates
- **User IDs are nullable**: System-created entities may not have user context

### 2. Organization Role IDs (from OrganizationRoleEnum)
- **Creator (1)**: Original organization creator
- **CoOwner (2)**: Co-owner with full permissions
- **Admin (3)**: Administrator
- **Moderator (4)**: Content moderation
- **Member (5)**: Regular member
- **Viewer (6)**: Read-only access

**Event Deletion Permission**: Creator (1), CoOwner (2), or Admin (3) can delete organization events

### 3. Soft Delete Strategy
- Use EF Core 10 **named query filters** for soft delete
- Filter name: `"SoftDelete"`
- Filter predicate: `!entity.IsDeleted`
- Allows selective disabling: `.IgnoreQueryFilters("SoftDelete")`

---

## Entity Relationships Critical for Event Deletion

```
Event
  └─> ActorId (Guid)
       └─> Actor
            ├─> UserId (Guid?) - If personal event
            └─> OrganizationId (Guid?) - If organization event
                 └─> Organization
                      └─> Members (OrganizationMember collection)
                           ├─> UserId
                           └─> OrganizationRoleId (1=Creator, 2=CoOwner, 3=Admin)
```

**Authorization Logic**:
1. Check if user has `Admin` role in UserRole table → Full access
2. Get event's ActorId
3. Get actor's OrganizationId
4. If OrganizationId exists:
   - Query OrganizationMember where OrganizationId AND UserId
   - Check if OrganizationRoleId in (1, 2, 3)
5. If personal event (actor.UserId == current user) → Allow delete

---

## Technical Constraints

### EF Core Query Filters
- **Named filters** require EF Core 10+
- Syntax: `modelBuilder.Entity<T>().HasQueryFilter(name: "FilterName", predicate: expr)`
- Disable globally: `.IgnoreQueryFilters()`
- Disable specific: `.IgnoreQueryFilters("SoftDelete")`

### SaveChanges Override Pattern
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
{
    var userId = GetCurrentUserId(); // From IHttpContextAccessor
    var now = DateTime.UtcNow;

    foreach (var entry in ChangeTracker.Entries())
    {
        if (entry.Entity is IAuditableEntity auditable)
        {
            if (entry.State == EntityState.Added)
            {
                auditable.CreatedAt = now;
                auditable.CreatedBy = userId;
            }
            else if (entry.State == EntityState.Modified)
            {
                auditable.UpdatedAt = now;
                auditable.UpdatedBy = userId;
            }
        }

        if (entry.Entity is ISoftDeletable deletable && entry.State == EntityState.Deleted)
        {
            entry.State = EntityState.Modified;
            deletable.IsDeleted = true;
            deletable.DeletedAt = now;
            deletable.DeletedBy = userId;
        }
    }

    return await base.SaveChangesAsync(ct);
}
```

---

## Next Immediate Steps (Priority Order)

### 1. Update ExploreDbContext (Explore.Persistence/ExploreDbContext.cs)
**Current Location**: Line 44-84 (ApplyGlobalQueryFilters method)

**Tasks**:
1. Add named soft delete filters for each entity:
   ```csharp
   modelBuilder.Entity<Event>().HasQueryFilter(
       name: "SoftDelete",
       predicate: e => !e.IsDeleted);
   // Repeat for Organization, OrganizationMember, EventSession, Actor, User
   ```

2. Override SaveChangesAsync (currently minimal at lines 86-100):
   - Add IHttpContextAccessor injection
   - Implement audit field auto-population
   - Convert hard deletes to soft deletes
   - Keep tenant validation logic

3. Add GetCurrentUserId helper method

### 2. Update DeleteEventCommandHandler
**File**: `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`
**Current**: Lines 21-44 - Basic existence check, no real authorization

**Add Dependencies**:
```csharp
private readonly IOrganizationMemberRepository _organizationMemberRepository;
private readonly IUserRoleRepository _userRoleRepository;
private readonly IHttpContextAccessor _httpContextAccessor;
```

**Authorization Logic** (pseudocode):
```csharp
// 1. Extract userId from token (sub claim)
// 2. Check if user has Admin role → Allow
// 3. Get event's actor
// 4. If actor.OrganizationId:
//    - Get org member where OrganizationId AND UserId
//    - Check if OrganizationRoleId in (1, 2, 3) → Allow
// 5. If actor.UserId == userId → Allow (personal event)
// 6. Else → Forbid
```

### 3. Update Generic Repository Delete Method
**File**: `Explore.Persistence/Repositories/GenericRepository.cs`
**Current**: Hard delete at line ~35-40

**Change**:
```csharp
public async Task Delete(T entity)
{
    if (entity is ISoftDeletable deletable)
    {
        deletable.IsDeleted = true;
        _dbContext.Entry(entity).State = EntityState.Modified;
    }
    else
    {
        _dbContext.Set<T>().Remove(entity);
    }
    await _dbContext.SaveChangesAsync();
}
```

**Add new method**:
```csharp
public async Task HardDelete(T entity)
{
    _dbContext.Set<T>().Remove(entity);
    await _dbContext.SaveChangesAsync();
}
```

### 4. Create EF Core Migration
```bash
cd Explore.Persistence
dotnet ef migrations add AddAuditAndSoftDeleteFields -s ..\Explore.API\Explore.API.csproj
```

**Expected columns** (per entity):
- created_at (timestamp with time zone, NOT NULL)
- created_by (uuid, NULL)
- updated_at (timestamp with time zone, NULL)
- updated_by (uuid, NULL)
- is_deleted (boolean, NOT NULL, DEFAULT false)
- deleted_at (timestamp with time zone, NULL)
- deleted_by (uuid, NULL)

### 5. Update IGenericRepository Interface
**File**: `Explore.Application/Contracts/Persistence/IGenericRepository.cs`

Add:
```csharp
Task HardDelete(T entity);
```

---

## Testing Checklist (After Implementation)

### Soft Delete Tests
- [ ] Event soft delete sets IsDeleted=true
- [ ] Soft deleted events don't appear in GetAll queries
- [ ] .IgnoreQueryFilters() returns soft deleted events
- [ ] Audit fields (DeletedAt, DeletedBy) populated correctly

### Authorization Tests
- [ ] Admin can delete any event
- [ ] Organization Creator can delete org event
- [ ] Organization CoOwner can delete org event
- [ ] Organization Admin can delete org event
- [ ] Organization Moderator CANNOT delete org event
- [ ] Organization Member CANNOT delete org event
- [ ] Non-member CANNOT delete org event
- [ ] User can delete their personal event
- [ ] User CANNOT delete another user's personal event

### Audit Tests
- [ ] CreatedAt set on entity creation
- [ ] CreatedBy set on entity creation (if user context available)
- [ ] UpdatedAt set on entity update
- [ ] UpdatedBy set on entity update

---

## Tricky Bits / Watch Outs

### 1. IHttpContextAccessor in DbContext
- DbContext needs `IHttpContextAccessor` injected for userId
- Add to constructor: `private readonly IHttpContextAccessor? _httpContextAccessor;`
- Make nullable - migrations and seeding won't have HTTP context
- Extract userId safely:
  ```csharp
  private Guid? GetCurrentUserId()
  {
      var userIdClaim = _httpContextAccessor?.HttpContext?.User?.FindFirst("sub")?.Value
          ?? _httpContextAccessor?.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

      return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
  }
  ```

### 2. Organization Null Check
- Actor can have EITHER UserId OR OrganizationId, not both
- Check both:
  ```csharp
  if (actor.OrganizationId.HasValue)
  {
      // Organization event - check membership
  }
  else if (actor.UserId.HasValue)
  {
      // Personal event - check ownership
  }
  ```

### 3. Tenant Query Filters + Soft Delete Filters
- Both filters must coexist
- Order matters in some EF Core versions
- Apply tenant filter first, then soft delete filter

### 4. Migration Conflicts
- Organization already has CreatedAt/UpdatedAt (nullable)
- Migration needs to:
  1. Add new nullable audit fields
  2. Backfill CreatedAt with existing value OR DateTime.UtcNow
  3. Make CreatedAt NOT NULL
  4. Keep UpdatedAt nullable

---

## References

### Documentation
- EF Core 10 Named Query Filters: https://github.com/dotnet/entityframework.docs/blob/main/entity-framework/core/querying/filters.md
- SaveChanges Interceptors: https://github.com/dotnet/entityframework.docs/blob/main/entity-framework/core/logging-events-diagnostics/interceptors.md

### Key Files to Reference
- `Explore.Domain/Enums/OrganizationRoleEnum.cs` - Role IDs
- `Explore.Application/Features/OrganizationMembers/Handlers/Commands/AddOrganizationMemberCommandHandler.cs` - Example org role checks
- `Explore.Persistence/ExploreDbContext.cs` - Existing tenant filters

---

## Quick Resume Instructions

**To continue from where we left off**:

1. Read this file
2. Start with task 1: Update ExploreDbContext
3. Then task 2: Update DeleteEventCommandHandler
4. Then task 3: Update repositories
5. Create migration
6. Test authorization flow

**Files to edit next**:
- `Explore.Persistence/ExploreDbContext.cs` (lines 44-100)
- `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`
- `Explore.Persistence/Repositories/GenericRepository.cs`
- `Explore.Application/Contracts/Persistence/IGenericRepository.cs`
