# Render Modes

Blazor supports multiple render modes for different performance and deployment scenarios.

---

## ISLAMU Event Architecture

ISLAMU Event uses a **hybrid Blazor architecture**:

```
┌─────────────────────────────────────────────────────────────┐
│              Blazor Hybrid Architecture                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Explore.Blazor (Server)                                    │
│  ┌────────────────────────────────────────────────────┐    │
│  │  • Server-side Blazor (BFF pattern)                │    │
│  │  • OIDC Authentication with Keycloak               │    │
│  │  • Cookie-based auth                               │    │
│  │  • HttpContext access                              │    │
│  │  • Components/Pages/*.razor                        │    │
│  │  • @rendermode="InteractiveAuto"                   │    │
│  └────────────────────────────────────────────────────┘    │
│                          ↓                                  │
│  Explore.Blazor.Client (WebAssembly)                        │
│  ┌────────────────────────────────────────────────────┐    │
│  │  • Client-side Blazor (WASM)                       │    │
│  │  • Runs in browser                                 │    │
│  │  • No server access                                │    │
│  │  • Shared components                               │    │
│  │  • Layout/MainLayout.razor                         │    │
│  └────────────────────────────────────────────────────┘    │
│                                                             │
│  Render Mode: InteractiveAuto                               │
│  ├─ Starts with Server (fast initial load)                 │
│  ├─ Downloads WASM in background                           │
│  └─ Switches to client-side after download                 │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## Available Render Modes

| Render Mode | Execution | Real-time | Latency | Server Load | Offline |
|-------------|-----------|-----------|---------|-------------|---------|
| **Static SSR** | Server (no interactivity) | ❌ | Low | Low | ❌ |
| **InteractiveServer** | Server via SignalR | ✅ | Medium | High | ❌ |
| **InteractiveWebAssembly** | Browser (WASM) | ✅ | Low | Low | ✅ |
| **InteractiveAuto** | Server → WASM | ✅ | Low | Medium | ⚠️ |

---

## InteractiveAuto (Project Default)

**Best of both worlds**: Starts with Server, transitions to WebAssembly.

### How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                  InteractiveAuto Flow                       │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. First Visit (no WASM cached)                            │
│     ├─ Component renders via Server (fast!)                │
│     ├─ WASM downloads in background                        │
│     └─ SignalR connection active                           │
│                                                             │
│  2. WASM Download Complete                                  │
│     ├─ Seamless switch to client-side                      │
│     ├─ SignalR disconnects                                 │
│     └─ Component now runs in browser                       │
│                                                             │
│  3. Subsequent Visits                                       │
│     ├─ WASM cached in browser                              │
│     └─ Runs client-side immediately                        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Usage

```razor
@* Project default - no attribute needed *@
@page "/events"
@rendermode InteractiveAuto

<PageTitle>Events</PageTitle>

<MudContainer>
    <MudText Typo="Typo.h4">Events</MudText>
    @* Component content *@
</MudContainer>

@code {
    protected override async Task OnInitializedAsync()
    {
        // This runs on Server first visit, then WASM on subsequent visits
    }
}
```

### When to Use

✅ **Use InteractiveAuto when**:
- You want fast initial load (Server) + offline capability (WASM)
- Component doesn't rely on server-only APIs (HttpContext, etc.)
- You want the best user experience

❌ **Don't use InteractiveAuto when**:
- Component accesses HttpContext (server-only)
- Component uses server-side dependencies
- You need consistent execution environment

---

## InteractiveServer

**Server-side execution** via SignalR connection.

### How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                  InteractiveServer Flow                     │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  Client (Browser)           Server                          │
│  ─────────────────         ──────                           │
│                                                             │
│  User clicks button ──────▶ SignalR message                 │
│                             Event handler runs              │
│                             Component updates               │
│                             Diff calculated                 │
│  UI updates ◀────────────── Diff sent via SignalR          │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Usage

```razor
@page "/admin/dashboard"
@rendermode InteractiveServer

@inject IHttpContextAccessor HttpContextAccessor

<MudContainer>
    <MudText>User IP: @_userIp</MudText>  @* Server-only data *@
</MudContainer>

@code {
    private string _userIp = string.Empty;

    protected override void OnInitialized()
    {
        // ✅ Access HttpContext (server-only)
        _userIp = HttpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }
}
```

### When to Use

✅ **Use InteractiveServer when**:
- Need access to HttpContext (cookies, IP, headers)
- Need server-side file system access
- Component has heavy dependencies (large libraries)
- Real-time updates via SignalR are beneficial

❌ **Don't use InteractiveServer when**:
- Latency matters (international users)
- Server resources are constrained
- Offline functionality required

### Pros & Cons

**Pros**:
- ✅ Small download size (no WASM)
- ✅ Fast initial load
- ✅ Access to server-side APIs
- ✅ Full .NET framework available

**Cons**:
- ❌ Requires active connection
- ❌ Latency on every interaction
- ❌ Higher server resource usage
- ❌ No offline support

---

## InteractiveWebAssembly

**Client-side execution** in the browser via WebAssembly.

### How It Works

```
┌─────────────────────────────────────────────────────────────┐
│                InteractiveWebAssembly Flow                  │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  1. Browser downloads .NET runtime + app DLLs (WASM)        │
│  2. Component executes entirely in browser                  │
│  3. No server communication except HTTP API calls           │
│  4. Offline-capable (with service worker)                   │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Usage

```razor
@page "/events/map"
@rendermode InteractiveWebAssembly

@inject HttpClient Http

<MudContainer>
    <div id="map"></div>  @* JS interop for maps *@
</MudContainer>

@code {
    protected override async Task OnInitializedAsync()
    {
        // ✅ Runs in browser (low latency)
        var events = await Http.GetFromJsonAsync<List<EventListDto>>("api/v1/events");
    }
}
```

### When to Use

✅ **Use InteractiveWebAssembly when**:
- Offline functionality required
- Low latency is critical (interactive maps, games)
- Reducing server load is important
- Component doesn't need server-side APIs

❌ **Don't use InteractiveWebAssembly when**:
- Need HttpContext access
- Need server-side file system
- App size is a concern (WASM is large)
- SEO is critical (WASM doesn't prerender)

### Pros & Cons

**Pros**:
- ✅ Runs offline (with service worker)
- ✅ Zero latency on interactions
- ✅ Reduced server load
- ✅ Progressive Web App (PWA) capable

**Cons**:
- ❌ Large initial download (~2-3 MB compressed)
- ❌ Slower initial load
- ❌ No access to server-side APIs
- ❌ Limited .NET library compatibility

---

## Static Server Rendering (SSR)

**No interactivity** - pure server-side rendering.

### Usage

```razor
@page "/about"
@* No @rendermode directive = Static SSR *@

<MudContainer>
    <MudText Typo="Typo.h4">About Us</MudText>
    <MudText>
        ISLAMU Event is a federated event discovery platform...
    </MudText>
</MudContainer>

@code {
    // No interactive code - purely static
}
```

### When to Use

✅ **Use Static SSR when**:
- Page has no interactivity (about, terms, privacy)
- SEO is critical
- Fastest possible load time needed

❌ **Don't use Static SSR when**:
- Page needs buttons, forms, or interactivity

---

## Mixing Render Modes

You can use different render modes in the same app.

### Example: Hybrid Page

```razor
@page "/events"
@rendermode InteractiveAuto

<PageTitle>Events</PageTitle>

<MudContainer>
    @* Interactive component (InteractiveAuto) *@
    <EventFilters @bind-Filters="filters" />

    @* Static component (no interactivity) *@
    <StaticFooter />
</MudContainer>

@code {
    private EventFilterDto filters = new();
}
```

### Per-Component Render Mode

```razor
@* Parent uses InteractiveAuto *@
@page "/dashboard"
@rendermode InteractiveAuto

<MudContainer>
    @* This child uses InteractiveServer *@
    <AdminPanel @rendermode="InteractiveServer" />
</MudContainer>
```

---

## Prerendering

Components can be **prerendered** on the server for faster initial display.

### Enable Prerendering

```razor
@page "/events"
@rendermode @(new InteractiveAutoRenderMode(prerender: true))

<MudContainer>
    @if (_events == null)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        @foreach (var evt in _events)
        {
            <MudCard>@evt.Title</MudCard>
        }
    }
</MudContainer>

@code {
    private List<EventListDto>? _events;

    protected override async Task OnInitializedAsync()
    {
        // ⚠️ This runs TWICE with prerendering:
        // 1. On server (prerender)
        // 2. On client (interactive render)
        _events = await Http.GetFromJsonAsync<List<EventListDto>>("api/v1/events");
    }
}
```

### Prerendering Lifecycle

```
1. Server prerender → OnInitializedAsync runs → HTML sent to browser
2. WASM loads → OnInitializedAsync runs AGAIN → Component becomes interactive
```

### Detecting Prerendering

```csharp
@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ Only runs AFTER prerendering (on client)
            await LoadDataAsync();
        }
    }
}
```

---

## Server vs WASM Context Differences

| Feature | Server | WASM |
|---------|--------|------|
| **HttpContext** | ✅ Available | ❌ Not available |
| **File System** | ✅ Full access | ❌ No access |
| **Environment Variables** | ✅ Available | ❌ Not available |
| **SignalR** | ✅ Built-in | ❌ Manual setup |
| **LocalStorage** | ❌ Via JS interop | ✅ Via JS interop |
| **Performance** | Network latency | Zero latency |

### Server-Only Code

```razor
@inject IHttpContextAccessor HttpContextAccessor

@code {
    protected override void OnInitialized()
    {
        // ✅ Only works in InteractiveServer
        var cookie = HttpContextAccessor.HttpContext?.Request.Cookies["theme"];
    }
}
```

### WASM-Only Code

```razor
@inject IJSRuntime JS

@code {
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // ✅ LocalStorage works better in WASM
            var theme = await JS.InvokeAsync<string>("localStorage.getItem", "theme");
        }
    }
}
```

### Cross-Compatible Code

```razor
@inject HttpClient Http

@code {
    protected override async Task OnInitializedAsync()
    {
        // ✅ Works in both Server and WASM
        var events = await Http.GetFromJsonAsync<List<EventListDto>>("api/v1/events");
    }
}
```

---

## Best Practices

### ✅ DO

- ✅ Use `InteractiveAuto` as default (project standard)
- ✅ Use `InteractiveServer` for admin pages with HttpContext
- ✅ Use `InteractiveWebAssembly` for offline-capable features
- ✅ Use Static SSR for non-interactive content (about, terms)
- ✅ Be aware of prerendering double-execution
- ✅ Use `OnAfterRenderAsync` for client-only initialization

### ❌ DON'T

- ❌ Don't access `HttpContext` in `InteractiveAuto` or `InteractiveWebAssembly`
- ❌ Don't call expensive operations twice during prerendering
- ❌ Don't assume execution context (check `OperatingSystem.IsBrowser()` if needed)
- ❌ Don't use server-side file paths in WASM components

---

## ISLAMU Event Render Mode Strategy

| Page/Component | Render Mode | Reason |
|----------------|-------------|--------|
| **Home** | InteractiveAuto | Fast load + interactive filters |
| **Event List** | InteractiveAuto | Filtering, sorting, pagination |
| **Event Details** | InteractiveAuto | RSVP, comments, sharing |
| **Admin Dashboard** | InteractiveServer | HttpContext, auth cookies |
| **Event Map** | InteractiveWebAssembly | Offline maps, low latency |
| **About/Terms** | Static SSR | No interactivity, SEO |

---

## Troubleshooting

### Issue: Component doesn't work in WASM

**Cause**: Using HttpContext or server-only APIs

**Solution**: Use `InteractiveServer` or refactor to use HTTP APIs

### Issue: OnInitializedAsync runs twice

**Cause**: Prerendering enabled

**Solution**: Move logic to `OnAfterRenderAsync(firstRender)` or disable prerendering

### Issue: SignalR connection errors

**Cause**: Component using `InteractiveServer` but Server project not configured

**Solution**: Ensure `AddInteractiveServerComponents()` in Program.cs

---

**Related Resources**:
- [component-structure.md](component-structure.md) - Component lifecycle
- [mudblazor-components.md](mudblazor-components.md) - UI components
- [common-patterns.md](common-patterns.md) - Real-world patterns
