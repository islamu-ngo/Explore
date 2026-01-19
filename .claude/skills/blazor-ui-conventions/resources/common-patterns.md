# Common Blazor UI Patterns

> **Project-Agnostic UI Implementation Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

This document describes frequently used UI implementation patterns in Blazor applications, covering forms, dialogs, tables, navigation, loading states, error handling, search/filter functionalities, and infinite scroll. These patterns ensure consistency, reusability, and maintainability across the user interface.

---

## 1. Form Patterns

Forms are central to data input. They are typically built using MudBlazor components and integrated with validation.

### Basic Form with MudBlazor Components

```razor
@inject IMediator Mediator
@inject ISnackbar Snackbar
@inject NavigationManager NavigationManager

<MudCard>
    <MudCardHeader>
        <MudText Typo="Typo.h5">Create New {Entity}</MudText>
    </MudCardHeader>
    <MudCardContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="{Entity} Title"
                      Required="true"
                      RequiredError="Title is required"
                      MaxLength="200"
                      Class="mb-4" />

        <MudTextField @bind-Value="_dto.Description"
                      Label="Description"
                      Lines="5"
                      MaxLength="500"
                      Counter="500"
                      Class="mb-4" />

        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       Required="true"
                       MinDate="DateTime.Today"
                       Class="mb-4" />

        <MudSelect @bind-Value="_dto.{LookupEntity}Id"
                   Label="{LookupEntity}"
                   Required="true"
                   RequiredError="{LookupEntity} is required"
                   Class="mb-4">
            @foreach (var item in _{lookupEntities})
            {
                <MudSelectItem Value="@item.Id">@item.FullName</MudSelectItem>
            }
        </MudSelect>
    </MudCardContent>
    <MudCardActions Class="d-flex justify-end">
        <MudButton OnClick="Cancel" Variant="Variant.Text">Cancel</MudButton>
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   OnClick="Submit"
                   Disabled="_isSubmitting"
                   Class="ml-2">
            @if (_isSubmitting)
            {
                <MudProgressCircular Size="Size.Small" Indeterminate="true" Class="mr-2" />
                <MudText>Creating...</MudText>
            }
            else
            {
                <text>Create {Entity}</text>
            }
        </MudButton>
    </MudCardActions>
</MudCard>

@code {
    private Create{Entity}Dto _dto = new();
    private DateTime? _startDate;
    private bool _isSubmitting;
    private List<{LookupEntity}Dto> _{lookupEntities} = new();

    protected override async Task OnInitializedAsync()
    {
        // Load dropdown data
        // _{lookupEntities} = await Mediator.Send(new Get{LookupEntity}ListRequest());
    }

    private async Task Submit()
    {
        // Basic client-side validation check
        if (!_startDate.HasValue)
        {
            Snackbar.Add("Start date is required", Severity.Error);
            return;
        }

        _dto.FirstSessionDate = DateOnly.FromDateTime(_startDate.Value);
        _isSubmitting = true;

        try
        {
            var command = new Create{Entity}Command { {Entity}Dto = _dto };
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                Snackbar.Add("{Entity} created successfully", Severity.Success);
                NavigationManager.NavigateTo($"/{entities}/{result.Id}");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    Snackbar.Add(error, Severity.Error);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {entity}.");
            Snackbar.Add("An unexpected error occurred.", Severity.Error);
        }
        finally
        {
            _isSubmitting = false;
        }
    }

    private void Cancel()
    {
        NavigationManager.NavigateTo("/{entities}");
    }
}
```

### Form with FluentValidation Integration

For more complex validation rules, especially those involving cross-field or asynchronous checks, FluentValidation is integrated with Blazor's `EditForm`.

```razor
@using FluentValidation
@using Microsoft.AspNetCore.Components.Forms

<EditForm Model="@_dto" OnValidSubmit="HandleValidSubmit" OnInvalidSubmit="HandleInvalidSubmit">
    <FluentValidationValidator @ref="_validator" />
    <ValidationSummary />

    <MudCard>
        <MudCardContent>
            <MudTextField @bind-Value="_dto.Title"
                          For="@(() => _dto.Title)"
                          Label="{Entity} Title"
                          Immediate="true" />

            <MudTextField @bind-Value="_dto.Email"
                          For="@(() => _dto.Email)"
                          Label="Contact Email"
                          Immediate="true" />
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
    private Create{Entity}Dto _dto = new();
    private FluentValidationValidator? _validator;

    private async Task HandleValidSubmit()
    {
        var command = new Create{Entity}Command { {Entity}Dto = _dto };
        await Mediator.Send(command);
        Snackbar.Add("Form submitted successfully!", Severity.Success);
    }

    private void HandleInvalidSubmit()
    {
        Snackbar.Add("Please correct the form errors.", Severity.Error);
    }
}
```

---

## 2. Dialog Patterns

MudDialog is used for modal interactions, from simple confirmations to complex data entry forms.

### Confirmation Dialog (`ConfirmDialog.razor`)

A reusable component for displaying a confirmation message with customizable text and button styling.

```razor
@* ConfirmDialog.razor *@
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

**Usage (Opening the Dialog)**:
```razor
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<MudButton OnClick="Delete{Entity}">Delete {Entity}</MudButton>

@code {
    private async Task Delete{Entity}()
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "Are you sure you want to delete this {entity}? This action cannot be undone.",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialogOptions = new DialogOptions { MaxWidth = MaxWidth.ExtraSmall, FullWidth = true };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>(
            "Confirm Deletion",
            parameters,
            dialogOptions);

        var result = await dialog.Result;

        if (!result.Canceled && (bool)(result.Data ?? false))
        {
            Snackbar.Add("{Entity} deleted successfully", Severity.Success);
            // ... Call backend to delete ...
        }
        else
        {
            Snackbar.Add("Deletion cancelled", Severity.Info);
        }
    }
}
```

### Form Dialog (`Create{Entity}Dialog.razor`)

Embedding a form within a dialog for data input.

```razor
@* Create{Entity}Dialog.razor *@
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Create New {Entity}</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="{Entity} Title"
                      Required="true" />
        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       MinDate="DateTime.Today" />
        @* ... other form fields ... *@
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

    private Create{Entity}Dto _dto = new();
    private DateTime? _startDate;
    private bool _isSubmitting;

    private void Cancel() => MudDialog.Cancel();

    private async Task Submit()
    {
        if (!_startDate.HasValue) { Snackbar.Add("Start date is required", Severity.Error); return; }
        _dto.FirstSessionDate = DateOnly.FromDateTime(_startDate.Value);
        _isSubmitting = true;

        try
        {
            var command = new Create{Entity}Command { {Entity}Dto = _dto };
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                Snackbar.Add("{Entity} created successfully", Severity.Success);
                MudDialog.Close(DialogResult.Ok(result.Id)); // ✅ Close with the new entity ID
            }
            else
            {
                foreach (var error in result.Errors) Snackbar.Add(error, Severity.Error);
            }
        }
        finally { _isSubmitting = false; }
    }
}
```

---

## 3. Table Patterns

`MudTable` is the primary component for displaying tabular data, offering features like sorting, pagination, and filtering.

### CRUD Table

A common pattern for displaying a list of entities with actions for viewing, editing, and deleting.

```razor
@inject IMediator Mediator
@inject IDialogService DialogService
@inject ISnackbar Snackbar
@inject NavigationManager NavigationManager

<MudTable Items="@_{entities}" Hover="true" Loading="@_isLoading" LoadingProgressColor="Color.Info">
    <ToolBarContent>
        <MudText Typo="Typo.h6">{Entities} List</MudText>
        <MudSpacer />
        <MudButton Variant="Variant.Filled"
                   Color="Color.Primary"
                   StartIcon="@Icons.Material.Filled.Add"
                   OnClick="CreateNew{Entity}">
            Create {Entity}
        </MudButton>
    </ToolBarContent>
    <HeaderContent>
        <MudTh>Title</MudTh>
        <MudTh>Date</MudTh>
        <MudTh>Location</MudTh>
        <MudTh>Status</MudTh>
        <MudTh Style="width: 150px">Actions</MudTh>
    </HeaderContent>
    <RowTemplate>
        <MudTd DataLabel="Title">@context.Title</MudTd>
        <MudTd DataLabel="Date">@context.FirstSessionDate?.ToShortDateString()</MudTd>
        <MudTd DataLabel="Location">@context.LocationFullName</MudTd>
        <MudTd DataLabel="Status">
            <MudChip Size="Size.Small"
                     Color="@GetStatusColor(context.StatusFullName)">
                @context.StatusFullName
            </MudChip>
        </MudTd>
        <MudTd DataLabel="Actions">
            <MudIconButton Icon="@Icons.Material.Filled.Visibility"
                           Size="Size.Small"
                           OnClick="@(() => View{Entity}(context.Id))" />
            <MudIconButton Icon="@Icons.Material.Filled.Edit"
                           Size="Size.Small"
                           Color="Color.Primary"
                           OnClick="@(() => Edit{Entity}(context.Id))" />
            <MudIconButton Icon="@Icons.Material.Filled.Delete"
                           Size="Size.Small"
                           Color="Color.Error"
                           OnClick="@(() => Delete{Entity}(context.Id))" />
        </MudTd>
    </RowTemplate>
    <PagerContent>
        <MudTablePager />
    </PagerContent>
</MudTable>

@code {
    private List<{Entity}ListDto> _{entities} = new();
    private bool _isLoading;

    protected override async Task OnInitializedAsync()
    {
        await Load{Entities}();
    }

    private async Task Load{Entities}()
    {
        _isLoading = true;
        try
        {
            _{entities} = await Mediator.Send(new Get{Entity}ListRequest());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load {entities}.");
            Snackbar.Add("Failed to load {entities}.", Severity.Error);
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

    private async Task CreateNew{Entity}()
    {
        var dialog = await DialogService.ShowAsync<Create{Entity}Dialog>("Create {Entity}");
        var result = await dialog.Result;

        if (!result.Canceled && result.Data is {IdType} {entity}Id)
        {
            Snackbar.Add($"{Entity} created with ID: {{entity}Id}", Severity.Success);
            await Load{Entities}();
        }
    }

    private void View{Entity}({IdType} id) => NavigationManager.NavigateTo($"/{entities}/{id}");
    private void Edit{Entity}({IdType} id) => NavigationManager.NavigateTo($"/{entities}/{id}/edit");

    private async Task Delete{Entity}({IdType} id)
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "Are you sure you want to delete this {entity}? This action is irreversible.",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>("Confirm Deletion", parameters);
        var result = await dialog.Result;

        if (!result.Canceled && (bool)(result.Data ?? false))
        {
            var deleteCommand = new Delete{Entity}Command { Id = id };
            var deleteResult = await Mediator.Send(deleteCommand);

            if (deleteResult)
            {
                Snackbar.Add("{Entity} deleted successfully", Severity.Success);
                await Load{Entities}();
            }
            else
            {
                Snackbar.Add("Failed to delete {entity}.", Severity.Error);
            }
        }
    }
}
```

---

## 4. Navigation Patterns

### Programmatic Navigation

Using `NavigationManager` for dynamic page routing.

```razor
@inject NavigationManager NavigationManager

@code {
    private void NavigateTo{Entity}Details({IdType} {entity}Id)
    {
        NavigationManager.NavigateTo($"/{entities}/{{entity}Id}");
    }

    private void NavigateTo{Entities}List()
    {
        NavigationManager.NavigateTo("/{entities}");
    }

    private void OpenExternalLink(string url)
    {
        NavigationManager.NavigateTo(url, forceLoad: true);
    }
}
```

### Navigation with Query Parameters

For filtering, sorting, or complex state that needs to be reflected in the URL.

```razor
@inject NavigationManager NavigationManager
@using Microsoft.AspNetCore.WebUtilities

@code {
    private {IdType}? _{parentEntity}IdFilter;
    private {LookupIdType}? _{lookupEntity}Filter;

    protected override void OnInitialized()
    {
        var uri = new Uri(NavigationManager.Uri);
        var query = QueryHelpers.ParseQuery(uri.Query);

        if (query.TryGetValue("{parentEntity}Id", out var parentIdValue) && {IdType}.TryParse(parentIdValue, out var parsedParentId))
        {
            _{parentEntity}IdFilter = parsedParentId;
        }
        if (query.TryGetValue("{lookupEntity}Id", out var lookupValue) && {LookupIdType}.TryParse(lookupValue, out var parsedLookup))
        {
            _{lookupEntity}Filter = parsedLookup;
        }
    }

    private void ApplyFiltersAndNavigate()
    {
        var queryParams = new Dictionary<string, object?>
        {
            ["{parentEntity}Id"] = _{parentEntity}IdFilter,
            ["{lookupEntity}Id"] = _{lookupEntity}Filter,
            ["page"] = 1
        };

        var uri = NavigationManager.GetUriWithQueryParameters(queryParams);
        NavigationManager.NavigateTo(uri);
    }
}
```

---

## 5. Loading State Patterns

Providing visual feedback during data loading is crucial for good UX.

### Simple Loading Indicator

```razor
@if (_isLoading)
{
    <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
    <MudText Class="ml-2">Loading {entities}...</MudText>
}
else if (_{entities}.Any())
{
    @* Display {entities} *@
}
else
{
    <MudText>No {entities} found matching your criteria.</MudText>
}

@code {
    private bool _isLoading;
    private List<{Entity}ListDto> _{entities} = new();
}
```

### Skeleton Loading

Mimics the layout of the content that is about to be loaded, providing a better perceived performance.

```razor
@if (_isLoading)
{
    <MudGrid>
        @for (int i = 0; i < 6; i++)
        {
            <MudItem xs="12" sm="6" md="4" lg="3">
                <MudCard Elevation="1">
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="200px" Animation="Animation.Pulse" />
                    <MudCardContent>
                        <MudSkeleton Width="80%" Height="30px" />
                        <MudSkeleton Width="60%" Height="20px" Class="mt-2" />
                    </MudCardContent>
                </MudCard>
            </MudItem>
        }
    </MudGrid>
}
else
{
    @* Actual {entity} cards *@
}
```

### Progress Overlay

Useful for full-screen loading or when waiting for an action to complete.

```razor
<MudOverlay Visible="_isLoading" DarkBackground="true" Absolute="true" ZIndex="MudBlazor.Defaults.ZIndex.Snackbar">
    <MudProgressCircular Indeterminate="true" Size="Size.Large" />
</MudOverlay>

<MudContainer>
    @* Page content here, will be overlaid when loading *@
</MudContainer>

@code {
    private bool _isLoading;
}
```

---

## 6. Error Handling Patterns

Robust error handling improves user experience and helps with debugging.

### Try-Catch with User Feedback

Wrap API calls or critical operations in `try-catch` blocks to handle exceptions gracefully and provide feedback to the user via `ISnackbar`.

```razor
@inject ISnackbar Snackbar
@inject ILogger<MyComponent> _logger

@code {
    private async Task LoadData()
    {
        _isLoading = true;
        try
        {
            _{entities} = await Mediator.Send(new Get{Entity}ListRequest());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP Request failed during {entity} load.");
            Snackbar.Add("Network error. Please check your connection or try again.", Severity.Error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unexpected error occurred during {entity} load.");
            Snackbar.Add("An unexpected error occurred. Please try again later.", Severity.Error);
        }
        finally
        {
            _isLoading = false;
        }
    }
}
```

### Blazor `ErrorBoundary` Component

The `ErrorBoundary` component catches unhandled exceptions in its child content and renders fallback UI.

```razor
<ErrorBoundary>
    <ChildContent>
        <{Entity}List />
    </ChildContent>
    <ErrorContent Context="exception">
        <MudAlert Severity="Severity.Error" Variant="Variant.Filled" Class="mt-4">
            <MudText Typo="Typo.h6">Something went wrong!</MudText>
            <MudText>An error occurred while rendering the {entity} list.</MudText>
            <MudText Class="mt-2">Error details: @exception.Message</MudText>
            <MudButton OnClick="@(() => exception.Recover())" Color="Color.Warning" Class="mt-3">
                Try Again
            </MudButton>
        </MudAlert>
    </ErrorContent>
</ErrorBoundary>
```

### Backend Command Result Handling

For command (write) operations, the backend returns a `BaseCommandResponse` which contains `Success`, `Message`, `Id`, and a list of `Errors`. This allows for structured error reporting.

```razor
@code {
    private async Task CreateNew{Entity}()
    {
        var command = new Create{Entity}Command { {Entity}Dto = _dto };
        var result = await Mediator.Send(command);

        if (result.Success)
        {
            Snackbar.Add("{Entity} created successfully", Severity.Success);
            NavigationManager.NavigateTo($"/{entities}/{result.Id}");
        }
        else
        {
            foreach (var error in result.Errors)
            {
                Snackbar.Add(error, Severity.Error);
            }
            if (!string.IsNullOrEmpty(result.Message))
            {
                Snackbar.Add(result.Message, Severity.Warning);
            }
        }
    }
}
```

---

## 7. Search and Filter Patterns

Providing intuitive ways for users to find and narrow down data.

### Debounced Search Input

Prevents excessive updates or API calls by waiting for a pause in user typing.

```razor
<MudTextField @bind-Value="_searchTerm"
              Label="Search {Entities}"
              Immediate="true"
              DebounceInterval="300"
              OnDebounceIntervalElapsed="OnSearch"
              Adornment="Adornment.End"
              AdornmentIcon="@Icons.Material.Filled.Search" />

<MudGrid>
    @foreach (var item in _filtered{Entities})
    {
        <MudItem xs="12" md="6" lg="4">
            <{Entity}Card {Entity}="@item" />
        </MudItem>
    }
</MudGrid>

@code {
    private string _searchTerm = string.Empty;
    private List<{Entity}ListDto> _{entities} = new();
    private List<{Entity}ListDto> _filtered{Entities} = new();

    protected override async Task OnInitializedAsync()
    {
        _{entities} = await Mediator.Send(new Get{Entity}ListRequest());
        _filtered{Entities} = _{entities};
    }

    private void OnSearch()
    {
        if (string.IsNullOrWhiteSpace(_searchTerm))
        {
            _filtered{Entities} = _{entities};
        }
        else
        {
            _filtered{Entities} = _{entities}
                .Where(e => e.Title.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ||
                           (e.Description?.Contains(_searchTerm, StringComparison.OrdinalIgnoreCase) ?? false))
                .ToList();
        }
    }
}
```

### Filter Panel

A dedicated section for applying multiple filter criteria.

```razor
<MudGrid>
    <MudItem xs="12" md="3">
        <MudPaper Class="pa-4">
            <MudText Typo="Typo.h6" Class="mb-4">Filters</MudText>

            <MudSelect @bind-Value="_filters.{ParentEntity}Id"
                       Label="{ParentEntity}"
                       Clearable="true"
                       OnClearButtonClick="Clear{ParentEntity}"
                       Class="mb-3">
                @foreach (var item in _{parentEntities})
                {
                    <MudSelectItem Value="@item.Id">@item.FullName</MudSelectItem>
                }
            </MudSelect>

            <MudSelect @bind-Value="_filters.{LookupEntity}Id"
                       Label="{LookupEntity}"
                       Clearable="true"
                       Class="mb-4">
                @foreach (var item in _{lookupEntities})
                {
                    <MudSelectItem Value="@item.Id">@item.FullName</MudSelectItem>
                }
            </MudSelect>

            <MudButton Variant="Variant.Filled"
                       Color="Color.Primary"
                       FullWidth="true"
                       OnClick="ApplyFilters"
                       Class="mb-2">
                Apply Filters
            </MudButton>

            <MudButton Variant="Variant.Text"
                       FullWidth="true"
                       OnClick="ClearAllFilters">
                Clear All
            </MudButton>
        </MudPaper>
    </MudItem>

    <MudItem xs="12" md="9">
        <MudGrid>
            @foreach (var item in _filtered{Entities})
            {
                <MudItem xs="12" sm="6" lg="4">
                    <{Entity}Card {Entity}="@item" />
                </MudItem>
            }
        </MudGrid>
    </MudItem>
</MudGrid>

@code {
    private {Entity}FilterDto _filters = new();
    private List<{Entity}ListDto> _filtered{Entities} = new();
    private List<{ParentEntity}Dto> _{parentEntities} = new();
    private List<{LookupEntity}Dto> _{lookupEntities} = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadFilterOptions();
        await ApplyFilters();
    }

    private async Task LoadFilterOptions() { /* ... */ }

    private async Task ApplyFilters()
    {
        var request = new Get{Entity}ListRequest
        {
            {ParentEntity}Id = _filters.{ParentEntity}Id,
            {LookupEntity}Id = _filters.{LookupEntity}Id,
        };

        _filtered{Entities} = await Mediator.Send(request);
    }

    private async Task ClearAllFilters()
    {
        _filters = new {Entity}FilterDto();
        await ApplyFilters();
    }

    private void Clear{ParentEntity}()
    {
        _filters.{ParentEntity}Id = null;
    }
}
```

---

## 8. Infinite Scroll Pattern

Loads more data as the user scrolls to the bottom of a list, improving performance for very long lists.

```razor
@inject IJSRuntime JS

<div @ref="_scrollContainer" style="height: 600px; overflow-y: auto;">
    <MudGrid>
        @foreach (var item in _{entities})
        {
            <MudItem xs="12" md="6" lg="4">
                <{Entity}Card {Entity}="@item" />
            </MudItem>
        }
    </MudGrid>

    @if (_hasMore)
    {
        <div @ref="_loadMoreTrigger" class="d-flex justify-center py-4">
            <MudProgressCircular Indeterminate="true" Color="Color.Primary" />
            <MudText Class="ml-2">Loading more...</MudText>
        </div>
    }
    else if (_{entities}.Any())
    {
        <div class="d-flex justify-center py-4">
            <MudText Color="Color.Secondary">No more {entities} to load.</MudText>
        </div>
    }
</div>

@code {
    private ElementReference _scrollContainer;
    private ElementReference _loadMoreTrigger;
    private List<{Entity}ListDto> _{entities} = new();
    private int _currentPage = 1;
    private bool _hasMore = true;
    private bool _isLoading = false;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await JS.InvokeVoidAsync("app.registerIntersectionObserver", _loadMoreTrigger, DotNetObjectReference.Create(this), nameof(LoadMore));
            await LoadMore{Entities}();
        }
    }

    [JSInvokable]
    public async Task LoadMore()
    {
        if (_isLoading || !_hasMore) return;

        _isLoading = true;
        _currentPage++;

        await LoadMore{Entities}();

        _isLoading = false;
        StateHasChanged();
    }

    private async Task LoadMore{Entities}()
    {
        var request = new Get{Entity}ListRequest
        {
            Page = _currentPage,
            PageSize = 12
        };

        var result = await Mediator.Send(request);

        if (result?.{Entities} != null && result.{Entities}.Any())
        {
            _{entities}.AddRange(result.{Entities});
            _hasMore = result.HasMore;
        }
        else
        {
            _hasMore = false;
        }
    }

    public void Dispose()
    {
        JS.InvokeVoidAsync("app.disposeIntersectionObserver", _loadMoreTrigger);
    }
}
```

**`wwwroot/js/site.js` (for Intersection Observer)**:
```javascript
window.app = {
    _intersectionObservers: new Map(),

    registerIntersectionObserver: (element, dotnetHelper, methodName) => {
        if (!element) {
            console.warn("Intersection Observer: Element not found.");
            return;
        }

        const options = {
            root: element.parentElement,
            rootMargin: '0px',
            threshold: 0.1
        };

        const observer = new IntersectionObserver(entries => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    dotnetHelper.invokeMethodAsync(methodName);
                }
            });
        }, options);

        observer.observe(element);
        app._intersectionObservers.set(element, observer);
    },

    disposeIntersectionObserver: (element) => {
        if (app._intersectionObservers.has(element)) {
            const observer = app._intersectionObservers.get(element);
            observer.unobserve(element);
            observer.disconnect();
            app._intersectionObservers.delete(element);
        }
    }
};
```

---

## 9. Pagination Patterns

Client-side pagination using `MudPagination` for filtered data with page info display.

### Client-Side Pagination with MudPagination

```razor
<MudContainer>
    <!-- Filters Section -->
    <div class="filters mb-4">
        <MudTextField @bind-Value="searchText"
                      Placeholder="Search"
                      Immediate="true"
                      DebounceInterval="500"
                      TextChanged="OnSearch" />
    </div>

    <!-- Data Grid -->
    <MudGrid>
        @if (isLoading)
        {
            @for (int i = 0; i < 6; i++)
            {
                <MudItem xs="12" sm="6" md="4">
                    <MudSkeleton SkeletonType="SkeletonType.Rectangle" Height="200px" />
                </MudItem>
            }
        }
        else if (!Filtered{Entities}.Any())
        {
            <MudItem xs="12" Class="d-flex justify-center align-center pa-8">
                <div class="text-center">
                    <MudIcon Icon="@Icons.Material.Filled.SearchOff" Size="Size.Large" Color="Color.Secondary" />
                    <MudText Typo="Typo.h6" Color="Color.Secondary">No {entities} found</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary" Class="mt-2">
                        Try adjusting your filters or search query
                    </MudText>
                </div>
            </MudItem>
        }
        else
        {
            @foreach (var item in Filtered{Entities})
            {
                <MudItem xs="12" sm="6" md="4">
                    <{Entity}Card {Entity}="@item" />
                </MudItem>
            }
        }
    </MudGrid>

    <!-- Pagination Controls -->
    <div class="pagination-wrapper d-flex justify-center mt-4">
        <MudPagination Count="@TotalPages"
                       Selected="@currentPage"
                       SelectedChanged="@((int page) => OnPageChanged(page))"
                       Color="Color.Primary"
                       ShowFirstButton="true"
                       ShowLastButton="true" />
    </div>

    <!-- Results Info -->
    <div class="results-info d-flex justify-center mt-2">
        <MudText Typo="Typo.body2" Color="Color.Surface">
            Showing @((currentPage - 1) * itemsPerPage + 1) - @(Math.Min(currentPage * itemsPerPage, AllFiltered{Entities}.Count)) of @AllFiltered{Entities}.Count {entities} (Page @currentPage of @TotalPages)
        </MudText>
    </div>
</MudContainer>

@code {
    private int currentPage = 1;
    private int itemsPerPage = 6;
    private string searchText = "";
    private {IdType}? selectedCategoryId;
    private bool isLoading = true;
    private ICollection<{Entity}ListDto> all{Entities} = new List<{Entity}ListDto>();

    /// <summary>
    /// All {entities} after applying filters (before pagination).
    /// </summary>
    private List<{Entity}ListDto> AllFiltered{Entities}
    {
        get
        {
            var filtered{Entities} = all{Entities}.AsEnumerable();

            if (!string.IsNullOrEmpty(searchText))
            {
                filtered{Entities} = filtered{Entities}.Where(e =>
                    e.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (e.Description?.Contains(searchText, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            if (selectedCategoryId.HasValue)
            {
                filtered{Entities} = filtered{Entities}.Where(e => e.CategoryId == selectedCategoryId.Value);
            }

            return filtered{Entities}.ToList();
        }
    }

    /// <summary>
    /// Paginated subset of filtered {entities} for current page display.
    /// </summary>
    private List<{Entity}ListDto> Filtered{Entities}
    {
        get
        {
            return AllFiltered{Entities}
                .Skip((currentPage - 1) * itemsPerPage)
                .Take(itemsPerPage)
                .ToList();
        }
    }

    private int TotalPages
    {
        get
        {
            var count = AllFiltered{Entities}.Count;
            return count > 0 ? (int)Math.Ceiling((double)count / itemsPerPage) : 1;
        }
    }

    private void OnPageChanged(int page)
    {
        currentPage = page;
    }

    /// <summary>
    /// CRITICAL: Always reset currentPage = 1 when any filter changes.
    /// </summary>
    private void OnSearch(string value)
    {
        searchText = value;
        currentPage = 1; // ✅ Reset pagination on filter change
    }

    private void OnCategoryChanged({IdType}? categoryId)
    {
        selectedCategoryId = categoryId;
        currentPage = 1; // ✅ Reset pagination on filter change
    }
}
```

### Key Pagination Patterns

| Pattern | Description |
|---------|-------------|
| `AllFiltered{Entities}` | Property that applies all filters but NO pagination |
| `Filtered{Entities}` | Property that applies Skip/Take for current page |
| `TotalPages` | Computed from `AllFiltered{Entities}.Count / itemsPerPage` |
| `currentPage = 1` | **Always reset** when any filter changes |

### MudPagination Component Properties

```razor
<MudPagination
    Count="@TotalPages"               @* Total number of pages *@
    Selected="@currentPage"           @* Current selected page (1-indexed) *@
    SelectedChanged="OnPageChanged"   @* Event callback when page changes *@
    Color="Color.Primary"             @* Button color *@
    ShowFirstButton="true"            @* Show |< button *@
    ShowLastButton="true"             @* Show >| button *@
    ShowPreviousButton="true"         @* Show < button (default true) *@
    ShowNextButton="true"             @* Show > button (default true) *@
    BoundaryCount="1"                 @* Pages shown at start/end *@
    MiddleCount="3"                   @* Pages shown around current *@
/>
```

### Results Info Pattern

Always show pagination context to users:

```razor
<MudText Typo="Typo.body2">
    Showing @StartItem - @EndItem of @TotalItems results
</MudText>

@code {
    private int StartItem => (currentPage - 1) * itemsPerPage + 1;
    private int EndItem => Math.Min(currentPage * itemsPerPage, AllFiltered{Entities}.Count);
    private int TotalItems => AllFiltered{Entities}.Count;
}
```

### Server-Side Pagination (Alternative)

For large datasets, use server-side pagination with `PaginatedResult<T>`:

```razor
@code {
    private PaginatedResult<{Entity}ListDto>? _pagedResult;
    private int _currentPage = 1;
    private int _pageSize = 20;

    protected override async Task OnInitializedAsync()
    {
        await LoadPageAsync(_currentPage);
    }

    private async Task LoadPageAsync(int page)
    {
        isLoading = true;
        try
        {
            _pagedResult = await {Entity}Service.Get{Entities}PagedAsync(page, _pageSize);
            _currentPage = page;
        }
        finally
        {
            isLoading = false;
        }
    }

    private async Task OnPageChanged(int page)
    {
        await LoadPageAsync(page);
    }

    private int TotalPages => _pagedResult?.TotalPages ?? 1;
    private IEnumerable<{Entity}ListDto> Items => _pagedResult?.Items ?? Enumerable.Empty<{Entity}ListDto>();
}
```

---

**Related Resources**:
- [component-design.md](component-design.md) - Component lifecycle and communication.
- [mudblazor-usage.md](mudblazor-usage.md) - Specific MudBlazor component usage.
- [state-management.md](state-management.md) - How to manage state for these patterns.
