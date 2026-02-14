# Blazor Frontend Architecture

> **Project-Agnostic Blazor Hybrid UI Guide**
>
> Placeholders use `{Placeholder}` syntax - see [TEMPLATE_GLOSSARY.md](TEMPLATE_GLOSSARY.md).

**Last Updated**: January 2026

---

## Placeholder Substitutions

| Placeholder | Replace With | Example (ISLAMU Event) |
|-------------|--------------|------------------------|
| `{Project}` | Your solution name | `Explore` |
| `{Project}.Blazor` | Blazor Server (BFF) project | `Explore.Blazor` |
| `{Project}.Blazor.Client` | Blazor WASM project | `Explore.Blazor.Client` |
| `{Project}.API` | Backend API project | `Explore.API` |
| `{Entity}` | Main entity (singular) | `Event` |
| `{Entities}` | Main entity (plural) | `Events` |
| `{entity}` | camelCase entity | `event` |
| `{IdType}` | Primary key type | `Guid` |

---

## Table of Contents

1. [Overview](#1-overview)
2. [Project Structure](#2-project-structure)
3. [Render Modes](#3-render-modes)
4. [Service Layer Architecture](#4-service-layer-architecture)
5. [Component Patterns](#5-component-patterns)
6. [MudBlazor Usage](#6-mudblazor-usage)
7. [State Management](#7-state-management)
8. [Pagination Patterns](#8-pagination-patterns)
9. [Dialog Patterns](#9-dialog-patterns)
10. [Authentication & Authorization](#10-authentication--authorization)
11. [Theming](#11-theming)
12. [CSS & Styling Conventions](#12-css--styling-conventions)
13. [Error Handling](#13-error-handling)
14. [Best Practices](#14-best-practices)

---

## 1. Overview

A **Blazor Hybrid** architecture combines:

- **Blazor Server** (`{Project}.Blazor`): Acts as the Backend-for-Frontend (BFF)
- **Blazor WebAssembly** (`{Project}.Blazor.Client`): Contains UI components and pages

### Key Architecture Decisions

| Aspect | Decision | Rationale |
|--------|----------|-----------|
| Render Mode | `InteractiveAuto` | Fast initial load (server), then WASM for subsequent |
| UI Library | MudBlazor | Material Design, comprehensive components |
| API Communication | NSwag-generated client | Type-safe, auto-generated from OpenAPI |
| Authentication | BFF + Cookie | No tokens exposed to browser |
| Proxy | YARP | Token forwarding to backend API |

### Architecture Flow (Generic)

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Browser                                   │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │          {Project}.Blazor.Client (WASM/Server)                │  │
│  │  • Pages ({Entity}List, {Entity}Detail, etc.)                 │  │
│  │  • Components ({Entity}Manager, Dialogs, etc.)                │  │
│  │  • Services (I{Entity}Service, I{RelatedEntity}Service, etc.) │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              │                                      │
│                        Cookie Auth                                  │
└──────────────────────────────┼──────────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────────┐
│                    {Project}.Blazor (BFF Server)                    │
│  • OIDC Authentication with Identity Provider                       │
│  • Session Cookie Management                                        │
│  • YARP Reverse Proxy → API                                         │
│  • AccessTokenForwardingHandler                                     │
│  • X-Tenant-Id Header Injection                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                         JWT Bearer Token
                               │
┌──────────────────────────────▼──────────────────────────────────────┐
│                        {Project}.API                                │
│  • REST Endpoints                                                   │
│  • JWT Validation                                                   │
│  • Multi-Tenant Query Filters                                       │
└─────────────────────────────────────────────────────────────────────┘
```

### Implementation Example: ISLAMU Event

```
┌─────────────────────────────────────────────────────────────────────┐
│                           Browser                                   │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │              Explore.Blazor.Client (WASM/Server)              │  │
│  │  • Pages (EventList, EventDetail, etc.)                       │  │
│  │  • Components (EventSessionManager, Dialogs, etc.)            │  │
│  │  • Services (IEventService, IOrganizationService, etc.)       │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                              │                                      │
│                        Cookie Auth                                  │
└──────────────────────────────┼──────────────────────────────────────┘
                               │
┌──────────────────────────────▼──────────────────────────────────────┐
│                    Explore.Blazor (BFF Server)                      │
│  • OIDC Authentication with Keycloak                                │
│  • Session Cookie Management                                        │
│  • YARP Reverse Proxy → API                                         │
│  • AccessTokenForwardingHandler                                     │
│  • X-Tenant-Id Header Injection                                     │
└──────────────────────────────┬──────────────────────────────────────┘
                               │
                         JWT Bearer Token
                               │
┌──────────────────────────────▼──────────────────────────────────────┐
│                        Explore.API                                  │
│  • REST Endpoints                                                   │
│  • JWT Validation                                                   │
│  • Multi-Tenant Query Filters                                       │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Project Structure

### Generic Template

#### {Project}.Blazor (Server/BFF)

```
{Project}.Blazor/
├── Components/
│   ├── App.razor               # Root application component
│   ├── Routes.razor            # Routing configuration
│   └── _Imports.razor          # Global imports
├── Services/
│   ├── CircuitAccessTokenService.cs  # Token storage for SignalR
│   └── ServerCookieForwardingHandler.cs
├── Program.cs                  # DI, OIDC, YARP configuration
└── appsettings.json            # Identity provider, API URLs
```

#### {Project}.Blazor.Client (UI)

```
{Project}.Blazor.Client/
├── Pages/                      # Routable pages
│   ├── {Entity}/
│   │   ├── {Entity}List.razor     # {Entity} discovery with filters
│   │   ├── {Entity}Detail.razor   # Single {entity} view
│   │   ├── {Entity}Edit.razor     # {Entity} editing
│   │   ├── Create{Entity}.razor   # Multi-step {entity} creation
│   │   └── My{Entities}.razor     # User's {entities}
│   ├── {RelatedEntity}/
│   │   ├── Create{RelatedEntity}.razor
│   │   ├── {RelatedEntity}Profile.razor
│   │   └── My{RelatedEntities}.razor
│   ├── Admin/
│   │   ├── AdminList.razor        # Admin dashboard
│   │   ├── Categories.razor       # Category management
│   │   ├── Tags.razor             # Tag management
│   │   └── Locations.razor        # Location management
│   └── User/
│       ├── UserProfile.razor
│       ├── MyRegistrations.razor
│       └── Settings.razor
├── Components/                 # Reusable components
│   ├── {Entity}/
│   │   ├── {Entity}Manager.razor
│   │   ├── Create{ChildEntity}Dialog.razor
│   │   ├── Edit{ChildEntity}Dialog.razor
│   │   └── Delete{Entity}Dialog.razor
│   ├── Admin/
│   │   ├── CreateCategoryDialog.razor
│   │   └── EditCategoryDialog.razor
│   ├── ImageUpload.razor
│   ├── S3Image.razor
│   └── {Entity}Registration.razor
├── Layout/
│   ├── MainLayout.razor        # Application shell
│   ├── NavMenu.razor           # Navigation
│   ├── Footer.razor            # Footer
│   └── AnnouncementBar.razor   # Top announcement
├── Services/                   # Service layer
│   ├── {Entity}Service.cs
│   ├── {RelatedEntity}Service.cs
│   ├── CategoryService.cs
│   ├── AuthStateService.cs
│   └── [Other]Service.cs
├── Clients/
│   └── {Entity}ApiClient.g.cs     # NSwag-generated API client
├── Configuration/
│   └── TenantConfiguration.cs
└── _Imports.razor              # Global imports
```

### Implementation Example: ISLAMU Event

#### Explore.Blazor (Server/BFF)

```
Explore.Blazor/
├── Components/
│   ├── App.razor               # Root application component
│   ├── Routes.razor            # Routing configuration
│   └── _Imports.razor          # Global imports
├── Services/
│   ├── CircuitAccessTokenService.cs  # Token storage for SignalR
│   └── ServerCookieForwardingHandler.cs
├── Program.cs                  # DI, OIDC, YARP configuration
└── appsettings.json            # Keycloak, API URLs
```

#### Explore.Blazor.Client (UI)

```
Explore.Blazor.Client/
├── Pages/                      # Routable pages
│   ├── Event/
│   │   ├── EventList.razor     # Event discovery with filters
│   │   ├── EventDetail.razor   # Single event view
│   │   ├── EventEdit.razor     # Event editing
│   │   ├── CreateEvent.razor   # Multi-step event creation
│   │   └── MyEvents.razor      # User's events
│   ├── Organization/
│   │   ├── CreateOrganization.razor
│   │   ├── OrganizationProfile.razor
│   │   └── MyOrganizations.razor
│   ├── Admin/
│   │   ├── AdminList.razor     # Admin dashboard
│   │   ├── Categories.razor    # Category management
│   │   ├── Tags.razor          # Tag management
│   │   └── Locations.razor     # Location management
│   └── User/
│       ├── UserProfile.razor
│       ├── MyRegistrations.razor
│       └── Settings.razor
├── Components/                 # Reusable components
│   ├── Event/
│   │   ├── EventSessionManager.razor
│   │   ├── CreateSessionDialog.razor
│   │   ├── EditSessionDialog.razor
│   │   └── DeleteEventDialog.razor
│   ├── Admin/
│   │   ├── CreateCategoryDialog.razor
│   │   └── EditCategoryDialog.razor
│   ├── ImageUpload.razor
│   ├── S3Image.razor
│   └── EventRegistration.razor
├── Layout/
│   ├── MainLayout.razor        # Application shell
│   ├── NavMenu.razor           # Navigation
│   ├── Footer.razor            # Footer
│   └── AnnouncementBar.razor   # Top announcement
├── Services/                   # Service layer
│   ├── EventService.cs
│   ├── OrganizationService.cs
│   ├── CategoryService.cs
│   ├── AuthStateService.cs
│   └── [Entity]Service.cs
├── Clients/
│   └── EventApiClient.g.cs     # NSwag-generated API client
├── Configuration/
│   └── TenantConfiguration.cs
└── _Imports.razor              # Global imports
```

---

## 3. Render Modes

### Policy: InteractiveAuto First

The application strictly adheres to the **`InteractiveAuto`** render mode policy for all standard UI pages.

**Configuration**:
```csharp
// Program.cs
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Explore.Blazor.Client._Imports).Assembly);
```

**Why InteractiveAuto?**
1.  **Instant Load**: First visit renders HTML immediately on the server (SSR).
2.  **Seamless Transition**: The browser connects via SignalR (InteractiveServer) while the WASM bundle downloads in the background.
3.  **Offline Capability**: Subsequent visits (or once loaded) switch to WebAssembly for client-side interactivity, reducing server load.

### Decision Matrix

| Scenario | Render Mode | Reason |
| :--- | :--- | :--- |
| **Standard Pages** (Events, Profile) | `InteractiveAuto` | Best balance of speed and interactivity. |
| **Admin Dashboards** | `InteractiveAuto` | Consistent UX; admin users benefit from WASM performance too. |
| **Static Content** (About, Terms) | `SSR` (Static) | No interactivity needed; fastest render. |
| **Real-time / Heavy Compute** | `InteractiveServer` | Only if WASM performance is insufficient (Exception case). |

### Implementation

**Page-Level (Preferred)**:
```razor
@page "/events"
@rendermode InteractiveAuto
```

**Component-Level (Avoid if possible)**:
Inherits the page's render mode automatically. Only specify if a specific component needs a *different* mode than its parent (rare).

---

## 4. Service Layer Architecture

### Interface-Based Design

All services implement an interface for testability.

**Generic Template:**

```csharp
// Interface definition
public interface I{Entity}Service
{
    Task<ICollection<{Entity}ListDto>> GetAll{Entities}Async();
    Task<{Entity}Dto?> Get{Entity}ByIdAsync({IdType} {entity}Id);
    Task<BaseCommandResponse<{IdType}>?> Create{Entity}Async(Create{Entity}Dto dto);
    Task<bool> Delete{Entity}Async({IdType} {entity}Id);
}

// Implementation
public class {Entity}Service : I{Entity}Service
{
    private readonly I{Entity}ApiClient _apiClient;
    private readonly ILogger<{Entity}Service> _logger;

    public {Entity}Service(I{Entity}ApiClient apiClient, ILogger<{Entity}Service> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<{Entity}ListDto>> GetAll{Entities}Async()
    {
        try
        {
            _logger.LogInformation("[{ENTITY} SERVICE] Fetching all {entities}...");
            var response = await _apiClient.{Entity}GETAsync(pageNumber: 1, pageSize: 100);
            return response?.Items ?? new List<{Entity}ListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[{ENTITY} SERVICE] API error: {StatusCode}", ex.StatusCode);
            return new List<{Entity}ListDto>();
        }
    }
}
```

**Implementation Example: ISLAMU Event**

```csharp
// Interface definition
public interface IEventService
{
    Task<ICollection<EventListDto>> GetAllEventsAsync();
    Task<EventDto?> GetEventByIdAsync(Guid eventId);
    Task<BaseCommandResponseOfGuid?> CreateEventAsync(CreateEventDto dto);
    Task<bool> DeleteEventAsync(Guid eventId);
}

// Implementation
public class EventService : IEventService
{
    private readonly IEventApiClient _apiClient;
    private readonly ILogger<EventService> _logger;

    public EventService(IEventApiClient apiClient, ILogger<EventService> logger)
    {
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ICollection<EventListDto>> GetAllEventsAsync()
    {
        try
        {
            _logger.LogInformation("[EVENT SERVICE] Fetching all events...");
            var response = await _apiClient.EventGETAsync(pageNumber: 1, pageSize: 100);
            return response?.Items ?? new List<EventListDto>();
        }
        catch (ApiException ex)
        {
            _logger.LogError(ex, "[EVENT SERVICE] API error: {StatusCode}", ex.StatusCode);
            return new List<EventListDto>();
        }
    }
}
```

### Service Registration

Services are registered in `Program.cs`.

**Generic Template:**

```csharp
// {Project}.Blazor/Program.cs
builder.Services.AddScoped<I{Entity}Service, {Entity}Service>();
builder.Services.AddScoped<I{RelatedEntity}Service, {RelatedEntity}Service>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthStateService, AuthStateService>();
// ... etc
```

**Implementation Example: ISLAMU Event**

```csharp
// Explore.Blazor/Program.cs
builder.Services.AddScoped<IEventService, EventService>();
builder.Services.AddScoped<IOrganizationService, OrganizationService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuthStateService, AuthStateService>();
// ... etc
```

### NSwag API Client

The API client is auto-generated from OpenAPI spec.

**Generic Template:**

```csharp
// Configured in Program.cs
builder.Services.AddHttpClient<I{Entity}ApiClient, {Entity}ApiClient>(client =>
    {
        client.BaseAddress = new Uri({project}ApiBaseUrl);
    })
    .AddHttpMessageHandler<AccessTokenForwardingHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Dev SSL handling if needed
    });
```

**Implementation Example: ISLAMU Event**

```csharp
// Configured in Program.cs
builder.Services.AddHttpClient<IEventApiClient, EventApiClient>(client =>
    {
        client.BaseAddress = new Uri(exploreApiBaseUrl);
    })
    .AddHttpMessageHandler<AccessTokenForwardingHandler>()
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Dev SSL handling if needed
    });
```

---

## 5. Component Patterns

### Page Components

Page components are routable and typically:
1. Load data in `OnInitializedAsync`
2. Manage local state
3. Delegate to services for API calls

**Generic Template:**

```razor
@page "/{entities}"
@inject I{Entity}Service {Entity}Service
@inject ILogger<{Entity}List> Logger

<PageTitle>{Entities}</PageTitle>

@if (_isLoading)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    @foreach (var item in _{entities})
    {
        <{Entity}Card {Entity}="@item" />
    }
}

@code {
    private bool _isLoading = true;
    private ICollection<{Entity}ListDto> _{entities} = new List<{Entity}ListDto>();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _{entities} = await {Entity}Service.GetAll{Entities}Async();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading {entities}");
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

**Implementation Example: ISLAMU Event**

```razor
@page "/events"
@inject IEventService EventService
@inject ILogger<EventList> Logger

<PageTitle>Explore Events</PageTitle>

@if (_isLoading)
{
    <MudProgressCircular Indeterminate="true" />
}
else
{
    @foreach (var evt in _events)
    {
        <EventCard Event="@evt" />
    }
}

@code {
    private bool _isLoading = true;
    private ICollection<EventListDto> _events = new List<EventListDto>();

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _events = await EventService.GetAllEventsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Error loading events");
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### Reusable Components

Components receive data via parameters and emit events.

**Generic Template:**

```razor
@* {Entity}Card.razor *@
<MudCard Class="{entity}-card">
    <MudCardContent>
        <MudText Typo="Typo.h6">@{Entity}.Title</MudText>
    </MudCardContent>
    <MudCardActions>
        <MudButton OnClick="@(() => OnViewDetails.InvokeAsync({Entity}.Id))">
            View Details
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter, EditorRequired]
    public {Entity}ListDto {Entity} { get; set; } = null!;

    [Parameter]
    public EventCallback<{IdType}> OnViewDetails { get; set; }
}
```

**Implementation Example: ISLAMU Event**

```razor
@* EventCard.razor *@
<MudCard Class="event-card">
    <MudCardContent>
        <MudText Typo="Typo.h6">@Event.Title</MudText>
    </MudCardContent>
    <MudCardActions>
        <MudButton OnClick="@(() => OnViewDetails.InvokeAsync(Event.Id))">
            View Details
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    [Parameter, EditorRequired]
    public EventListDto Event { get; set; } = null!;

    [Parameter]
    public EventCallback<Guid> OnViewDetails { get; set; }
}
```

### Parameter Conventions

| Convention | Example |
|------------|---------|
| Required parameters | `[Parameter, EditorRequired]` |
| Nullable parameters | `[Parameter] public Guid? ParentId { get; set; }` |
| Event callbacks | `[Parameter] public EventCallback<T> OnChanged { get; set; }` |
| Cascading values | `[CascadingParameter] public Task<AuthenticationState> AuthState { get; set; }` |

---

## 6. MudBlazor Usage

### Required Providers

In `MainLayout.razor`:

```razor
<MudThemeProvider Theme="@_theme" IsDarkMode="@_isDarkMode" />
<MudPopoverProvider />
<MudDialogProvider />
<MudSnackbarProvider />
```

### Common Components

**Data Display**:
```razor
@* DataGrid with CRUD *@
<MudDataGrid T="EventSessionListDto" Items="@sessions"
             ReadOnly="false" EditMode="DataGridEditMode.Cell"
             Bordered="true" Dense="true" Hover="true">
    <Columns>
        <PropertyColumn Property="x => x.Title" />
        <PropertyColumn Property="x => x.StartTime" Format="s" />
        <TemplateColumn StickyRight="true">
            <CellTemplate>
                <MudIconButton Icon="@Icons.Material.Filled.Edit"
                               OnClick="@(() => Edit(context.Item))" />
            </CellTemplate>
        </TemplateColumn>
    </Columns>
</MudDataGrid>
```

**Forms**:
```razor
<MudForm @ref="_form" @bind-IsValid="@_isValid">
    <MudTextField @bind-Value="model.Title"
                  Label="Title"
                  Required="true"
                  RequiredError="Title is required" />

    <MudSelect @bind-Value="model.CategoryId"
               Label="Category"
               AnchorOrigin="Origin.BottomCenter">
        @foreach (var cat in categories)
        {
            <MudSelectItem Value="@cat.Id">@cat.FullName</MudSelectItem>
        }
    </MudSelect>

    <MudDatePicker @bind-Date="model.StartDate"
                   Label="Start Date" />
</MudForm>
```

**Feedback**:
```razor
@inject ISnackbar Snackbar

@code {
    private async Task SaveAsync()
    {
        var result = await Service.CreateAsync(model);
        if (result?.Success == true)
        {
            Snackbar.Add("Created successfully!", Severity.Success);
        }
        else
        {
            Snackbar.Add(result?.Message ?? "Error", Severity.Error);
        }
    }
}
```

---

## 7. State Management

### 7.1. URL-Based State (The Source of Truth)

For list views, filters, and pagination, the **URL is the single source of truth**. This ensures deep-linking works and state survives refreshes.

**Pattern**: Use `[SupplyParameterFromQuery]`.

```csharp
@page "/events"

@code {
    [SupplyParameterFromQuery(Name = "q")]
    public string? SearchTerm { get; set; }

    [SupplyParameterFromQuery]
    public int Page { get; set; } = 1;

    protected override async Task OnInitializedAsync()
    {
        // Load data based on URL parameters
        await LoadDataAsync();
    }

    private void OnSearch(string term)
    {
        // Update URL, triggering navigation and re-initialization
        Navigation.NavigateTo(
            Navigation.GetUriWithQueryParameters(new Dictionary<string, object?>
            {
                ["q"] = term,
                ["Page"] = 1 // Reset page on filter change
            }));
    }
}
```

### 7.2. Cascading Values (Global Context)

Use `CascadingValue` for read-only global context like Tenant, Theme, or User Identity.

```razor
@* MainLayout.razor *@
<CascadingValue Value="@_tenantContext" Name="TenantContext" IsFixed="true">
    @Body
</CascadingValue>
```

### 7.3. Service State (Caching)

Use Scoped Services to cache data that doesn't change often but is needed across components (e.g., User Profile, Lookup Data).

**Do NOT** use static fields for state (creates bugs in Server mode).

### 7.4. AuthStateService

Centralized authentication state:

```csharp
public interface IAuthStateService
{
    Task<string> GetCurrentUserIdAsync();
    Task<Guid> GetCurrentTenantIdAsync();
    Task<bool> IsAuthenticatedAsync();
}
```

---

## 8. Pagination Patterns

### Client-Side Pagination

For smaller datasets (< 1000 items), load all and paginate client-side.

**Generic Template:**

```razor
<MudPagination Count="@TotalPages"
               Selected="@_currentPage"
               SelectedChanged="@OnPageChanged"
               Color="Color.Primary"
               ShowFirstButton="true"
               ShowLastButton="true" />

<MudText Typo="Typo.body2">
    Showing @((_currentPage - 1) * _pageSize + 1) -
    @(Math.Min(_currentPage * _pageSize, _allItems.Count))
    of @_allItems.Count items
</MudText>

@code {
    private int _currentPage = 1;
    private int _pageSize = 10;
    private List<{Entity}ListDto> _allItems = new();

    private List<{Entity}ListDto> PagedItems => _allItems
        .Skip((_currentPage - 1) * _pageSize)
        .Take(_pageSize)
        .ToList();

    private int TotalPages => _allItems.Count > 0
        ? (int)Math.Ceiling((double)_allItems.Count / _pageSize)
        : 1;

    private void OnPageChanged(int page)
    {
        _currentPage = page;
    }
}
```

**Implementation Example: ISLAMU Event**

```razor
@code {
    private int _currentPage = 1;
    private int _pageSize = 10;
    private List<EventListDto> _allItems = new();

    private List<EventListDto> PagedItems => _allItems
        .Skip((_currentPage - 1) * _pageSize)
        .Take(_pageSize)
        .ToList();

    private int TotalPages => _allItems.Count > 0
        ? (int)Math.Ceiling((double)_allItems.Count / _pageSize)
        : 1;

    private void OnPageChanged(int page)
    {
        _currentPage = page;
    }
}
```

### Server-Side Pagination

For large datasets, use API pagination.

**Generic Template:**

```csharp
// Service method
public async Task<PagedResult<{Entity}ListDto>> Get{Entities}PagedAsync(int page, int pageSize)
{
    var response = await _apiClient.{Entity}GETAsync(pageNumber: page, pageSize: pageSize);
    return new PagedResult<{Entity}ListDto>
    {
        Items = response?.Items?.ToList() ?? new List<{Entity}ListDto>(),
        TotalCount = response?.TotalCount ?? 0,
        PageNumber = page,
        PageSize = pageSize
    };
}
```

```razor
@code {
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalCount = 0;
    private List<{Entity}ListDto> _items = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadPageAsync(_currentPage);
    }

    private async Task LoadPageAsync(int page)
    {
        var result = await {Entity}Service.Get{Entities}PagedAsync(page, _pageSize);
        _items = result.Items;
        _totalCount = result.TotalCount;
        _currentPage = page;
    }

    private int TotalPages => _totalCount > 0
        ? (int)Math.Ceiling((double)_totalCount / _pageSize)
        : 1;

    private async Task OnPageChanged(int page)
    {
        await LoadPageAsync(page);
    }
}
```

**Implementation Example: ISLAMU Event**

```csharp
// Service method
public async Task<PagedResult<EventListDto>> GetEventsPagedAsync(int page, int pageSize)
{
    var response = await _apiClient.EventGETAsync(pageNumber: page, pageSize: pageSize);
    return new PagedResult<EventListDto>
    {
        Items = response?.Items?.ToList() ?? new List<EventListDto>(),
        TotalCount = response?.TotalCount ?? 0,
        PageNumber = page,
        PageSize = pageSize
    };
}
```

```razor
@code {
    private int _currentPage = 1;
    private int _pageSize = 10;
    private int _totalCount = 0;
    private List<EventListDto> _items = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadPageAsync(_currentPage);
    }

    private async Task LoadPageAsync(int page)
    {
        var result = await EventService.GetEventsPagedAsync(page, _pageSize);
        _items = result.Items;
        _totalCount = result.TotalCount;
        _currentPage = page;
    }

    private int TotalPages => _totalCount > 0
        ? (int)Math.Ceiling((double)_totalCount / _pageSize)
        : 1;

    private async Task OnPageChanged(int page)
    {
        await LoadPageAsync(page);
    }
}
```

### Filtering with Pagination

When filters change, reset to page 1.

**Generic Template:**

```razor
@code {
    private {IdType}? _selectedCategoryId;
    private string _searchText = "";

    private async Task OnCategoryChanged({IdType}? categoryId)
    {
        _selectedCategoryId = categoryId;
        _currentPage = 1;  // Reset to first page
        await ApplyFiltersAsync();
    }

    private void OnSearch(string value)
    {
        _searchText = value;
        _currentPage = 1;  // Reset to first page
    }

    private List<{Entity}ListDto> FilteredItems => _allItems
        .Where(e => string.IsNullOrEmpty(_searchText) ||
                    e.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
        .Where(e => !_selectedCategoryId.HasValue ||
                    e.CategoryId == _selectedCategoryId.Value)
        .ToList();
}
```

**Implementation Example: ISLAMU Event**

```razor
@code {
    private Guid? _selectedCategoryId;
    private string _searchText = "";

    private async Task OnCategoryChanged(Guid? categoryId)
    {
        _selectedCategoryId = categoryId;
        _currentPage = 1;  // Reset to first page
        await ApplyFiltersAsync();
    }

    private void OnSearch(string value)
    {
        _searchText = value;
        _currentPage = 1;  // Reset to first page
    }

    private List<EventListDto> FilteredItems => _allItems
        .Where(e => string.IsNullOrEmpty(_searchText) ||
                    e.Title.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
        .Where(e => !_selectedCategoryId.HasValue ||
                    e.CategoryId == _selectedCategoryId.Value)
        .ToList();
}
```

---

## 9. Dialog Patterns

### Creating Dialogs

```razor
@* CreateCategoryDialog.razor *@
<MudDialog>
    <DialogContent>
        <MudForm @ref="_form" @bind-IsValid="@_isValid">
            <MudTextField @bind-Value="_model.FullName"
                          Label="Name" Required="true" />
        </MudForm>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Primary"
                   Disabled="@(!_isValid)"
                   OnClick="Submit">Create</MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!;

    private MudForm _form = null!;
    private bool _isValid;
    private CreateCategoryDto _model = new();

    private void Cancel() => MudDialog.Cancel();

    private void Submit() => MudDialog.Close(DialogResult.Ok(_model));
}
```

### Opening Dialogs

```csharp
@inject IDialogService DialogService

@code {
    private async Task OpenCreateDialogAsync()
    {
        var options = new DialogOptions
        {
            CloseOnEscapeKey = true,
            MaxWidth = MaxWidth.Small,
            FullWidth = true
        };

        var dialog = await DialogService.ShowAsync<CreateCategoryDialog>(
            "Create Category",
            options);

        var result = await dialog.Result;

        if (!result.Canceled)
        {
            var newCategory = (CreateCategoryDto)result.Data;
            await SaveCategoryAsync(newCategory);
        }
    }
}
```

### Dialogs with Parameters

```csharp
var parameters = new DialogParameters
{
    { "EventId", eventId },
    { "Title", "Edit Event" }
};

var dialog = await DialogService.ShowAsync<EditEventDialog>(
    "Edit Event",
    parameters,
    options);
```

### Confirmation Dialogs

```csharp
var confirmed = await DialogService.ShowMessageBox(
    "Delete Item",
    "Are you sure you want to delete this item?",
    yesText: "Delete",
    cancelText: "Cancel",
    options: new DialogOptions
    {
        FullWidth = false,
        MaxWidth = MaxWidth.ExtraSmall
    }
);

if (confirmed == true)
{
    await DeleteItemAsync(itemId);
}
```

---

## 10. Authentication & Authorization

### Checking Authentication State

```razor
@inject AuthenticationStateProvider AuthStateProvider

<AuthorizeView>
    <Authorized>
        <MudText>Welcome, @context.User.Identity?.Name!</MudText>
        <MudButton Href="/logout">Logout</MudButton>
    </Authorized>
    <NotAuthorized>
        <MudButton Href="/login">Login</MudButton>
    </NotAuthorized>
</AuthorizeView>
```

### Protected Pages

```razor
@page "/my-events"
@attribute [Authorize]

@* Page content only visible to authenticated users *@
```

### Using AuthStateService

```csharp
@inject IAuthStateService AuthState

@code {
    protected override async Task OnInitializedAsync()
    {
        if (!await AuthState.IsAuthenticatedAsync())
        {
            Navigation.NavigateTo("/login?returnUrl=/my-events");
            return;
        }

        var userId = await AuthState.GetCurrentUserIdAsync();
        var tenantId = await AuthState.GetCurrentTenantIdAsync();

        // Load user-specific data
    }
}
```

### User ID Claim Extraction

The `AuthStateService` uses a fallback chain:

```csharp
var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
             ?? user.FindFirst("sub")?.Value
             ?? user.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value
             ?? user.FindFirst("sid")?.Value;
```

---

## 11. Theming

### Theme Configuration

In `MainLayout.razor`:

```csharp
@code {
    private bool _isDarkMode = false;
    private MudTheme? _theme;

    protected override void OnInitialized()
    {
        _theme = new()
        {
            PaletteLight = _lightPalette,
            PaletteDark = _darkPalette,
            LayoutProperties = new LayoutProperties()
        };
    }

    private readonly PaletteLight _lightPalette = new()
    {
        Black = "#110e2d",
        AppbarText = "#424242",
        AppbarBackground = "rgba(255,255,255,0.8)",
        DrawerBackground = "#ffffff",
        GrayLight = "#e8e8e8",
        GrayLighter = "#f9f9f9",
    };

    private readonly PaletteDark _darkPalette = new()
    {
        Primary = "#7e6fff",
        Surface = "#1e1e2d",
        Background = "#1a1a27",
        // ... etc
    };
}
```

### Theme Persistence

Theme preference is stored in localStorage and cookie:

```javascript
// wwwroot/js/theme.js
window.ExploreTheme = {
    getStoredTheme: () => localStorage.getItem('theme'),
    setStoredTheme: (theme) => localStorage.setItem('theme', theme),
    setThemeCookie: (theme) => {
        document.cookie = `theme=${theme};path=/;max-age=31536000`;
    }
};
```

### Dark Mode Toggle

```csharp
private async Task DarkModeToggle()
{
    _isDarkMode = !_isDarkMode;
    var themeValue = _isDarkMode ? "dark" : "light";

    await JSRuntime.InvokeVoidAsync("ExploreTheme.setStoredTheme", themeValue);
    await JSRuntime.InvokeVoidAsync("ExploreTheme.setThemeCookie", themeValue);
}
```

---

## 12. CSS & Styling Conventions

### Strategy: BEM within Isolation

We use a hybrid approach combining **Blazor CSS Isolation** (for scoping) with **BEM Naming** (for structure).

**Goal**: 100% of components should have a corresponding `.razor.css` file.

### 12.1. File Structure (Co-location)

Always place the CSS file next to the component.

```
Components/
├── EventCard.razor
└── EventCard.razor.css       ✅ Correct
```

### 12.2. BEM Naming Convention

Even inside isolated CSS, we use BEM. This reduces specificity wars and makes the code self-documenting.

**Pattern**:
- **Block**: `.component-name` (e.g., `.event-card`)
- **Element**: `.component-name__element` (e.g., `.event-card__title`)
- **Modifier**: `.component-name--modifier` (e.g., `.event-card--featured`)

**Why BEM in Isolation?**
- Isolation adds a random attribute `[b-xyz]`.
- If you just use `h1` or `.title`, it's hard to debug which component owns it.
- `.event-card__title[b-xyz]` is unambiguous in DevTools.

### 12.3. Styling Child Components

**Rule**: A parent component should NOT style its children's internals.

**✅ Good (Props)**:
Pass a Class parameter.
```razor
<ChildComponent Class="my-context-class" />
```

**⚠️ Caution (::deep)**:
Use `::deep` only when styling 3rd party components (MudBlazor) where you can't change the source.
```css
/* Styling MudBlazor internals */
.my-component ::deep .mud-input {
    border: 1px solid red;
}
```

### 12.4. Global Styles

Global styles (`wwwroot/css/app.css`) are restricted to:
1.  CSS Variables (Theming colors, spacing).
2.  Utility classes (`.isl-flex-center`).
3.  Reset/Normalize rules.

Everything else belongs in `.razor.css`.

---

### CSS Isolation Architecture

Blazor provides **CSS isolation** via `.razor.css` files that scope styles to individual components, preventing global collisions.

#### How CSS Isolation Works

**Build-Time Process**:
1. Blazor generates unique scope attribute: `b-<10-char-string>` (e.g., `b-3xxtam6d07`)
2. Appends scope attribute to all selectors in `.razor.css`
3. Applies scope attribute to rendered DOM elements
4. Bundles scoped CSS into `{Project}.styles.css`

**Example Transformation**:
```css
/* Author in Counter.razor.css */
h1 { color: brown; }

/* Compiled in Explore.styles.css */
h1[b-3xxtam6d07] { color: brown; }
```

**Rendered HTML**:
```html
<h1 b-3xxtam6d07>Counter</h1>
```

**Result**: Styles apply ONLY to component's elements, never globally.

---

### BEM Methodology with CSS Isolation

Use BEM (Block Element Modifier) naming **even with isolation** for explicit, maintainable styling hooks.

#### BEM Pattern Structure

```
.block
.block__element
.block--modifier
.block__element--modifier
```

#### Component Example (EventCard)

**File Structure**:
```
Components/
├── EventCard.razor
└── EventCard.razor.css
```

**EventCard.razor**:
```razor
<MudCard Class="event-card event-card--featured">
    <MudCardHeader Class="event-card__header">
        <MudText Typo="Typo.h6" Class="event-card__title">@Title</MudText>
        <span class="event-card__badge event-card__badge--new">New</span>
    </MudCardHeader>
    <MudCardContent Class="event-card__body">
        <MudText Class="event-card__description">@Description</MudText>
    </MudCardContent>
    <MudCardActions Class="event-card__footer">
        <MudButton Class="event-card__action">Register</MudButton>
    </MudCardActions>
</MudCard>
```

**EventCard.razor.css** (scoped automatically):
```css
/* Block */
.event-card {
    border-radius: 12px;
    transition: box-shadow 0.2s ease;
}

.event-card:hover {
    box-shadow: var(--mud-elevation-4);
}

/* Block modifier */
.event-card--featured {
    border-left: 4px solid var(--mud-palette-success);
}

/* Elements */
.event-card__header {
    background: var(--mud-palette-surface-variant);
}

.event-card__title {
    font-weight: 600;
    color: var(--mud-palette-primary);
}

.event-card__badge {
    display: inline-block;
    padding: 4px 8px;
    border-radius: 4px;
    font-size: 0.75rem;
}

/* Element modifier */
.event-card__badge--new {
    background: var(--mud-palette-success);
    color: var(--mud-palette-success-text);
}

.event-card__body {
    padding: 16px;
}

.event-card__description {
    line-height: 1.6;
}

.event-card__footer {
    justify-content: flex-end;
}
```

**Compiled Output** (automatic):
```css
/* All selectors receive scope attribute */
.event-card[b-xyz123] { ... }
.event-card--featured[b-xyz123] { ... }
.event-card__header[b-xyz123] { ... }
/* Prevents collision with other components' .event-card */
```

---

### Styling Child Components

#### Pattern A: Child's Own CSS (Preferred)

Each component styles itself in its own `.razor.css`.

```css
/* Parent.razor.css */
.parent {
    display: grid;
    gap: 16px;
}

/* EventCard.razor.css (child owns its styles) */
.event-card {
    padding: 12px;
    background: var(--mud-palette-surface);
}
```

**Why**: Separation of concerns; child owns its presentation.

---

#### Pattern B: Wrapper Container (Safe Descendant Styling)

Wrap child in HTML element to enable parent's descendant selectors.

```razor
<div class="parent">
    <div class="parent__child-wrapper">
        <ChildComponent />
    </div>
</div>
```

```css
/* Parent.razor.css */
.parent__child-wrapper {
    padding: 8px;
    border: 1px solid var(--mud-palette-divider);
}
```

**Why**: Wrapper gets scope attribute; you can style without penetrating child.

---

#### Pattern C: The ::deep Selector (Last Resort)

**Purpose**: Penetrate child component encapsulation.

**Transformation**:
```css
/* Author */
.parent ::deep .child-element { color: red; }

/* Compiled */
.parent[b-xyz123] .child-element { color: red; }
```

**Example** - Styling MudBlazor Component:
```css
/* Host.razor.css */
.host ::deep .mud-table-cell {
    padding: 12px 16px;
}
```

**When to Use ::deep**:
- ✅ Styling third-party components with no exposed API
- ✅ Overriding MudBlazor internals when necessary

**When NOT to Use ::deep**:
- ❌ Styling your own child components (use child's `.razor.css`)
- ❌ First resort (prefer wrapper or component parameters)

**Tradeoffs**:
- ⚠️ **Fragile**: Coupled to child's internal markup
- ⚠️ **Upgrade Risk**: Library updates may break selectors

---

### CSS Isolation File Structure

**Required Pattern**:
- Place `ComponentName.razor.css` next to `ComponentName.razor`
- File names must match (case-insensitive)
- Reference `{Project}.styles.css` bundle in `App.razor` or `index.html`

**Example Structure**:
```
Components/
├── EventList.razor
├── EventList.razor.css       ✅ Scoped to EventList
├── EventCard.razor
└── EventCard.razor.css       ✅ Scoped to EventCard
```

**Bundle Reference** (`App.razor`):
```html
<link href="Explore.styles.css" rel="stylesheet" />
```

---

### Debugging Scoped CSS

**Step 1: Inspect Element Scope Attribute**
```html
<h1 b-3xxtam6d07>Counter</h1>
```

**Step 2: Find Matching Selector in DevTools**
```css
h1[b-3xxtam6d07] { color: brown; }
```

**Step 3: Verify Bundle Reference**

Check `App.razor` includes:
```html
<link href="Explore.styles.css" rel="stylesheet" />
```

#### Common Issues & Solutions

| Symptom | Cause | Fix |
|---------|-------|-----|
| Styles not applying | Missing bundle reference | Add `<link>` to `{Project}.styles.css` |
| Child not styled | Using parent CSS | Add `.razor.css` to child OR use ::deep |
| ::deep not working | Child renders to body | Use component's `Target` parameter |

---

### Global Styles

Global styles go in `wwwroot/css/app.css`:

```css
/* Custom utility classes */
.isl-typo-h5 {
    font-size: 1.5rem;
    font-weight: 600;
    line-height: 1.3;
}

.isl-button-pill {
    border-radius: 20px;
    padding: 8px 16px;
    cursor: pointer;
}

.isl-popover-menu {
    border-radius: 8px;
    box-shadow: var(--mud-elevation-4);
}

.isl-popover-menu__option {
    padding: 8px 12px;
    cursor: pointer;
    border-radius: 4px;
}

.isl-popover-menu__option:hover {
    background-color: var(--mud-palette-action-default-hover);
}
```

---

## 13. Error Handling

### Service-Level Error Handling

```csharp
public async Task<ICollection<EventListDto>> GetAllEventsAsync()
{
    try
    {
        var response = await _apiClient.EventGETAsync(pageNumber: 1, pageSize: 100);
        return response?.Items ?? new List<EventListDto>();
    }
    catch (ApiException ex) when (ex.StatusCode == 401)
    {
        _logger.LogWarning("Unauthorized access attempt");
        return new List<EventListDto>();
    }
    catch (ApiException ex) when (ex.StatusCode == 404)
    {
        _logger.LogInformation("Resource not found");
        return new List<EventListDto>();
    }
    catch (ApiException ex)
    {
        _logger.LogError(ex, "API error: {StatusCode}", ex.StatusCode);
        return new List<EventListDto>();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        return new List<EventListDto>();
    }
}
```

### Component-Level Error Handling

```razor
@code {
    private bool _hasError = false;
    private string? _errorMessage;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            await LoadDataAsync();
        }
        catch (Exception ex)
        {
            _hasError = true;
            _errorMessage = "Failed to load data. Please try again.";
            Logger.LogError(ex, "Error in OnInitializedAsync");
        }
    }
}

@if (_hasError)
{
    <MudAlert Severity="Severity.Error">@_errorMessage</MudAlert>
    <MudButton OnClick="RetryAsync">Retry</MudButton>
}
```

### Global Error Boundary

```razor
@* In App.razor *@
<ErrorBoundary>
    <ChildContent>
        <Router AppAssembly="@typeof(App).Assembly">
            <!-- ... -->
        </Router>
    </ChildContent>
    <ErrorContent Context="ex">
        <MudAlert Severity="Severity.Error">
            An unexpected error occurred. Please refresh the page.
        </MudAlert>
    </ErrorContent>
</ErrorBoundary>
```

---

## 14. Best Practices

### DO ✅

1. **Use service layer** - Never call `IEventApiClient` directly from components
2. **Handle loading states** - Show skeletons or spinners during data fetching
3. **Reset pagination on filter change** - Always reset to page 1
4. **Use `[EditorRequired]`** - For mandatory parameters
5. **Log appropriately** - Use structured logging with context
6. **Dispose resources** - Implement `IDisposable` when needed
7. **Use `StateHasChanged()`** - Only when necessary (after async callbacks)

### DON'T ❌

1. **Don't store tokens in WASM** - BFF handles token management
2. **Don't call API directly** - Use the generated NSwag client via services
3. **Don't use `Console.WriteLine`** - Use `ILogger<T>` instead
4. **Don't block with `.Result`** - Always use `await`
5. **Don't ignore errors** - Handle and log all exceptions
6. **Don't hardcode URLs** - Use configuration

### Performance Tips

```csharp
// Use parallel loading for independent data
protected override async Task OnInitializedAsync()
{
    var eventsTask = EventService.GetAllEventsAsync();
    var categoriesTask = CategoryService.GetAllCategoriesAsync();
    var tagsTask = TagService.GetAllTagsAsync();

    await Task.WhenAll(eventsTask, categoriesTask, tagsTask);

    _events = await eventsTask;
    _categories = await categoriesTask;
    _tags = await tagsTask;
}

// Use virtualization for large lists
<MudVirtualize Items="@_largeList" Context="item" OverscanCount="5">
    <ItemContent>
        <EventCard Event="@item" />
    </ItemContent>
</MudVirtualize>
```

---

## Related Documentation

- **[ARCHITECTURE.md](ARCHITECTURE.md)** - Overall system architecture
- **[SECURITY.md](SECURITY.md)** - Authentication and authorization
- **[QUICK_REFERENCE.md](QUICK_REFERENCE.md)** - Critical coding rules

## Skills

- **`blazor-ui-conventions`** - MudBlazor patterns and component structure
- **`blazor-bff-patterns`** - BFF architecture and YARP configuration
- **`auth-patterns`** - Authentication and Keycloak integration
