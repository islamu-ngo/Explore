# State Management - Blazor Application

This document provides a comprehensive overview of state management patterns and best practices for Blazor applications within the ISLAMU Event project. Effective state management is crucial for building responsive, scalable, and maintainable Blazor UIs.

---

## 1. State Management Approaches

Blazor offers several approaches to manage component state, each suited for different scenarios:

| Approach | Use Case | Scope | Mutability | Examples |
|----------|----------|-------|------------|----------|
| **Component State** | Simple, isolated data within a single component. | Single component instance | Mutable | Counter, form input values |
| **Parameters** | Passing data from a parent component to a child component. | Parent to immediate child | Read-only (child perspective) | Event data to a card component |
| **EventCallback** | Notifying a parent component of events or changes from a child. | Child to immediate parent | N/A (event notification) | Button click, form submission |
| **CascadingValue** | Sharing data down a deeply nested component hierarchy without prop-drilling. | Component tree | Read-only (typically) | Authentication state, theme settings |
| **Scoped Services** | Sharing mutable state across multiple, unrelated components within the same user session (Blazor Server) or application instance (Blazor WebAssembly). | User session / App instance | Mutable | Filter criteria, shopping cart |
| **Singleton Services** | Global, application-wide immutable state or configuration. Use sparingly for mutable state, and ensure thread safety. | Entire application | Mutable (carefully) / Read-only | Feature flags, application settings |

---

## 2. Component State

This is the most basic form of state management, where data is stored directly within the component's `@code` block.

```razor
@code {
    private List<EventListDto> _events = new(); // Private field for component's data
    private bool _isLoading = false;
    private string _searchTerm = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        // Fetch events
        _isLoading = false;
    }

    private void HandleSearch(string newSearchTerm)
    {
        _searchTerm = newSearchTerm;
        // Re-filter events
    }
}
```

**Best Practices**:
*   ✅ Use private fields (often prefixed with `_`) for internal component state.
*   ✅ Initialize collections (`new List<T>()`) to prevent null reference exceptions.
*   ✅ Use nullable types (`?`) for optional data.

---

## 3. Parameters - Parent to Child Communication

Parameters are used to pass data from a parent component to its direct child component. They are typically unidirectional.

**Parent Component Example**:
```razor
<EventCard Event="@selectedEvent"
           ShowDetails="true"
           OnEventSelected="HandleEventSelected" /> @* OnEventSelected is an EventCallback *@

@code {
    private EventDto? selectedEvent; // Data held by the parent

    private void HandleEventSelected(Guid eventId)
    {
        // Logic to update parent state based on child action
    }
}
```

**Child Component (`EventCard.razor`) Example**:
```razor
<MudCard>
    <MudCardContent>
        <MudText Typo="Typo.h6">@Event.Title</MudText>
        @if (ShowDetails)
        {
            <MudText Typo="Typo.body2">@Event.Description</MudText>
        }
    </MudCardContent>
</MudCard>

@code {
    [Parameter] // Marks this property as a parameter
    public EventDto Event { get; set; } = null!; // Parent passes Event object

    [Parameter]
    public bool ShowDetails { get; set; } // Parent passes a boolean flag
}
```
*For detailed usage and best practices for parameters, refer to [component-design.md](component-design.md).*

---

## 4. EventCallback - Child to Parent Communication

`EventCallback<T>` is the standard Blazor mechanism for a child component to notify its parent component about an event or a change, allowing the parent to react.

### Basic EventCallback

```razor
@* Child Component *@
<MudButton OnClick="NotifyParentSave">Save</MudButton>

@code {
    [Parameter]
    public EventCallback OnSave { get; set; } // No data passed

    private async Task NotifyParentSave()
    {
        await OnSave.InvokeAsync(); // Invokes the parent's registered method
    }
}
```

### EventCallback with Data

```razor
@* Child Component *@
@code {
    [Parameter]
    public EventCallback<Guid> OnEventDeleted { get; set; } // Pass a Guid (eventId) to parent

    private async Task DeleteEvent()
    {
        // ... deletion logic ...
        await OnEventDeleted.InvokeAsync(eventId); // Notify parent with the deleted event's ID
    }
}

@* Parent Component *@
<EventList OnEventDeleted="HandleEventDeleted" />

@code {
    private async Task HandleEventDeleted(Guid eventId)
    {
        Snackbar.Add($"Event {eventId} deleted successfully", Severity.Success);
        await LoadEvents(); // Parent reloads its list of events
    }
}
```
*For detailed usage and best practices for EventCallback, refer to [component-design.md](component-design.md).*

---

## 5. CascadingValue - Deep Hierarchy Data Sharing

`CascadingValue` is ideal for sharing data down a deeply nested component hierarchy without explicitly passing it through every intermediate component (prop-drilling). Common uses include authentication state, themes, or application-wide settings.

### Basic CascadingValue Example

**`App.razor` (or a root layout component)**:
```razor
<CascadingAuthenticationState> @* Provides AuthenticationStateProvider *@
    <CascadingValue Value="@_appTheme" Name="AppTheme"> @* Custom cascading value *@
        <Router AppAssembly="@typeof(App).Assembly">
            <Found Context="routeData">
                <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
            </Found>
            <NotFound>
                <LayoutView Layout="@typeof(MainLayout)">
                    <p role="alert">Sorry, there's nothing at this address.</p>
                </LayoutView>
            </NotFound>
        </Router>
    </CascadingValue>
</CascadingAuthenticationState>

@code {
    private MudTheme _appTheme = new MudTheme(); // Example theme object
}
```

**Consuming in a deeply nested child component**:
```csharp
@code {
    [CascadingParameter] // For AuthenticationState, no Name needed
    public Task<AuthenticationState> AuthenticationState { get; set; } = null!;

    [CascadingParameter(Name = "AppTheme")] // Use Name to specify which value to cascade
    public MudTheme Theme { get; set; } = null!;

    private bool _isAuthenticated;
    private string _userName = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthenticationState;
        _isAuthenticated = authState.User.Identity?.IsAuthenticated == true;
        _userName = authState.User.Identity?.Name ?? "Guest";
        // Use Theme object
    }
}
```

### ISLAMU Event Specific Pattern - Theme Management

The application's dark/light theme setting is cascaded from `App.razor` based on a cookie, then consumed in `MainLayout.razor`.

**`App.razor`**:
```razor
@inject IHttpContextAccessor HttpContextAccessor // Only available in Blazor Server

@code {
    private bool _isDarkTheme;

    protected override void OnInitialized()
    {
        // Read theme preference from cookie for initial render
        var themeCookie = HttpContextAccessor.HttpContext?.Request.Cookies["theme"];
        _isDarkTheme = themeCookie == "dark";
    }
}

<CascadingValue Value="_isDarkTheme" Name="InitialTheme">
    <Routes @rendermode="InteractiveAuto" /> @* Renders root routes *@
</CascadingValue>
```

**`MainLayout.razor`**:
```razor
@inherits LayoutComponentBase

@code {
    [CascadingParameter(Name = "InitialTheme")]
    public bool InitialIsDarkTheme { get; set; } // Consumes the cascaded value

    private MudTheme _currentTheme = new MudTheme();
    private bool _isDarkMode;

    protected override void OnInitialized()
    {
        _isDarkMode = InitialIsDarkTheme; // Initialize local state from cascaded value
        // Further theme customization if needed
    }
}

<MudThemeProvider @bind-IsDarkMode="_isDarkMode" Theme="_currentTheme" />
<MudDialogProvider />
<MudSnackbarProvider />

@body
```
*For more details on theming, refer to [theming.md](theming.md).*

---

## 6. Scoped Services - Shared State Across Components

Scoped services are excellent for managing shared, mutable state that needs to persist across multiple components within the same user session (Blazor Server) or application instance (Blazor WebAssembly).

### Create a State Service

```csharp
// Services/EventStateService.cs
using System;
using System.Collections.Generic;

public class EventStateService
{
    private Guid? _selectedEventId; // The shared state

    // Event to notify subscribers when state changes
    public event Action? OnChange;

    public Guid? SelectedEventId
    {
        get => _selectedEventId;
        set
        {
            if (_selectedEventId != value) // Only update if value changed
            {
                _selectedEventId = value;
                NotifyStateChanged(); // Notify subscribers
            }
        }
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

### Register the Service

Register scoped services in `Program.cs` for both `Explore.Blazor` (Server) and `Explore.Blazor.Client` (WebAssembly).

**`Program.cs` (Blazor Server or WASM)**:
```csharp
builder.Services.AddScoped<EventStateService>();
```

### Use the Service in Components

**Component A (Sets State)**:
```razor
@inject EventStateService EventState // Inject the service

<MudButton OnClick="SelectEvent">Select This Event</MudButton>

@code {
    [Parameter] public Guid EventId { get; set; }

    private void SelectEvent()
    {
        EventState.SelectedEventId = EventId; // Update the shared state
    }
}
```

**Component B (Reacts to State)**:
```razor
@inject EventStateService EventState
@implements IDisposable // Implement IDisposable for event cleanup

<MudText Typo="Typo.h6">Currently Selected Event: @EventState.SelectedEventId</MudText>

@code {
    protected override void OnInitialized()
    {
        EventState.OnChange += StateHasChanged; // Subscribe to state changes, force re-render
    }

    public void Dispose()
    {
        EventState.OnChange -= StateHasChanged; // ✅ Unsubscribe to prevent memory leaks
    }
}
```

### Advanced State Service with `IMediator`

Services can also encapsulate logic for fetching or updating state using `IMediator`.

```csharp
// Services/OrganizationStateService.cs
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Explore.Application.DTOs.Organization; // Assume this DTO exists
using Explore.Application.Features.Organizations.Requests.Queries; // Assume this query exists

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

    public IReadOnlyList<OrganizationDto> Organizations => _organizations.AsReadOnly(); // Read-only access

    public async Task LoadOrganizationsAsync()
    {
        if (_isLoaded) return; // Load only once per session/instance

        var request = new GetOrganizationListRequest();
        _organizations = await _mediator.Send(request);
        _isLoaded = true;
        NotifyStateChanged();
    }

    public async Task RefreshOrganizationsAsync()
    {
        _isLoaded = false; // Force reload
        await LoadOrganizationsAsync();
    }

    private void NotifyStateChanged() => OnChange?.Invoke();
}
```

---

## 7. Singleton Services - Global State

Singleton services persist for the entire application lifetime. They are best suited for truly global, immutable data (e.g., application configuration, feature flags). Avoid using them for mutable state unless carefully managed for thread safety, especially in Blazor Server.

```csharp
// Services/ApplicationConfigService.cs
public class ApplicationConfigService
{
    public string AppVersion { get; } = "1.0.0";
    public bool FeatureXEnabled { get; } = true;
    public DateTime AppStartUpTime { get; } = DateTime.UtcNow;
}
```

**Register**:
```csharp
builder.Services.AddSingleton<ApplicationConfigService>();
```

**Use**:
```razor
@inject ApplicationConfigService Config

<MudText>App Version: @Config.AppVersion</MudText>
```

**⚠️ Warning**: In Blazor Server, a singleton service is shared across *all users*. Any mutable state in a singleton will be visible and modifiable by every connected user, potentially leading to critical bugs and security vulnerabilities. Use `AddScoped` for per-user state.

---

## 8. State Management Patterns Summary

### Pattern 1: Parent Manages State

The parent component holds the shared state and passes it down to children via parameters. Children use `EventCallback` to inform the parent of changes.

```razor
@* Parent Component *@
<EventList Events="@_events" OnEventSelected="HandleSelection" />
<EventDetails Event="@_selectedEvent" />

@code {
    private List<EventListDto> _events = new();
    private EventDto? _selectedEvent;

    private async Task HandleSelection(Guid eventId)
    {
        // Parent fetches details and updates state, which re-renders EventDetails
        var request = new GetEventDetailRequest { Id = eventId };
        _selectedEvent = await Mediator.Send(request);
    }
}
```

### Pattern 2: Service Manages State (Recommended for Unrelated Components)

A scoped service holds the shared state, and components inject the service to read/update the state and subscribe to change notifications.

```csharp
// Component A (Publishes change)
@inject EventStateService State

<MudButton OnClick="Select">Select Event</MudButton>

@code {
    [Parameter] public Guid EventId { get; set; }
    private void Select() => State.SelectedEventId = EventId;
}

// Component B (Subscribes to change)
@inject EventStateService State
@implements IDisposable

<MudText>Currently Selected Event: @State.SelectedEventId</MudText>

@code {
    protected override void OnInitialized() => State.OnChange += StateHasChanged;
    public void Dispose() => State.OnChange -= StateHasChanged;
}
```

---

## 9. Authentication State

Blazor provides robust built-in mechanisms for managing user authentication and authorization state.

### Accessing Authentication State Declaratively (`AuthorizeView`)

```razor
<CascadingAuthenticationState>
    <AuthorizeView>
        <Authorized>
            <MudText>Welcome, @context.User.Identity?.Name!</MudText>
            <MudButton OnClick="CreateEvent">Create Event</MudButton>
        </Authorized>
        <NotAuthorized>
            <MudButton Href="/login">Login to Create Events</MudButton>
        </NotAuthorized>
        <Authorizing>
            <MudProgressCircular Indeterminate="true" Size="Size.Small" /> Loading authentication...
        </Authorizing>
    </AuthorizeView>
</CascadingAuthenticationState>
```

### Accessing Authentication State Programmatically

```razor
@inject AuthenticationStateProvider AuthStateProvider

@code {
    private UserDto? _currentUser;
    private bool _isAuthenticated;

    protected override async Task OnInitializedAsync()
    {
        var authState = await AuthStateProvider.GetAuthenticationStateAsync();
        var user = authState.User; // ClaimsPrincipal object

        _isAuthenticated = user.Identity?.IsAuthenticated == true;

        if (_isAuthenticated)
        {
            _currentUser = new UserDto
            {
                Id = Guid.TryParse(user.FindFirst("sub")?.Value, out var id) ? id : Guid.Empty,
                Email = user.FindFirst("email")?.Value ?? string.Empty,
                Name = user.Identity.Name ?? string.Empty,
                // Extract other claims as needed
            };
        }
    }
}
```

### Role-Based Authorization

```razor
<AuthorizeView Roles="Admin,Organizer"> @* Only visible to users with Admin OR Organizer role *@
    <Authorized>
        <MudButton>Delete Event</MudButton>
    </Authorized>
    <NotAuthorized>
        <MudText Color="Color.Error">Access Denied: Insufficient Permissions</MudText>
    </NotAuthorized>
</AuthorizeView>
```

---

**Related Resources**:
- [component-design.md](component-design.md) - Component lifecycle and parameter basics.
- [`blazor-bff-patterns`](../../blazor-bff-patterns/SKILL.md) - Authentication state serialization and token forwarding for hybrid apps.
- [`auth-patterns`](../../auth-patterns/SKILL.md) - General authentication and authorization rules.
