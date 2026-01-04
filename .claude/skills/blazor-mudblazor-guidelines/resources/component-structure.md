# Component Structure

## Blazor Component Anatomy

Blazor components use `.razor` files combining HTML markup with C# code.

### Single-File Component (.razor)

```razor
@page "/events"
@using MudBlazor
@inject IMediator Mediator
@inject ISnackbar Snackbar

<PageTitle>Events</PageTitle>

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4">@Title</MudText>

    @if (_isLoading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudButton OnClick="LoadEvents">Refresh</MudButton>
    }
</MudContainer>

@code {
    private string Title { get; set; } = "Event List";
    private bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        await LoadEvents();
        _isLoading = false;
    }

    private async Task LoadEvents()
    {
        // Load data
    }
}
```

### Code-Behind Pattern (.razor + .razor.cs)

**EventList.razor**:
```razor
@page "/events"
@using MudBlazor
@inherits EventListBase

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4">@Title</MudText>
    <MudButton OnClick="LoadEvents">Refresh</MudButton>
</MudContainer>
```

**EventList.razor.cs**:
```csharp
using Microsoft.AspNetCore.Components;
using MediatR;
using MudBlazor;

namespace Explore.Blazor.Components.Pages;

public partial class EventListBase : ComponentBase
{
    [Inject] protected IMediator Mediator { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    protected string Title { get; set; } = "Event List";

    protected override async Task OnInitializedAsync()
    {
        await LoadEvents();
    }

    protected async Task LoadEvents()
    {
        // Load data
    }
}
```

**When to use each**:
- **Single-file**: Simple components, prototyping, small pages
- **Code-behind**: Complex components, testable logic, large pages

---

## Component Lifecycle

```
┌─────────────────────────────────────────────────────────────────┐
│                    COMPONENT LIFECYCLE                          │
├─────────────────────────────────────────────────────────────────┤
│                                                                 │
│  Constructor                                                    │
│  ├─ Component instance created                                 │
│  └─ Dependency injection properties NOT yet available          │
│                                                                 │
│         ↓                                                       │
│                                                                 │
│  SetParametersAsync(ParameterView parameters)                  │
│  ├─ Parameters received from parent                            │
│  ├─ Injected properties NOW available                          │
│  └─ Can override to intercept parameter setting                │
│                                                                 │
│         ↓                                                       │
│                                                                 │
│  OnInitialized() / OnInitializedAsync()                        │
│  ├─ Component initialized (called ONCE)                        │
│  ├─ ✅ Load data here                                          │
│  ├─ ✅ Initialize state                                        │
│  └─ Parameters are available                                   │
│                                                                 │
│         ↓                                                       │
│                                                                 │
│  OnParametersSet() / OnParametersSetAsync()                    │
│  ├─ Called EVERY TIME parameters change                        │
│  ├─ ✅ React to parameter changes                              │
│  └─ ⚠️ Also called after OnInitialized (first time)            │
│                                                                 │
│         ↓                                                       │
│                                                                 │
│  BuildRenderTree()                                             │
│  ├─- Render markup to DOM                                      │
│  └─ (Internal, rarely overridden)                              │
│                                                                 │
│         ↓                                                       │
│                                                                 │
│  OnAfterRender(bool firstRender) / OnAfterRenderAsync(...)     │
│  ├─ Component rendered to DOM                                  │
│  ├─ ✅ JavaScript interop here                                 │
│  ├─ ✅ Access DOM elements                                     │
│  └─ ⚠️ Don't call StateHasChanged() here (infinite loop)       │
│                                                                 │
│         ↓                                                       │
│                                                                 │
│  [User interaction / Parameter change]                         │
│  ├─ StateHasChanged() called                                   │
│  └─ Re-render cycle begins                                     │
│                                                                 │
│         ↓                                                       │
│                                                                 │
│  Dispose()                                                     │
│  └─ Component removed from UI                                  │
│                                                                 │
└─────────────────────────────────────────────────────────────────┘
```

### Lifecycle Method Examples

#### OnInitializedAsync - Data Loading

```csharp
@code {
    private List<EventListDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        // ✅ Load data here (called ONCE)
        var request = new GetEventListRequest();
        _events = await Mediator.Send(request);
    }
}
```

#### OnParametersSetAsync - React to Changes

```csharp
@code {
    [Parameter]
    public Guid OrganizationId { get; set; }

    private List<EventListDto>? _events;

    protected override async Task OnParametersSetAsync()
    {
        // ✅ Reload when OrganizationId changes
        // ⚠️ This runs AFTER OnInitialized on first load
        if (OrganizationId != Guid.Empty)
        {
            var request = new GetEventListRequest { OrganizationId = OrganizationId };
            _events = await Mediator.Send(request);
        }
    }
}
```

#### OnAfterRenderAsync - JavaScript Interop

```csharp
@inject IJSRuntime JS

@code {
    private ElementReference _mapElement;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ Initialize JavaScript libraries
            await JS.InvokeVoidAsync("initializeMap", _mapElement);
        }
    }
}
```

#### IDisposable - Cleanup

```csharp
@implements IDisposable

@code {
    private Timer? _timer;

    protected override void OnInitialized()
    {
        _timer = new Timer(_ => StateHasChanged(), null, 1000, 1000);
    }

    public void Dispose()
    {
        // ✅ Clean up resources
        _timer?.Dispose();
    }
}
```

---

## Parameters

Parameters pass data from parent to child components.

### Basic Parameter

```csharp
@code {
    [Parameter]
    public string Title { get; set; } = string.Empty;  // ✅ Default value

    [Parameter]
    public EventDto Event { get; set; } = null!;  // ✅ Non-nullable

    [Parameter]
    public int? MaxItems { get; set; }  // ✅ Optional parameter
}
```

### Parameter Validation

```csharp
@code {
    [Parameter]
    public Guid EventId { get; set; }

    protected override void OnParametersSet()
    {
        // ✅ Validate required parameters
        if (EventId == Guid.Empty)
        {
            throw new ArgumentException("EventId is required", nameof(EventId));
        }
    }
}
```

### Parameter Best Practices

#### ✅ DO: Store Parameter in Private Field

```csharp
@code {
    [Parameter]
    public bool Expanded { get; set; }

    private bool _isExpanded;  // ✅ Private field

    protected override void OnParametersSet()
    {
        _isExpanded = Expanded;  // ✅ Copy to private field
    }

    private void Toggle()
    {
        _isExpanded = !_isExpanded;  // ✅ Modify private field
    }
}
```

#### ❌ DON'T: Modify Parameter Directly

```csharp
@code {
    [Parameter]
    public bool Expanded { get; set; }

    private void Toggle()
    {
        Expanded = !Expanded;  // ❌ DON'T modify parameter directly
        // Will be overwritten on next parent render!
    }
}
```

---

## EventCallback - Child to Parent Communication

`EventCallback<T>` enables child components to notify parents of events.

### Basic EventCallback

**Parent Component**:
```razor
<EventCard Event="@selectedEvent" OnDelete="HandleDelete" />

@code {
    private EventDto? selectedEvent;

    private async Task HandleDelete(Guid eventId)
    {
        // Handle delete
        Snackbar.Add("Event deleted", Severity.Success);
    }
}
```

**Child Component**:
```razor
<MudCard>
    <MudCardActions>
        <MudButton Color="Color.Error" OnClick="DeleteClicked">Delete</MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter]
    public EventDto Event { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    private async Task DeleteClicked()
    {
        // ✅ Invoke parent callback
        await OnDelete.InvokeAsync(Event.Id);
    }
}
```

### EventCallback with Confirmation

```csharp
@inject IDialogService DialogService

@code {
    [Parameter]
    public EventCallback<Guid> OnDelete { get; set; }

    [Parameter]
    public EventDto Event { get; set; } = null!;

    private async Task DeleteClicked()
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = $"Delete '{Event.Title}'?",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirm", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnDelete.InvokeAsync(Event.Id);
        }
    }
}
```

### Two-Way Binding Pattern

**Parent**:
```razor
<SearchBox @bind-SearchTerm="searchTerm" />

<MudText>Searching for: @searchTerm</MudText>

@code {
    private string searchTerm = string.Empty;
}
```

**Child (SearchBox.razor)**:
```razor
<MudTextField @bind-Value="_searchTerm"
              Label="Search"
              Immediate="true"
              DebounceInterval="300"
              OnDebounceIntervalElapsed="OnSearchChanged" />

@code {
    private string _searchTerm = string.Empty;

    [Parameter]
    public string SearchTerm { get; set; } = string.Empty;

    [Parameter]
    public EventCallback<string> SearchTermChanged { get; set; }

    protected override void OnParametersSet()
    {
        _searchTerm = SearchTerm;
    }

    private async Task OnSearchChanged()
    {
        // ✅ Notify parent of change (two-way binding)
        await SearchTermChanged.InvokeAsync(_searchTerm);
    }
}
```

**Convention**: For two-way binding with `@bind-PropertyName`, child must have:
- `[Parameter] public T PropertyName { get; set; }`
- `[Parameter] public EventCallback<T> PropertyNameChanged { get; set; }`

---

## Component Communication Patterns

### 1. Parent → Child (Parameters)

```razor
@* Parent *@
<EventCard Event="@selectedEvent" ShowDetails="true" />
```

### 2. Child → Parent (EventCallback)

```razor
@* Child *@
@code {
    [Parameter]
    public EventCallback<Guid> OnEdit { get; set; }

    private async Task Edit()
    {
        await OnEdit.InvokeAsync(Event.Id);
    }
}
```

### 3. Sibling Communication (Through Parent)

```razor
@* Parent coordinates siblings *@
<EventList OnEventSelected="HandleEventSelected" />
<EventDetails EventId="@selectedEventId" />

@code {
    private Guid selectedEventId;

    private void HandleEventSelected(Guid eventId)
    {
        selectedEventId = eventId;  // Updates EventDetails
    }
}
```

### 4. Cascading Values (Deep Hierarchy)

```razor
@* App.razor *@
<CascadingValue Value="@currentUser" Name="CurrentUser">
    <CascadingValue Value="@theme" Name="Theme">
        <Router />
    </CascadingValue>
</CascadingValue>

@code {
    private UserDto? currentUser;
    private MudTheme theme = new();
}
```

**Consuming in deep child**:
```csharp
@code {
    [CascadingParameter(Name = "CurrentUser")]
    public UserDto? CurrentUser { get; set; }

    [CascadingParameter(Name = "Theme")]
    public MudTheme Theme { get; set; } = null!;
}
```

### 5. Service-Based Communication

```csharp
// Shared service
public class EventStateService
{
    public event Action<Guid>? OnEventSelected;

    public void SelectEvent(Guid eventId)
    {
        OnEventSelected?.Invoke(eventId);
    }
}

// Component A
@inject EventStateService EventState

@code {
    private void Select(Guid id)
    {
        EventState.SelectEvent(id);
    }
}

// Component B
@inject EventStateService EventState

@code {
    protected override void OnInitialized()
    {
        EventState.OnEventSelected += HandleEventSelected;
    }

    private void HandleEventSelected(Guid eventId)
    {
        // React to event
    }

    public void Dispose()
    {
        EventState.OnEventSelected -= HandleEventSelected;
    }
}
```

---

## StateHasChanged - Manual Re-rendering

Call `StateHasChanged()` when state changes outside the normal Blazor event flow.

### When to Call StateHasChanged

```csharp
@code {
    private Timer? _timer;
    private int _counter;

    protected override void OnInitialized()
    {
        // ✅ Timer callback isn't a Blazor event
        _timer = new Timer(_ =>
        {
            _counter++;
            StateHasChanged();  // ✅ Required to update UI
        }, null, 1000, 1000);
    }
}
```

### When NOT to Call StateHasChanged

```csharp
@code {
    private async Task LoadData()
    {
        _data = await Http.GetFromJsonAsync<List<EventDto>>("api/v1/events");
        // ❌ StateHasChanged() NOT needed - Blazor auto-renders after event handlers
    }

    private void HandleClick()
    {
        _counter++;
        // ❌ StateHasChanged() NOT needed - UI updates automatically
    }
}
```

**Blazor auto-renders after**:
- Button clicks (`OnClick`)
- Form submissions
- `EventCallback` invocations
- Lifecycle methods

---

## Best Practices Summary

| Practice | Example |
|----------|---------|
| ✅ Load data in `OnInitializedAsync` | `_events = await Mediator.Send(request);` |
| ✅ React to parameters in `OnParametersSet` | Check if parameter changed, reload data |
| ✅ JavaScript interop in `OnAfterRenderAsync` | Initialize JS libraries after render |
| ✅ Store parameters in private fields | Avoid modifying `[Parameter]` directly |
| ✅ Use `EventCallback<T>` for child → parent | Type-safe event communication |
| ✅ Implement `IDisposable` for cleanup | Dispose timers, event subscriptions |
| ✅ Use two-way binding with convention | `PropertyName` + `PropertyNameChanged` |
| ❌ Don't modify parameters directly | Parent re-render will overwrite changes |
| ❌ Don't call `StateHasChanged()` in `OnAfterRender` | Causes infinite loop |
| ❌ Don't call `StateHasChanged()` after event handlers | Blazor does this automatically |

---

**Related Resources**:
- [mudblazor-components.md](mudblazor-components.md) - MudBlazor-specific components
- [state-management.md](state-management.md) - Advanced state patterns
- [render-modes.md](render-modes.md) - InteractiveAuto, Server, WebAssembly
