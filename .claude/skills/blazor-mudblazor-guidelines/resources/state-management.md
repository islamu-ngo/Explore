# State Management

State management patterns for Blazor applications in ISLAMU Event.

---

## State Management Approaches

| Approach | Use Case | Scope |
|----------|----------|-------|
| **Component State** | Simple, isolated component data | Single component |
| **Parameters** | Parent-child communication | Component hierarchy |
| **EventCallback** | Child-to-parent events | Component hierarchy |
| **CascadingValue** | Deep hierarchy data sharing | Component tree |
| **Scoped Services** | Shared state across components | Circuit (Server) / App (WASM) |
| **Singleton Services** | Global app state | Entire application |

---

## Component State

Local state stored in `@code` block.

```razor
@code {
    private List<EventListDto> _events = new();
    private bool _isLoading;
    private string _searchTerm = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        await LoadEvents();
        _isLoading = false;
    }

    private async Task LoadEvents()
    {
        // Load events
    }
}
```

**Best Practices**:
- ✅ Use private fields (prefix with `_`)
- ✅ Initialize collections to avoid null reference exceptions
- ✅ Use nullable types for optional data (`EventDto?`)

---

## Parameters - Parent to Child

Pass data down the component tree.

**Parent Component**:
```razor
<EventCard Event="@selectedEvent"
           ShowDetails="true"
           OnEventSelected="HandleEventSelected" />

@code {
    private EventDto? selectedEvent;

    private void HandleEventSelected(Guid eventId)
    {
        // Handle selection
    }
}
```

**Child Component (EventCard.razor)**:
```razor
<MudCard>
    <MudCardContent>
        <MudText>@Event.Title</MudText>

        @if (ShowDetails)
        {
            <MudText Typo="Typo.body2">@Event.Description</MudText>
        }
    </MudCardContent>
    <MudCardActions>
        <MudButton OnClick="SelectEvent">Select</MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter]
    public EventDto Event { get; set; } = null!;

    [Parameter]
    public bool ShowDetails { get; set; }

    [Parameter]
    public EventCallback<Guid> OnEventSelected { get; set; }

    private async Task SelectEvent()
    {
        await OnEventSelected.InvokeAsync(Event.Id);
    }
}
```

---

## EventCallback - Child to Parent

Enable child components to notify parents.

### Basic EventCallback

```razor
@* Child Component *@
<MudButton OnClick="NotifyParent">Save</MudButton>

@code {
    [Parameter]
    public EventCallback OnSave { get; set; }

    private async Task NotifyParent()
    {
        await OnSave.InvokeAsync();
    }
}
```

### EventCallback with Data

```razor
@* Child Component *@
@code {
    [Parameter]
    public EventCallback<Guid> OnEventDeleted { get; set; }

    private async Task Delete()
    {
        // Delete logic
        await OnEventDeleted.InvokeAsync(eventId);
    }
}

@* Parent Component *@
<EventList OnEventDeleted="HandleEventDeleted" />

@code {
    private async Task HandleEventDeleted(Guid eventId)
    {
        Snackbar.Add("Event deleted", Severity.Success);
        await LoadEvents();  // Reload list
    }
}
```

### Multiple EventCallbacks

```razor
@* Child Component *@
@code {
    [Parameter]
    public EventCallback<Guid> OnEdit { get; set; }

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    [Parameter]
    public EventCallback<Guid> OnView { get; set; }

    private async Task Edit() => await OnEdit.InvokeAsync(Event.Id);
    private async Task Delete() => await OnDelete.InvokeAsync(Event.Id);
    private async Task View() => await OnView.InvokeAsync(Event.Id);
}
```

---

## CascadingValue - Deep Hierarchy

Share data across multiple levels without passing through every component.

### Basic CascadingValue

**App.razor**:
```razor
@inject AuthenticationStateProvider AuthStateProvider

<CascadingAuthenticationState>
    <CascadingValue Value="@_currentUser" Name="CurrentUser">
        <CascadingValue Value="@_theme" Name="AppTheme">
            <Router AppAssembly="@typeof(App).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
                </Found>
            </Router>
        </CascadingValue>
    </CascadingValue>
</CascadingAuthenticationState>

@code {
    private UserDto? _currentUser;
    private MudTheme _theme = new MudTheme();

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            _currentUser = new UserDto
            {
                Id = Guid.Parse(user.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
                Email = user.FindFirst("email")?.Value ?? string.Empty,
                Name = user.Identity.Name ?? string.Empty
            };
        }
    }
}
```

**Deep Child Component**:
```razor
@code {
    [CascadingParameter(Name = "CurrentUser")]
    public UserDto? CurrentUser { get; set; }

    [CascadingParameter(Name = "AppTheme")]
    public MudTheme Theme { get; set; } = null!;

    protected override void OnInitialized()
    {
        if (CurrentUser != null)
        {
            // Use current user
        }
    }
}
```

### ISLAMU Event Pattern - Theme Cascading

**App.razor**:
```razor
@inject IHttpContextAccessor HttpContextAccessor

@code {
    var theme = HttpContextAccessor.HttpContext?.Request.Cookies["theme"];
    var isDark = theme == "dark";
}

<CascadingValue Value="isDark" Name="InitialTheme">
    <Routes @rendermode="InteractiveAuto" />
</CascadingValue>
```

**MainLayout.razor**:
```razor
@inherits LayoutComponentBase

@code {
    [CascadingParameter(Name = "InitialTheme")]
    public bool IsDarkMode { get; set; }

    private MudTheme _theme = new();
    private bool _isDarkMode;

    protected override void OnInitialized()
    {
        _isDarkMode = IsDarkMode;
    }
}

<MudThemeProvider @bind-IsDarkMode="_isDarkMode" Theme="_theme" />
<MudDialogProvider />
<MudSnackbarProvider />
```

### CascadingValue with Complex Object

```razor
@* Root Component *@
<CascadingValue Value="@_appState">
    <Router AppAssembly="@typeof(App).Assembly" />
</CascadingValue>

@code {
    private AppState _appState = new();

    public class AppState
    {
        public UserDto? CurrentUser { get; set; }
        public List<OrganizationDto> Organizations { get; set; } = new();
        public MudTheme Theme { get; set; } = new();
    }
}

@* Consumer Component *@
@code {
    [CascadingParameter]
    public AppState AppState { get; set; } = null!;

    protected override void OnInitialized()
    {
        var user = AppState.CurrentUser;
        var orgs = AppState.Organizations;
    }
}
```

---

## Scoped Services

Share state across components in the same circuit (Server) or app instance (WASM).

### Create State Service

```csharp
// Services/EventStateService.cs
public class EventStateService
{
    private Guid? _selectedEventId;

    // Event for notifying subscribers
    public event Action? OnChange;

    public Guid? SelectedEventId
    {
        get => _selectedEventId;
        set
        {
            if (_selectedEventId != value)
            {
                _selectedEventId = value;
                NotifyStateChanged();
            }
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

### Register Service

**Program.cs (Blazor Server)**:
```csharp
builder.Services.AddScoped<EventStateService>();
```

**Program.cs (Blazor WASM)**:
```csharp
builder.Services.AddScoped<EventStateService>();
```

### Use Service in Components

**Component A - Set State**:
```razor
@inject EventStateService EventState

<MudButton OnClick="SelectEvent">Select Event</MudButton>

@code {
    private void SelectEvent()
    {
        EventState.SelectedEventId = eventId;
    }
}
```

**Component B - React to State**:
```razor
@inject EventStateService EventState
@implements IDisposable

<MudText>Selected Event: @EventState.SelectedEventId</MudText>

@code {
    protected override void OnInitialized()
    {
        EventState.OnChange += StateHasChanged;
    }

    public void Dispose()
    {
        EventState.OnChange -= StateHasChanged;
    }
}
```

### Advanced State Service with MediatR

```csharp
// Services/OrganizationStateService.cs
public class OrganizationStateService
{
    private readonly IMediator _mediator;
    private List<OrganizationDto> _organizations = new();
    private bool _isLoaded;

    public OrganizationStateService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public event Action? OnChange;

    public IReadOnlyList<OrganizationDto> Organizations => _organizations.AsReadOnly();

    public async Task LoadOrganizationsAsync()
    {
        if (_isLoaded) return;

        var request = new GetOrganizationListRequest();
        _organizations = await _mediator.Send(request);
        _isLoaded = true;
        NotifyStateChanged();
    }

    public async Task RefreshAsync()
    {
        _isLoaded = false;
        await LoadOrganizationsAsync();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

**Usage**:
```razor
@inject OrganizationStateService OrgState
@implements IDisposable

@if (!_isLoaded)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    <MudSelect @bind-Value="selectedOrg">
        @foreach (var org in OrgState.Organizations)
        {
            <MudSelectItem Value="@org">@org.Name</MudSelectItem>
        }
    </MudSelect>
}

@code {
    private bool _isLoaded;
    private OrganizationDto? selectedOrg;

    protected override async Task OnInitializedAsync()
    {
        OrgState.OnChange += OnStateChanged;
        await OrgState.LoadOrganizationsAsync();
        _isLoaded = true;
    }

    private void OnStateChanged()
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        OrgState.OnChange -= OnStateChanged;
    }
}
```

---

## Singleton Services - Global State

Use for truly global, app-wide state (use sparingly).

```csharp
// Services/ApplicationStateService.cs
public class ApplicationStateService
{
    private string _applicationVersion = "1.0.0";

    public event Action? OnChange;

    public string ApplicationVersion
    {
        get => _applicationVersion;
        set
        {
            if (_applicationVersion != value)
            {
                _applicationVersion = value;
                NotifyStateChanged();
            }
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

**Register**:
```csharp
builder.Services.AddSingleton<ApplicationStateService>();
```

**⚠️ Warning**: Singleton services persist for the entire application lifetime. Use scoped services for user-specific data.

---

## State Management Patterns

### Pattern 1: Parent Manages State

Parent component holds state, passes to children via parameters.

```razor
@* Parent *@
<EventList Events="@_events" OnEventSelected="HandleSelection" />
<EventDetails Event="@_selectedEvent" />

@code {
    private List<EventListDto> _events = new();
    private EventDto? _selectedEvent;

    private async Task HandleSelection(Guid eventId)
    {
        var request = new GetEventDetailRequest { Id = eventId };
        _selectedEvent = await Mediator.Send(request);
    }
}
```

### Pattern 2: Service Manages State

Service holds state, components subscribe.

```razor
@* Component A *@
@inject EventStateService State

<MudButton OnClick="Select">Select</MudButton>

@code {
    private void Select() => State.SelectedEventId = eventId;
}

@* Component B *@
@inject EventStateService State
@implements IDisposable

<MudText>@State.SelectedEventId</MudText>

@code {
    protected override void OnInitialized() => State.OnChange += StateHasChanged;
    public void Dispose() => State.OnChange -= StateHasChanged;
}
```

### Pattern 3: Hybrid (Parent + Service)

Parent manages local state, service for cross-component communication.

```razor
@inject EventStateService GlobalState

<EventList Events="@_localEvents" />

@code {
    private List<EventListDto> _localEvents = new();  // Local state

    protected override async Task OnInitializedAsync()
    {
        GlobalState.OnChange += OnGlobalStateChanged;
        await LoadEvents();
    }

    private async Task OnGlobalStateChanged()
    {
        // Reload when global state changes
        await LoadEvents();
    }
}
```

---

## Authentication State

Blazor provides built-in authentication state management.

### Access Authentication State

```razor
<AuthorizeView>
    <Authorized>
        <MudText>Welcome, @context.User.Identity?.Name!</MudText>
        <MudButton OnClick="CreateEvent">Create Event</MudButton>
    </Authorized>
    <NotAuthorized>
        <MudButton Href="/login">Login</MudButton>
    </NotAuthorized>
</AuthorizeView>
```

### Access in Code

```razor
@inject AuthenticationStateProvider AuthStateProvider

@code {
    private UserDto? _currentUser;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity?.IsAuthenticated == true)
        {
            var userId = user.FindFirst("sub")?.Value;
            var email = user.FindFirst("email")?.Value;
            var name = user.Identity.Name;

            _currentUser = new UserDto
            {
                Id = Guid.Parse(userId ?? Guid.Empty.ToString()),
                Email = email ?? string.Empty,
                Name = name ?? string.Empty
            };
        }
    }
}
```

### Role-Based Authorization

```razor
<AuthorizeView Roles="Admin,Organizer">
    <Authorized>
        <MudButton>Delete Event</MudButton>
    </Authorized>
    <NotAuthorized>
        <MudText Color="Color.Error">Access Denied</MudText>
    </NotAuthorized>
</AuthorizeView>
```

---

## Best Practices

| Pattern | When to Use |
|---------|-------------|
| **Component State** | Data only needed in one component |
| **Parameters** | Parent-child data flow (1-2 levels) |
| **EventCallback** | Child needs to notify parent of events |
| **CascadingValue** | Data needed deep in component tree (3+ levels) |
| **Scoped Service** | Shared state across multiple unrelated components |
| **Singleton Service** | Global app configuration (read-only preferred) |

### ✅ DO

- ✅ Use component state for isolated data
- ✅ Use parameters for direct parent-child communication
- ✅ Use EventCallback for child-to-parent events
- ✅ Use CascadingValue for theme, user, or app-wide settings
- ✅ Use scoped services for shared mutable state
- ✅ Implement `IDisposable` when subscribing to events
- ✅ Unsubscribe from events in `Dispose()`

### ❌ DON'T

- ❌ Don't use singleton services for user-specific data
- ❌ Don't pass parameters through many levels (use CascadingValue)
- ❌ Don't forget to dispose event subscriptions (memory leaks)
- ❌ Don't modify `[Parameter]` properties directly
- ❌ Don't use static state (breaks isolation in Blazor Server)

---

## ISLAMU Event State Patterns

### User Profile State

```csharp
// Services/UserProfileStateService.cs
public class UserProfileStateService
{
    private readonly IMediator _mediator;
    private UserProfileDto? _profile;

    public UserProfileStateService(IMediator mediator)
    {
        _mediator = mediator;
    }

    public event Action? OnChange;

    public UserProfileDto? Profile => _profile;

    public async Task LoadProfileAsync(Guid userId)
    {
        var request = new GetUserProfileRequest { UserId = userId };
        _profile = await _mediator.Send(request);
        NotifyStateChanged();
    }

    public async Task UpdateProfileAsync(UpdateUserProfileDto dto)
    {
        var command = new UpdateUserProfileCommand { ProfileDto = dto };
        await _mediator.Send(command);
        await LoadProfileAsync(dto.UserId);
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

### Event Filter State

```csharp
// Services/EventFilterStateService.cs
public class EventFilterStateService
{
    private EventFilterDto _filters = new();

    public event Action? OnChange;

    public EventFilterDto Filters => _filters;

    public void UpdateFilter(Action<EventFilterDto> updateAction)
    {
        updateAction(_filters);
        NotifyStateChanged();
    }

    public void ClearFilters()
    {
        _filters = new EventFilterDto();
        NotifyStateChanged();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}

public class EventFilterDto
{
    public Guid? OrganizationId { get; set; }
    public int? AudienceAgeId { get; set; }
    public int? AudienceGenderId { get; set; }
    public string? SearchTerm { get; set; }
}
```

---

**Related Resources**:
- [component-structure.md](component-structure.md) - Component lifecycle, parameters
- [mudblazor-components.md](mudblazor-components.md) - MudBlazor components
- [common-patterns.md](common-patterns.md) - Real-world implementation patterns
