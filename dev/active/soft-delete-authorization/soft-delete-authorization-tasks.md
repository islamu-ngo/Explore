# Soft Delete & Event Authorization - Task Checklist

**Last Updated**: 2026-01-19

---

## Phase 1: Authorization Hardening ✅ COMPLETE

- [x] Audit all API controllers for missing authorization
- [x] Add [Authorize(Roles="Admin")] to TenantController (all ops)
- [x] Add [Authorize(Roles="Admin")] to TenantSettingsController (all ops)
- [x] Add [Authorize(Roles="Admin")] to TenantUserController (all ops)
- [x] Add [Authorize(Roles="Admin")] to SyncStateController (all ops)
- [x] Add [Authorize(Roles="Admin")] to IndexedDidController (all ops)
- [x] Add [Authorize(Roles="Admin")] to AtprotoRecordController (all ops)
- [x] Add [Authorize(Roles="Admin")] to ActorKeyStoreController (all ops)
- [x] Add [Authorize(Roles="Admin")] to UserAuthenticationTokenController (all ops)
- [x] Add [Authorize(Roles="Admin")] to UserExternalLoginController (all ops)
- [x] Fix OrganizationController.UpdateStatusType (was AllowAnonymous!)
- [x] Verify user-facing entities keep proper [AllowAnonymous] for GET, [Authorize] for writes

**Acceptance**: All admin-only endpoints secured, production-ready

---

## Phase 2: Soft Delete Infrastructure ✅ COMPLETE

- [x] Research EF Core 10 named query filters documentation
- [x] Research SaveChanges override patterns for audit fields
- [x] Analyze OrganizationMember and role authorization patterns
- [x] Create IAuditableEntity interface (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- [x] Create ISoftDeletable interface (IsDeleted, DeletedAt, DeletedBy)
- [x] Update Event entity with audit + soft delete fields
- [x] Update Organization entity with audit + soft delete fields
- [x] Update OrganizationMember entity with audit + soft delete fields
- [x] Update EventSession entity with audit + soft delete fields
- [x] Update Actor entity with audit + soft delete fields
- [x] Update User entity with audit + soft delete fields

**Acceptance**: All major entities implement IAuditableEntity and ISoftDeletable

---

## Phase 3: DbContext Implementation 🟡 IN PROGRESS

### 3.1: Named Query Filters for Soft Delete
- [ ] Add IHttpContextAccessor to ExploreDbContext constructor
- [ ] Create private GetCurrentUserId() helper method
- [ ] Add named "SoftDelete" query filter for Event entity
  - **Acceptance**: Filter excludes IsDeleted=true events by default
- [ ] Add named "SoftDelete" query filter for Organization entity
- [ ] Add named "SoftDelete" query filter for OrganizationMember entity
- [ ] Add named "SoftDelete" query filter for EventSession entity
- [ ] Add named "SoftDelete" query filter for Actor entity
- [ ] Add named "SoftDelete" query filter for User entity
  - **Acceptance**: All 6 entities have working soft delete filters

### 3.2: SaveChanges Override for Audit Fields
- [ ] Enhance SaveChangesAsync to detect IAuditableEntity entries
- [ ] Set CreatedAt = DateTime.UtcNow for EntityState.Added
- [ ] Set CreatedBy = GetCurrentUserId() for EntityState.Added
- [ ] Set UpdatedAt = DateTime.UtcNow for EntityState.Modified
- [ ] Set UpdatedBy = GetCurrentUserId() for EntityState.Modified
  - **Acceptance**: Audit fields auto-populate on save

### 3.3: Soft Delete on EntityState.Deleted
- [ ] Detect EntityState.Deleted for ISoftDeletable entities
- [ ] Change EntityState.Deleted → EntityState.Modified
- [ ] Set IsDeleted = true
- [ ] Set DeletedAt = DateTime.UtcNow
- [ ] Set DeletedBy = GetCurrentUserId()
  - **Acceptance**: Hard deletes converted to soft deletes automatically

**File**: `Explore.Persistence/ExploreDbContext.cs` (lines 44-100)

---

## Phase 4: Event Deletion Authorization ⏳ NOT STARTED

### 4.1: Update DeleteEventCommandHandler Dependencies
- [ ] Add IOrganizationMemberRepository field
- [ ] Add IUserRoleRepository field
- [ ] Add IHttpContextAccessor field
- [ ] Update constructor to inject all dependencies
  - **Acceptance**: Handler has access to all needed repositories

### 4.2: Implement Admin Check
- [ ] Extract userId from HTTP context (sub claim with fallbacks)
- [ ] Query UserRole table for Admin role (MasterCode="Admin")
- [ ] If user is Admin → Return true (full access)
  - **Acceptance**: Admins can delete any event

### 4.3: Implement Organization Event Authorization
- [ ] Get event's actor from ActorRepository
- [ ] Check if actor.OrganizationId has value
- [ ] If yes: Query OrganizationMember where OrganizationId AND UserId
- [ ] Check if OrganizationRoleId in (1=Creator, 2=CoOwner, 3=Admin)
- [ ] If match → Return true (org admin can delete)
- [ ] If no match → Return false (unauthorized)
  - **Acceptance**: Org Creators/CoOwners/Admins can delete org events

### 4.4: Implement Personal Event Authorization
- [ ] Check if actor.UserId == current userId
- [ ] If yes → Return true (owner can delete own event)
- [ ] If no → Return false (unauthorized)
  - **Acceptance**: Users can delete their own personal events

**File**: `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`

---

## Phase 5: Repository Updates ⏳ NOT STARTED

### 5.1: Update GenericRepository Delete Method
- [ ] Check if entity implements ISoftDeletable
- [ ] If yes: Set IsDeleted=true, change state to Modified
- [ ] If no: Keep existing Remove() behavior
- [ ] SaveChangesAsync will handle audit fields
  - **Acceptance**: Soft deletable entities use soft delete

### 5.2: Add HardDelete Method
- [ ] Create new HardDelete method in GenericRepository
- [ ] Always use Remove() for actual database deletion
- [ ] Reserved for admin-only operations
  - **Acceptance**: HardDelete permanently removes from DB

### 5.3: Update IGenericRepository Interface
- [ ] Add Task HardDelete(T entity) signature
  - **Acceptance**: Interface matches implementation

**Files**:
- `Explore.Persistence/Repositories/GenericRepository.cs`
- `Explore.Application/Contracts/Persistence/IGenericRepository.cs`

---

## Phase 6: Database Migration ⏳ NOT STARTED

### 6.1: Create Migration
- [ ] Run: `cd Explore.Persistence`
- [ ] Run: `dotnet ef migrations add AddAuditAndSoftDeleteFields -s ..\Explore.API\Explore.API.csproj`
- [ ] Verify migration includes all audit columns (created_at, created_by, etc.)
- [ ] Verify migration includes all soft delete columns (is_deleted, deleted_at, deleted_by)
  - **Acceptance**: Migration file created successfully

### 6.2: Handle Organization Special Case
- [ ] Organization already has CreatedAt/UpdatedAt (nullable)
- [ ] Migration should:
  - Add created_by, updated_by columns
  - Add is_deleted (boolean, default false)
  - Add deleted_at, deleted_by columns
  - Backfill existing created_at values
  - Make created_at NOT NULL (if currently nullable)
  - **Acceptance**: No data loss, all constraints correct

### 6.3: Apply Migration
- [ ] Review migration SQL
- [ ] Apply migration: `dotnet ef database update -s ..\Explore.API\Explore.API.csproj`
- [ ] Verify columns exist in database
  - **Acceptance**: Database schema updated

---

## Phase 7: Testing & Verification ⏳ NOT STARTED

### 7.1: Soft Delete Functionality Tests
- [ ] Create event, soft delete, verify IsDeleted=true
- [ ] Query events, verify soft deleted event not returned
- [ ] Use .IgnoreQueryFilters("SoftDelete"), verify soft deleted event returned
- [ ] Verify DeletedAt and DeletedBy populated correctly
  - **Acceptance**: Soft delete working as expected

### 7.2: Authorization Tests - Admin
- [ ] Admin user deletes any event (should succeed)
- [ ] Admin user deletes org event they don't belong to (should succeed)
- [ ] Admin user deletes another user's personal event (should succeed)
  - **Acceptance**: Admins have full delete access

### 7.3: Authorization Tests - Organization Events
- [ ] Org Creator deletes org event (should succeed)
- [ ] Org CoOwner deletes org event (should succeed)
- [ ] Org Admin deletes org event (should succeed)
- [ ] Org Moderator tries to delete org event (should fail)
- [ ] Org Member tries to delete org event (should fail)
- [ ] Non-member tries to delete org event (should fail)
  - **Acceptance**: Only Creator/CoOwner/Admin can delete org events

### 7.4: Authorization Tests - Personal Events
- [ ] User deletes their own personal event (should succeed)
- [ ] User tries to delete another user's personal event (should fail)
  - **Acceptance**: Users can only delete their own events

### 7.5: Audit Field Tests
- [ ] Create event, verify CreatedAt populated
- [ ] Create event as authenticated user, verify CreatedBy populated
- [ ] Update event, verify UpdatedAt populated
- [ ] Update event as authenticated user, verify UpdatedBy populated
  - **Acceptance**: Audit fields working correctly

---

## Phase 8: Documentation & Cleanup ⏳ NOT STARTED

- [ ] Update ARCHITECTURE.md with soft delete explanation
- [ ] Document .IgnoreQueryFilters("SoftDelete") usage for admins
- [ ] Add code comments explaining authorization logic
- [ ] Update API endpoint documentation for delete operations
  - **Acceptance**: Documentation complete and accurate

---

## Known Issues / Technical Debt

**None currently** - Fresh implementation

---

## Performance Considerations

- **Query filters add WHERE clauses** - Should use indexes on is_deleted column
- **Consider adding index**: CREATE INDEX idx_events_is_deleted ON events(is_deleted) WHERE is_deleted = false;
- **Soft delete accumulation** - May need periodic hard delete job for old soft-deleted records

---

## Security Considerations

- ✅ Admin authorization properly secured
- ✅ Organization approval endpoint fixed (was AllowAnonymous)
- ⚠️ HardDelete method should only be exposed to system administrators
- ⚠️ Consider audit log for hard deletes (permanent data loss)
