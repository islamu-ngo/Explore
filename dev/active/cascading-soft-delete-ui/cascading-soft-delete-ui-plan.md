# Cascading Soft Delete & Blazor UI Authorization - Implementation Plan

**Created**: 2026-01-19
**Status**: Planning Phase
**Priority**: High (Production Readiness)

---

## Executive Summary

Implement cascading soft delete for EventSessions when an Event is deleted, and update the Blazor UI to properly handle event deletion with organization-based authorization. This ensures data integrity, provides a clean user experience, and maintains enterprise-grade quality for production deployment.

### Key Objectives

1. **Cascading Soft Delete**: When an Event is soft deleted, automatically soft delete all related EventSessions
2. **Blazor UI Authorization**: Show/hide delete button based on user's authorization (organization membership check)
3. **Clean UX**: Proper confirmation dialogs, loading states, error handling, and success feedback
4. **Enterprise Quality**: Comprehensive logging, proper error messages, unit tests

---

## Current State Analysis

### Backend (✅ Partially Complete)

**DeleteEventCommandHandler** (`Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`):
- ✅ Implements three-tier authorization (System Admin → Org Creator/CoOwner/Admin → Personal Owner)
- ✅ Uses `ICurrentUserService` abstraction (Clean Architecture compliant)
- ✅ Comprehensive logging for audit trails
- ❌ **MISSING**: Does not cascade soft delete to EventSessions
- ❌ **MISSING**: Does not query EventSessionRepository to soft delete related sessions

**Soft Delete Infrastructure**:
- ✅ `ISoftDeletable` interface implemented on Event and EventSession
- ✅ `ExploreDbContext.SaveChangesAsync` override converts hard deletes to soft deletes
- ✅ Global query filters exclude soft-deleted records (`!e.IsDeleted`)
- ✅ Audit fields auto-populated (CreatedAt, UpdatedAt, DeletedAt, DeletedBy)

**EventSession Entity**:
- ✅ Has `IsDeleted`, `DeletedAt`, `DeletedBy` fields
- ✅ Foreign key relationship to Event (`EventId`)
- ✅ Global query filter for soft delete

### Frontend (❌ Incomplete)

**EventDetail.razor** (`Explore.Blazor.Client/Pages/Event/EventDetail.razor`):
- ✅ Has Delete button at line 32-38
- ❌ Delete button always visible (no authorization check)
- ❌ No visual feedback for authorization state
- ❌ Basic confirmation dialog (needs improvement)

**MyEvents.razor** (`Explore.Blazor.Client/Pages/Event/MyEvents.razor`):
- ✅ Lists user's events with organization filter
- ❌ No inline delete action for event cards
- ❌ No authorization check for delete capability

**EventService** (`Explore.Blazor.Client/Services/EventService.cs`):
- ✅ Has `DeleteEventAsync(Guid eventId)` method (line 160-183)
- ✅ Calls API endpoint with proper error handling
- ❌ No authorization pre-check before API call
- ❌ No feedback to UI about authorization capability

### Identified Gaps

1. **Backend**: EventSessions not cascaded when Event is deleted
2. **Frontend**: No authorization check for delete button visibility
3. **Frontend**: No user feedback about why delete button is hidden/disabled
4. **Frontend**: No optimistic UI updates
5. **Testing**: No unit tests for cascading soft delete logic

---

## Proposed Future State

### Backend Enhancement

**DeleteEventCommandHandler** will:
1. Query all EventSessions for the Event being deleted
2. Soft delete each EventSession individually (respecting audit fields)
3. Log the cascade operation for audit trail
4. Maintain transaction integrity (all or nothing)

### Frontend Enhancement

**EventDetail.razor** will:
1. Check user's authorization capability on page load
2. Show/hide delete button based on authorization
3. Show informative tooltip if delete is not authorized
4. Display loading overlay during delete operation
5. Navigate to MyEvents after successful deletion

**MyEvents.razor** will:
1. Show inline delete icon for each event card
2. Apply same authorization checks as EventDetail
3. Implement optimistic UI (remove card immediately, rollback on failure)

**EventService** will:
1. Add `CanDeleteEventAsync(Guid eventId)` method for authorization pre-check
2. Improve error messages for different failure scenarios
3. Add detailed logging for debugging

---

## Implementation Phases

### Phase 1: Backend - Cascading Soft Delete (2-3 hours)

**Goal**: Implement automatic cascading soft delete for EventSessions when Event is deleted

#### Task 1.1: Update DeleteEventCommandHandler
**File**: `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`

**Changes**:
- Add `IEventSessionRepository` dependency
- Query all EventSessions for the Event: `var sessions = await _eventSessionRepository.GetSessionsByEvent(eventId)`
- Soft delete each session: `foreach (var session in sessions) { await _eventSessionRepository.Delete(session); }`
- Add logging: "Cascading soft delete: {Count} event sessions marked as deleted for event {EventId}"

**Acceptance Criteria**:
- [ ] IEventSessionRepository injected via constructor
- [ ] All EventSessions for the Event are retrieved before deletion
- [ ] Each EventSession is soft deleted individually (audit fields populated)
- [ ] Logs clearly indicate cascade operation
- [ ] If Event delete succeeds, all sessions must be deleted (transaction boundary)

**Effort**: M
**Risk**: Medium (transaction handling, N+1 query concern)
**Related Skills**: `cqrs-mediatr-guidelines`, `dotnet-efcore-guidelines`

#### Task 1.2: Add Transaction Boundary
**File**: Same as Task 1.1

**Changes**:
- Wrap Event and EventSession deletes in transaction scope
- If any session delete fails, rollback Event delete
- Use `_eventRepository` transaction capabilities or explicit `TransactionScope`

**Acceptance Criteria**:
- [ ] Event and all EventSessions deleted atomically
- [ ] If any operation fails, entire delete is rolled back
- [ ] Error logged with specific failure details

**Effort**: S
**Risk**: Low
**Related Skills**: `dotnet-efcore-guidelines`

#### Task 1.3: Add Logging and Error Handling
**File**: Same as Task 1.1

**Changes**:
- Log entry: "Starting event deletion for event {EventId}, user {UserId}"
- Log entry: "Found {Count} event sessions to cascade delete"
- Log entry per session: "Soft deleting event session {SessionId}"
- Log success: "Event {EventId} and {Count} sessions successfully deleted by user {UserId}"
- Log failure: "Event deletion failed for event {EventId}: {ErrorMessage}"

**Acceptance Criteria**:
- [ ] All major steps logged with structured logging
- [ ] Session count logged before cascade
- [ ] Success/failure clearly logged
- [ ] Exceptions include stack trace for debugging

**Effort**: S
**Risk**: Low
**Related Skills**: `error-tracking`

---

### Phase 2: Frontend - Authorization Check Service (1-2 hours)

**Goal**: Create reusable service method to check if user can delete an event

#### Task 2.1: Add CanDeleteEventAsync to EventService
**File**: `Explore.Blazor.Client/Services/EventService.cs`

**Changes**:
- Add method: `Task<bool> CanDeleteEventAsync(Guid eventId)`
- Fetch event details: `var eventDto = await GetEventByIdAsync(eventId)`
- Check if event has `ActorOrganizationId` (organization event)
- If organization event:
  - Fetch user's organizations: `await _organizationService.GetMyOrganizationsAsync()`
  - Check if user is member of event's organization with Creator/CoOwner/Admin role
- If personal event:
  - Check if current user is the event owner
- Return `true` if authorized, `false` otherwise

**Acceptance Criteria**:
- [ ] Method returns `true` for events user can delete
- [ ] Method returns `false` for events user cannot delete
- [ ] No exceptions thrown for unauthorized scenarios
- [ ] Proper logging for authorization decisions

**Effort**: M
**Risk**: Medium (organization membership check complexity)
**Related Skills**: `blazor-ui-conventions`

#### Task 2.2: Create OrganizationService (if not exists)
**File**: `Explore.Blazor.Client/Services/OrganizationService.cs`

**Changes**:
- Create `IOrganizationService` interface
- Implement `GetMyOrganizationsAsync()` - fetches organizations user is a member of
- Implement `GetOrganizationMemberRoleAsync(Guid organizationId, Guid userId)` - gets user's role in org
- Register in DI container

**Acceptance Criteria**:
- [ ] Service returns user's organizations with membership details
- [ ] Service includes role information (Creator=1, CoOwner=2, Admin=3, etc.)
- [ ] Proper error handling and logging
- [ ] Registered in `Program.cs` or `ServiceCollectionExtensions`

**Effort**: M (if creating new), S (if already exists)
**Risk**: Low
**Related Skills**: `blazor-ui-conventions`

---

### Phase 3: Frontend - EventDetail Page Updates (2-3 hours)

**Goal**: Update EventDetail page to show/hide delete button based on authorization

#### Task 3.1: Add Authorization Check on Page Load
**File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor` and `.razor.cs`

**Changes**:
- Add field: `private bool _canDelete = false;`
- In `OnInitializedAsync` or `LoadEventDetailsAsync`:
  ```csharp
  _canDelete = await _eventService.CanDeleteEventAsync(EventId);
  StateHasChanged();
  ```
- Update delete button visibility:
  ```razor
  @if (_canDelete)
  {
      <MudButton StartIcon="@Icons.Material.Filled.Delete"
                 Variant="Variant.Outlined"
                 Color="Color.Error"
                 Size="Size.Small"
                 OnClick="OpenDeleteDialog">
          Delete
      </MudButton>
  }
  ```

**Acceptance Criteria**:
- [ ] Delete button only visible if user is authorized
- [ ] Authorization check happens on page load
- [ ] Loading state shown while checking authorization
- [ ] No console errors or warnings

**Effort**: S
**Risk**: Low
**Related Skills**: `blazor-ui-conventions`

#### Task 3.2: Improve Delete Confirmation Dialog
**File**: Same as Task 3.1

**Changes**:
- Update `OpenDeleteDialog` to show comprehensive information:
  - Event title
  - Number of sessions that will be deleted
  - Warning about permanent action (even though soft delete)
  - Confirmation: "Type DELETE to confirm"
- Add loading state during deletion
- Show success snackbar after deletion
- Navigate to `/myevents` after successful deletion

**Acceptance Criteria**:
- [ ] Dialog shows event title and session count
- [ ] Requires explicit confirmation (e.g., typing DELETE)
- [ ] Loading overlay covers page during delete operation
- [ ] Success message shown before navigation
- [ ] Navigation happens automatically after 2 seconds

**Effort**: M
**Risk**: Low
**Related Skills**: `blazor-ui-conventions`

#### Task 3.3: Add Informative Tooltip for Hidden Delete Button
**File**: Same as Task 3.1

**Changes**:
- When delete button is hidden (`_canDelete == false`), show disabled button with tooltip:
  ```razor
  @if (!_canDelete && _eventDetails != null)
  {
      <MudTooltip Text="You don't have permission to delete this event" Placement="Placement.Top">
          <MudButton StartIcon="@Icons.Material.Filled.Delete"
                     Variant="Variant.Outlined"
                     Color="Color.Error"
                     Size="Size.Small"
                     Disabled="true">
              Delete
          </MudButton>
      </MudTooltip>
  }
  ```

**Acceptance Criteria**:
- [ ] Disabled button shown with clear tooltip
- [ ] Tooltip explains why delete is not allowed
- [ ] Consistent with MudBlazor design system

**Effort**: S
**Risk**: Low
**Related Skills**: `blazor-ui-conventions`

---

### Phase 4: Frontend - MyEvents Page Updates (1-2 hours)

**Goal**: Add inline delete action to event cards in MyEvents page

#### Task 4.1: Add Inline Delete Icon to Event Cards
**File**: `Explore.Blazor.Client/Pages/Event/MyEvents.razor`

**Changes**:
- Add delete icon to each event card (top-right corner or action menu)
- Check authorization for each event: `_eventService.CanDeleteEventAsync(event.Id)`
- Store authorization results in dictionary: `Dictionary<Guid, bool> _deleteAuthorizations`
- Show/hide delete icon based on authorization

**Acceptance Criteria**:
- [ ] Delete icon visible only for authorized events
- [ ] Icon styled consistently with MudBlazor
- [ ] Icon positioned in event card header or action area
- [ ] No performance issues with authorization checks (consider caching)

**Effort**: M
**Risk**: Low
**Related Skills**: `blazor-ui-conventions`

#### Task 4.2: Implement Optimistic UI for Delete
**File**: Same as Task 4.1

**Changes**:
- On delete icon click, show confirmation dialog
- After confirmation, immediately remove event card from UI
- Call `DeleteEventAsync` in background
- If delete fails, re-add event card and show error snackbar
- If delete succeeds, show success snackbar

**Acceptance Criteria**:
- [ ] Event card removed immediately after confirmation
- [ ] Smooth animation for card removal
- [ ] Error handling with rollback on failure
- [ ] Success feedback shown to user

**Effort**: M
**Risk**: Medium (state management complexity)
**Related Skills**: `blazor-ui-conventions`

---

### Phase 5: Testing & Verification (2-3 hours)

**Goal**: Comprehensive testing of all features

#### Task 5.1: Unit Tests for DeleteEventCommandHandler
**File**: `Event.Application.UnitTests/Features/Events/Commands/DeleteEventCommandHandlerTests.cs`

**Test Cases**:
1. `DeleteEvent_CascadesSoftDeleteToAllEventSessions_Success`
   - Arrange: Event with 3 sessions
   - Act: Delete event
   - Assert: Event and all 3 sessions marked as IsDeleted=true
2. `DeleteEvent_WithNoSessions_Success`
   - Arrange: Event with 0 sessions
   - Act: Delete event
   - Assert: Event deleted, no errors
3. `DeleteEvent_TransactionRollback_OnSessionDeleteFailure`
   - Arrange: Event with sessions, mock session delete failure
   - Act: Delete event
   - Assert: Event NOT deleted, all operations rolled back
4. `DeleteEvent_UnauthorizedUser_ReturnsFalse`
   - Arrange: User not in organization, not admin, not owner
   - Act: Delete event
   - Assert: Returns false, event NOT deleted

**Acceptance Criteria**:
- [ ] All test cases pass
- [ ] Code coverage > 90% for DeleteEventCommandHandler
- [ ] Tests use mocking for repositories
- [ ] Tests verify logging calls

**Effort**: M
**Risk**: Low
**Related Skills**: `cqrs-mediatr-guidelines`

#### Task 5.2: Integration Tests for Cascading Delete
**File**: `Event.API.IntegrationTests/Features/Events/DeleteEventIntegrationTests.cs`

**Test Cases**:
1. `DeleteEvent_WithSessions_AllSoftDeleted`
   - Create event with 3 sessions
   - Delete event via API
   - Query database: verify Event.IsDeleted=true, all 3 sessions IsDeleted=true
2. `DeleteEvent_IgnoreQueryFilters_CanStillReadSoftDeleted`
   - Delete event
   - Query with `.IgnoreQueryFilters()`: verify record exists with IsDeleted=true

**Acceptance Criteria**:
- [ ] Integration tests run against test database
- [ ] Tests verify actual database state
- [ ] Tests clean up after themselves

**Effort**: M
**Risk**: Low
**Related Skills**: `dotnet-efcore-guidelines`

#### Task 5.3: Manual Testing Checklist
**Location**: Browser testing

**Test Scenarios**:
1. EventDetail page - Admin user:
   - [ ] Can see delete button
   - [ ] Delete succeeds
   - [ ] Navigated to MyEvents after delete
2. EventDetail page - Organization Creator:
   - [ ] Can see delete button for organization events
   - [ ] Cannot see delete button for other organization's events
3. EventDetail page - Organization Member (non-admin):
   - [ ] Cannot see delete button (or see disabled button with tooltip)
4. MyEvents page - Organization Admin:
   - [ ] Delete icon visible for organization events
   - [ ] Delete icon not visible for other events
   - [ ] Optimistic UI: card removed immediately, success message shown
5. Database verification:
   - [ ] After delete, Event.IsDeleted=true
   - [ ] After delete, all EventSessions.IsDeleted=true
   - [ ] Audit fields populated: DeletedAt, DeletedBy

**Acceptance Criteria**:
- [ ] All scenarios tested and documented
- [ ] No console errors during testing
- [ ] All authorization scenarios work correctly

**Effort**: S
**Risk**: Low

---

## Technical Implementation Details

### Backend: Cascading Soft Delete Pattern

```csharp
public async Task<bool> Handle(DeleteEventCommand request, CancellationToken cancellationToken)
{
    var userId = _currentUserService.UserId;
    if (userId == null)
    {
        _logger.LogWarning("Delete event failed: User ID not found");
        return false;
    }

    var @event = await _eventRepository.GetById(request.Id);
    if (@event == null)
    {
        _logger.LogWarning("Delete event failed: Event {EventId} not found", request.Id);
        return false;
    }

    var isAuthorized = await IsUserAuthorizedToDelete(@event, userId.Value, cancellationToken);
    if (!isAuthorized)
    {
        _logger.LogWarning("Delete event failed: User {UserId} not authorized", userId.Value);
        return false;
    }

    // NEW: Cascade soft delete to EventSessions
    var sessions = await _eventSessionRepository.GetSessionsByEvent(request.Id);
    _logger.LogInformation("Cascading soft delete: {Count} event sessions found for event {EventId}",
        sessions.Count, request.Id);

    foreach (var session in sessions)
    {
        _logger.LogDebug("Soft deleting event session {SessionId} for event {EventId}",
            session.Id, request.Id);
        await _eventSessionRepository.Delete(session);
    }

    // Soft delete the event itself
    await _eventRepository.Delete(@event);

    _logger.LogInformation("Event {EventId} and {Count} sessions successfully deleted by user {UserId}",
        request.Id, sessions.Count, userId.Value);

    return true;
}
```

### Frontend: Authorization Check Pattern

```csharp
// EventService.cs
public async Task<bool> CanDeleteEventAsync(Guid eventId)
{
    try
    {
        var eventDto = await GetEventByIdAsync(eventId);
        if (eventDto == null)
        {
            _logger.LogWarning("[EVENT SERVICE] Cannot check delete permission: Event {EventId} not found", eventId);
            return false;
        }

        // System admin check would need to be done via user context or claims
        // For now, we'll rely on API authorization

        // If organization event, check membership
        if (eventDto.ActorOrganizationId.HasValue)
        {
            var myOrgs = await _organizationService.GetMyOrganizationsAsync();
            var memberOrg = myOrgs.FirstOrDefault(o => o.Id == eventDto.ActorOrganizationId.Value);

            if (memberOrg != null)
            {
                // Check if user has Creator (1), CoOwner (2), or Admin (3) role
                var role = memberOrg.OrganizationRoleId;
                var canDelete = role == 1 || role == 2 || role == 3;
                _logger.LogInformation("[EVENT SERVICE] User has role {Role} in organization, canDelete={CanDelete}",
                    role, canDelete);
                return canDelete;
            }
        }

        // TODO: Add personal event ownership check
        // Would need to compare eventDto.ActorUserId with current user's ID

        return false;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "[EVENT SERVICE] Error checking delete permission for event {EventId}", eventId);
        return false;
    }
}
```

### Frontend: EventDetail Delete Button

```razor
@* EventDetail.razor *@
@if (_canDelete)
{
    <MudButton StartIcon="@Icons.Material.Filled.Delete"
               Variant="Variant.Outlined"
               Color="Color.Error"
               Size="Size.Small"
               OnClick="OpenDeleteDialog"
               Disabled="@_isDeleting">
        @if (_isDeleting)
        {
            <MudProgressCircular Size="Size.Small" Indeterminate="true" />
            <span class="ml-2">Deleting...</span>
        }
        else
        {
            <span>Delete</span>
        }
    </MudButton>
}
else if (_eventDetails != null && !_isLoadingAuth)
{
    <MudTooltip Text="You don't have permission to delete this event" Placement="Placement.Top">
        <MudButton StartIcon="@Icons.Material.Filled.Delete"
                   Variant="Variant.Outlined"
                   Color="Color.Error"
                   Size="Size.Small"
                   Disabled="true">
            Delete
        </MudButton>
    </MudTooltip>
}
```

---

## Risk Assessment

### High Risk Items

1. **Transaction Boundary for Cascade Delete**
   - **Risk**: If Event delete succeeds but Session deletes fail, data inconsistency
   - **Mitigation**: Use explicit transaction scope or ensure DbContext transaction handling
   - **Contingency**: Add cleanup job to find Events without Sessions and fix inconsistencies

2. **Performance: N+1 Query for Sessions**
   - **Risk**: Deleting event with 100+ sessions = 100+ DELETE queries
   - **Mitigation**: Use bulk operations or optimize EF Core queries
   - **Contingency**: Monitor performance, add batch delete if needed

### Medium Risk Items

1. **Authorization Check Accuracy**
   - **Risk**: User sees delete button but API rejects (or vice versa)
   - **Mitigation**: Ensure frontend and backend authorization logic is identical
   - **Contingency**: Backend always validates; frontend is UX convenience

2. **Organization Membership Data Staleness**
   - **Risk**: User's organization role changes between page load and delete action
   - **Mitigation**: Backend always re-validates; frontend shows updated state on error
   - **Contingency**: Show error message, refresh page data

### Low Risk Items

1. **UI State Management**
   - **Risk**: Optimistic UI shows event deleted but API call fails
   - **Mitigation**: Proper rollback mechanism, clear error messages
   - **Contingency**: Refresh page to restore accurate state

---

## Success Metrics

### Functional Metrics
- [ ] Event deletion cascades to all EventSessions (100% of cases)
- [ ] Delete button only visible for authorized users (100% accuracy)
- [ ] All authorization scenarios tested and working (Admin, Org Admin, Owner, Unauthorized)
- [ ] Zero data integrity issues (no orphaned EventSessions)

### Performance Metrics
- [ ] Event deletion with 10 sessions completes in < 2 seconds
- [ ] Event deletion with 100 sessions completes in < 10 seconds
- [ ] Authorization check on page load completes in < 500ms
- [ ] No N+1 query issues (verify with EF Core logging)

### Code Quality Metrics
- [ ] Unit test coverage > 90% for DeleteEventCommandHandler
- [ ] All public methods in EventService have tests
- [ ] No console errors or warnings in browser
- [ ] All logging statements follow structured logging pattern

### User Experience Metrics
- [ ] Users understand why delete button is hidden (tooltip)
- [ ] Delete confirmation dialog is clear and informative
- [ ] Success/error feedback is immediate and clear
- [ ] No UI freezing during delete operation

---

## Dependencies

### External Services
- **None** - All operations are internal to the Explore application

### Code Dependencies
- **IEventSessionRepository** - Must have `GetSessionsByEvent(Guid eventId)` method
- **IOrganizationService** (Frontend) - May need to create if doesn't exist
- **MudBlazor** - For UI components (already in use)

### Data Dependencies
- **EventSession.EventId** - Foreign key must be valid and indexed
- **OrganizationMember** - Must have current user's organization roles
- **Actor** - Must have OrganizationId and UserId properly set

---

## Timeline Estimate

| Phase | Effort | Status |
|-------|--------|--------|
| 1. Backend Cascading Delete | 2-3 hours | ⏳ Not Started |
| 2. Frontend Authorization Service | 1-2 hours | ⏳ Not Started |
| 3. EventDetail Page Updates | 2-3 hours | ⏳ Not Started |
| 4. MyEvents Page Updates | 1-2 hours | ⏳ Not Started |
| 5. Testing & Verification | 2-3 hours | ⏳ Not Started |
| **Total** | **8-13 hours** | **0% Complete** |

---

## Rollback Plan

If issues arise in production:

1. **Backend Issues (Cascade Delete)**:
   - Revert DeleteEventCommandHandler to previous version (without cascade)
   - Deploy hotfix
   - Manually clean up orphaned EventSessions with SQL script

2. **Frontend Issues (Authorization)**:
   - Revert EventDetail and MyEvents pages to previous versions
   - Hide delete buttons completely (safer than showing to unauthorized users)
   - Fix authorization logic in dev environment

3. **Data Integrity Issues**:
   - Run cleanup script to find Events with IsDeleted=true but EventSessions with IsDeleted=false
   - Soft delete orphaned EventSessions manually
   - Add database constraint to prevent future orphans

---

## Related Documentation

- **CLAUDE.md** - Project overview and conventions
- **docs/GOVERNANCE.md** - Clean Architecture rules
- **docs/BLAZOR.md** - Blazor UI patterns
- **.claude/skills/blazor-ui-conventions** - MudBlazor patterns
- **.claude/skills/cqrs-mediatr-guidelines** - CQRS command patterns
- **dev/active/soft-delete-authorization/** - Original soft delete implementation plan
