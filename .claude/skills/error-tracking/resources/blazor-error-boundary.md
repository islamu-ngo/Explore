# Blazor Error Boundary

> **Project-Agnostic Error Boundary Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

The `ErrorBoundary` component in Blazor is a powerful tool for gracefully handling unhandled exceptions that occur during the rendering or lifecycle of its child components. Instead of crashing the entire UI, it allows you to display a fallback UI and optionally log the error.

---

## 1. `ErrorBoundary` Usage

Wrap any potentially error-prone content with an `ErrorBoundary` component. It provides two `RenderFragment` properties: `ChildContent` (the content to protect) and `ErrorContent` (the UI to display if an error occurs).

**File**: `{Project}.Blazor/Components/Pages/{Entities}.razor`

```razor
@page "/{entities}"
@inject ILogger<{Entities}> _logger

<MudText Typo="Typo.h4" Class="mb-4">{Entities} Overview</MudText>

@* Wrap the component that fetches and displays {entity} data *@
<ErrorBoundary>
    <ChildContent>
        @if (_{entities} == null)
        {
            <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
            <MudText Class="ml-2">Loading {entities}...</MudText>
        }
        else if (_{entities}.Any())
        {
            <MudGrid Spacing="3">
                @foreach (var item in _{entities})
                {
                    <MudItem xs="12" sm="6" md="4" lg="3">
                        <{Entity}Card {Entity}="@item" />
                    </MudItem>
                }
            </MudGrid>
        }
        else
        {
            <MudText>No {entities} found. Try creating one!</MudText>
        }
    </ChildContent>
    <ErrorContent Context="exception">
        <MudAlert Severity="Severity.Error" Variant="Variant.Filled" Class="mt-4">
            <MudText Typo="Typo.h6">An unexpected error occurred!</MudText>
            <MudText>We're sorry, but we couldn't load the {entity} data right now.</MudText>
            <MudText Class="mt-2">Error details: <i>@exception.Message</i></MudText>
            <MudButton OnClick="@(() => _errorBoundary?.Recover())" Color="Color.Warning" Class="mt-3">
                Try Reloading
            </MudButton>
        </MudAlert>
        @code {
            // ✅ Log the exception to your logging provider (e.g., Application Insights, Sentry)
            protected override void OnInitialized()
            {
                _logger.LogError(exception, "An unhandled UI error occurred in {Entity}List component.");
            }
        }
    </ErrorContent>
</ErrorBoundary>

@code {
    private List<{Entity}ListDto>? _{entities};
    private ErrorBoundary? _errorBoundary;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            _{entities} = await Http.GetFromJsonAsync<List<{Entity}ListDto>>("api/v1/{entity}");
        }
        catch (Exception ex)
        {
            // ✅ Log the exception here, but then re-throw it to let the ErrorBoundary catch it
            _logger.LogError(ex, "Failed to load {entities} in OnInitializedAsync.");
            throw; // IMPORTANT: Re-throw to propagate to the ErrorBoundary
        }
    }
}
```

---

## 2. Global Error Handling with `ErrorBoundary`

You can place an `ErrorBoundary` at a higher level, such as in `App.razor` or `MainLayout.razor`, to catch errors from a larger portion of your application.

**File**: `{Project}.Blazor/Components/App.razor`

```razor
<ErrorBoundary @ref="_errorBoundary">
    <ChildContent>
        <CascadingAuthenticationState>
            <Router AppAssembly="@typeof(App).Assembly">
                <Found Context="routeData">
                    <RouteView RouteData="@routeData" DefaultLayout="@typeof(MainLayout)" />
                    <FocusOnNavigate RouteData="@routeData" Selector="h1" />
                </Found>
                <NotFound>
                    <PageTitle>Not found</PageTitle>
                    <LayoutView Layout="@typeof(MainLayout)">
                        <p role="alert">Sorry, there's nothing at this address.</p>
                    </LayoutView>
                </NotFound>
            </Router>
        </CascadingAuthenticationState>
    </ChildContent>
    <ErrorContent Context="exception">
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="mud-theme-error pa-4 ma-2 rounded-lg">
            <MudText Typo="Typo.h5" Class="mb-2">Application Error</MudText>
            <MudText Class="mb-3">
                An unexpected error occurred. Please try refreshing the page or contact support.
            </MudText>
            <MudText Typo="Typo.body2" Color="Color.Dark" Class="mb-4">
                Error ID: @Guid.NewGuid().ToString() <br />
                Message: @exception.Message
            </MudText>
            <MudButton Variant="Variant.Filled" Color="Color.Warning" OnClick="@(() => _errorBoundary?.Recover())">
                Reload Page
            </MudButton>
        </MudContainer>
    </ErrorContent>
</ErrorBoundary>

@code {
    private ErrorBoundary? _errorBoundary;
}
```

---

## 3. Best Practices for `ErrorBoundary`

*   **Strategic Placement**: Place `ErrorBoundary` components at logical boundaries in your UI where you want to gracefully handle failures without affecting the entire application.
*   **Log Errors**: Always log the `exception` caught by the `ErrorContent` to your logging provider (`ILogger`, Sentry, Application Insights) to track and resolve issues.
*   **User-Friendly Messages**: Provide clear, concise, and helpful messages in the `ErrorContent` to the user, instructing them on possible next steps (e.g., "try again," "contact support"). Avoid exposing raw exception details to end-users.
*   **Recovery Option**: Offer a "Try Again" or "Reload" button that calls `ErrorBoundary.Recover()` to reset the boundary and re-render its `ChildContent`.
*   **Avoid Nested `ErrorBoundary`**: Do not nest `ErrorBoundary` components too deeply, as it can make debugging more complex.
*   **Propagate Exceptions**: If an error occurs during data fetching (`OnInitializedAsync`, `OnParametersSetAsync`), ensure you `throw` the exception after logging it so that the `ErrorBoundary` can catch it. If you `catch` and *don't* `throw`, the `ErrorBoundary` won't activate.

---

**Related Resources**:
- [api-exception-handling.md](api-exception-handling.md) - How API errors are handled.
- [mediatr-logging-behavior.md](mediatr-logging-behavior.md) - Logging errors in MediatR pipeline.
- [`blazor-ui-conventions`](../../blazor-ui-conventions/SKILL.md) - General Blazor UI guidelines.
