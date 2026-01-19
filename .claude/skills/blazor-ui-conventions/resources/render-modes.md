# Render Modes - Blazor Interactive Modes

> **Project-Agnostic Blazor Render Mode Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

This document explains the various Blazor render modes and provides guidance on when to use each in hybrid Blazor applications. Understanding render modes is crucial for optimizing performance, user experience, and resource usage.

---

## 1. Blazor Hybrid Architecture

A typical hybrid Blazor application combines Blazor Server for initial page loads and server-side logic, and Blazor WebAssembly for client-side interactivity and offline capabilities. This is achieved primarily through the `InteractiveAuto` render mode.

```mermaid
graph TD
    subgraph Blazor Hybrid Application
        A[{Project}.Blazor (Server Project)] -- Initial Render / Server Interactivity --> B[Browser (Client)]
        C[{Project}.Blazor.Client (WASM Project)] -- Client-side Interactivity --> B
    end

    subgraph Key Characteristics
        S1[Blazor Server (BFF)] --> S2(OIDC Auth, HttpContext, YARP Proxy)
        W1[Blazor WebAssembly] --> W2(Runs in browser, Offline, Low Latency)
        Auto[InteractiveAuto Render Mode] --> Auto1(Starts Server-side, transitions to WASM)
    end

    B -- Downloads WASM in background --> C
    BFF -- Forwards API calls --> API_Backend[{Project}.API (Backend)]

    S2 --- S1
    W2 --- W1
    Auto1 --- Auto
```

---

## 2. Overview of Available Render Modes

Blazor provides four primary render modes, each with distinct characteristics regarding where the UI is rendered and where event handling occurs.

| Render Mode | Execution Location | Interactivity | Initial Load Time | Server Load | Offline Support | Use Case |
|-------------|--------------------|---------------|-------------------|-------------|-----------------|----------|
| **Static Server Rendering (SSR)** | Server | No | Fastest | Low | No | Static pages (About, Privacy) |
| **Interactive Server** | Server (via SignalR) | Yes | Fast | High | No | Server-dependent logic (HttpContext), administrative dashboards |
| **Interactive WebAssembly** | Browser (WASM) | Yes | Slow (initial) | Low | Yes | Highly interactive, offline-capable features (games, rich editors) |
| **Interactive Auto** | Server (initially) then Browser (WASM) | Yes | Fast | Medium (initially) | Partial | Default for most interactive pages in hybrid apps |

---

## 3. InteractiveAuto (Project Default)

**InteractiveAuto** mode is the recommended default for most interactive components in a Blazor Hybrid application. It offers a balance between fast initial load times and client-side performance.

### How It Works:

1.  **Initial Load**: The component is rendered and handled server-side (like Interactive Server). This ensures a very fast first page load.
2.  **WASM Download**: In the background, the Blazor WebAssembly runtime and application assets (`.wasm` files) are downloaded to the client's browser.
3.  **Seamless Transition**: Once the WebAssembly assets are downloaded, the component's interactivity is seamlessly transferred to the client. The SignalR connection is terminated, and subsequent interactions are handled directly by the browser-side WebAssembly.
4.  **Subsequent Visits**: On later visits, if the WebAssembly assets are cached, the component will render and operate client-side immediately.

### Usage:

Apply `InteractiveAuto` to routable components (`@page` directive) or individual interactive components.

```razor
@page "/{entities}"
@rendermode InteractiveAuto @* Apply to the root component of the interactive part *@

<PageTitle>{Entities}</PageTitle>

<MudContainer>
    <MudText Typo="Typo.h4">Upcoming {Entities}</MudText>
    @* Your interactive component content here *@
</MudContainer>

@code {
    protected override async Task OnInitializedAsync()
    {
        // This method will execute twice with prerendering: once on the server, then on the client.
        // Be mindful of side effects that should only occur once (e.g., fetching data that's already part of the prerendered HTML).
    }
}
```

### When to Use InteractiveAuto:

*   **Default for Interactive Components**: When you need client-side interactivity (buttons, forms, dynamic content).
*   **Optimized User Experience**: Provides a fast initial page load (server-rendered) and low-latency interactivity (client-rendered).
*   **Most Pages**: Suitable for most application pages where interactivity is key.

### When NOT to Use InteractiveAuto:

*   **Server-Only Dependencies**: If a component *must* access server-side resources like `HttpContext` or the file system (use `InteractiveServer`).
*   **Very Small Static Content**: For purely static pages, `Static Server Rendering` is more efficient (no WASM download overhead).

---

## 4. InteractiveServer

**Interactive Server** mode keeps the component's UI and event handling on the server, using a SignalR connection to communicate UI updates and events to/from the client.

### How It Works:

1.  **Server-Side Rendering**: The UI is rendered on the server and sent as HTML to the client.
2.  **SignalR Connection**: A persistent SignalR connection is established between the client and the server.
3.  **Event Handling**: User interactions (e.g., button clicks) are sent via SignalR to the server.
4.  **UI Updates**: The server processes the event, updates the component's state, recalculates the UI diff, and sends only the necessary changes back to the client via SignalR, which then updates the DOM.

### Usage:

```razor
@page "/admin/system-status"
@rendermode InteractiveServer

@inject IHttpContextAccessor HttpContextAccessor @* Access to HttpContext is server-only *@

<MudContainer>
    <MudText Typo="Typo.h6">Server Time: @_serverTime</MudText>
    <MudText>User IP: @_userIp</MudText>
    <MudButton OnClick="RefreshStatus">Refresh</MudButton>
</MudContainer>

@code {
    private string _serverTime = string.Empty;
    private string _userIp = string.Empty;

    protected override void OnInitialized()
    {
        // Can directly access server-side resources like HttpContext
        _serverTime = DateTime.Now.ToString("F");
        _userIp = HttpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
    }

    private void RefreshStatus()
    {
        _serverTime = DateTime.Now.ToString("F");
        // StateHasChanged() is implicitly called after event handlers
    }
}
```

### When to Use InteractiveServer:

*   **Server-Side Access**: When a component *needs* direct access to server-side APIs, `HttpContext` (for cookies, IP address, headers), or the server's file system.
*   **Heavy Dependencies**: For components that rely on large server-side libraries or perform computationally intensive tasks.
*   **Real-Time Updates**: Ideal for applications requiring frequent, low-latency updates from the server (e.g., chat applications, dashboards).

### When NOT to Use InteractiveServer:

*   **High Latency Concerns**: Each user interaction involves a round trip to the server, which can introduce noticeable latency for users geographically distant from the server.
*   **High Server Load**: Maintaining SignalR connections and rendering components server-side for many concurrent users can consume significant server resources.
*   **Offline Functionality**: Components in this mode cannot function offline.

---

## 5. InteractiveWebAssembly

**Interactive WebAssembly** mode executes the component entirely client-side within the browser using WebAssembly.

### How It Works:

1.  **Initial Download**: The client browser downloads the .NET runtime (as WebAssembly) and the application's DLLs. This can be a significant initial download.
2.  **Client-Side Execution**: Once downloaded, the entire component (UI, logic, event handling) runs directly in the user's browser, without any server round-trips for interactivity.
3.  **API Communication**: Client-side components communicate with backend APIs via standard HTTP requests.

### Usage:

```razor
@page "/offline-editor"
@rendermode InteractiveWebAssembly

@inject HttpClient Http

<MudContainer>
    <MudText Typo="Typo.h4">Offline {Entity} Editor</MudText>
    <MudTextField @bind-Value="{entity}Title" Label="{Entity} Title" />
    <MudButton OnClick="SaveDraft">Save Draft Locally</MudButton>
    <MudButton OnClick="SyncToServer">Sync to Server</MudButton>
</MudContainer>

@code {
    private string {entity}Title = string.Empty;

    protected override async Task OnInitializedAsync()
    {
        // Runs entirely in the browser (low interaction latency)
        // Can access browser storage APIs for offline persistence
    }

    private async Task SaveDraft()
    {
        // Logic to save draft to browser's IndexedDB or localStorage
    }

    private async Task SyncToServer()
    {
        // Make HTTP call to backend API
        // await Http.PostAsJsonAsync("api/v1/{entities}/drafts", draft{Entity});
    }
}
```

### When to Use InteractiveWebAssembly:

*   **Offline Functionality**: Essential for applications that need to work fully or partially offline (e.g., using service workers).
*   **Low Interaction Latency**: Ideal for highly interactive components (e.g., rich text editors, games, interactive diagrams) where immediate UI feedback is crucial.
*   **Reduced Server Load**: Once downloaded, the client handles all processing, significantly reducing the load on the server.

### When NOT to Use InteractiveWebAssembly:

*   **Large Initial Download**: The initial download of the .NET runtime and application DLLs can be substantial (several MBs), leading to a slower initial page load.
*   **No Server-Side Access**: Components cannot directly access server-side resources (`HttpContext`, file system).
*   **SEO Concerns**: Prerendering helps, but dynamic content might still pose SEO challenges compared to pure server-rendered pages.

---

## 6. Static Server Rendering (SSR)

**Static Server Rendering (SSR)** is the most basic render mode, where components are rendered once on the server into static HTML. There is no interactivity.

### How It Works:

1.  **Server-Side Render**: The component is rendered into HTML on the server.
2.  **Static HTML Delivery**: The resulting static HTML is sent to the client.
3.  **No Interactivity**: No JavaScript bundle is loaded, and no SignalR connection is established for the component. User interactions on such pages will require a full page refresh.

### Usage:

For static pages like "About Us" or "Privacy Policy," where no client-side interaction is needed.

```razor
@page "/about"
@* No @rendermode directive means Static SSR by default *@

<PageTitle>About {Project}</PageTitle>

<MudContainer MaxWidth="MaxWidth.Medium">
    <MudText Typo="Typo.h4" Class="mb-4">About Us</MudText>
    <MudText Class="mb-3">
        {Project} is a platform designed to connect
        communities with engaging content.
    </MudText>
    <MudText Class="mb-3">
        Our mission is to foster knowledge, strengthen bonds, and facilitate access
        to high-quality resources.
    </MudText>
    <MudLink Href="https://example.org" Target="_blank">Learn more</MudLink>
</MudContainer>

@code {
    // No interactive C# code here, as the component is static
}
```

### When to Use Static SSR:

*   **Static Content**: Ideal for pages with purely static content that doesn't require any client-side interaction.
*   **SEO Optimization**: Best for search engine optimization (SEO) as the full content is available in the initial HTML.
*   **Fastest Initial Load**: Since no WebAssembly runtime or SignalR connection is involved, these pages load extremely quickly.

### When NOT to Use Static SSR:

*   **Any Interactivity**: Not suitable for pages that require any form of user interaction (buttons, forms, dynamic updates).

---

## 7. Mixing Render Modes

You can use different render modes for different components within the same application, or even on the same page.

### Page-Level Render Mode

Apply `@rendermode` to the `@page` directive for the entire page's interactivity.

```razor
@page "/{entities}"
@rendermode InteractiveAuto @* This page and its children are InteractiveAuto *@
```

### Per-Component Render Mode

Apply `@rendermode` to individual component tags to override the page's render mode or make a specific component interactive on a static page.

```razor
@page "/static-page" @* Static SSR by default *@

<MudContainer>
    <MudText Typo="Typo.h4">Static Content</MudText>

    @* This component will be interactive, running in InteractiveServer mode *@
    <LoginFormComponent @rendermode="InteractiveServer" />

</MudContainer>
```

---

## 8. Prerendering

**Prerendering** is enabled by default for `InteractiveAuto`, `InteractiveServer`, and `InteractiveWebAssembly` components. It involves rendering the component on the server first to generate static HTML, which is sent to the client. Once the interactive host starts, the component is rendered again.

### How Prerendering Affects Lifecycle:

*   **`OnInitializedAsync` runs twice**: Once during the server prerender, and again when the component is rendered interactively on the client.
*   **`OnAfterRenderAsync(true)` runs twice**: The first `firstRender` will be on the server, the second on the client.

### Implications of Prerendering:

*   **Fast Initial UX**: Users see content immediately while the interactive client loads.
*   **SEO Friendly**: Search engines index the fully rendered HTML.
*   **Careful with Side Effects**: Avoid side effects (e.g., API calls, database writes) in `OnInitializedAsync` that should only happen once.

### Best Practice for Prerendering with Side Effects:

Move logic with side effects to `OnAfterRenderAsync(true)` or ensure your code handles duplicate execution gracefully.

```csharp
@page "/{entities}"
@rendermode @(new InteractiveAutoRenderMode(prerender: true)) @* Prerendering enabled *@

<MudContainer>
    @if (_{entities} == null)
    {
        <MudProgressCircular Indeterminate="true" />
    }
    else
    {
        @foreach (var item in _{entities})
        {
            <MudCard>@item.Title</MudCard>
        }
    }
</MudContainer>

@code {
    private List<{Entity}ListDto>? _{entities};

    protected override async Task OnInitializedAsync()
    {
        // Only fetch data once if it's not going to be part of the prerendered HTML or if side effects are handled
        // For data that is not critical for prerendering, consider fetching it only client-side.
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && OperatingSystem.IsBrowser()) // Ensure this only runs once on the client-side
        {
            // Fetch data here if it shouldn't run on the server prerender,
            // or if it's dependent on client-side state.
            // _{entities} = await Http.GetFromJsonAsync<List<{Entity}ListDto>>("api/v1/{entities}");
            // StateHasChanged(); // Required if you update state after async operation in OnAfterRenderAsync
        }
    }
}
```

---

## 9. Server vs. WASM Context Differences

It's critical to understand the differences in environment when developing Blazor Hybrid applications.

| Feature | Blazor Server | Blazor WebAssembly |
|---------|---------------|--------------------|
| **`HttpContext`** | Available | Not available |
| **File System Access** | Full server access | No direct access |
| **Database Access** | Direct via ORM | Via API calls only |
| **Environment Variables** | Server's variables | No direct access |
| **`IJSRuntime`** | Via SignalR (latency) | Direct browser access (low latency) |
| **`localStorage`/`sessionStorage`** | Via JS interop | Via JS interop (direct) |
| **Network Latency** | Higher (server round-trips) | Lower (client-side execution) |
| **CPU-Intensive Tasks** | On server | On client browser |

### Server-Only Code Example:

```razor
@inject IHttpContextAccessor HttpContextAccessor

@code {
    private string _userAgent = string.Empty;

    protected override void OnInitialized()
    {
        // This code will only run correctly in Blazor Server or during server prerendering
        _userAgent = HttpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString() ?? "Unknown";
    }
}
```

### WASM-Only Code Example:

```razor
@inject IJSRuntime JSRuntime

@code {
    private string _themePreference = string.Empty;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender && OperatingSystem.IsBrowser())
        {
            // This code is specifically for the browser environment
            _themePreference = await JSRuntime.InvokeAsync<string>("localStorage.getItem", "theme");
            StateHasChanged();
        }
    }
}
```

---

## Best Practices for Render Modes

### DO:

*   **Default to `InteractiveAuto`**: For most interactive components, `InteractiveAuto` provides the best balance of performance and user experience in a hybrid app.
*   **Use `InteractiveServer` for Server-Specific Needs**: Reserve `InteractiveServer` for components that genuinely require direct server-side access (e.g., `HttpContext`, file system).
*   **Use `InteractiveWebAssembly` for Offline/High Interactivity**: Leverage `InteractiveWebAssembly` for parts of the application that benefit from offline capabilities or extremely low interaction latency.
*   **Use Static SSR for Non-Interactive Content**: Optimize SEO and initial load times for purely static pages.
*   **Handle Prerendering Carefully**: Be aware of `OnInitializedAsync` running twice; move side effects to `OnAfterRenderAsync(firstRender && OperatingSystem.IsBrowser())` if they should only happen once on the client.
*   **Conditional Code Execution**: Use `OperatingSystem.IsBrowser()` or `OperatingSystem.IsOSPlatform("BROWSER")` to execute client-side specific code when needed.

### DON'T:

*   **Don't Overuse `InteractiveServer`**: Avoid using `InteractiveServer` unnecessarily due to its higher server resource consumption and potential latency.
*   **Don't Assume `HttpContext` Availability**: Components in `InteractiveAuto` or `InteractiveWebAssembly` modes cannot reliably access `HttpContext`. Use the BFF pattern or server-side services for such needs.
*   **Don't Put Server-Side Logic in WASM**: Business logic that requires server resources (database, external APIs directly) should remain on the server (API project) and be accessed via HTTP calls from WASM.

---

**Related Resources**:
- [component-design.md](component-design.md) - General Blazor component structure and lifecycle.
- [mudblazor-usage.md](mudblazor-usage.md) - UI framework-specific components.
- [state-management.md](state-management.md) - How render modes impact state management patterns.
- [`blazor-bff-patterns`](../../blazor-bff-patterns/SKILL.md) - Context on the Backend-for-Frontend architecture.
