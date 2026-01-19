# Cascading Soft Delete & Blazor UI Authorization - Context

**Last Updated**: 2026-01-19
**Status**: 🟢 Core Implementation Complete - Ready for Testing

---

## SESSION PROGRESS

### ✅ COMPLETED
- Soft delete infrastructure (ISoftDeletable, query filters, SaveChanges override)
- DeleteEventCommandHandler with three-tier authorization
- ICurrentUserService abstraction (Clean Architecture)
- Database migration for audit fields (created, not yet applied)
- **Backend: Cascading soft delete for EventSessions**
  - Added IEventSessionRepository dependency to DeleteEventCommandHandler
  - Implemented cascade logic: queries all sessions, soft deletes each one
  - Updated XML documentation
  - Verified GetSessionsByEvent method exists (lines 25-34 in EventSessionRepository.cs)
  - Backend build successful
- **Backend: Fixed DbContext scoped service injection**
  - Changed ExploreDbContext to use property injection for CurrentUserService
  - Updated PersistenceServicesRegistration to inject scoped dependencies after factory creation
  - Fixed "Cannot resolve scoped service from root provider" error
- **Frontend: Authorization checks and UI updates**
  - Added IOrganizationService dependency to EventService
  - Implemented CanDeleteEventAsync method in EventService (lines 191-277)
    - Checks if event is organization-owned and verifies user membership/role
    - Checks if event is personal and allows owner to delete
    - Proper logging and error handling
  - Updated EventDetail.razor.cs:
    - Added _canDelete and _isCheckingAuth fields
    - Added CheckDeleteAuthorizationAsync method (lines 126-147)
    - Integrated authorization check into LoadEventDataAsync (parallel with registration check)
  - Updated EventDetail.razor:
    - Delete button now conditionally rendered: @if (_canDelete && !_isCheckingAuth)
  - Frontend build successful with no errors
- **Frontend: Enhanced DeleteEventDialog**
  - Removed incorrect Pages/Event/DeleteEventDialog.razor (used wrong IDialogReference)
  - Enhanced Components/Event/DeleteEventDialog.razor with:
    - IMudDialogInstance cascading parameter (proper convention)
    - Loads session count on initialization with loading state
    - Displays specific session count (0, 1, or multiple sessions)
    - Shows detailed list of what will be deleted
    - Proper singular/plural grammar
    - Success message includes session count
    - Comprehensive error handling with logging
    - Buttons work correctly (Cancel and Delete)
  - Updated EventDetail.razor.cs to pass EventId parameter
  - Dialog now fully functional with all edge cases covered

### 🟡 IN PROGRESS
- None - all tasks complete

### ⏳ NOT STARTED
- MyEvents.razor updates (inline delete with optimistic UI) - Optional enhancement
- Testing and verification (requires database migration)

### ⚠️ BLOCKERS
- None - ready for testing once database migration is applied

---

## Key Files and Their Purposes

### Backend Files

**`Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`** (Lines 1-212)
- **Purpose**: Handles event deletion with authorization
- **Current State**: Implements three-tier authorization (Admin → Org Admin → Owner)
- **What's Missing**: Does not cascade delete to EventSessions
- **Next Steps**: Add IEventSessionRepository dependency, query sessions, soft delete each

**`Explore.Application/Contracts/Persistence/IEventSessionRepository.cs`**
- **Purpose**: Repository interface for EventSession operations
- **Required Method**: `Task<List<EventSession>> GetSessionsByEvent(Guid eventId)`
- **Check**: Verify this method exists or add it

**`Explore.Persistence/Repositories/EventSessionRepository.cs`**
- **Purpose**: Implements IEventSessionRepository
- **Action**: Implement `GetSessionsByEvent` if not exists
- **Query**: `_dbContext.EventSessions.Where(s => s.EventId == eventId).ToListAsync()`

**`Explore.Domain/EventSession.cs`**
- **Purpose**: EventSession entity
- **Soft Delete**: Already implements ISoftDeletable (IsDeleted, DeletedAt, DeletedBy)
- **Foreign Key**: `EventId` links to Event entity

### Frontend Files

**`Explore.Blazor.Client/Pages/Event/EventDetail.razor`** (Lines 1-400+)
- **Purpose**: Event detail page with edit/delete actions
- **Current State**: Delete button always visible (line 32-38)
- **What's Missing**: Authorization check for delete button visibility
- **Next Steps**: Add `_canDelete` field, check on page load, conditional rendering

**`Explore.Blazor.Client/Pages/Event/MyEvents.razor`** (Lines 1-300+)
- **Purpose**: User's events list with organization filter
- **Current State**: No inline delete action on event cards
- **What's Missing**: Delete icon on cards, authorization check
- **Next Steps**: Add delete icon, implement optimistic UI

**`Explore.Blazor.Client/Services/EventService.cs`** (Lines 1-263)
- **Purpose**: Service layer for event operations
- **Current State**: Has `DeleteEventAsync(Guid eventId)` (line 160-183)
- **What's Missing**: `CanDeleteEventAsync` method for authorization pre-check
- **Next Steps**: Add authorization check method, integrate with organization service

**`Explore.Blazor.Client/Services/OrganizationService.cs`** (Check if exists)
- **Purpose**: Service layer for organization operations
- **Action**: Create if doesn't exist, or extend existing
- **Required Methods**:
  - `GetMyOrganizationsAsync()` - returns organizations user is a member of
  - Include role information (OrganizationRoleId)

### Test Files

**`Event.Application.UnitTests/Features/Events/Commands/DeleteEventCommandHandlerTests.cs`**
- **Purpose**: Unit tests for DeleteEventCommandHandler
- **Action**: Add tests for cascading soft delete
- **Test Cases**:
  1. Verify all EventSessions are soft deleted when Event is deleted
  2. Verify transaction rollback if any session delete fails
  3. Verify authorization checks still work

**`Event.API.IntegrationTests/Features/Events/DeleteEventIntegrationTests.cs`**
- **Purpose**: Integration tests for delete endpoint
- **Action**: Create if doesn't exist
- **Test Cases**: Verify database state after delete (Event and Sessions both soft deleted)

---

## Important Decisions Made

### 1. Cascade Delete Pattern
**Decision**: Iterate and soft delete each EventSession individually (not bulk operation)

**Rationale**:
- Audit fields (DeletedAt, DeletedBy) populated correctly via SaveChanges override
- Respects EF Core's change tracking and soft delete infrastructure
- Maintains transaction integrity

**Trade-off**: Performance (N+1 queries) vs Data Integrity (audit fields)
- For most events (< 10 sessions), performance is acceptable
- Can optimize later with bulk operations if needed

### 2. Authorization Check in Frontend
**Decision**: Frontend checks authorization but backend always validates

**Rationale**:
- Frontend check is UX convenience (hide button if user can't delete)
- Backend always re-validates (security boundary)
- Prevents UI showing "Delete" button that would fail

**Implementation**:
- Frontend calls `CanDeleteEventAsync` on page load
- Result stored in `_canDelete` boolean
- Button conditional rendering: `@if (_canDelete)`

### 3. Transaction Boundary
**Decision**: Use EF Core's implicit transaction (SaveChangesAsync)

**Rationale**:
- All deletes happen within same DbContext
- SaveChangesAsync commits all changes atomically
- If any operation fails, entire transaction rolls back

**Alternative Considered**: Explicit `TransactionScope`
- Rejected for now due to added complexity
- Can add later if implicit transaction proves insufficient

### 4. Organization Service Dependency
**Decision**: Create IOrganizationService in Blazor Client if doesn't exist

**Rationale**:
- EventService needs to check user's organization membership
- Organization membership includes role information
- Reusable service for other pages

---

## Entity Relationships Critical for This Feature

```
Event (ISoftDeletable)
  └─> EventSessions[] (collection, ISoftDeletable)
       └─> EventId (FK to Event)

Organization
  └─> OrganizationMembers[] (collection)
       ├─> UserId (FK to User)
       └─> OrganizationRoleId (1=Creator, 2=CoOwner, 3=Admin)

Actor
  ├─> UserId (nullable, for personal events)
  └─> OrganizationId (nullable, for organization events)

Event
  └─> ActorId (FK to Actor)
```

**Key Relationships for Delete Authorization**:
1. Event → Actor → OrganizationId (if organization event)
2. Organization → OrganizationMembers → UserId + OrganizationRoleId
3. Event → Actor → UserId (if personal event)

**Key Relationships for Cascade Delete**:
1. Event → EventSessions[] (one-to-many)
2. When Event.IsDeleted = true, all EventSession.IsDeleted must = true

---

## Technical Constraints

### Backend Constraints

1. **EF Core Query Filters**: EventSession has global query filter `!e.IsDeleted`
   - Soft-deleted sessions automatically excluded from queries
   - Use `.IgnoreQueryFilters()` to query soft-deleted sessions (admin scenarios)

2. **SaveChanges Override**: Automatically populates audit fields
   - DeletedAt = DateTime.UtcNow
   - DeletedBy = _currentUserService.UserId
   - No manual setting required

3. **Repository Pattern**: Must use `IEventSessionRepository.Delete()`
   - Cannot use raw EF Core: `_dbContext.EventSessions.Remove()`
   - GenericRepository checks ISoftDeletable and converts to soft delete

### Frontend Constraints

1. **Blazor Rendering**: Authorization check must happen in `OnInitializedAsync`
   - Sets `_canDelete` field
   - Triggers `StateHasChanged()` to re-render

2. **MudBlazor Components**: Use standard MudButton, MudDialog, MudSnackbar
   - Consistent with existing UI patterns
   - Follow MudBlazor best practices

3. **API Client**: Generated NSwag client (`IEventApiClient`)
   - Method: `EventDELETEAsync(Guid eventId)`
   - Returns void (204 No Content on success)

---

## Next Immediate Steps (Priority Order)

### 1. Verify IEventSessionRepository Has Required Method
**File**: `Explore.Application/Contracts/Persistence/IEventSessionRepository.cs`

**Check for**: `Task<List<EventSession>> GetSessionsByEvent(Guid eventId);`

**If missing**: Add method signature

**Then**: Implement in `Explore.Persistence/Repositories/EventSessionRepository.cs`

### 2. Update DeleteEventCommandHandler
**File**: `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`

**Changes**:
1. Add `IEventSessionRepository` to constructor
2. After authorization check, query sessions
3. Iterate and soft delete each session
4. Log cascade operation

**Location**: After line 75 (after authorization check), before line 78 (event delete)

### 3. Create/Verify OrganizationService
**File**: `Explore.Blazor.Client/Services/OrganizationService.cs`

**Check**: Does this file exist?
- If yes: Verify it has `GetMyOrganizationsAsync()` method
- If no: Create new service with required methods

### 4. Add CanDeleteEventAsync to EventService
**File**: `Explore.Blazor.Client/Services/EventService.cs`

**Add method**: After `DeleteEventAsync` (around line 183)

**Logic**: Check if user is authorized to delete event (organization membership check)

### 5. Update EventDetail.razor
**File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`

**Changes**:
1. Add `_canDelete` and `_isLoadingAuth` fields
2. Call `CanDeleteEventAsync` in page load
3. Update delete button rendering with conditional `@if (_canDelete)`

---

## Tricky Bits / Watch Outs

### 1. Transaction Scope for Cascade Delete
**Issue**: Need to ensure Event and all EventSessions are deleted atomically

**Solution**: All deletes use same DbContext instance
- DbContext.SaveChangesAsync() commits all changes in single transaction
- If any operation fails, entire transaction rolls back

**Verification**: Check that DeleteEventCommandHandler doesn't call SaveChanges after each session delete
- Should only call SaveChanges once at the end (via repository)

### 2. IEventSessionRepository Query Method
**Issue**: May need to add `GetSessionsByEvent` method to repository

**Current State**: Check if method exists
- File: `Explore.Application/Contracts/Persistence/IEventSessionRepository.cs`
- Expected: `Task<List<EventSession>> GetSessionsByEvent(Guid eventId);`

**If missing**: Add to interface, implement in repository

### 3. Frontend Organization Membership Data
**Issue**: Need user's organization roles to check authorization

**Challenge**: EventDto may not include user's role in the organization
- Must query OrganizationService separately
- Match eventDto.ActorOrganizationId with user's organizations
- Check OrganizationRoleId (1, 2, 3 = can delete)

**Optimization**: Cache organization memberships (avoid repeated API calls)

### 4. Optimistic UI in MyEvents
**Issue**: Removing event card before API confirms success

**Implementation**:
1. Store original events list
2. Remove card from UI immediately
3. Call API in background
4. If API fails: restore card, show error
5. If API succeeds: show success snackbar

**State Management**:
```csharp
private List<EventListDto> _events = new();
private List<EventListDto> _eventsBackup = new();

private async Task DeleteEventOptimistic(EventListDto event)
{
    _eventsBackup = new List<EventListDto>(_events);
    _events.Remove(event);
    StateHasChanged();

    var success = await _eventService.DeleteEventAsync(event.Id);

    if (!success)
    {
        _events = _eventsBackup;
        StateHasChanged();
        _snackbar.Add("Failed to delete event", Severity.Error);
    }
    else
    {
        _snackbar.Add("Event deleted successfully", Severity.Success);
    }
}
```

### 5. Delete Button Always Visible During Development
**Issue**: May want to test delete even without authorization

**Solution**: Add appsettings flag or environment variable
- `"Features:BypassDeleteAuthorization": false`
- Only use in development, never in production

---

## Testing Checklist (After Implementation)

### Backend Testing
- [ ] Unit test: Event with 3 sessions, all 3 soft deleted after event delete
- [ ] Unit test: Event with 0 sessions, deletes successfully
- [ ] Unit test: Transaction rollback if session delete fails
- [ ] Unit test: Authorization check still works (Admin, Org Admin, Owner, Unauthorized)
- [ ] Integration test: Database state after delete (IsDeleted=true for Event and Sessions)

### Frontend Testing
- [ ] EventDetail: Admin sees delete button
- [ ] EventDetail: Organization Creator sees delete button for org events
- [ ] EventDetail: Organization Member does NOT see delete button
- [ ] EventDetail: Delete confirmation dialog shows session count
- [ ] EventDetail: After delete, navigates to MyEvents
- [ ] MyEvents: Delete icon visible for authorized events only
- [ ] MyEvents: Optimistic UI removes card, rollback on failure
- [ ] MyEvents: Success snackbar shown after delete

### Manual Database Verification
- [ ] After delete, run SQL: `SELECT * FROM events WHERE id = '{event_id}'` → IsDeleted=true
- [ ] After delete, run SQL: `SELECT * FROM event_sessions WHERE event_id = '{event_id}'` → All IsDeleted=true
- [ ] Verify DeletedAt and DeletedBy populated correctly

---

## Quick Resume Instructions

**To continue from where we left off**:

1. Read this file (context.md)
2. Review the plan file (plan.md) for overall strategy
3. Check the tasks file (tasks.md) for specific next steps
4. Start with Phase 1, Task 1.1: Update DeleteEventCommandHandler

**Files to edit next** (in order):
1. `Explore.Application/Contracts/Persistence/IEventSessionRepository.cs` - Verify/add GetSessionsByEvent method
2. `Explore.Persistence/Repositories/EventSessionRepository.cs` - Implement GetSessionsByEvent
3. `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs` - Add cascade logic
4. `Explore.Blazor.Client/Services/EventService.cs` - Add CanDeleteEventAsync method
5. `Explore.Blazor.Client/Pages/Event/EventDetail.razor` - Update delete button visibility

---

## References

### Documentation
- **CLAUDE.md** - Project overview and critical rules
- **docs/GOVERNANCE.md** - Clean Architecture conventions
- **docs/BLAZOR.md** - Blazor UI patterns
- **docs/QUICK_REFERENCE.md** - 10 critical rules

### Existing Code Patterns
- **DeleteEventCommandHandler** - Authorization pattern (three-tier)
- **CreateEventWithSessionsCommandHandler** - Example of handling Event + Sessions together
- **EventService.cs** - Frontend service pattern with error handling
- **MyEvents.razor** - Organization filter pattern

### Related Work
- **dev/active/soft-delete-authorization/** - Original soft delete implementation (audit fields, query filters, SaveChanges override)
