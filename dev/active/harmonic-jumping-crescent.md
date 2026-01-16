# Blazor Enterprise Refactoring - ISLAMU Event

## Executive Summary

Transform the ISLAMU Event Blazor frontend into an enterprise-grade application that fully utilizes all API capabilities. This plan addresses critical architectural issues, implements missing features, and establishes production-ready patterns.

**Scope**: 45+ entities, 180+ DTOs, complete event lifecycle management, organization workflows, and advanced filtering.

**Timeline**: 40-60 hours of focused implementation.

**Outcome**: Production-ready, maintainable, enterprise-grade Blazor WASM+Server application.

---

## Critical Issues Identified

### 🔴 High Priority (Breaks Core Functionality)

1. **Event Editing Broken**
   - Sessions not updated when editing event
   - Categories/tags not persisted on update
   - New sessions added in editor but ignored on save
   - **Impact**: Cannot modify events after creation

2. **Event Session Management**
   - Sessions created AFTER event (not atomic)
   - No rollback if session creation fails
   - Cannot edit existing sessions in event edit flow
   - **Impact**: Data inconsistency, orphaned events

3. **Hard-coded TenantId**
   - `Guid.Parse("00000000-0000-0000-0000-000000000001")` in CreateEvent
   - Should come from authenticated user context
   - **Impact**: Multi-tenancy broken, security issue

4. **No State Management**
   - UserId extracted multiple times
   - TenantId hard-coded
   - No global auth context
   - **Impact**: Maintainability nightmare

### 🟡 Medium Priority (Missing Features)

5. **Organization Member Management**
   - API has full invitation workflow
   - No UI for invitations, member roles, positions
   - **Impact**: Cannot manage organization teams

6. **Advanced Event Filtering**
   - API supports filtering by madhab, age, gender, language, location
   - UI only implements basic filters
   - **Impact**: Poor discovery experience

7. **Lookup Table Management**
   - No admin UI for EventTypes, Formats, Statuses, Languages, etc.
   - **Impact**: Cannot configure system without DB access

8. **Registration Flow Incomplete**
   - Approval workflow not implemented
   - No registration status tracking
   - **Impact**: Manual events cannot use approval system

### 🟢 Low Priority (Optimization)

9. **HTTP Client Anti-Pattern**
   - ImageStorageService creates new HttpClient per upload
   - Should use IHttpClientFactory
   - **Impact**: Socket exhaustion risk

10. **Sequential Operations**
    - Category/tag assignment loops sequentially
    - Should parallelize after event creation
    - **Impact**: Slow event creation

---

## MudBlazor 7 Best Practices Integration

Based on latest MudBlazor documentation, we'll follow these patterns:

**Form Validation**:
```razor
<EditForm Model="@model" OnValidSubmit="OnValidSubmit">
    <DataAnnotationsValidator/>
    <MudTextField @bind-Value="model.Title" For="@(() => model.Title)" Label="Title" />
    <ValidationSummary />
</EditForm>
```

**Dialog Pattern**:
```csharp
var parameters = new DialogParameters<MyDialog>
{
    { x => x.ContentText, "Dialog content" },
    { x => x.Data, myData }
};
var dialog = await DialogService.ShowAsync<MyDialog>("Title", parameters);
var result = await dialog.Result;
```

**Code-Behind Pattern** (Recommended for complex components):
```csharp
// Component.razor
@inherits ComponentBase
@code { /* minimal inline code */ }

// Component.razor.cs
public partial class Component : ComponentBase
{
    [Inject] private IService Service { get; set; }
    // All logic here
}
```

**EventCallback Usage**:
```csharp
[Parameter] public EventCallback<T> OnDataChanged { get; set; }
await OnDataChanged.InvokeAsync(data);
```

---

## Implementation Strategy

### Phase 1: Foundation & Architecture (8-12 hours)

**Goal**: Fix critical architectural issues, establish enterprise patterns.

#### 1.1 State Management Foundation
**Files to Create/Modify**:
- `Explore.Blazor.Client/Services/AuthStateService.cs` (new)
- `Explore.Blazor.Client/Providers/TenantContextProvider.cs` (new)
- `Explore.Blazor/Components/App.razor` (modify)

**Implementation**:
```csharp
// AuthStateService.cs - Central auth state management
public class AuthStateService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public async Task<string> GetCurrentUserIdAsync()
    {
        // Fallback chain: sub → nameidentifier → sid
        return userId ?? throw new UnauthorizedException();
    }

    public async Task<Guid> GetCurrentTenantIdAsync()
    {
        // Extract from claims or tenant context
        return tenantId;
    }
}

// TenantContextProvider.razor - Cascading parameter provider
<CascadingValue Value="@tenantContext">
    @ChildContent
</CascadingValue>
```

**Acceptance Criteria**:
- ✅ TenantId/UserId extracted once per request
- ✅ Available via DI or cascading parameter
- ✅ No hard-coded GUIDs anywhere
- ✅ Unauthorized users redirected to login

---

#### 1.2 Fix HTTP Client Pattern
**Files to Modify**:
- `Explore.Blazor.Client/Services/ImageStorageService.cs`
- `Explore.Blazor/Program.cs`

**Implementation**:
```csharp
// Program.cs - Register named HTTP client
builder.Services.AddHttpClient("S3Upload", client => {
    client.Timeout = TimeSpan.FromMinutes(5);
});

// ImageStorageService.cs
private readonly IHttpClientFactory _httpClientFactory;

public async Task<bool> UploadImageAsync(string uploadUrl, IBrowserFile file)
{
    using var s3Client = _httpClientFactory.CreateClient("S3Upload");
    // ... upload logic
}
```

**Acceptance Criteria**:
- ✅ IHttpClientFactory used for all HTTP calls
- ✅ No `new HttpClient()` anywhere
- ✅ Upload timeout configurable

---

#### 1.3 Service Layer Refactoring
**Files to Create/Modify**:
- `Explore.Blazor.Client/Services/EventOrchestrationService.cs` (new)
- `Explore.Blazor.Client/Services/EventService.cs` (modify)

**Implementation**:
```csharp
// EventOrchestrationService.cs - Handle complex multi-step operations
public class EventOrchestrationService
{
    public async Task<CreateEventResult> CreateCompleteEventAsync(
        CreateEventDto eventDto,
        List<SessionEditorModel> sessions,
        List<Guid> categoryIds,
        List<Guid> tagIds,
        IBrowserFile? featuredImage)
    {
        // Transactional orchestration:
        // 1. Upload image (if provided)
        // 2. Create event
        // 3. Create sessions IN PARALLEL
        // 4. Assign categories/tags IN PARALLEL
        // 5. Rollback on any failure

        try
        {
            // ... implementation
        }
        catch (Exception ex)
        {
            // Rollback created entities
            await RollbackEventAsync(createdEventId);
            throw;
        }
    }
}
```

**Acceptance Criteria**:
- ✅ Event + sessions created atomically
- ✅ Rollback on failure
- ✅ Categories/tags assigned in parallel
- ✅ Clear separation of concerns

---

### Phase 2: Event Management Fixes (10-15 hours)

#### 2.1 Fix Event Creation Flow
**Files to Modify**:
- `Explore.Blazor.Client/Pages/Event/CreateEvent.razor`
- `Explore.Blazor.Client/Pages/Event/CreateEvent.razor.cs` (create code-behind)

**Implementation**:
```csharp
// CreateEvent.razor.cs
private async Task OnCreate()
{
    var result = await _orchestrationService.CreateCompleteEventAsync(
        eventDto: CreateEventDtoFromForm(),
        sessions: _sessions,
        categoryIds: _selectedCategories,
        tagIds: _selectedTags,
        featuredImage: _selectedImage
    );

    if (result.Success)
    {
        _snackbar.Add("Event created successfully!", Severity.Success);
        _navigationManager.NavigateTo($"/events/{result.EventId}");
    }
    else
    {
        _snackbar.Add($"Failed: {result.ErrorMessage}", Severity.Error);
    }
}
```

**Acceptance Criteria**:
- ✅ TenantId from auth context (not hard-coded)
- ✅ Event + sessions created atomically
- ✅ Rollback on failure
- ✅ User feedback on success/failure
- ✅ Minimum 1 session enforced

---

#### 2.2 Fix Event Editing Flow
**Files to Modify**:
- `Explore.Blazor.Client/Pages/Event/EventEdit.razor`
- `Explore.Blazor.Client/Pages/Event/EventEdit.razor.cs` (create code-behind)

**Implementation**:
```csharp
// EventEdit.razor.cs
private async Task OnUpdate()
{
    var result = await _orchestrationService.UpdateCompleteEventAsync(
        eventId: EventId,
        eventDto: UpdateEventDtoFromForm(),
        sessions: _sessions, // Include added/modified/deleted sessions
        categoryIds: _selectedCategories,
        tagIds: _selectedTags,
        featuredImage: _selectedImage
    );

    if (result.Success)
    {
        _snackbar.Add("Event updated successfully!", Severity.Success);
        await ReloadEventDetails();
    }
}
```

**Session Change Tracking**:
```csharp
// Track session changes
private List<SessionEditorModel> _sessionsToAdd = new();
private List<SessionEditorModel> _sessionsToUpdate = new();
private List<Guid> _sessionsToDelete = new();

// EventOrchestrationService.UpdateCompleteEventAsync
public async Task<UpdateEventResult> UpdateCompleteEventAsync(...)
{
    // 1. Update event details
    // 2. Delete removed sessions
    // 3. Update existing sessions IN PARALLEL
    // 4. Create new sessions IN PARALLEL
    // 5. Update categories/tags (diff and apply)
    // 6. Update featured image if changed
}
```

**Acceptance Criteria**:
- ✅ Event details updated
- ✅ Sessions can be added/edited/deleted
- ✅ Categories/tags updated (diff applied)
- ✅ Featured image replaceable
- ✅ Optimistic UI updates
- ✅ Rollback on failure

---

#### 2.3 Session Editor Component Refactoring
**Files to Modify**:
- `Explore.Blazor.Client/Components/Event/EventSessionEditor.razor`
- `Explore.Blazor.Client/Components/Event/EventSessionEditor.razor.cs` (create code-behind)

**Implementation**:
```csharp
// EventSessionEditor.razor.cs - Improved validation
[Parameter] public SessionEditorModel Session { get; set; }
[Parameter] public EventCallback<SessionEditorModel> SessionChanged { get; set; }
[Parameter] public EventCallback OnRemove { get; set; }
[Parameter] public List<LocationDto> AvailableLocations { get; set; }
[Parameter] public List<LanguageDto> AvailableLanguages { get; set; }
[Parameter] public List<RegistrationModeDto> AvailableRegistrationModes { get; set; }

private ValidationResult ValidateSession()
{
    var errors = new List<string>();

    if (string.IsNullOrWhiteSpace(Session.Title))
        errors.Add("Session title is required");

    if (Session.EndTime <= Session.StartTime)
        errors.Add("End time must be after start time");

    if (Session.StartTime < DateTime.Now)
        errors.Add("Session must start in the future");

    return new ValidationResult { IsValid = errors.Count == 0, Errors = errors };
}
```

**Acceptance Criteria**:
- ✅ Client-side validation
- ✅ Visual error indicators
- ✅ Date/time picker improvements
- ✅ Language multi-select works correctly
- ✅ Location dropdown with search

---

### Phase 3: Organization Management (8-12 hours)

#### 3.1 Organization Member Management
**Files to Create**:
- `Explore.Blazor.Client/Pages/Organization/OrganizationMembers.razor`
- `Explore.Blazor.Client/Pages/Organization/OrganizationMembers.razor.cs`
- `Explore.Blazor.Client/Components/Organization/AddMemberDialog.razor`
- `Explore.Blazor.Client/Components/Organization/UpdateMemberRoleDialog.razor`

**Features**:
- Member list with roles/positions
- Add member (email invitation)
- Update member role
- Remove member
- Pending invitations list

**Implementation**:
```csharp
// OrganizationMembers.razor.cs
private async Task LoadMembersAsync()
{
    _members = await _organizationMemberService.GetMembersAsync(OrganizationId);
    _invitations = await _organizationMemberService.GetInvitationsAsync();
}

private async Task OnAddMember()
{
    var dialog = await _dialogService.ShowAsync<AddMemberDialog>(
        "Add Member",
        new DialogParameters { ["OrganizationId"] = OrganizationId }
    );

    var result = await dialog.Result;
    if (!result.Cancelled)
    {
        await LoadMembersAsync();
    }
}
```

**Acceptance Criteria**:
- ✅ List all organization members
- ✅ Add members via email invitation
- ✅ Update member roles (Admin/Manager/Member)
- ✅ Remove members with confirmation
- ✅ Display pending invitations
- ✅ Accept/decline invitations

---

#### 3.2 Organization Invitation Workflow
**Files to Modify**:
- `Explore.Blazor.Client/Services/OrganizationMemberService.cs`
- `Explore.Blazor.Client/Pages/User/MyInvitations.razor` (new)

**Implementation**:
```csharp
// OrganizationMemberService.cs
public async Task<List<OrganizationInvitationDto>> GetMyInvitationsAsync()
{
    return await _apiClient.InvitationsAsync();
}

public async Task<bool> AcceptInvitationAsync(int invitationId)
{
    await _apiClient.Invitations2Async(invitationId);
    return true;
}

public async Task<bool> DeclineInvitationAsync(int invitationId)
{
    await _apiClient.Invitations3Async(invitationId);
    return true;
}
```

**Acceptance Criteria**:
- ✅ User can view all pending invitations
- ✅ Accept invitation → auto-join organization
- ✅ Decline invitation → remove from pending
- ✅ Notification badge for pending invitations

---

### Phase 4: Advanced Event Discovery (6-10 hours)

#### 4.1 Implement All Filters
**Files to Modify**:
- `Explore.Blazor.Client/Pages/Event/EventList.razor`
- `Explore.Blazor.Client/Pages/Event/EventList.razor.cs`

**Filters to Add**:
1. **Madhab Filter** - Islamic school (Hanafi, Maliki, Shafi'i, Hanbali, All)
2. **Audience Age Filter** - Children, Youth, Adults, Seniors, All Ages
3. **Audience Gender Filter** - Men-only, Women-only, Mixed, Family
4. **Language Filter** - Arabic, English, French, etc.
5. **Location Radius Filter** - Within X km of location (PostGIS)
6. **Event Status Filter** - Upcoming, Ongoing, Completed, Cancelled

**Implementation**:
```csharp
// EventList.razor.cs - Filter state
private int? _selectedMadhabId;
private int? _selectedAudienceAgeId;
private int? _selectedAudienceGenderId;
private List<int> _selectedLanguageIds = new();
private LocationFilterModel? _locationFilter;

// Filter application
private IEnumerable<EventListDto> AllFilteredEvents
{
    get
    {
        var filtered = _allEvents.AsEnumerable();

        if (_selectedMadhabId.HasValue)
            filtered = filtered.Where(e => e.MadhabId == _selectedMadhabId);

        if (_selectedAudienceAgeId.HasValue)
            filtered = filtered.Where(e => e.AudienceAgeId == _selectedAudienceAgeId);

        if (_selectedAudienceGenderId.HasValue)
            filtered = filtered.Where(e => e.AudienceGenderId == _selectedAudienceGenderId);

        if (_selectedLanguageIds.Any())
            filtered = filtered.Where(e => e.Languages?.Any(l => _selectedLanguageIds.Contains(l.Id)) == true);

        // ... other filters

        return filtered;
    }
}
```

**Acceptance Criteria**:
- ✅ All filter dropdowns populated from lookup tables
- ✅ Filters apply correctly
- ✅ URL parameters for filter state (shareable links)
- ✅ Clear all filters button
- ✅ Active filter chips display

---

#### 4.2 Server-Side Filtering API Integration
**Files to Modify**:
- `Explore.Blazor.Client/Services/EventService.cs`
- `Explore.Blazor.Client/Pages/Event/EventList.razor.cs`

**Implementation**:
```csharp
// EventService.cs - Add filter parameters
public async Task<PaginatedResult<EventListDto>> GetEventsAsync(
    int pageNumber = 1,
    int pageSize = 20,
    int? madhabId = null,
    int? audienceAgeId = null,
    int? audienceGenderId = null,
    List<int>? languageIds = null,
    int? eventTypeId = null,
    int? eventFormatId = null,
    string? searchTerm = null)
{
    // Call API with query parameters
    return await _apiClient.EventGETAsync(
        pageNumber, pageSize, madhabId, audienceAgeId,
        audienceGenderId, languageIds, eventTypeId,
        eventFormatId, searchTerm
    );
}
```

**Note**: If API doesn't support these query parameters, client-side filtering is acceptable for MVP.

**Acceptance Criteria**:
- ✅ Pagination works with filters
- ✅ Performance acceptable (< 2s load time)
- ✅ Loading states during filter changes

---

### Phase 5: Registration & Approval Flow (4-6 hours)

#### 5.1 Registration Status Tracking
**Files to Modify**:
- `Explore.Blazor.Client/Pages/User/MyRegistrations.razor`
- `Explore.Blazor.Client/Components/Event/RegistrationStatusBadge.razor` (new)

**Implementation**:
```razor
<!-- RegistrationStatusBadge.razor -->
@if (ApprovalStatus == "Pending")
{
    <MudChip Color="Color.Warning" Icon="@Icons.Material.Filled.HourglassEmpty">
        Pending Approval
    </MudChip>
}
else if (ApprovalStatus == "Approved")
{
    <MudChip Color="Color.Success" Icon="@Icons.Material.Filled.CheckCircle">
        Approved
    </MudChip>
}
else if (ApprovalStatus == "Rejected")
{
    <MudChip Color="Color.Error" Icon="@Icons.Material.Filled.Cancel">
        Rejected
    </MudChip>
}
```

**Acceptance Criteria**:
- ✅ Display registration status badges
- ✅ Filter registrations by status
- ✅ Cancel pending registration
- ✅ Re-register after rejection

---

#### 5.2 Event Organizer Approval Dashboard
**Files to Create**:
- `Explore.Blazor.Client/Pages/Organization/EventRegistrationApprovals.razor`
- `Explore.Blazor.Client/Pages/Organization/EventRegistrationApprovals.razor.cs`

**Features**:
- List pending registrations for organization's events
- Approve/reject registrations
- Bulk approve/reject
- Notification to users on status change

**Implementation**:
```csharp
// EventRegistrationApprovals.razor.cs
private async Task ApproveRegistrationAsync(Guid registrationId)
{
    var updateDto = new UpdateEventRegistrationDto
    {
        Id = registrationId,
        ApprovalStatusId = 2 // Approved
    };

    var result = await _eventRegistrationService.UpdateAsync(updateDto);

    if (result.Success)
    {
        _snackbar.Add("Registration approved", Severity.Success);
        await LoadPendingRegistrationsAsync();
    }
}
```

**Acceptance Criteria**:
- ✅ List all pending registrations for org events
- ✅ Approve registration
- ✅ Reject registration with reason
- ✅ Bulk operations
- ✅ Real-time updates (SignalR optional)

---

### Phase 6: Admin & Configuration (6-8 hours)

#### 6.1 Lookup Table Management
**Files to Create**:
- `Explore.Blazor.Client/Pages/Admin/LookupTableManagement.razor`
- `Explore.Blazor.Client/Components/Admin/LookupTableEditor.razor`

**Tables to Manage**:
- EventType, EventFormat, EventStatus
- AudienceAge, AudienceGender
- Madhab, Language
- RegistrationMode, ApprovalStatus
- FileType, ActorType, VisibilityType

**Implementation**:
```csharp
// LookupTableManagement.razor.cs - Generic CRUD
private async Task<List<T>> LoadLookupTableAsync<T>(string entityName)
{
    return entityName switch
    {
        "EventType" => await _adminService.GetEventTypesAsync(),
        "Language" => await _adminService.GetLanguagesAsync(),
        // ... other tables
        _ => throw new NotImplementedException()
    };
}
```

**Acceptance Criteria**:
- ✅ List all lookup tables
- ✅ CRUD operations for each table
- ✅ Validation (unique MasterCode)
- ✅ Prevent deletion if referenced
- ✅ Admin role required

---

#### 6.2 Category & Tag Management
**Files to Create**:
- `Explore.Blazor.Client/Pages/Admin/CategoryManagement.razor`
- `Explore.Blazor.Client/Pages/Admin/TagManagement.razor`
- `Explore.Blazor.Client/Components/Admin/CategoryTreeView.razor`

**Features**:
- Hierarchical category tree display
- Drag-and-drop to reorder categories
- Add/edit/delete categories
- Prevent circular references
- Tag CRUD with TagType assignment

**Implementation**:
```razor
<!-- CategoryTreeView.razor -->
<MudTreeView T="CategoryDto" Items="@_categories" @bind-SelectedValue="@_selectedCategory">
    <ItemTemplate>
        <MudTreeViewItem Value="@context" Text="@context.FullName">
            @if (context.Children?.Any() == true)
            {
                @foreach (var child in context.Children)
                {
                    <CategoryTreeView Categories="@child.Children" />
                }
            }
        </MudTreeViewItem>
    </ItemTemplate>
</MudTreeView>
```

**Acceptance Criteria**:
- ✅ Display category hierarchy
- ✅ Add subcategories
- ✅ Edit category details
- ✅ Delete category (if no events use it)
- ✅ Circular reference validation
- ✅ Tag CRUD with TagType filtering

---

### Phase 7: Component Optimization & Best Practices (4-6 hours)

#### 7.1 Code-Behind Pattern
**Goal**: Separate logic from markup for all complex components.

**Files to Refactor**:
- Create `.razor.cs` files for:
  - `CreateEvent.razor`
  - `EventEdit.razor`
  - `EventDetail.razor`
  - `EventList.razor`
  - `MyEvents.razor`
  - `OrganizationDetails.razor`

**Pattern**:
```csharp
// EventList.razor
@page "/events"
@inherits EventListBase

<MudContainer>
    <!-- Markup only -->
</MudContainer>

// EventList.razor.cs
public partial class EventListBase : ComponentBase
{
    [Inject] private IEventService EventService { get; set; }

    private List<EventListDto> _events = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadEventsAsync();
    }

    private async Task LoadEventsAsync()
    {
        _events = await EventService.GetAllEventsAsync();
        StateHasChanged();
    }
}
```

**Acceptance Criteria**:
- ✅ All pages with 200+ lines use code-behind
- ✅ Markup files < 300 lines
- ✅ Logic testable separately
- ✅ No warnings/errors after refactoring

---

#### 7.2 Validation Improvements
**Files to Create**:
- `Explore.Blazor.Client/Validators/CreateEventDtoValidator.cs` (client-side)
- `Explore.Blazor.Client/Validators/SessionEditorModelValidator.cs`

**Implementation**:
```csharp
// FluentValidation for client-side
public class CreateEventDtoClientValidator : AbstractValidator<CreateEventDto>
{
    public CreateEventDtoClientValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Event title is required")
            .MaximumLength(500).WithMessage("Title too long");

        RuleFor(x => x.StartTime)
            .GreaterThan(DateTime.Now).WithMessage("Event must start in future");

        RuleFor(x => x.Sessions)
            .NotEmpty().WithMessage("At least one session required");
    }
}

// In CreateEvent.razor
<EditForm Model="@createDto" OnValidSubmit="@OnCreate">
    <FluentValidationValidator Validator="@_validator" />
    <ValidationSummary />

    <!-- Form fields -->
</EditForm>
```

**Acceptance Criteria**:
- ✅ Client-side validation before API call
- ✅ Clear error messages
- ✅ Field-level validation indicators
- ✅ Form cannot submit if invalid

---

#### 7.3 Loading States & Error Handling
**Files to Modify**:
- All pages with data loading

**Pattern**:
```razor
@if (_loading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (_error != null)
{
    <MudAlert Severity="Severity.Error">@_error</MudAlert>
    <MudButton OnClick="@Retry">Retry</MudButton>
}
else if (!_events.Any())
{
    <MudText>No events found</MudText>
}
else
{
    <!-- Display events -->
}
```

**Acceptance Criteria**:
- ✅ Loading states for all async operations
- ✅ Error messages displayed clearly
- ✅ Retry buttons for failed operations
- ✅ Empty states with helpful messages

---

### Phase 6: Manual Testing & Validation (DEFERRED)

**Note**: Automated testing deferred to separate task. Manual testing will be performed during implementation.

**Manual Testing Checklist** (to verify during implementation):
- [ ] Create event with single session
- [ ] Create event with multiple sessions, languages, speakers
- [ ] Edit event and modify sessions
- [ ] Delete event
- [ ] Register for event
- [ ] Cancel registration
- [ ] Search events
- [ ] Filter events by category, tag, madhab, format
- [ ] Organization member invite workflow
- [ ] Accept/decline organization invitation
- [ ] Approve/reject event registration (organizer view)
- [ ] Upload featured image
- [ ] View event details
- [ ] Navigate between pages

---

## Critical Files Reference

### Services
- `Explore.Blazor.Client/Services/EventService.cs` - Event CRUD
- `Explore.Blazor.Client/Services/EventOrchestrationService.cs` - **NEW** Multi-step event operations
- `Explore.Blazor.Client/Services/AuthStateService.cs` - **NEW** Auth state management
- `Explore.Blazor.Client/Services/ImageStorageService.cs` - Image upload
- `Explore.Blazor.Client/Services/OrganizationMemberService.cs` - Member management
- `Explore.Blazor.Client/Services/EventRegistrationService.cs` - Registration management

### Pages
- `Explore.Blazor.Client/Pages/Event/CreateEvent.razor` - Event creation form
- `Explore.Blazor.Client/Pages/Event/EventEdit.razor` - Event editing form
- `Explore.Blazor.Client/Pages/Event/EventDetail.razor` - Event details view
- `Explore.Blazor.Client/Pages/Event/EventList.razor` - Event discovery
- `Explore.Blazor.Client/Pages/Organization/OrganizationMembers.razor` - **NEW** Member management
- `Explore.Blazor.Client/Pages/Admin/LookupTableManagement.razor` - **NEW** Admin config

### Components
- `Explore.Blazor.Client/Components/Event/EventSessionEditor.razor` - Session editor
- `Explore.Blazor.Client/Components/ImageUpload.razor` - Image upload widget
- `Explore.Blazor.Client/Components/Organization/AddMemberDialog.razor` - **NEW** Add member
- `Explore.Blazor.Client/Components/Admin/CategoryTreeView.razor` - **NEW** Category hierarchy

### Providers
- `Explore.Blazor.Client/Providers/TenantContextProvider.razor` - **NEW** Tenant context
- `Explore.Blazor/Components/App.razor` - Root component (wrap with providers)

---

## Verification Steps

### After Phase 1 (Foundation)
1. Verify no hard-coded TenantId anywhere (`git grep "00000000-0000-0000-0000-000000000001"`)
2. Verify AuthStateService returns correct userId/tenantId
3. Verify IHttpClientFactory used for all HTTP calls
4. Build succeeds with no warnings

### After Phase 2 (Event Management)
1. Create event with 3 sessions → verify all sessions saved
2. Edit event → add session → verify session persisted
3. Edit event → delete session → verify session removed
4. Update event categories/tags → verify changes saved
5. Create event without image → verify default image used

### After Phase 3 (Organization)
1. Invite user to organization → verify invitation created
2. Accept invitation → verify user added as member
3. Update member role → verify role changed
4. Remove member → verify member removed

### After Phase 4 (Discovery)
1. Filter by madhab → verify correct events shown
2. Filter by audience age → verify correct events shown
3. Combine filters → verify AND logic works
4. Clear filters → verify all events shown

### After Phase 5 (Registration)
1. Register for event → verify registration created
2. Check approval status → verify correct status shown
3. Organizer approves → verify status updated
4. Organizer rejects → verify status updated

### After Phase 6 (Admin)
1. Add new EventType → verify saved
2. Edit Language → verify updated
3. Delete unused Madhab → verify deleted
4. Try delete referenced EventType → verify prevented

### End-to-End Verification
1. Create organization
2. Invite member
3. Member accepts
4. Create event with sessions/languages/speakers
5. User registers for event
6. Organizer approves registration
7. User views "My Registrations" → sees approved status
8. Organizer edits event → adds session
9. User sees updated event details

---

## Risk Mitigation

### Data Loss Prevention
- Implement undo/redo for form changes
- Auto-save drafts to localStorage
- Confirmation dialogs for destructive actions
- Soft-delete pattern for events (archive instead of hard delete)

### Performance Optimization
- Lazy loading for large datasets
- Virtual scrolling for long lists
- Debounce search inputs (500ms)
- Parallel API calls where possible
- Cache lookup tables (1 hour TTL)

### Security Considerations
- Always validate TenantId matches authenticated user's tenant
- Never expose other tenants' data
- Sanitize user inputs
- CSRF protection on all forms
- Rate limiting on API calls

---

## Success Metrics (Phases 1-5)

1. **Critical Issues Resolved**: ✅
   - Event editing fully functional (sessions/categories save correctly)
   - Hard-coded TenantId removed, proper auth context established
   - Organization member management with invitation workflow complete
   - Advanced filtering implemented (madhab, age, gender, language)

2. **Code Quality**: ✅
   - No hard-coded GUIDs or magic values
   - IHttpClientFactory pattern throughout
   - Proper error handling with user feedback
   - MudBlazor 7 best practices followed

3. **Performance**: ✅
   - Event list loads < 2s
   - Event creation < 5s (with image upload)
   - Filter changes < 1s

4. **User Experience**: ✅
   - No unhandled exceptions in critical flows
   - Clear loading states (MudProgressCircular)
   - User feedback via MudSnackbar on all actions
   - Validation messages clear and actionable

5. **Maintainability**: ✅
   - Service layer properly orchestrates complex operations
   - Code-behind pattern for complex components
   - Consistent naming and structure
   - Transactional safety with rollback

6. **Production Readiness**: ✅
   - All user-facing features functional
   - Manual testing checklist completed
   - No critical bugs or data loss scenarios
   - Ready for deployment

---

## Post-Implementation Improvements

### Future Enhancements (Not in Scope)
1. Real-time notifications (SignalR)
2. Calendar view for events
3. Map view with clustering (PostGIS integration)
4. Advanced analytics dashboard
5. Event recommendation engine
6. Federated event discovery (ATProto/ActivityPub)
7. Mobile app (MAUI)
8. Offline mode (PWA)

---

## Estimated Timeline (Phases 1-5 Only)

| Phase | Hours | Dependencies | Priority |
|-------|-------|--------------|----------|
| Phase 1: Foundation & Architecture | 8-12 | None | 🔴 Critical |
| Phase 2: Event Management Fixes | 10-15 | Phase 1 | 🔴 Critical |
| Phase 3: Organization Management | 8-12 | Phase 1 | 🔴 Critical |
| Phase 4: Advanced Event Discovery | 6-10 | Phase 2 | 🔴 Critical |
| Phase 5: Registration & Approval Flow | 4-6 | Phase 2 | 🔴 Critical |

**Total**: 36-55 hours (conservative: 45 hours / ~1 week full-time)

**Deferred to Later** (separate tasks):
- Phase 6: Admin & Configuration (lookup tables, categories, tags)
- Phase 7: Component Optimization (code-behind refactoring, validation improvements)
- Phase 8: Automated Testing (unit tests, integration tests)
- Phase 9: Documentation (component guides, developer onboarding)

---

## Implementation Order

**Week 1** (Full-time: 40 hours):
- **Days 1-2**: Phase 1 (Foundation) - 10 hours
  - AuthStateService, TenantContextProvider
  - HTTP client factory pattern
  - EventOrchestrationService scaffolding

- **Days 3-4**: Phase 2 (Event Management) - 14 hours
  - Fix CreateEvent flow (atomic event+sessions)
  - Fix EventEdit flow (session updates working)
  - Refactor EventSessionEditor component

- **Day 5**: Phase 3 (Organization) - 8 hours
  - OrganizationMembers page
  - Invitation workflow UI

**Week 2 (Part-time or continued)** (8-16 hours):
- **Days 1-2**: Phase 4 (Discovery) - 8 hours
  - Add madhab, age, gender, language filters
  - Improve filter UI/UX

- **Day 3**: Phase 5 (Registration) - 6 hours
  - Registration status badges
  - Approval dashboard for organizers

**Buffer**: 5-10 hours for unexpected issues, refinements, manual testing

---

This plan transforms the Blazor frontend into a production-ready, enterprise-grade application that fully utilizes all API capabilities while following industry best practices.