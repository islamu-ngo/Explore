# Common Blazor UI Patterns

This document describes frequently used UI implementation patterns in the ISLAMU Event Blazor application, covering forms, dialogs, tables, navigation, loading states, error handling, search/filter functionalities, and infinite scroll. These patterns ensure consistency, reusability, and maintainability across the user interface.

---

## 1. Form Patterns

Forms are central to data input. They are typically built using MudBlazor components and integrated with validation.

### Basic Form with MudBlazor Components

```razor
@inject IMediator Mediator
@inject ISnackbar Snackbar
@inject NavigationManager NavigationManager

<MudCard>
    <MudCardHeader>
        <MudText Typo="Typo.h5">Create New Event</MudText>
    </MudCardHeader>
    <MudCardContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="Event Title"
                      Required="true"
                      RequiredError="Title is required"
                      MaxLength="200"
                      Class="mb-4" />

        <MudTextField @bind-Value="_dto.Description"
                      Label="Description"
                      Lines="5"
                      MaxLength="500"
                      Counter="500"
                      Class="mb-4" />

        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       Required="true"
                       MinDate="DateTime.Today"
                       Class="mb-4" />

        <MudSelect @bind-Value="_dto.AudienceAgeId"
                   Label="Audience Age"
                   Required="true"
                   RequiredError="Audience Age is required"
                   Class="mb-4">
            @foreach (var age in _audienceAges)
            {
                <MudSelectItem Value="@age.Id">@age.Name</MudSelectItem>
            }
        </MudSelect>
    </MudCardContent>
    <MudCardActions Class="d-flex justify-end">
        <MudButton OnClick="Cancel" Variant="Variant.Text">Cancel</MudButton>
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   OnClick="Submit"
                   Disabled="_isSubmitting"
                   Class="ml-2">
            @if (_isSubmitting)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                <MudText>Creating...</MudText>
            }
            else
            {
                <text>Create Event</text>
            }
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    private CreateEventDto _dto = new();
    private DateTime? _startDate;
    private bool _isSubmitting;
    private List<AudienceAgeDto> _audienceAges = new(); // Assume this is loaded from a service/mediator

    protected override async Task OnInitializedAsync()
    {
        // Load dropdown data
        // _audienceAges = await Mediator.Send(new GetAudienceAgeListRequest());
    }

    private async Task Submit()
    {
        // Basic client-side validation check
        if (!_startDate.HasValue)
        {
            Snackbar.Add("Start date is required", Severity.Error);
            return;
        }

        _dto.FirstSessionDate = DateOnly.FromDateTime(_startDate.Value); // Map DateTime? to DateOnly?
        _isSubmitting = true;

        try
        {
            var command = new CreateEventCommand { EventDto = _dto };
            var result = await Mediator.Send(command); // Send command via MediatR

            if (result.Success)
            {
                Snackbar.Add("Event created successfully", Severity.Success);
                NavigationManager.NavigateTo($"/events/{result.Id}"); // Navigate on success
            }
            else
            {
                // Display validation errors from the backend
                foreach (var error in result.Errors)
                {
                    Snackbar.Add(error, Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating event.");
            Snackbar.Add("An unexpected error occurred.", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/events"); // Navigate away from the form
    }
}
```

### Form with FluentValidation Integration

For more complex validation rules, especially those involving cross-field or asynchronous checks, FluentValidation is integrated with Blazor's `EditForm`.

```razor
@using FluentValidation // Need to install FluentValidation.AspNetCore for this
@using Microsoft.AspNetCore.Components.Forms // For EditForm and ValidationSummary

<EditForm Model="@_dto" OnValidSubmit="HandleValidSubmit" OnInvalidSubmit="HandleInvalidSubmit">
    <FluentValidationValidator @ref="_validator" /> @* Provides component-level access to validation state *@
    <ValidationSummary /> @* Displays all validation messages *@

    <MudCard>
        <MudCardContent>
            <MudTextField @bind-Value="_dto.Title"
                          For="@(() => _dto.Title)" @* Links to the property for validation *@
                          Label="Event Title"
                          Immediate="true" /> @* Validates on blur or keyup *@

            <MudTextField @bind-Value="_dto.Email"
                          For="@(() => _dto.Email)"
                          Label="Contact Email"
                          Immediate="true" />
        </MudCardContent>
        <MudCardActions>
            <MudButton ButtonType="ButtonType.Submit"
                       Variant="Variant.Filled"
                       Color="Color.Primary">
                Submit
            </MudButton>
        </MudCardActions>
    </MudCard>
</EditForm>

@code {
    private CreateEventDto _dto = new(); // The model being validated
    private FluentValidationValidator? _validator; // Reference to the validator component
    
    // An instance of your FluentValidation validator for the DTO
    // private CreateEventDtoValidator _dtoValidator = new CreateEventDtoValidator(); 

    private async Task HandleValidSubmit()
    {
        // Form is valid based on FluentValidation rules, proceed with submission
        var command = new CreateEventCommand { EventDto = _dto };
        await Mediator.Send(command);
        Snackbar.Add("Form submitted successfully!", Severity.Success);
    }

    private void HandleInvalidSubmit()
    {
        Snackbar.Add("Please correct the form errors.", Severity.Error);
    }
}
```

---

## 2. Dialog Patterns

MudDialog is used for modal interactions, from simple confirmations to complex data entry forms.

### Confirmation Dialog (`ConfirmDialog.razor`)

A reusable component for displaying a confirmation message with customizable text and button styling.

```razor
@* ConfirmDialog.razor *@
<MudDialog>
    <DialogContent>
        <MudText>@ContentText</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="@Color"
                   Variant="Variant.Filled"
                   OnClick="Submit">
            @ButtonText
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!; // ✅ Injected by IDialogService

    [Parameter]
    public string ContentText { get; set; } = "Are you sure?";

    [Parameter]
    public string ButtonText { get; set; } = "OK";

    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    private void Submit() => MudDialog.Close(DialogResult.Ok(true)); // Return true on confirm
    private void Cancel() => MudDialog.Cancel(); // Return canceled result
}
```

**Usage (Opening the Dialog)**:
```razor
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<MudButton OnClick="DeleteEvent">Delete Event</MudButton>

@code {
    private async Task DeleteEvent()
    {
        var parameters = new DialogParameters // Pass parameters to the ConfirmDialog component
        {
            ["ContentText"] = "Are you sure you want to delete this event? This action cannot be undone.",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(
            "Confirm Deletion", // Dialog title
            parameters,
            dialogOptions);

        var result = await dialog.Result; // Wait for the dialog to close

        if (!result.Canceled && (bool)(result.Data ?? false)) // Check if not canceled and data is true
        {
            // User confirmed, perform deletion
            Snackbar.Add("Event deleted successfully", Severity.Success);
            // ... Call backend to delete ...
        }
        else
        {
            Snackbar.Add("Deletion cancelled", Severity.Info);
        }
    }
}
```

### Form Dialog (`CreateEventDialog.razor`)

Embedding a form within a dialog for data input.

```razor
@* CreateEventDialog.razor *@
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Create New Event</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="Event Title"
                      Required="true" />
        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       MinDate="DateTime.Today" />
        @* ... other form fields ... *@
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="Submit"
                   Disabled="_isSubmitting">
            Create
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!;

    private CreateEventDto _dto = new();
    private DateTime? _startDate;
    private bool _isSubmitting;

    private void Cancel() => MudDialog.Cancel();

    private async Task Submit()
    {
        if (!_startDate.HasValue) { Snackbar.Add("Start date is required", Severity.Error); return; }
        _dto.FirstSessionDate = DateOnly.FromDateTime(_startDate.Value);
        _isSubmitting = true;

        try
        {
            var command = new CreateEventCommand { EventDto = _dto };
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                Snackbar.Add("Event created successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(result.Id)); // ✅ Close with the new entity ID
            }
            else
            {
                foreach (var error in result.Errors) Snackbar.Add(error, Severity.Error);
            }
        }
        finally { _isSubmitting = false; }
    }
}
```

---

## 3. Table Patterns

`MudTable` is the primary component for displaying tabular data, offering features like sorting, pagination, and filtering.

### CRUD Table

A common pattern for displaying a list of entities with actions for viewing, editing, and deleting.

```razor
@inject IMediator Mediator
@inject IDialogService DialogService
@inject ISnackbar Snackbar
@inject NavigationManager NavigationManager

<MudTable Items="@_events" Hover="true" Loading="@_isLoading" LoadingProgressColor="Color.Info">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Events List</MudText>
        <MudSpacer />
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="CreateNewEvent">
            Create Event
        </MudButton>
    </ToolBarContent>
    <HeaderContent>
        <MudTh>Title</MudTh>
        <MudTh>Date</MudTh>
        <MudTh>Location</MudTh>
        <MudTh>Status</MudTh>
        <MudTh Style="width: 150px">Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Title">@context.Title</MudTd>
        <MudTd DataLabel="Date">@context.FirstSessionDate?.ToShortDateString()</MudTd>
        <MudTd DataLabel="Location">@context.Location</MudTd> @* Assuming Location is a property in EventListDto *@
        <MudTd DataLabel="Status">
            <MudChip Size="Size.Small"
                     Color="@GetStatusColor(context.EventStatusFullName)">
                @context.EventStatusFullName
            </MudChip>
        </MudTd>
        <MudTd DataLabel="Actions">
            <MudIconButton Icon="@Icons.Material.Filled.Visibility"
                           Size="Size.Small"
                           OnClick="@(() => ViewEvent(context.Id))" />
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Size="Size.Small"
                           Color="Color.Primary"
                           OnClick="@(() => EditEvent(context.Id))" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           Size="Size.Small"
                           Color="Color.Error"
                           OnClick="@(() => DeleteEvent(context.Id))" />
        </MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager />
    </PagerContent>
</MudTable>

@code {
    private List<EventListDto> _events = new();
    private bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        await LoadEvents();
    }

    private async Task LoadEvents()
    {
        _isLoading = true;
        try
        {
            _events = await Mediator.Send(new GetEventListRequest());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load events.");
            Snackbar.Add("Failed to load events.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }

    private Color GetStatusColor(string status) => status switch
    {
        "Published" => Color.Success,
        "Draft" => Color.Warning,
        "Cancelled" => Color.Error,
        _ => Color.Default
    };

    private async Task CreateNewEvent()
    {
        var dialog = await DialogService.ShowAsync<CreateEventDialog>("Create Event");
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is Guid eventId)
        {
            Snackbar.Add($"Event created with ID: {eventId}", Severity.Success);
            await LoadEvents(); // Refresh table data
        }
    }

    private void ViewEvent(Guid id) => NavigationManager.NavigateTo($"/events/{id}");
    private void EditEvent(Guid id) => NavigationManager.NavigateTo($"/events/{id}/edit");

    private async Task DeleteEvent(Guid id)
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "Are you sure you want to delete this event? This action is irreversible.",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirm Deletion", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && (bool)(result.Data ?? false))
        {
            var deleteCommand = new DeleteEventCommand { Id = id };
            var deleteResult = await Mediator.Send(deleteCommand);

            if (deleteResult) // Assuming DeleteEventCommand returns bool
            {
                Snackbar.Add("Event deleted successfully", Severity.Success);
                await LoadEvents(); // Refresh table data
            }
            else
            {
                Snackbar.Add("Failed to delete event.", Severity.Error);
            }
        }
    }
}
```

---

## 4. Navigation Patterns

### Programmatic Navigation

Using `NavigationManager` for dynamic page routing.

```razor
@inject NavigationManager NavigationManager

@code {
    private void NavigateToEventDetails(Guid eventId)
    {
        NavigationManager.NavigateTo($"/events/{eventId}");
    }

    private void NavigateToEventsList()
    {
        NavigationManager.NavigateTo("/events");
    }

    private void OpenExternalLink(string url)
    {
        NavigationManager.NavigateTo(url, forceLoad: true); // forceLoad reloads the page
    }
}
```

### Navigation with Query Parameters

For filtering, sorting, or complex state that needs to be reflected in the URL.

```razor
@inject NavigationManager NavigationManager
@using Microsoft.AspNetCore.WebUtilities // Required for QueryHelpers

@code {
    private Guid? _organizationIdFilter;
    private int? _audienceAgeFilter;

    protected override void OnInitialized()
    {
        // Read query parameters on component initialization
        var uri = new Uri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("organizationId", out var orgIdValue) && Guid.TryParse(orgIdValue, out var parsedOrgId))
        {
            _organizationIdFilter = parsedOrgId;
        }
        if (query.TryGetValue("audienceAge", out var ageValue) && int.TryParse(ageValue, out var parsedAge))
        {
            _audienceAgeFilter = parsedAge;
        }
    }

    private void ApplyFiltersAndNavigate()
    {
        var queryParams = new Dictionary<string, object?>
        {
            ["organizationId"] = _organizationIdFilter,
            ["audienceAge"] = _audienceAgeFilter,
            ["page"] = 1 // Reset to first page when filters change
        };

        var uri = NavigationManager.GetUriWithQueryParameters(queryParams);
        NavigationManager.NavigateTo(uri);
    }
}
```

---

## 5. Loading State Patterns

Providing visual feedback during data loading is crucial for good UX.

### Simple Loading Indicator

```razor
@if (_isLoading)
{
    <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
    <MudText Class="ml-2">Loading events...</MudText>
}
else if (_events.Any())
{
    @* Display events *@
}
else
{
    <MudText>No events found matching your criteria.</MudText>
}

@code {
    private bool _isLoading;
    private List<EventListDto> _events = new();
}
```

### Skeleton Loading

Mimics the layout of the content that is about to be loaded, providing a better perceived performance.

```razor
@if (_isLoading)
{
    <MudGrid>
        @for (int i = 0; i < 6; i++) // Show 6 skeleton cards
        {
            <MudItem xs="12" sm="6" md="4" lg="3">
                <MudCard Elevation="1">
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="200px" Animation="Animation.Pulse" />
                    <MudCardContent>
                        <MudSkeleton Width="80%" Height="30px" />
                        <MudSkeleton Width="60%" Height="20px" Class="mt-2" />
                    </MudCardContent>
                </MudCard>
            </MudItem>
        }
    </MudGrid>
}
else
{
    @* Actual event cards *@
}
```

### Progress Overlay

Useful for full-screen loading or when waiting for an action to complete.

```razor
<MudOverlay Visible="_isLoading" DarkBackground="true" Absolute="true" ZIndex="MudBlazor.Defaults.ZIndex.Snackbar">
    <MudProgressCircular Indeterminate="true" Size="Size.Large" />
</MudOverlay>

<MudContainer>
    @* Page content here, will be overlaid when loading *@
</MudContainer>

@code {
    private bool _isLoading;
}
```

---

## 6. Error Handling Patterns

Robust error handling improves user experience and helps with debugging.

### Try-Catch with User Feedback

Wrap API calls or critical operations in `try-catch` blocks to handle exceptions gracefully and provide feedback to the user via `ISnackbar`.

```razor
@inject ISnackbar Snackbar
@inject ILogger<MyComponent> _logger // Inject ILogger for server-side logging

@code {
    private async Task LoadData()
    {
        _isLoading = true;
        try
        {
            _events = await Mediator.Send(new GetEventListRequest());
        }
        catch (HttpRequestException ex) // Handle network-related or API call errors
        {
            _logger.LogError(ex, "HTTP Request failed during event load.");
            Snackbar.Add("Network error. Please check your connection or try again.", Severity.Error);
        }
        catch (Exception ex) // Catch any other unexpected errors
        {
            _logger.LogError(ex, "An unexpected error occurred during event load.");
            Snackbar.Add("An unexpected error occurred. Please try again later.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### Blazor `ErrorBoundary` Component

The `ErrorBoundary` component catches unhandled exceptions in its child content and renders fallback UI.

```razor
<ErrorBoundary>
    <ChildContent>
        <EventList /> @* Component that might throw an exception *@
    </ChildContent>
    <ErrorContent Context="exception">
        <MudAlert Severity="Severity.Error" Variant="Variant.Filled" Class="mt-4">
            <MudText Typo="Typo.h6">Something went wrong!</MudText>
            <MudText>An error occurred while rendering the event list.</MudText>
            <MudText Class="mt-2">Error details: @exception.Message</MudText>
            <MudButton OnClick="@(() => exception.Recover())" Color="Color.Warning" Class="mt-3">
                Try Again
            </MudButton>
        </MudAlert>
    </ErrorContent>
</ErrorBoundary>
```

### Backend Command Result Handling

For command (write) operations, the backend returns a `BaseCommandResponse` which contains `Success`, `Message`, `Id`, and a list of `Errors`. This allows for structured error reporting.

```razor
@code {
    private async Task CreateNewEvent()
    {
        var command = new CreateEventCommand { EventDto = _dto };
        var result = await Mediator.Send(command);

        if (result.Success)
        {
            Snackbar.Add("Event created successfully", Severity.Success);
            NavigationManager.NavigateTo($"/events/{result.Id}");
        }
        else
        {
            // Display specific errors returned from the backend validator/handler
            foreach (var error in result.Errors)
            {
                Snackbar.Add(error, Severity.Error);
            }
            if (!string.IsNullOrEmpty(result.Message))
            {
                Snackbar.Add(result.Message, Severity.Warning);
            }
        }
    }
}
```

---

## 7. Search and Filter Patterns

Providing intuitive ways for users to find and narrow down data.

### Debounced Search Input

Prevents excessive updates or API calls by waiting for a pause in user typing.

```razor
<MudTextField @bind-Value="_searchTerm"
              Label="Search Events"
              Immediate="true" @* Triggers ValueChanged/OnDebounceIntervalElapsed on every keypress *@
              DebounceInterval="300" @* Wait 300ms after last keypress before invoking OnDebounceIntervalElapsed *@
              OnDebounceIntervalElapsed="OnSearch"
              Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Filled.Search" />

<MudGrid>
    @foreach (var evt in _filteredEvents)
    {
        <MudItem xs="12" md="6" lg="4">
            <EventCard Event="@evt" />
        </MudItem>
    }
</MudGrid>

@code {
    private string _searchTerm = string.Empty;
    private List<EventListDto> _events = new(); // Full list of events
    private List<EventListDto> _filteredEvents = new(); // List displayed to user

    protected override async Task OnInitializedAsync()
    {
        _events = await Mediator.Send(new GetEventListRequest()); // Load all events
        _filteredEvents = _events; // Initially show all
    }

    private void OnSearch()
    {
        // Apply client-side filtering based on _searchTerm
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            _filteredEvents = _events;
        }
        else
        {
            _filteredEvents = _events
                .Where(e => e.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           e.Description.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }
    }
}
```

### Filter Panel

A dedicated section for applying multiple filter criteria.

```razor
<MudGrid>
    <MudItem xs="12" md="3"> @* Occupy 1/4 width on medium screens and up *@
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.h6" Class="mb-4">Filters</MudText>

            <MudSelect @bind-Value="_filters.OrganizationId"
                       Label="Organization"
                       Clearable="true"
                       OnClearButtonClick="ClearOrganization"
                       Class="mb-3">
                @foreach (var org in _organizations)
                {
                    <MudSelectItem Value="@org.Id">@org.Name</MudSelectItem>
                }
            </MudSelect>

            <MudSelect @bind-Value="_filters.AudienceAgeId"
                       Label="Audience Age"
                       Clearable="true"
                       Class="mb-4">
                @foreach (var age in _audienceAges)
                {
                    <MudSelectItem Value="@age.Id">@age.Name</MudSelectItem>
                }
            </MudSelect>

            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       FullWidth="true"
                       OnClick="ApplyFilters"
                       Class="mb-2">
                Apply Filters
            </MudButton>

            <MudButton Variant="Variant.Text"
                       FullWidth="true"
                       OnClick="ClearAllFilters">
                Clear All
            </MudButton>
        </MudPaper>
    </MudItem>

    <MudItem xs="12" md="9"> @* Occupy 3/4 width on medium screens and up *@
        <MudGrid>
            @foreach (var evt in _filteredEvents)
            {
                <MudItem xs="12" sm="6" lg="4">
                    <EventCard Event="@evt" />
                </MudItem>
            }
        </MudGrid>
    </MudItem>
</MudGrid>

@code {
    private EventFilterDto _filters = new(); // DTO for holding filter values
    private List<EventListDto> _filteredEvents = new();
    private List<OrganizationDto> _organizations = new();
    private List<AudienceAgeDto> _audienceAges = new();

    protected override async Task OnInitializedAsync()
    {
        // Load initial filter options (organizations, audience ages)
        await LoadFilterOptions();
        await ApplyFilters(); // Apply initial filters
    }

    private async Task LoadFilterOptions() { /* ... */ }

    private async Task ApplyFilters()
    {
        var request = new GetEventListRequest // Assume GetEventListRequest accepts filter DTO
        {
            OrganizationId = _filters.OrganizationId,
            AudienceAgeId = _filters.AudienceAgeId,
            // ... other filters
        };

        _filteredEvents = await Mediator.Send(request);
    }

    private async Task ClearAllFilters()
    {
        _filters = new EventFilterDto(); // Reset filter DTO
        await ApplyFilters();
    }

    private void ClearOrganization()
    {
        _filters.OrganizationId = null; // Clear specific filter
    }
}
```

---

## 8. Infinite Scroll Pattern

Loads more data as the user scrolls to the bottom of a list, improving performance for very long lists.

```razor
@inject IJSRuntime JS

<div @ref="_scrollContainer" style="height: 600px; overflow-y: auto;">
    <MudGrid>
        @foreach (var evt in _events)
        {
            <MudItem xs="12" md="6" lg="4">
                <EventCard Event="@evt" />
            </MudItem>
        }
    </MudGrid>

    @if (_hasMore)
    {
        <div @ref="_loadMoreTrigger" class="d-flex justify-center py-4">
            <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
            <MudText Class="ml-2">Loading more...</MudText>
        </div>
    }
    else if (_events.Any())
    {
        <div class="d-flex justify-center py-4">
            <MudText Color="Color.Secondary">No more events to load.</MudText>
        </div>
    }
</div>

@code {
    private ElementReference _scrollContainer;
    private ElementReference _loadMoreTrigger; // A placeholder element at the bottom to observe
    private List<EventListDto> _events = new();
    private int _currentPage = 1;
    private bool _hasMore = true; // Indicates if there's more data to load
    private bool _isLoading = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // Initialize the Intersection Observer via JS interop
            // This JS function will invoke 'LoadMore' when '_loadMoreTrigger' becomes visible
            await JS.InvokeVoidAsync("app.registerIntersectionObserver", _loadMoreTrigger, DotNetObjectReference.Create(this), nameof(LoadMore));
            await LoadMoreEvents(); // Load initial set
        }
    }

    [JSInvokable] // Mark method as invokable from JavaScript
    public async Task LoadMore()
    {
        if (_isLoading || !_hasMore) return; // Prevent multiple loads or loading if no more data

        _isLoading = true;
        _currentPage++;

        await LoadMoreEvents();

        _isLoading = false;
        StateHasChanged(); // Force re-render after data is loaded
    }

    private async Task LoadMoreEvents()
    {
        var request = new GetEventListRequest
        {
            Page = _currentPage,
            PageSize = 12 // Number of items to load per scroll
        };

        // Assume this request returns a PagedResultDto with a list of events and a TotalCount or HasMore flag
        var result = await Mediator.Send(request); 
        
        if (result?.Events != null && result.Events.Any())
        {
            _events.AddRange(result.Events);
            _hasMore = result.HasMore; // Update hasMore based on backend response
        }
        else
        {
            _hasMore = false;
        }
    }

    // Don't forget to dispose of the Intersection Observer
    public void Dispose()
    {
        JS.InvokeVoidAsync("app.disposeIntersectionObserver", _loadMoreTrigger);
    }
}
```

**`wwwroot/js/site.js` (for Intersection Observer)**:
```javascript
window.app = {
    // Stores observers by element to clean them up later
    _intersectionObservers: new Map(),

    registerIntersectionObserver: (element, dotnetHelper, methodName) => {
        if (!element) {
            console.warn("Intersection Observer: Element not found.");
            return;
        }

        const options = {
            root: element.parentElement, // Observe relative to parent scroll container
            rootMargin: '0px',
            threshold: 0.1 // Trigger when 10% of target is visible
        };

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    dotnetHelper.invokeMethodAsync(methodName);
                }
            });
        }, options);

        observer.observe(element);
        app._intersectionObservers.set(element, observer);
    },

    disposeIntersectionObserver: (element) => {
        if (app._intersectionObservers.has(element)) {
            const observer = app._intersectionObservers.get(element);
            observer.unobserve(element);
            observer.disconnect();
            app._intersectionObservers.delete(element);
        }
    }
};
```

---

**Related Resources**:
- [component-design.md](component-design.md) - Component lifecycle and communication.
- [mudblazor-usage.md](mudblazor-usage.md) - Specific MudBlazor component usage.
- [state-management.md](state-management.md) - How to manage state for these patterns.
