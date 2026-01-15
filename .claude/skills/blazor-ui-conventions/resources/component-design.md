# Component Design - Structure, Lifecycle, and Communication

This document outlines best practices for designing and structuring Blazor components, covering component anatomy, lifecycle, parameters, `EventCallback` for communication, and the code-behind pattern.

---

## 1. Blazor Component Anatomy

Blazor components are primarily defined in `.razor` files, which combine HTML markup with C# code. You can choose between a single-file approach or a code-behind approach.

### Single-File Component (.razor)

Convenient for simpler components or prototyping where C# logic is minimal.

```razor
@page "/events" @* Makes this component routable *@
@using MudBlazor @* Common using directives *@
@inject IMediator Mediator @* Inject dependencies *@
@inject ISnackbar Snackbar

<PageTitle>Events List</PageTitle> @* Sets browser tab title *@

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4">@Title</MudText> @* Access C# properties in markup *@

    @if (_isLoading)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        <MudButton OnClick="LoadEvents">Refresh Events</MudButton> @* Event handler *@
    }
</MudContainer>

@code { @* C# code block *@
    private string Title { get; set; } = "Event List";
    private bool _isLoading;

    protected override async Task OnInitializedAsync() @* Lifecycle method *@
    {
        _isLoading = true;
        await LoadEvents();
        _isLoading = false;
    }

    private async Task LoadEvents()
    {
        // Logic to load event data
        // _events = await Mediator.Send(new GetEventListRequest());
    }
}
```

### Code-Behind Pattern (.razor + .razor.cs)

Recommended for more complex components, pages, or when separating UI from logic improves readability and testability.

**`EventList.razor`**:
```razor
@page "/events"
@using MudBlazor
@inherits EventListBase @* Inherit from the code-behind class *@

<MudContainer MaxWidth="MaxWidth.Large">
    <MudText Typo="Typo.h4">@Title</MudText>
    <MudButton OnClick="LoadEvents">Refresh Events</MudButton>
</MudContainer>
```

**`EventList.razor.cs`**:
```csharp
using Microsoft.AspNetCore.Components; // Base component functionality
using MediatR; // Example dependency
using MudBlazor; // Example dependency

namespace Explore.Blazor.Components.Pages; // Namespace matching component location

// ✅ The code-behind class must inherit from ComponentBase or another base class
public partial class EventListBase : ComponentBase
{
    // ✅ Injected properties are available in markup and code-behind
    [Inject] protected IMediator Mediator { get; set; } = null!;
    [Inject] protected ISnackbar Snackbar { get; set; } = null!;

    // Protected properties are accessible from the .razor file
    protected string Title { get; set; } = "Event List";

    protected override async Task OnInitializedAsync()
    {
        // Initialization logic
        await LoadEvents();
    }

    protected async Task LoadEvents()
    {
        // Data loading logic
    }
}
```

### When to Use Each Pattern:

*   **Single-file**: Use for simple, self-contained UI elements, prototyping, or small components where the C# logic is minimal.
*   **Code-behind**: Prefer for complex components, pages, or any component where the C# logic is substantial and separating it from the UI markup enhances clarity, testability, or maintainability.

---

## 2. Component Lifecycle

Understanding the Blazor component lifecycle is crucial for performing operations at the correct time (e.g., data fetching, JavaScript interop, resource cleanup).

```mermaid
graph TD
    A[Constructor] --> B[SetParametersAsync]
    B --> C{firstRender?}
    C -- Yes --> D[OnInitialized / OnInitializedAsync]
    D --> E[OnParametersSet / OnParametersSetAsync]
    E --> F[BuildRenderTree]
    F --> G[OnAfterRender / OnAfterRenderAsync (firstRender: true)]
    G --> H{Parameter changed / StateHasChanged called?}
    H -- Yes --> E
    H -- No --> I[Waiting for events...]
    I --> H
    G --> J[OnAfterRender / OnAfterRenderAsync (firstRender: false)]
    J --> H
    H -- Component Removed --> K[Dispose]

    subgraph Notes
        D_Note[✅ Data fetching here]
        G_Note[✅ JS interop here]
        K_Note[✅ Resource cleanup]
    end

    D --- D_Note
    G --- G_Note
    K --- K_Note
```

### Key Lifecycle Methods and Their Use Cases:

#### `OnInitializedAsync` - Initial Data Loading

Use this method to fetch initial data for the component. It's called only **once** when the component is first initialized.

```csharp
@code {
    private List<EventListDto>? _events;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        try
        {
            var request = new GetEventListRequest();
            _events = await Mediator.Send(request); // Fetch data from API/service
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

#### `OnParametersSetAsync` - Reacting to Parameter Changes

This method is called every time parameters supplied by the parent component change, and also after `OnInitializedAsync` on the first render. Use it to react to new parameter values.

```csharp
@code {
    [Parameter]
    public Guid OrganizationId { get; set; }

    private List<EventListDto>? _events;
    private Guid _currentOrganizationId; // Track previous parameter value

    protected override async Task OnParametersSetAsync()
    {
        // ✅ Only reload data if OrganizationId has actually changed
        if (OrganizationId != _currentOrganizationId && OrganizationId != Guid.Empty)
        {
            _currentOrganizationId = OrganizationId; // Update tracking variable
            var request = new GetEventListRequest { OrganizationId = OrganizationId };
            _events = await Mediator.Send(request);
        }
    }
}
```

#### `OnAfterRenderAsync` - JavaScript Interop and DOM Access

Use this method for tasks that require the component's HTML to be rendered in the DOM, such as initializing JavaScript libraries or accessing DOM elements. It is called twice: once after the component's first render (`firstRender = true`), and then after every subsequent re-render (`firstRender = false`).

```csharp
@inject IJSRuntime JS

@code {
    private ElementReference _mapElement; // Reference to a DOM element

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ Initialize JavaScript libraries or interact with the DOM here
            await JS.InvokeVoidAsync("initializeMap", _mapElement);
        }
    }
}
```

#### `IDisposable` - Resource Cleanup

Implement `IDisposable` to release resources when the component is removed from the UI (e.g., unsubscribe from events, dispose timers).

```csharp
@implements IDisposable

@code {
    private System.Timers.Timer? _timer;

    protected override void OnInitialized()
    {
        _timer = new System.Timers.Timer(1000);
        _timer.Elapsed += (sender, e) => InvokeAsync(StateHasChanged); // Update UI on timer tick
        _timer.Start();
    }

    public void Dispose()
    {
        // ✅ Clean up resources to prevent memory leaks
        _timer?.Dispose();
        _timer = null;
    }
}
```

---

## 3. Parameters - Parent to Child Communication

Parameters are the primary mechanism for passing data from a parent component to a child component.

### Declaring Parameters

Use the `[Parameter]` attribute on public properties with `get; set;`.

```csharp
@code {
    [Parameter]
    public string Title { get; set; } = string.Empty; // ✅ Good practice to provide a default value

    [Parameter]
    public EventDto Event { get; set; } = null!; // ✅ Use null-forgiving operator for required complex objects

    [Parameter]
    public int? MaxItems { get; set; } // ✅ Use nullable types for optional parameters
}
```

### Parameter Validation

It's good practice to validate critical parameters in `OnParametersSet` or `OnParametersSetAsync`.

```csharp
@code {
    [Parameter]
    public Guid EventId { get; set; }

    protected override void OnParametersSet()
    {
        if (EventId == Guid.Empty)
        {
            throw new ArgumentException("EventId parameter cannot be an empty GUID.", nameof(EventId));
        }
    }
}
```

### Parameter Best Practices:

*   **✅ DO: Store Parameter in Private Field for Local Modification**: If a parameter needs to be modified locally within the child component, copy its value to a private field first.

    ```csharp
    @code {
        [Parameter]
        public bool Expanded { get; set; } // Parent controls initial state

        private bool _isExpanded; // Local state for internal use

        protected override void OnParametersSet()
        {
            _isExpanded = Expanded; // Copy parameter value to local field
        }

        private void Toggle()
        {
            _isExpanded = !_isExpanded; // Modify local state
            // If the parent needs to know about this change, use EventCallback
        }
    }
    ```

*   **❌ DON'T: Modify Parameter Directly**: Parameters are typically read-only from the child's perspective. Modifying them directly will often be overwritten on the next render cycle by the parent's value, or lead to unexpected behavior.

    ```csharp
    @code {
        [Parameter]
        public bool Expanded { get; set; }

        private void Toggle()
        {
            Expanded = !Expanded; // ❌ This change might be overwritten by the parent
        }
    }
    ```

---

## 4. EventCallback - Child to Parent Communication

`EventCallback<T>` is the standard way for child components to notify their parent components of events or changes.

### Basic EventCallback

**Parent Component (`EventList.razor`)**:
```razor
<EventCard Event="@selectedEvent" OnDelete="HandleDelete" />

@code {
    private EventDto? selectedEvent;

    private async Task HandleDelete(Guid eventId) // Method in parent to handle the event
    {
        // Perform deletion logic
        Snackbar.Add($"Event {eventId} deleted", Severity.Success);
    }
}
```

**Child Component (`EventCard.razor`)**:
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
    public EventCallback<Guid> OnDelete { get; set; } // EventCallback to notify parent

    private async Task DeleteClicked()
    {
        // ✅ Invoke the parent's registered method, passing the Event.Id
        await OnDelete.InvokeAsync(Event.Id);
    }
}
```

### EventCallback with Confirmation Dialog

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
            ["ContentText"] = $"Are you sure you want to delete '{Event.Title}'?",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirm Deletion", parameters);
        var result = await dialog.Result;

        if (!result.Canceled)
        {
            await OnDelete.InvokeAsync(Event.Id); // Only invoke if user confirmed
        }
    }
}
```

### Two-Way Binding Pattern (`@bind-`)

Blazor's `@bind-PropertyName` syntax is a shorthand for a parameter and an `EventCallback` pair.

**Parent Component**:
```razor
<SearchBox @bind-SearchTerm="searchTerm" /> @* Two-way binding *@

<MudText>Current Search Term: @searchTerm</MudText>

@code {
    private string searchTerm = string.Empty; // Parent's state
}
```

**Child Component (`SearchBox.razor`)**:
```razor
<MudTextField @bind-Value="_localSearchTerm"
              Label="Search"
              Immediate="true"
              DebounceInterval="300"
              OnDebounceIntervalElapsed="OnSearchChanged" />

@code {
    private string _localSearchTerm = string.Empty; // Child's local state

    [Parameter]
    public string SearchTerm { get; set; } = string.Empty; // Parent's parameter

    [Parameter]
    public EventCallback<string> SearchTermChanged { get; set; } // EventCallback for parent notification

    protected override void OnParametersSet()
    {
        // Initialize child's local state from parent's parameter
        if (_localSearchTerm != SearchTerm)
        {
            _localSearchTerm = SearchTerm;
        }
    }

    private async Task OnSearchChanged()
    {
        // ✅ Invoke the EventCallback to update the parent's SearchTerm property
        await SearchTermChanged.InvokeAsync(_localSearchTerm);
    }
}
```

**Convention for `@bind-PropertyName`**:
For Blazor to enable two-way binding using `@bind-PropertyName`, the child component must define:
*   A `[Parameter]` named `PropertyName` (e.g., `SearchTerm`).
*   An `[Parameter]` of type `EventCallback<T>` named `PropertyNameChanged` (e.g., `SearchTermChanged`).

---

## 5. Component Communication Patterns

Beyond direct parent-child communication, here are other common patterns:

### 5.1. Sibling Communication (Through Parent)

The parent component acts as an intermediary, managing state shared between siblings.

```razor
@* Parent Component *@
<EventList OnEventSelected="HandleEventSelected" /> @* Child 1 notifies parent *@
<EventDetails EventId="@_selectedEventId" /> @* Parent updates Child 2 *@

@code {
    private Guid _selectedEventId;

    private void HandleEventSelected(Guid eventId)
    {
        _selectedEventId = eventId; // Parent updates its state, which re-renders EventDetails
    }
}
```

### 5.2. Cascading Values (Deep Hierarchy)

Use `CascadingValue` and `[CascadingParameter]` to efficiently pass data down a deeply nested component tree without prop-drilling.

**`App.razor` (or a root layout)**:
```razor
<CascadingValue Value="@currentUser" Name="CurrentUser"> @* Name is important for multiple cascading values *@
    <CascadingValue Value="@_appTheme" Name="AppTheme">
        <Router AppAssembly="@typeof(App).Assembly" />
    </CascadingValue>
</CascadingValue>
```

**Consuming in a deeply nested child**:
```csharp
@code {
    [CascadingParameter(Name = "CurrentUser")]
    public UserDto? CurrentUser { get; set; }

    [CascadingParameter(Name = "AppTheme")]
    public MudTheme Theme { get; set; } = null!;

    protected override void OnInitialized()
    {
        if (CurrentUser != null) { /* Use current user data */ }
    }
}
```
*For more details, see [state-management.md](resources/state-management.md).*

### 5.3. Service-Based Communication

For more complex scenarios, especially when components are unrelated or when state needs to be globally accessible, use a shared service.

```csharp
// Shared service: EventStateService.cs
public class EventStateService
{
    public event Action<Guid>? OnEventSelected; // Event for subscribers

    public void SelectEvent(Guid eventId)
    {
        OnEventSelected?.Invoke(eventId); // Notify all subscribers
    }
}

// Component A (publishes event)
@inject EventStateService EventState

<MudButton OnClick="Select">Select Event</MudButton>

@code {
    private void Select()
    {
        EventState.SelectEvent(eventId);
    }
}

// Component B (subscribes to event)
@inject EventStateService EventState
@implements IDisposable

@code {
    protected override void OnInitialized()
    {
        EventState.OnEventSelected += HandleEventSelected; // Subscribe
    }

    private void HandleEventSelected(Guid eventId)
    {
        // React to selected event
        StateHasChanged(); // Force UI update if necessary
    }

    public void Dispose()
    {
        EventState.OnEventSelected -= HandleEventSelected; // ✅ Unsubscribe to prevent memory leaks
    }
}
```
*For more details, see [state-management.md](resources/state-management.md).*

---

## 6. `StateHasChanged()` - Manual Re-rendering

Blazor typically re-renders components automatically after event handlers, lifecycle methods, or when parameters change. However, sometimes you need to force a re-render manually.

### When to Call `StateHasChanged()`:

Call `StateHasChanged()` when the component's state changes **outside of Blazor's normal event handling flow**. This often happens with:
*   Asynchronous callbacks (e.g., from `Task.Run`, `Timer` events).
*   Interop calls from JavaScript that modify component state.
*   Callbacks from non-Blazor services or event aggregators.

```csharp
@code {
    private System.Timers.Timer? _timer;
    private int _counter;

    protected override void OnInitialized()
    {
        _timer = new System.Timers.Timer(1000);
        // The Elapsed event handler is not part of Blazor's event system
        _timer.Elapsed += (sender, e) => InvokeAsync(StateHasChanged); // ✅ Use InvokeAsync to marshal to UI thread
        _timer.Start();
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
```

### When NOT to Call `StateHasChanged()`:

Avoid calling `StateHasChanged()` unnecessarily, as it can impact performance. Blazor is smart enough to detect changes after most common interactions.

```csharp
@code {
    private List<EventDto>? _data;

    private async Task LoadData()
    {
        _data = await Http.GetFromJsonAsync<List<EventDto>>("api/v1/events");
        // ❌ StateHasChanged() NOT needed - Blazor will re-render after this async method completes
    }

    private void HandleClick()
    {
        _counter++;
        // ❌ StateHasChanged() NOT needed - UI updates automatically after event handler
    }
}
```

---

## Best Practices Summary

| Practice | Explanation |
|----------|-------------|
| ✅ **Code-Behind for Complexity** | Use `.razor.cs` files for components with significant C# logic. |
| ✅ **`OnInitializedAsync` for Data** | Fetch initial data here. It runs once. |
| ✅ **`OnParametersSetAsync` for Parameter Reactions** | Use to reload data or update state when parameters change. |
| ✅ **`OnAfterRenderAsync` for JS Interop/DOM** | Perform operations that require the DOM to be ready. |
| ✅ **`IDisposable` for Cleanup** | Release resources (timers, event subscriptions) to prevent memory leaks. |
| ✅ **Parameters for Parent → Child** | Always use `[Parameter]` for inputs. |
| ✅ **`EventCallback<T>` for Child → Parent** | Use for notifying parents of events. For two-way binding, use `PropertyNameChanged`. |
| ✅ **Store Parameters Locally for Edits** | Copy parameter values to private fields if they need to be modified in the child. |
| ✅ **Cascading Values for Deep State** | Avoid prop-drilling by using `CascadingValue` for application-wide or theme-related data. |
| ✅ **Scoped Services for Shared State** | Manage shared state between unrelated components within a user session. |
| ❌ **Don't Modify Parameters Directly** | Changes will likely be overwritten by the parent. |
| ❌ **Don't Overuse `StateHasChanged()`** | Only call when state changes outside Blazor's event flow. |
| ❌ **Don't Forget to Dispose** | Unsubscribe from events to prevent memory leaks in long-running applications. |
| ❌ **Don't Use Static State in Blazor Server** | Static fields can be shared across users, leading to data leakage. |

---

**Related Resources**:
- [mudblazor-usage.md](mudblazor-usage.md) - MudBlazor component specific implementation.
- [state-management.md](state-management.md) - Deeper dive into state management patterns.
- [common-patterns.md](common-patterns.md) - Examples of forms, dialogs, and tables.
