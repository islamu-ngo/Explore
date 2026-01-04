# MudBlazor Components

Comprehensive guide to commonly used MudBlazor components in ISLAMU Event.

---

## MudGrid & MudItem - Responsive Layouts

MudBlazor uses a 12-column grid system with responsive breakpoints.

### Breakpoints

| Breakpoint | Size | Screen Width |
|------------|------|--------------|
| `xs` | Extra small | 0-599px (mobile) |
| `sm` | Small | 600-959px (tablet portrait) |
| `md` | Medium | 960-1279px (tablet landscape) |
| `lg` | Large | 1280-1919px (desktop) |
| `xl` | Extra large | 1920px+ (large desktop) |

### Basic Grid

```razor
<MudGrid>
    <MudItem xs="12">
        <MudPaper Class="pa-4">Full width on all screens</MudPaper>
    </MudItem>

    <MudItem xs="12" sm="6" md="4">
        <MudPaper Class="pa-4">
            Full on mobile, half on tablet, 1/3 on desktop
        </MudPaper>
    </MudItem>

    <MudItem xs="12" sm="6" md="4">
        <MudPaper Class="pa-4">Column 2</MudPaper>
    </MudItem>

    <MudItem xs="12" sm="12" md="4">
        <MudPaper Class="pa-4">Column 3</MudPaper>
    </MudItem>
</MudGrid>
```

### Event Card Grid (ISLAMU Pattern)

```razor
<MudGrid Spacing="3">
    @foreach (var evt in _events)
    {
        <MudItem xs="12" sm="6" md="4" lg="3">
            <MudCard>
                <MudCardMedia Image="@evt.CoverImageUrl" Height="200" />
                <MudCardContent>
                    <MudText Typo="Typo.h6">@evt.Title</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">
                        @evt.StartDate.ToShortDateString()
                    </MudText>
                    <MudText Typo="Typo.body2">@evt.Location</MudText>
                </MudCardContent>
                <MudCardActions>
                    <MudButton Variant="Variant.Text"
                               Color="Color.Primary"
                               OnClick="@(() => ViewDetails(evt.Id))">
                        View Details
                    </MudButton>
                </MudCardActions>
            </MudCard>
        </MudItem>
    }
</MudGrid>
```

### Grid with Spacing

```razor
<MudGrid Spacing="4">  @* Spacing: 0-10 (0 = no spacing, 10 = maximum) *@
    <MudItem xs="6">
        <MudPaper Class="pa-4">Item 1</MudPaper>
    </MudItem>
    <MudItem xs="6">
        <MudPaper Class="pa-4">Item 2</MudPaper>
    </MudItem>
</MudGrid>
```

---

## MudButton - Buttons

MudButton supports three variants and multiple colors.

### Button Variants

```razor
@* Filled (Default - has elevation) *@
<MudButton Variant="Variant.Filled" Color="Color.Primary">
    Create Event
</MudButton>

@* Outlined (border, no fill) *@
<MudButton Variant="Variant.Outlined" Color="Color.Primary">
    Edit
</MudButton>

@* Text (no border, no fill) *@
<MudButton Variant="Variant.Text" Color="Color.Primary">
    Cancel
</MudButton>

@* Disabled *@
<MudButton Variant="Variant.Filled" Disabled="true">
    Disabled
</MudButton>
```

### Button Colors

```razor
<MudButton Color="Color.Default">Default</MudButton>
<MudButton Color="Color.Primary">Primary</MudButton>
<MudButton Color="Color.Secondary">Secondary</MudButton>
<MudButton Color="Color.Tertiary">Tertiary</MudButton>
<MudButton Color="Color.Info">Info</MudButton>
<MudButton Color="Color.Success">Success</MudButton>
<MudButton Color="Color.Warning">Warning</MudButton>
<MudButton Color="Color.Error">Error</MudButton>
```

### Button with Icon

```razor
@using MudBlazor

<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           StartIcon="@Icons.Material.Filled.Add"
           OnClick="CreateEvent">
    Create Event
</MudButton>

<MudButton Variant="Variant.Outlined"
           EndIcon="@Icons.Material.Filled.ArrowForward">
    Next
</MudButton>

@code {
    private void CreateEvent()
    {
        // Handle click
    }
}
```

### IconButton

```razor
<MudIconButton Icon="@Icons.Material.Filled.Edit"
               Color="Color.Primary"
               Size="Size.Small"
               OnClick="@(() => Edit(eventId))" />

<MudIconButton Icon="@Icons.Material.Filled.Delete"
               Color="Color.Error"
               Variant="Variant.Filled"
               Size="Size.Medium" />
```

### Button Sizes

```razor
<MudButton Size="Size.Small">Small</MudButton>
<MudButton Size="Size.Medium">Medium</MudButton>  @* Default *@
<MudButton Size="Size.Large">Large</MudButton>
```

---

## MudTextField - Text Input

### Basic TextField

```razor
<MudTextField @bind-Value="title"
              Label="Event Title"
              Variant="Variant.Outlined" />

@code {
    private string title = string.Empty;
}
```

### TextField Variants

```razor
<MudTextField @bind-Value="value" Label="Text" Variant="Variant.Text" />
<MudTextField @bind-Value="value" Label="Filled" Variant="Variant.Filled" />
<MudTextField @bind-Value="value" Label="Outlined" Variant="Variant.Outlined" />
```

### TextField with Validation

```razor
<MudTextField @bind-Value="email"
              Label="Email"
              Required="true"
              RequiredError="Email is required"
              Validation="@(new EmailAddressAttribute())" />

<MudTextField @bind-Value="description"
              Label="Description"
              Lines="5"
              MaxLength="500"
              Counter="500" />

@code {
    private string email = string.Empty;
    private string description = string.Empty;
}
```

### TextField with Adornments

```razor
<MudTextField @bind-Value="price"
              Label="Price"
              Adornment="Adornment.Start"
              AdornmentIcon="@Icons.Material.Filled.AttachMoney" />

<MudTextField @bind-Value="capacity"
              Label="Capacity"
              Adornment="Adornment.End"
              AdornmentText="people" />

<MudTextField @bind-Value="password"
              Label="Password"
              InputType="@_passwordInputType"
              Adornment="Adornment.End"
              AdornmentIcon="@_passwordIcon"
              OnAdornmentClick="TogglePasswordVisibility" />

@code {
    private decimal price;
    private int capacity;
    private string password = string.Empty;
    private InputType _passwordInputType = InputType.Password;
    private string _passwordIcon = Icons.Material.Filled.VisibilityOff;

    private void TogglePasswordVisibility()
    {
        if (_passwordInputType == InputType.Password)
        {
            _passwordInputType = InputType.Text;
            _passwordIcon = Icons.Material.Filled.Visibility;
        }
        else
        {
            _passwordInputType = InputType.Password;
            _passwordIcon = Icons.Material.Filled.VisibilityOff;
        }
    }
}
```

### TextField with Debounce

```razor
<MudTextField @bind-Value="searchTerm"
              Label="Search Events"
              Immediate="true"
              DebounceInterval="300"
              OnDebounceIntervalElapsed="OnSearchChanged"
              Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Filled.Search" />

@code {
    private string searchTerm = string.Empty;

    private async Task OnSearchChanged()
    {
        // Search executes 300ms after user stops typing
        await LoadEvents(searchTerm);
    }
}
```

---

## MudSelect - Dropdown Selection

### Basic Select

```razor
<MudSelect @bind-Value="selectedAudienceAge"
           Label="Audience Age"
           Required="true">
    @foreach (var age in _audienceAges)
    {
        <MudSelectItem Value="@age.Id">@age.Name</MudSelectItem>
    }
</MudSelect>

@code {
    private int selectedAudienceAge;
    private List<AudienceAgeDto> _audienceAges = new();
}
```

### Select with Object Binding

```razor
<MudSelect @bind-Value="selectedOrganization"
           Label="Organization"
           ToStringFunc="@(org => org?.Name ?? string.Empty)">
    @foreach (var org in _organizations)
    {
        <MudSelectItem Value="@org">@org.Name</MudSelectItem>
    }
</MudSelect>

@code {
    private OrganizationDto? selectedOrganization;
    private List<OrganizationDto> _organizations = new();
}
```

### MultiSelect

```razor
<MudSelect @bind-SelectedValues="selectedTags"
           Label="Tags"
           MultiSelection="true"
           ToStringFunc="@(tag => tag)">
    @foreach (var tag in _availableTags)
    {
        <MudSelectItem Value="@tag">@tag</MudSelectItem>
    }
</MudSelect>

@code {
    private IEnumerable<string> selectedTags = new List<string>();
    private List<string> _availableTags = new() { "Tafsir", "Fiqh", "Hadith" };
}
```

---

## MudDialog - Dialogs and Modals

### Inline Dialog

```razor
<MudDialog>
    <DialogContent>
        <MudText>Are you sure you want to delete this event?</MudText>
        <MudText Typo="Typo.body2" Color="Color.Error">
            This action cannot be undone.
        </MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Error"
                   Variant="Variant.Filled"
                   OnClick="Confirm">
            Delete
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!;

    private void Cancel() => MudDialog.Cancel();

    private void Confirm() => MudDialog.Close(DialogResult.Ok(true));
}
```

### Opening Dialog from Component

```razor
@inject IDialogService DialogService

<MudButton OnClick="OpenDeleteDialog">Delete Event</MudButton>

@code {
    private async Task OpenDeleteDialog()
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "Are you sure you want to delete this event?",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(
            "Confirm Delete",
            parameters,
            options);

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            // User confirmed
            await DeleteEvent();
        }
    }

    private async Task DeleteEvent()
    {
        // Delete logic
    }
}
```

### Form Dialog

```razor
@* CreateEventDialog.razor *@
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudDialog>
    <DialogContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="Event Title"
                      Required="true" />

        <MudTextField @bind-Value="_dto.Description"
                      Label="Description"
                      Lines="3" />

        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       Required="true" />
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary"
                   Variant="Variant.Filled"
                   OnClick="Submit">
            Create
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!;

    private CreateEventDto _dto = new();
    private DateTime? _startDate;

    private void Cancel() => MudDialog.Cancel();

    private async Task Submit()
    {
        if (_startDate.HasValue)
        {
            _dto.StartDate = _startDate.Value;
        }

        var command = new CreateEventCommand { EventDto = _dto };
        var result = await Mediator.Send(command);

        if (result.Success)
        {
            Snackbar.Add("Event created successfully", Severity.Success);
            MudDialog.Close(DialogResult.Ok(result.Id));
        }
        else
        {
            Snackbar.Add("Failed to create event", Severity.Error);
        }
    }
}
```

---

## MudTable - Data Tables

### Simple Table

```razor
<MudTable Items="@_events" Hover="true" Breakpoint="Breakpoint.Sm">
    <HeaderContent>
        <MudTh>Title</MudTh>
        <MudTh>Date</MudTh>
        <MudTh>Location</MudTh>
        <MudTh>Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Title">@context.Title</MudTd>
        <MudTd DataLabel="Date">@context.StartDate.ToShortDateString()</MudTd>
        <MudTd DataLabel="Location">@context.Location</MudTd>
        <MudTd DataLabel="Actions">
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Size="Size.Small"
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
}
```

### Table with Sorting

```razor
<MudTable Items="@_events"
          Hover="true"
          SortLabel="Sort By"
          Breakpoint="Breakpoint.Sm">
    <HeaderContent>
        <MudTh>
            <MudTableSortLabel SortBy="new Func<EventListDto, object>(x => x.Title)">
                Title
            </MudTableSortLabel>
        </MudTh>
        <MudTh>
            <MudTableSortLabel SortBy="new Func<EventListDto, object>(x => x.StartDate)">
                Date
            </MudTableSortLabel>
        </MudTh>
        <MudTh>Location</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Title">@context.Title</MudTd>
        <MudTd DataLabel="Date">@context.StartDate.ToShortDateString()</MudTd>
        <MudTd DataLabel="Location">@context.Location</MudTd>
    </RowTemplate>
</MudTable>
```

### Server-Side Table with Pagination

```razor
@inject IMediator Mediator

<MudTable ServerData="@LoadServerData"
          Dense="true"
          Hover="true"
          @ref="_table">
    <ToolBarContent>
        <MudText Typo="Typo.h6">Events</MudText>
        <MudSpacer />
        <MudTextField @bind-Value="_searchString"
                      Placeholder="Search"
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Icons.Material.Filled.Search"
                      IconSize="Size.Medium"
                      Class="mt-0"
                      Immediate="true"
                      DebounceInterval="300"
                      OnDebounceIntervalElapsed="OnSearch" />
    </ToolBarContent>
    <HeaderContent>
        <MudTh>
            <MudTableSortLabel SortLabel="title" T="EventListDto">
                Title
            </MudTableSortLabel>
        </MudTh>
        <MudTh>
            <MudTableSortLabel SortLabel="date" T="EventListDto">
                Date
            </MudTableSortLabel>
        </MudTh>
        <MudTh>Location</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Title">@context.Title</MudTd>
        <MudTd DataLabel="Date">@context.StartDate.ToShortDateString()</MudTd>
        <MudTd DataLabel="Location">@context.Location</MudTd>
    </RowTemplate>
    <NoRecordsContent>
        <MudText>No events found</MudText>
    </NoRecordsContent>
    <LoadingContent>
        <MudProgressCircular Indeterminate="true" />
    </LoadingContent>
    <PagerContent>
        <MudTablePager PageSizeOptions="new int[] { 10, 25, 50, 100 }" />
    </PagerContent>
</MudTable>

@code {
    private MudTable<EventListDto>? _table;
    private string _searchString = string.Empty;

    private async Task<TableData<EventListDto>> LoadServerData(
        TableState state,
        CancellationToken token)
    {
        // Build request with pagination
        var request = new GetEventListRequest
        {
            Page = state.Page + 1,  // MudTable is 0-indexed
            PageSize = state.PageSize,
            SearchTerm = _searchString,
            SortBy = state.SortLabel,
            SortDescending = state.SortDirection == SortDirection.Descending
        };

        var response = await Mediator.Send(request, token);

        return new TableData<EventListDto>
        {
            Items = response.Events,
            TotalItems = response.TotalCount
        };
    }

    private void OnSearch()
    {
        _table?.ReloadServerData();
    }
}
```

---

## MudCard - Cards

```razor
<MudCard>
    <MudCardMedia Image="@eventImageUrl" Height="200" />
    <MudCardContent>
        <MudText Typo="Typo.h5">@eventTitle</MudText>
        <MudText Typo="Typo.body2" Color="Color.Secondary">
            @eventDate.ToShortDateString()
        </MudText>
        <MudText Typo="Typo.body2" Class="mt-2">
            @eventDescription
        </MudText>
    </MudCardContent>
    <MudCardActions>
        <MudButton Variant="Variant.Text" Color="Color.Primary">
            Learn More
        </MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary">
            Register
        </MudButton>
    </MudCardActions>
</MudCard>
```

---

## MudSnackbar - Notifications

```razor
@inject ISnackbar Snackbar

<MudButton OnClick="ShowSuccess">Success</MudButton>
<MudButton OnClick="ShowError">Error</MudButton>
<MudButton OnClick="ShowWarning">Warning</MudButton>
<MudButton OnClick="ShowInfo">Info</MudButton>

@code {
    private void ShowSuccess()
    {
        Snackbar.Add("Event created successfully!", Severity.Success);
    }

    private void ShowError()
    {
        Snackbar.Add("Failed to delete event", Severity.Error);
    }

    private void ShowWarning()
    {
        Snackbar.Add("Event capacity almost full", Severity.Warning);
    }

    private void ShowInfo()
    {
        Snackbar.Add("Event updated", Severity.Info);
    }
}
```

**Configure in Program.cs**:
```csharp
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = true;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 4000;
});
```

---

## MudContainer - Layout Container

```razor
<MudContainer MaxWidth="MaxWidth.Large" Class="mt-4">
    <MudText Typo="Typo.h4">Events</MudText>
    @* Content *@
</MudContainer>
```

**MaxWidth Options**:
- `MaxWidth.ExtraSmall` - 600px
- `MaxWidth.Small` - 960px
- `MaxWidth.Medium` - 1280px
- `MaxWidth.Large` - 1920px
- `MaxWidth.ExtraLarge` - 2560px
- `MaxWidth.False` - No max width

---

## Best Practices

| Component | Best Practice |
|-----------|---------------|
| **MudGrid** | Use responsive breakpoints (`xs`, `sm`, `md`, `lg`) for all layouts |
| **MudButton** | Use `Variant.Filled` for primary actions, `Variant.Text` for secondary |
| **MudTextField** | Always include `Label`, use `Variant.Outlined` for forms |
| **MudSelect** | Use `ToStringFunc` for object binding |
| **MudDialog** | Use `IDialogService` for programmatic dialogs |
| **MudTable** | Use `ServerData` for pagination with large datasets |
| **MudSnackbar** | Use specific severity levels (Success, Error, Warning, Info) |

---

**Related Resources**:
- [component-structure.md](component-structure.md) - Blazor component lifecycle
- [state-management.md](state-management.md) - CascadingValue, services
- [common-patterns.md](common-patterns.md) - Forms, navigation patterns
