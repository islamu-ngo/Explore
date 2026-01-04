# Common Patterns

Real-world implementation patterns for ISLAMU Event Blazor components.

---

## Form Patterns

### Basic Form with Validation

```razor
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudCard>
    <MudCardContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="Event Title"
                      Required="true"
                      RequiredError="Title is required"
                      MaxLength="200" />

        <MudTextField @bind-Value="_dto.Description"
                      Label="Description"
                      Lines="5"
                      MaxLength="500"
                      Counter="500" />

        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       Required="true"
                       MinDate="DateTime.Today" />

        <MudSelect @bind-Value="_dto.AudienceAgeId"
                   Label="Audience Age"
                   Required="true">
            @foreach (var age in _audienceAges)
            {
                <MudSelectItem Value="@age.Id">@age.Name</MudSelectItem>
            }
        </MudSelect>
    </MudCardContent>
    <MudCardActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   OnClick="Submit"
                   Disabled="_isSubmitting">
            @if (_isSubmitting)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" />
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
    private List<AudienceAgeDto> _audienceAges = new();

    protected override async Task OnInitializedAsync()
    {
        var request = new GetAudienceAgeListRequest();
        _audienceAges = await Mediator.Send(request);
    }

    private async Task Submit()
    {
        if (!_startDate.HasValue)
        {
            Snackbar.Add("Start date is required", Severity.Error);
            return;
        }

        _dto.StartDate = _startDate.Value;
        _isSubmitting = true;

        try
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
                foreach (var error in result.Errors)
                {
                    Snackbar.Add(error, Severity.Error);
                }
            }
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/events");
    }
}
```

### Form with FluentValidation

```razor
@using FluentValidation

<EditForm Model="@_dto" OnValidSubmit="HandleValidSubmit">
    <FluentValidationValidator @ref="_validator" />

    <MudCard>
        <MudCardContent>
            <MudTextField @bind-Value="_dto.Title"
                          For="@(() => _dto.Title)"
                          Label="Event Title"
                          Immediate="true" />

            <MudTextField @bind-Value="_dto.Email"
                          For="@(() => _dto.Email)"
                          Label="Contact Email"
                          Immediate="true" />

            <ValidationSummary />
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
    private CreateEventDto _dto = new();
    private FluentValidationValidator? _validator;

    private async Task HandleValidSubmit()
    {
        // Form is valid, submit
        var command = new CreateEventCommand { EventDto = _dto };
        await Mediator.Send(command);
    }
}
```

---

## Dialog Patterns

### Confirmation Dialog

**ConfirmDialog.razor**:
```razor
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
    private MudDialogInstance MudDialog { get; set; } = null!;

    [Parameter]
    public string ContentText { get; set; } = "Are you sure?";

    [Parameter]
    public string ButtonText { get; set; } = "OK";

    [Parameter]
    public Color Color { get; set; } = Color.Primary;

    private void Submit() => MudDialog.Close(DialogResult.Ok(true));
    private void Cancel() => MudDialog.Cancel();
}
```

**Usage**:
```razor
@inject IDialogService DialogService

<MudButton OnClick="DeleteEvent">Delete</MudButton>

@code {
    private async Task DeleteEvent()
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "Delete this event? This action cannot be undone.",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(
            "Confirm Delete",
            parameters);

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            var command = new DeleteEventCommand { Id = eventId };
            await Mediator.Send(command);
            Snackbar.Add("Event deleted", Severity.Success);
        }
    }
}
```

### Form Dialog

**CreateEventDialog.razor**:
```razor
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Create Event</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="Event Title"
                      Required="true" />

        <MudTextField @bind-Value="_dto.Description"
                      Label="Description"
                      Lines="3" />

        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       MinDate="DateTime.Today" />
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
        if (!_startDate.HasValue)
        {
            Snackbar.Add("Start date is required", Severity.Error);
            return;
        }

        _dto.StartDate = _startDate.Value;
        _isSubmitting = true;

        try
        {
            var command = new CreateEventCommand { EventDto = _dto };
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                Snackbar.Add("Event created", Severity.Success);
                MudDialog.Close(DialogResult.Ok(result.Id));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Snackbar.Add(error, Severity.Error);
                }
            }
        }
        finally
        {
            _isSubmitting = false;
        }
    }
}
```

**Opening the Dialog**:
```razor
@inject IDialogService DialogService

<MudButton OnClick="OpenCreateDialog">Create Event</MudButton>

@code {
    private async Task OpenCreateDialog()
    {
        var options = new DialogOptions
        {
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseOnEscapeKey = true
        };

        var dialog = await DialogService.ShowAsync<CreateEventDialog>(
            "Create Event",
            options);

        var result = await dialog.Result;

        if (!result.Canceled && result.Data is Guid eventId)
        {
            NavigationManager.NavigateTo($"/events/{eventId}");
        }
    }
}
```

---

## Table Patterns

### CRUD Table

```razor
@inject IMediator Mediator
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<MudTable Items="@_events" Hover="true" Loading="@_isLoading">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Events</MudText>
        <MudSpacer />
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="Create">
            Create Event
        </MudButton>
    </ToolBarContent>
    <HeaderContent>
        <MudTh>Title</MudTh>
        <MudTh>Date</MudTh>
        <MudTh>Location</MudTh>
        <MudTh>Status</MudTh>
        <MudTh Style="width: 120px">Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Title">@context.Title</MudTd>
        <MudTd DataLabel="Date">@context.StartDate.ToShortDateString()</MudTd>
        <MudTd DataLabel="Location">@context.Location</MudTd>
        <MudTd DataLabel="Status">
            <MudChip Size="Size.Small"
                     Color="@GetStatusColor(context.Status)">
                @context.Status
            </MudChip>
        </MudTd>
        <MudTd DataLabel="Actions">
            <MudIconButton Icon="@Icons.Material.Filled.Visibility"
                           Size="Size.Small"
                           OnClick="@(() => View(context.Id))" />
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Size="Size.Small"
                           Color="Color.Primary"
                           OnClick="@(() => Edit(context.Id))" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           Size="Size.Small"
                           Color="Color.Error"
                           OnClick="@(() => Delete(context.Id))" />
        </MudTd>
    </RowTemplate>
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
            var request = new GetEventListRequest();
            _events = await Mediator.Send(request);
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

    private async Task Create()
    {
        var dialog = await DialogService.ShowAsync<CreateEventDialog>("Create Event");
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await LoadEvents();
        }
    }

    private void View(Guid id) => NavigationManager.NavigateTo($"/events/{id}");

    private void Edit(Guid id) => NavigationManager.NavigateTo($"/events/{id}/edit");

    private async Task Delete(Guid id)
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "Delete this event?",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(
            "Confirm Delete",
            parameters);

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            var command = new DeleteEventCommand { Id = id };
            var deleteResult = await Mediator.Send(command);

            if (deleteResult.Success)
            {
                Snackbar.Add("Event deleted", Severity.Success);
                await LoadEvents();
            }
            else
            {
                Snackbar.Add("Failed to delete event", Severity.Error);
            }
        }
    }
}
```

---

## Navigation Patterns

### Programmatic Navigation

```razor
@inject NavigationManager NavigationManager

@code {
    private void NavigateToEvent(Guid eventId)
    {
        NavigationManager.NavigateTo($"/events/{eventId}");
    }

    private void NavigateToEvents()
    {
        NavigationManager.NavigateTo("/events");
    }

    private void NavigateExternal()
    {
        NavigationManager.NavigateTo("https://islamu.org", forceLoad: true);
    }
}
```

### Navigation with Query Parameters

```razor
@inject NavigationManager NavigationManager

@code {
    private void NavigateWithFilters()
    {
        var uri = NavigationManager.GetUriWithQueryParameters(new Dictionary<string, object?>
        {
            ["organizationId"] = organizationId,
            ["audienceAge"] = selectedAudienceAge,
            ["page"] = 1
        });

        NavigationManager.NavigateTo(uri);
    }

    private void ReadQueryParameters()
    {
        var uri = new Uri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("organizationId", out var orgId))
        {
            organizationId = Guid.Parse(orgId!);
        }
    }
}
```

---

## Loading State Patterns

### Simple Loading

```razor
@if (_isLoading)
{
    <MudProgressCircular Indeterminate="true" />
}
else if (_events.Any())
{
    <MudGrid>
        @foreach (var evt in _events)
        {
            <MudItem xs="12" md="6" lg="4">
                <EventCard Event="@evt" />
            </MudItem>
        }
    </MudGrid>
}
else
{
    <MudText>No events found</MudText>
}

@code {
    private List<EventListDto> _events = new();
    private bool _isLoading;
}
```

### Skeleton Loading

```razor
@if (_isLoading)
{
    <MudGrid>
        @for (int i = 0; i < 6; i++)
        {
            <MudItem xs="12" md="6" lg="4">
                <MudCard>
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="200px" />
                    <MudCardContent>
                        <MudSkeleton Width="60%" />
                        <MudSkeleton Width="40%" />
                    </MudCardContent>
                </MudCard>
            </MudItem>
        }
    </MudGrid>
}
else
{
    @* Actual content *@
}
```

### Progress Overlay

```razor
<MudOverlay Visible="_isLoading" DarkBackground="true">
    <MudProgressCircular Indeterminate="true" Size="Size.Large" />
</MudOverlay>

<MudContainer>
    @* Content *@
</MudContainer>

@code {
    private bool _isLoading;
}
```

---

## Error Handling Patterns

### Try-Catch with User Feedback

```razor
@inject ISnackbar Snackbar

@code {
    private async Task LoadEvents()
    {
        _isLoading = true;

        try
        {
            var request = new GetEventListRequest();
            _events = await Mediator.Send(request);
        }
        catch (HttpRequestException ex)
        {
            Snackbar.Add("Network error. Please check your connection.", Severity.Error);
            Console.WriteLine($"Error: {ex.Message}");
        }
        catch (Exception ex)
        {
            Snackbar.Add("An unexpected error occurred.", Severity.Error);
            Console.WriteLine($"Error: {ex.Message}");
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### Error Boundary

```razor
<ErrorBoundary>
    <ChildContent>
        <EventList />
    </ChildContent>
    <ErrorContent Context="exception">
        <MudAlert Severity="Severity.Error">
            <MudText Typo="Typo.h6">An error occurred</MudText>
            <MudText>@exception.Message</MudText>
            <MudButton OnClick="@(() => exception.Recover())">Retry</MudButton>
        </MudAlert>
    </ErrorContent>
</ErrorBoundary>
```

### Command Result Handling

```razor
@code {
    private async Task CreateEvent()
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
            // Display validation errors
            foreach (var error in result.Errors)
            {
                Snackbar.Add(error, Severity.Error);
            }
        }
    }
}
```

---

## Search and Filter Patterns

### Debounced Search

```razor
<MudTextField @bind-Value="_searchTerm"
              Label="Search Events"
              Immediate="true"
              DebounceInterval="300"
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
    private List<EventListDto> _events = new();
    private List<EventListDto> _filteredEvents = new();

    private async Task OnSearch()
    {
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

```razor
<MudGrid>
    <MudItem xs="12" md="3">
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.h6">Filters</MudText>

            <MudSelect @bind-Value="_filters.OrganizationId"
                       Label="Organization"
                       Clearable="true"
                       OnClearButtonClick="ClearOrganization">
                @foreach (var org in _organizations)
                {
                    <MudSelectItem Value="@org.Id">@org.Name</MudSelectItem>
                }
            </MudSelect>

            <MudSelect @bind-Value="_filters.AudienceAgeId"
                       Label="Audience Age"
                       Clearable="true">
                @foreach (var age in _audienceAges)
                {
                    <MudSelectItem Value="@age.Id">@age.Name</MudSelectItem>
                }
            </MudSelect>

            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       FullWidth="true"
                       OnClick="ApplyFilters">
                Apply Filters
            </MudButton>

            <MudButton Variant="Variant.Text"
                       FullWidth="true"
                       OnClick="ClearFilters">
                Clear All
            </MudButton>
        </MudPaper>
    </MudItem>

    <MudItem xs="12" md="9">
        <MudGrid>
            @foreach (var evt in _filteredEvents)
            {
                <MudItem xs="12" md="6" lg="4">
                    <EventCard Event="@evt" />
                </MudItem>
            }
        </MudGrid>
    </MudItem>
</MudGrid>

@code {
    private EventFilterDto _filters = new();
    private List<EventListDto> _filteredEvents = new();

    private async Task ApplyFilters()
    {
        var request = new GetEventListRequest
        {
            OrganizationId = _filters.OrganizationId,
            AudienceAgeId = _filters.AudienceAgeId
        };

        _filteredEvents = await Mediator.Send(request);
    }

    private async Task ClearFilters()
    {
        _filters = new EventFilterDto();
        await ApplyFilters();
    }

    private void ClearOrganization()
    {
        _filters.OrganizationId = null;
    }
}
```

---

## Infinite Scroll Pattern

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
        <div @ref="_loadMoreTrigger">
            <MudProgressCircular Indeterminate="true" />
        </div>
    }
</div>

@code {
    private ElementReference _scrollContainer;
    private ElementReference _loadMoreTrigger;
    private List<EventListDto> _events = new();
    private int _currentPage = 1;
    private bool _hasMore = true;
    private bool _isLoading;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("observeElement", _loadMoreTrigger,
                DotNetObjectReference.Create(this), nameof(LoadMore));
        }
    }

    [JSInvokable]
    public async Task LoadMore()
    {
        if (_isLoading || !_hasMore) return;

        _isLoading = true;
        _currentPage++;

        var request = new GetEventListRequest
        {
            Page = _currentPage,
            PageSize = 12
        };

        var result = await Mediator.Send(request);
        _events.AddRange(result.Events);
        _hasMore = result.HasMore;

        _isLoading = false;
        StateHasChanged();
    }
}
```

---

**Related Resources**:
- [component-structure.md](component-structure.md) - Component lifecycle, EventCallback
- [mudblazor-components.md](mudblazor-components.md) - MudBlazor component reference
- [state-management.md](state-management.md) - State sharing patterns
- [render-modes.md](render-modes.md) - InteractiveAuto, Server, WASM
