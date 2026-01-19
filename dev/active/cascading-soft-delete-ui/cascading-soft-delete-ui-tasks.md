# Cascading Soft Delete & Blazor UI Authorization - Task Checklist

**Last Updated**: 2026-01-19

---

## Phase 1: Backend - Cascading Soft Delete ⏳ NOT STARTED

### 1.1: Verify/Add GetSessionsByEvent Method
- [ ] Check if IEventSessionRepository has `GetSessionsByEvent(Guid eventId)` method
  - **File**: `Explore.Application/Contracts/Persistence/IEventSessionRepository.cs`
  - **Acceptance**: Method signature exists in interface
- [ ] If missing, add method signature:
  ```csharp
  Task<List<EventSession>> GetSessionsByEvent(Guid eventId);
  ```
  - **Acceptance**: Interface compiles without errors

### 1.2: Implement GetSessionsByEvent in Repository
- [ ] Implement method in EventSessionRepository
  - **File**: `Explore.Persistence/Repositories/EventSessionRepository.cs`
  - **Implementation**:
    ```csharp
    public async Task<List<EventSession>> GetSessionsByEvent(Guid eventId)
    {
        return await _dbContext.EventSessions
            .Where(s => s.EventId == eventId)
            .ToListAsync();
    }
    ```
  - **Acceptance**: Method returns all EventSessions for given Event ID
  - **Acceptance**: Query excludes soft-deleted sessions (via global filter)

### 1.3: Update DeleteEventCommandHandler - Add Dependency
- [ ] Add IEventSessionRepository to DeleteEventCommandHandler
  - **File**: `Explore.Application/Features/Events/Handlers/Commands/DeleteEventCommandHandler.cs`
  - **Location**: Line ~22-28 (private fields)
  - **Change**: Add `private readonly IEventSessionRepository _eventSessionRepository;`
  - **Constructor**: Add parameter to constructor (line ~30-46)
  - **Acceptance**: Handler compiles and has access to session repository

### 1.4: Implement Cascade Delete Logic
- [ ] Query all EventSessions for the Event being deleted
  - **File**: Same as 1.3
  - **Location**: After line 75 (after authorization check), before line 78 (event delete)
  - **Code**:
    ```csharp
    // Cascade soft delete to all EventSessions
    var sessions = await _eventSessionRepository.GetSessionsByEvent(request.Id);
    _logger.LogInformation("Cascading soft delete: {Count} event sessions found for event {EventId}",
        sessions.Count, request.Id);

    foreach (var session in sessions)
    {
        _logger.LogDebug("Soft deleting event session {SessionId} for event {EventId}",
            session.Id, request.Id);
        await _eventSessionRepository.Delete(session);
    }
    ```
  - **Acceptance**: All EventSessions for Event are retrieved and soft deleted
  - **Acceptance**: Logging clearly indicates cascade operation
  - **Acceptance**: Audit fields (DeletedAt, DeletedBy) populated for each session

### 1.5: Update Success Logging
- [ ] Update final success log to include session count
  - **File**: Same as 1.3
  - **Location**: Line ~82-85 (final log statement)
  - **Change**:
    ```csharp
    _logger.LogInformation(
        "Event {EventId} and {Count} sessions successfully deleted by user {UserId}",
        request.Id, sessions.Count, userId.Value);
    ```
  - **Acceptance**: Log message includes count of deleted sessions

### 1.6: Build and Verify Compilation
- [ ] Build solution: `dotnet build`
  - **Acceptance**: No compilation errors
  - **Acceptance**: DeleteEventCommandHandler constructor injects IEventSessionRepository

---

## Phase 2: Frontend - Authorization Service ⏳ NOT STARTED

### 2.1: Verify/Create OrganizationService
- [ ] Check if OrganizationService exists
  - **File**: `Explore.Blazor.Client/Services/OrganizationService.cs`
  - **Action**: If exists, verify it has `GetMyOrganizationsAsync()` method
  - **If missing**: Create new service file
- [ ] Implement IOrganizationService interface
  - **Interface**:
    ```csharp
    public interface IOrganizationService
    {
        Task<ICollection<OrganizationListDto>> GetMyOrganizationsAsync();
    }
    ```
  - **Acceptance**: Interface defined with required method

### 2.2: Implement GetMyOrganizationsAsync
- [ ] Implement method in OrganizationService
  - **File**: `Explore.Blazor.Client/Services/OrganizationService.cs`
  - **Implementation**:
    ```csharp
    public async Task<ICollection<OrganizationListDto>> GetMyOrganizationsAsync()
    {
        try
        {
            var response = await _apiClient.OrganizationMyAsync();
            return response ?? new List<OrganizationListDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ORG SERVICE] Error fetching my organizations");
            return new List<OrganizationListDto>();
        }
    }
    ```
  - **Acceptance**: Returns list of organizations user is a member of
  - **Acceptance**: Includes OrganizationMember data with RoleId

### 2.3: Register OrganizationService in DI
- [ ] Add service registration
  - **File**: `Explore.Blazor.Client/Program.cs` or service registration file
  - **Add**: `builder.Services.AddScoped<IOrganizationService, OrganizationService>();`
  - **Acceptance**: Service available for dependency injection

### 2.4: Add CanDeleteEventAsync to EventService
- [ ] Add method to EventService
  - **File**: `Explore.Blazor.Client/Services/EventService.cs`
  - **Location**: After `DeleteEventAsync` method (around line 183)
  - **Implementation**:
    ```csharp
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
  - **Acceptance**: Method returns true if user can delete event
  - **Acceptance**: Method returns false if user cannot delete event
  - **Acceptance**: Proper logging for authorization decisions

### 2.5: Update IEventService Interface
- [ ] Add method signature to IEventService
  - **File**: `Explore.Blazor.Client/Services/EventService.cs` (interface section)
  - **Add**: `Task<bool> CanDeleteEventAsync(Guid eventId);`
  - **Acceptance**: Interface includes new method

---

## Phase 3: Frontend - EventDetail Page ⏳ NOT STARTED

### 3.1: Add Authorization Fields to EventDetail
- [ ] Add private fields to EventDetail component
  - **File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor.cs` (or @code section in .razor)
  - **Add**:
    ```csharp
    private bool _canDelete = false;
    private bool _isLoadingAuth = true;
    private bool _isDeleting = false;
    ```
  - **Acceptance**: Fields declared and initialized

### 3.2: Check Authorization on Page Load
- [ ] Add authorization check in OnInitializedAsync
  - **File**: Same as 3.1
  - **Location**: In page load method (after event details loaded)
  - **Code**:
    ```csharp
    _isLoadingAuth = true;
    _canDelete = await _eventService.CanDeleteEventAsync(EventId);
    _isLoadingAuth = false;
    StateHasChanged();
    ```
  - **Acceptance**: Authorization checked when page loads
  - **Acceptance**: `_canDelete` updated based on user's permissions

### 3.3: Update Delete Button Rendering
- [ ] Make delete button conditional
  - **File**: `Explore.Blazor.Client/Pages/Event/EventDetail.razor`
  - **Location**: Line ~32-38 (delete button)
  - **Change**:
    ```razor
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
  - **Acceptance**: Delete button only visible if `_canDelete == true`
  - **Acceptance**: Disabled button with tooltip shown if `_canDelete == false`
  - **Acceptance**: Loading state shown while checking authorization

### 3.4: Improve Delete Confirmation Dialog
- [ ] Update OpenDeleteDialog method
  - **File**: Same as 3.1
  - **Location**: OpenDeleteDialog method
  - **Changes**:
    - Fetch session count: `var sessions = await _eventService.GetSessionsByEventAsync(EventId);`
    - Show session count in dialog: "This will also delete {sessions.Count} event session(s)"
    - Require confirmation: "Type DELETE to confirm"
  - **Acceptance**: Dialog shows event title
  - **Acceptance**: Dialog shows number of sessions that will be deleted
  - **Acceptance**: Dialog requires explicit confirmation

### 3.5: Add Success Feedback and Navigation
- [ ] Update delete success handler
  - **File**: Same as 3.1
  - **Location**: After successful delete
  - **Code**:
    ```csharp
    _isDeleting = true;
    StateHasChanged();

    var success = await _eventService.DeleteEventAsync(EventId);

    if (success)
    {
        _snackbar.Add("Event and all sessions deleted successfully", Severity.Success);
        await Task.Delay(2000); // Show success message for 2 seconds
        _navigationManager.NavigateTo("/myevents");
    }
    else
    {
        _snackbar.Add("Failed to delete event. Please try again.", Severity.Error);
        _isDeleting = false;
        StateHasChanged();
    }
    ```
  - **Acceptance**: Success snackbar shown after delete
  - **Acceptance**: Page navigates to MyEvents after 2 seconds
  - **Acceptance**: Error snackbar shown if delete fails
  - **Acceptance**: Loading state reset on failure

---

## Phase 4: Frontend - MyEvents Page ⏳ NOT STARTED

### 4.1: Add Authorization Dictionary to MyEvents
- [ ] Add private fields
  - **File**: `Explore.Blazor.Client/Pages/Event/MyEvents.razor.cs` (or @code section)
  - **Add**:
    ```csharp
    private Dictionary<Guid, bool> _deleteAuthorizations = new();
    private List<EventListDto> _eventsBackup = new();
    ```
  - **Acceptance**: Fields declared and initialized

### 4.2: Check Authorization for All Events
- [ ] Add authorization checks after loading events
  - **File**: Same as 4.1
  - **Location**: After loading `_events` list
  - **Code**:
    ```csharp
    foreach (var evt in _events)
    {
        var canDelete = await _eventService.CanDeleteEventAsync(evt.Id);
        _deleteAuthorizations[evt.Id] = canDelete;
    }
    StateHasChanged();
    ```
  - **Acceptance**: Authorization checked for each event
  - **Acceptance**: Results stored in dictionary for quick lookup

### 4.3: Add Inline Delete Icon to Event Cards
- [ ] Add delete icon to each event card
  - **File**: `Explore.Blazor.Client/Pages/Event/MyEvents.razor`
  - **Location**: In event card rendering (inside card header or actions area)
  - **Code**:
    ```razor
    @if (_deleteAuthorizations.GetValueOrDefault(evt.Id, false))
    {
        <MudIconButton Icon="@Icons.Material.Filled.Delete"
                       Color="Color.Error"
                       Size="Size.Small"
                       OnClick="@(() => OpenDeleteDialogForEvent(evt))"
                       aria-label="Delete event" />
    }
    ```
  - **Acceptance**: Delete icon only visible for events user can delete
  - **Acceptance**: Icon styled consistently with MudBlazor
  - **Acceptance**: Icon positioned in card header or action area

### 4.4: Implement Optimistic UI for Delete
- [ ] Add delete handler with optimistic UI
  - **File**: Same as 4.1
  - **Add method**:
    ```csharp
    private async Task DeleteEventOptimistic(EventListDto evt)
    {
        var confirmed = await _dialogService.ShowMessageBox(
            "Confirm Delete",
            $"Are you sure you want to delete '{evt.Title}'? This will also delete all event sessions.",
            yesText: "Delete",
            cancelText: "Cancel") ?? false;

        if (!confirmed) return;

        // Backup and remove from UI immediately
        _eventsBackup = new List<EventListDto>(_events);
        _events.Remove(evt);
        StateHasChanged();

        // Call API in background
        var success = await _eventService.DeleteEventAsync(evt.Id);

        if (!success)
        {
            // Rollback on failure
            _events = _eventsBackup;
            StateHasChanged();
            _snackbar.Add($"Failed to delete event '{evt.Title}'", Severity.Error);
        }
        else
        {
            _snackbar.Add($"Event '{evt.Title}' deleted successfully", Severity.Success);
        }
    }
    ```
  - **Acceptance**: Event card removed immediately after confirmation
  - **Acceptance**: Card restored if API call fails
  - **Acceptance**: Success/error snackbar shown

---

## Phase 5: Testing & Verification ⏳ NOT STARTED

### 5.1: Unit Tests for DeleteEventCommandHandler
- [ ] Create test file if doesn't exist
  - **File**: `Event.Application.UnitTests/Features/Events/Commands/DeleteEventCommandHandlerTests.cs`
- [ ] Test: DeleteEvent_CascadesSoftDeleteToAllEventSessions_Success
  - **Arrange**: Mock event with 3 sessions
  - **Act**: Call Handle(DeleteEventCommand)
  - **Assert**: Event and all 3 sessions have Delete called
  - **Acceptance**: Test passes
- [ ] Test: DeleteEvent_WithNoSessions_Success
  - **Arrange**: Mock event with 0 sessions
  - **Act**: Call Handle(DeleteEventCommand)
  - **Assert**: Event deleted, no errors
  - **Acceptance**: Test passes
- [ ] Test: DeleteEvent_UnauthorizedUser_ReturnsFalse
  - **Arrange**: User not in organization, not admin, not owner
  - **Act**: Call Handle(DeleteEventCommand)
  - **Assert**: Returns false, event NOT deleted
  - **Acceptance**: Test passes

### 5.2: Integration Tests for Cascading Delete
- [ ] Create test file
  - **File**: `Event.API.IntegrationTests/Features/Events/DeleteEventIntegrationTests.cs`
- [ ] Test: DeleteEvent_WithSessions_AllSoftDeleted
  - **Arrange**: Create event with 3 sessions in database
  - **Act**: Delete event via API endpoint
  - **Assert**: Query database, verify Event.IsDeleted=true and all EventSessions.IsDeleted=true
  - **Acceptance**: Test passes
- [ ] Test: DeleteEvent_IgnoreQueryFilters_CanStillReadSoftDeleted
  - **Arrange**: Delete event
  - **Act**: Query with `.IgnoreQueryFilters()`
  - **Assert**: Verify record exists with IsDeleted=true
  - **Acceptance**: Test passes

### 5.3: Manual Testing - Backend
- [ ] Test with Swagger/Postman: Delete event with sessions
  - **Verify**: All sessions soft deleted
  - **Verify**: Audit fields populated (DeletedAt, DeletedBy)
- [ ] Test with Swagger/Postman: Delete event without sessions
  - **Verify**: Event deleted successfully
- [ ] Test authorization: Admin deletes any event
  - **Verify**: Succeeds
- [ ] Test authorization: Organization Creator deletes org event
  - **Verify**: Succeeds
- [ ] Test authorization: Organization Member tries to delete
  - **Verify**: Fails (returns 403 or false)

### 5.4: Manual Testing - Frontend EventDetail
- [ ] As Admin: Visit event detail page
  - **Verify**: Delete button is visible
  - **Verify**: Delete succeeds
  - **Verify**: Navigates to MyEvents
- [ ] As Organization Creator: Visit organization event
  - **Verify**: Delete button is visible
  - **Verify**: Delete succeeds
- [ ] As Organization Member (non-admin): Visit organization event
  - **Verify**: Delete button is hidden or disabled with tooltip
- [ ] As regular user: Visit another user's personal event
  - **Verify**: Delete button is hidden or disabled with tooltip

### 5.5: Manual Testing - Frontend MyEvents
- [ ] As Organization Admin: View my events
  - **Verify**: Delete icon visible for organization events
  - **Verify**: Delete icon not visible for other events
- [ ] Click delete icon
  - **Verify**: Confirmation dialog appears
  - **Verify**: Event card removed immediately after confirmation
  - **Verify**: Success snackbar shown
- [ ] Click delete icon with network failure
  - **Verify**: Event card restored if API fails
  - **Verify**: Error snackbar shown

### 5.6: Database Verification
- [ ] After delete, run SQL query
  - **Query**: `SELECT * FROM events WHERE id = '{event_id}'`
  - **Verify**: `is_deleted = true`, `deleted_at` and `deleted_by` populated
- [ ] After delete, run SQL query
  - **Query**: `SELECT * FROM event_sessions WHERE event_id = '{event_id}'`
  - **Verify**: All sessions have `is_deleted = true`, `deleted_at` and `deleted_by` populated

---

## Known Issues / Technical Debt

**None currently** - Fresh implementation

---

## Performance Considerations

- **N+1 Query Issue**: Deleting event with 100+ sessions = 100+ individual DELETE queries
  - **Current Approach**: Acceptable for most events (< 10 sessions)
  - **Optimization**: If performance becomes issue, implement bulk soft delete
  - **Monitoring**: Add logging to track session count and delete duration

- **Frontend Authorization Checks**: Multiple API calls for authorization
  - **Current Approach**: Cache organization memberships in service
  - **Optimization**: Add expiration time (e.g., 5 minutes) to avoid stale data

---

## Security Considerations

- ✅ Backend always validates authorization (three-tier)
- ✅ Frontend authorization check is UX convenience only (not security boundary)
- ✅ Soft delete prevents permanent data loss (can recover if needed)
- ⚠️ Admin can view soft-deleted records with `.IgnoreQueryFilters()`
  - **Action**: Document this capability in admin documentation
  - **Action**: Add admin-only endpoint to view soft-deleted events (future)

---

## Acceptance Criteria Summary

### Phase 1: Backend
- [ ] Event deletion cascades to all EventSessions (100% of sessions)
- [ ] Audit fields populated for all deleted entities
- [ ] Comprehensive logging for cascade operations
- [ ] Transaction integrity maintained (all or nothing)

### Phase 2: Frontend Service
- [ ] CanDeleteEventAsync accurately determines authorization
- [ ] OrganizationService provides membership and role data
- [ ] Proper error handling and logging

### Phase 3: EventDetail Page
- [ ] Delete button visible only for authorized users
- [ ] Disabled button with tooltip for unauthorized users
- [ ] Confirmation dialog shows session count
- [ ] Success navigation to MyEvents page

### Phase 4: MyEvents Page
- [ ] Delete icon visible only for authorized events
- [ ] Optimistic UI: card removed immediately
- [ ] Rollback mechanism on API failure
- [ ] Clear success/error feedback

### Phase 5: Testing
- [ ] Unit tests cover all authorization scenarios
- [ ] Integration tests verify database state
- [ ] Manual testing completes all scenarios
- [ ] No console errors or warnings
