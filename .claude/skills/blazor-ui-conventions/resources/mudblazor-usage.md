# MudBlazor Usage - Components and Best Practices

> **Project-Agnostic MudBlazor Patterns**
>
> Placeholders use `{Placeholder}` syntax - see [docs/TEMPLATE_GLOSSARY.md](../../../../docs/TEMPLATE_GLOSSARY.md).

This document provides a comprehensive guide to commonly used MudBlazor components in Blazor applications, along with best practices for their implementation.

---

## 1. MudGrid & MudItem - Responsive Layouts

MudBlazor uses a powerful 12-column grid system that facilitates responsive layouts across various screen sizes.

### Breakpoints

| Breakpoint | Size | Screen Width | Typical Device |
|------------|------|--------------|----------------|
| `xs` | Extra small | 0-599px | Mobile phones (portrait) |
| `sm` | Small | 600-959px | Tablets (portrait) |
| `md` | Medium | 960-1279px | Tablets (landscape), small laptops |
| `lg` | Large | 1280-1919px | Laptops, desktop monitors |
| `xl` | Extra large | 1920px+ | Large desktop monitors |

### Basic Grid Structure

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

### {Entity} Card Grid Example

```razor
<MudGrid Spacing="3"> @* `Spacing` property controls gutter size (0-10) *@
    @foreach (var item in _{entities})
    {
        <MudItem xs="12" sm="6" md="4" lg="3"> @* Responsive sizing: 1 column on mobile, 2 on small, 3 on medium, 4 on large *@
            <MudCard Elevation="2"> @* Add elevation for a subtle shadow effect *@
                <MudCardMedia Image="@item.CoverImageUrl" Height="200" />
                <MudCardContent>
                    <MudText Typo="Typo.h6">@item.Title</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary">
                        @item.StartDate.ToShortDateString()
                    </MudText>
                    <MudText Typo="Typo.body2">@item.Location</MudText>
                </MudCardContent>
                <MudCardActions Class="d-flex justify-end"> @* Align actions to the end *@
                    <MudButton Variant="Variant.Text"
                               Color="Color.Primary"
                               OnClick="@(() => ViewDetails(item.Id))">
                        View Details
                    </MudButton>
                </MudCardActions>
            </MudCard>
        </MudItem>
    }
</MudGrid>
```

---

## 2. MudButton - Action Buttons

MudButton offers various styles, colors, and functionalities for user interaction.

### Button Variants

```razor
@* Filled (Default - has elevation and solid background) *@
<MudButton Variant="Variant.Filled" Color="Color.Primary" OnClick="Create{Entity}">
    Create {Entity}
</MudButton>

@* Outlined (border, transparent background) *@
<MudButton Variant="Variant.Outlined" Color="Color.Primary" OnClick="Edit">
    Edit
</MudButton>

@* Text (minimal styling, no border, no background) *@
<MudButton Variant="Variant.Text" Color="Color.Primary" OnClick="Cancel">
    Cancel
</MudButton>

@* Disabled state *@
<MudButton Variant="Variant.Filled" Disabled="true">
    Disabled
</MudButton>
```

### Button Colors

Utilize the predefined color palette for consistent branding.

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

### Buttons with Icons

```razor
@using MudBlazor.Icons

<MudButton Variant="Variant.Filled"
           Color="Color.Primary"
           StartIcon="@Filled.Add" @* Use Material Design Icons *@
           OnClick="Create{Entity}">
    Create {Entity}
</MudButton>

<MudButton Variant="Variant.Outlined"
           EndIcon="@Filled.ArrowForward">
    Next
</MudButton>
```

### MudIconButton - Icon-only Buttons

```razor
<MudIconButton Icon="@Filled.Edit"
               Color="Color.Primary"
               Size="Size.Small"
               OnClick="@(() => Edit({entity}Id))" />

<MudIconButton Icon="@Filled.Delete"
               Color="Color.Error"
               Variant="Variant.Filled" @* Can also have variants *@
               Size="Size.Medium" />
```

### Button Sizes

```razor
<MudButton Size="Size.Small">Small</MudButton>
<MudButton Size="Size.Medium">Medium</MudButton> @* Default size *@
<MudButton Size="Size.Large">Large</MudButton>
```

---

## 3. MudTextField - Text Input

MudTextField is the standard component for single and multi-line text input.

### Basic TextField

```razor
<MudTextField @bind-Value="title"
              Label="{Entity} Title"
              Variant="Variant.Outlined"
              Placeholder="e.g., Sample {Entity} Title" />

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

MudBlazor integrates seamlessly with ASP.NET Core's validation attributes and FluentValidation.

```razor
@using System.ComponentModel.DataAnnotations

<MudTextField @bind-Value="email"
              Label="Email"
              Required="true" @* Marks field as required for browser validation *@
              RequiredError="Email is required"
              Validation="@(new EmailAddressAttribute())" @* Example of data annotation validation *@
              HelperText="Enter a valid email address" />

<MudTextField @bind-Value="description"
              Label="Description"
              Lines="5" @* Multi-line input *@
              MaxLength="500" @* Browser-level max length *@
              Counter="500" @* Displays character count *@
              HelperText="Max 500 characters" />

@code {
    private string email = string.Empty;
    private string description = string.Empty;
}
```

### TextField with Adornments

Adornments enhance user experience by adding icons or text prefixes/suffixes.

```razor
<MudTextField @bind-Value="price"
              Label="Price"
              Adornment="Adornment.Start" @* Adornment at the start *@
              AdornmentIcon="@Filled.AttachMoney"
              AdornmentText="USD" />

<MudTextField @bind-Value="capacity"
              Label="Capacity"
              Adornment="Adornment.End" @* Adornment at the end *@
              AdornmentText="people" />

<MudTextField @bind-Value="password"
              Label="Password"
              InputType="@_passwordInputType"
              Adornment="Adornment.End"
              AdornmentIcon="@_passwordIcon"
              OnAdornmentClick="TogglePasswordVisibility" /> @* Toggle password visibility *@

@code {
    private decimal price;
    private int capacity;
    private string password = string.Empty;
    private InputType _passwordInputType = InputType.Password;
    private string _passwordIcon = Filled.VisibilityOff;

    private void TogglePasswordVisibility()
    {
        if (_passwordInputType == InputType.Password)
        {
            _passwordInputType = InputType.Text;
            _passwordIcon = Filled.Visibility;
        }
        else
        {
            _passwordInputType = InputType.Password;
            _passwordIcon = Filled.VisibilityOff;
        }
    }
}
```

### TextField with Debounce

Useful for search inputs to prevent excessive API calls.

```razor
<MudTextField @bind-Value="searchTerm"
              Label="Search {Entities}"
              Immediate="true" @* Triggers OnDebounceIntervalElapsed immediately on first input *@
              DebounceInterval="300" @* Wait 300ms after last keystroke *@
              OnDebounceIntervalElapsed="OnSearchChanged"
              Adornment="Adornment.End"
              AdornmentIcon="@Filled.Search" />

@code {
    private string searchTerm = string.Empty;

    private async Task OnSearchChanged()
    {
        // Search logic executes 300ms after user stops typing
        await Load{Entities}(searchTerm);
    }
}
```

---

## 4. MudSelect - Dropdown Selection

The standard component for single and multiple item selection from a list.

### Basic Select

```razor
<MudSelect @bind-Value="selected{LookupEntity}Id"
           Label="{LookupEntity}"
           Required="true"
           RequiredError="{LookupEntity} is required">
    @foreach (var item in _{lookupEntities})
    {
        <MudSelectItem Value="@item.Id">@item.FullName</MudSelectItem>
    }
</MudSelect>

@code {
    private {LookupIdType} selected{LookupEntity}Id;
    private List<{LookupEntity}Dto> _{lookupEntities} = new();
}
```

### Select with Object Binding

When you need to bind directly to a complex object instead of just its ID.

```razor
<MudSelect @bind-Value="selected{ParentEntity}"
           Label="{ParentEntity}"
           ToStringFunc="@(item => item?.FullName ?? string.Empty)" @* How the selected item is displayed *@
           Clearable="true"> @* Allows clearing the selection *@
    @foreach (var item in _{parentEntities})
    {
        <MudSelectItem Value="@item">@item.FullName</MudSelectItem>
    }
</MudSelect>

@code {
    private {ParentEntity}Dto? selected{ParentEntity};
    private List<{ParentEntity}Dto> _{parentEntities} = new();
}
```

### MultiSelect

For selecting multiple items from a list.

```razor
<MudSelect @bind-SelectedValues="selectedTags" @* Note `SelectedValues` for multi-select *@
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
    private List<string> _availableTags = new() { "Tag A", "Tag B", "Tag C" };
}
```

---

## 5. MudDialog - Dialogs and Modals

MudDialog provides a flexible way to present modal content to the user.

### Inline Dialog

Components designed to be hosted directly inside a `MudDialog`.

```razor
@* This component would be typically named like ConfirmDialog.razor *@
<MudDialog>
    <DialogContent>
        <MudText>@ContentText</MudText>
        <MudText Typo="Typo.body2" Color="Color.Error">
            This action cannot be undone.
        </MudText>
    </DialogContent>
    <DialogActions>
        <MudButton OnClick="Cancel">Cancel</MudButton>
        <MudButton Color="Color.Error"
                   Variant="Variant.Filled"
                   OnClick="Confirm">
            @ButtonText
        </MudButton>
    </DialogActions>
</MudDialog>

@code {
    [CascadingParameter]
    private MudDialogInstance MudDialog { get; set; } = null!; // Injected by IDialogService

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

### Opening Dialog Programmatically

Use `IDialogService` to show dialogs from any component.

```razor
@inject IDialogService DialogService
@inject ISnackbar Snackbar

<MudButton OnClick="Delete{Entity}">Delete {Entity}</MudButton>

@code {
    private async Task Delete{Entity}()
    {
        var parameters = new DialogParameters
        {
            ["ContentText"] = "Delete this {entity}? This action cannot be undone.",
            ["ButtonText"] = "Delete",
            ["Color"] = Color.Error
        };

        var dialog = await DialogService.ShowAsync<ConfirmDialog>( @* ConfirmDialog is the component to show *@
            "Confirm Delete", // Dialog title
            parameters,       // Parameters to pass to ConfirmDialog
            new DialogOptions { CloseOnEscapeKey = true, MaxWidth = MaxWidth.Small });

        var result = await dialog.Result; // Wait for dialog to close

        if (!result.Canceled)
        {
            // User confirmed, proceed with deletion
            Snackbar.Add("{Entity} deleted", Severity.Success);
            // ... deletion logic
        }
        else
        {
            Snackbar.Add("Deletion cancelled", Severity.Info);
        }
    }
}
```

### Form Dialog Example

You can host complex forms inside dialogs, returning their results.

```razor
@* Create{Entity}Dialog.razor *@
@inject IMediator Mediator
@inject ISnackbar Snackbar

<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">Create {Entity}</MudText>
    </TitleContent>
    <DialogContent>
        <MudTextField @bind-Value="_dto.Title"
                      Label="{Entity} Title"
                      Required="true" />
        @* ... other form fields ... *@
        <MudDatePicker @bind-Date="_startDate"
                       Label="Start Date"
                       MinDate="DateTime.Today" />
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
        _isSubmitting = true;
        try
        {
            // ... validation and API call ...
            var command = new Create{Entity}Command { {Entity}Dto = _dto };
            var result = await Mediator.Send(command);

            if (result.Success)
            {
                Snackbar.Add("{Entity} created", Severity.Success);
                MudDialog.Close(DialogResult.Ok(result.Id)); // Return the new {entity} ID
            }
            else
            {
                Snackbar.Add("Failed to create {entity}", Severity.Error);
                MudDialog.Close(DialogResult.Cancel()); // Or pass back errors
            }
        }
        finally
        {
            _isSubmitting = false;
        }
    }
}
```

---

## 6. MudTable - Data Tables

MudTable is a versatile component for displaying tabular data, offering sorting, pagination, and filtering.

### Simple Table

```razor
<MudTable Items="@_{entities}" Hover="true" Breakpoint="Breakpoint.Sm">
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
            <MudIconButton Icon="@Filled.Edit" Size="Size.Small" OnClick="@(() => Edit(context.Id))" />
            <MudIconButton Icon="@Filled.Delete" Size="Size.Small" Color="Color.Error" OnClick="@(() => Delete(context.Id))" />
        </MudTd>
    </RowTemplate>
</MudTable>

@code {
    private List<{Entity}ListDto> _{entities} = new();
}
```

### Table with Sorting

```razor
<MudTable Items="@_{entities}"
          Hover="true"
          SortLabel="Sort By" @* Accessibility label for sorting *@
          Breakpoint="Breakpoint.Sm">
    <HeaderContent>
        <MudTh>
            <MudTableSortLabel SortBy="new Func<{Entity}ListDto, object>(x => x.Title)"> @* Sort by Title property *@
                Title
            </MudTableSortLabel>
        </MudTh>
        <MudTh>
            <MudTableSortLabel SortBy="new Func<{Entity}ListDto, object>(x => x.StartDate)"> @* Sort by StartDate property *@
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

### Server-Side Table with Pagination and Search

For large datasets, fetching and processing data on the server is more efficient.

```razor
@inject IMediator Mediator

<MudTable ServerData="@LoadServerData" @* Key for server-side operations *@
          Dense="true"
          Hover="true"
          @ref="_table"> @* Ref to component instance for programmatic control *@
    <ToolBarContent>
        <MudText Typo="Typo.h6">{Entities}</MudText>
        <MudSpacer />
        <MudTextField @bind-Value="_searchString"
                      Placeholder="Search"
                      Adornment="Adornment.Start"
                      AdornmentIcon="@Filled.Search"
                      IconSize="Size.Medium"
                      Class="mt-0"
                      Immediate="true"
                      DebounceInterval="300"
                      OnDebounceIntervalElapsed="OnSearch" /> @* Debounced search *@
    </ToolBarContent>
    <HeaderContent>
        <MudTh>
            <MudTableSortLabel SortLabel="title" T="{Entity}ListDto"> @* SortLabel is key for ServerData *@
                Title
            </MudTableSortLabel>
        </MudTh>
        <MudTh>
            <MudTableSortLabel SortLabel="date" T="{Entity}ListDto">
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
        <MudText>No {entities} found</MudText>
    </NoRecordsContent>
    <LoadingContent>
        <MudProgressCircular Indeterminate="true" />
    </LoadingContent>
    <PagerContent> @* Table pager component *@
        <MudTablePager PageSizeOptions="new int[] { 10, 25, 50, 100 }" />
    </PagerContent>
</MudTable>

@code {
    private MudTable<{Entity}ListDto>? _table;
    private string _searchString = string.Empty;

    // This method is called by MudTable for server-side data fetching
    private async Task<TableData<{Entity}ListDto>> LoadServerData(
        TableState state,
        CancellationToken token)
    {
        // Build request for backend (e.g., CQRS Query)
        var request = new Get{Entity}ListRequest
        {
            Page = state.Page + 1,  // MudTable is 0-indexed, API is 1-indexed
            PageSize = state.PageSize,
            SearchTerm = _searchString,
            SortBy = state.SortLabel,
            SortDescending = state.SortDirection == SortDirection.Descending
        };

        var response = await Mediator.Send(request, token); // Assume response has {Entities} and TotalCount

        return new TableData<{Entity}ListDto>
        {
            Items = response.{Entities},
            TotalItems = response.TotalCount
        };
    }

    private void OnSearch()
    {
        _table?.ReloadServerData(); // Trigger data reload on search change
    }
}
```

---

## 7. MudCard - Information Cards

MudCard is a flexible content container with options for media, header, content, and actions.

```razor
<MudCard Elevation="4" Class="my-4">
    <MudCardMedia Image="@{entity}ImageUrl" Height="200" />
    <MudCardHeader>
        <CardHeaderContent>
            <MudText Typo="Typo.h5">@{entity}Title</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">
                @{entity}Date.ToShortDateString()
            </MudText>
        </CardHeaderContent>
    </MudCardHeader>
    <MudCardContent>
        <MudText Typo="Typo.body2" Class="mt-2">
            @{entity}Description
        </MudText>
    </MudCardContent>
    <MudCardActions Class="d-flex justify-end">
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

## 8. MudSnackbar - Notifications

MudSnackbar provides transient messages to users, used for success, error, warning, and info notifications.

```razor
@inject ISnackbar Snackbar

<MudButton OnClick="ShowSuccess">Show Success</MudButton>
<MudButton OnClick="ShowError">Show Error</MudButton>
<MudButton OnClick="ShowWarning">Show Warning</MudButton>
<MudButton OnClick="ShowInfo">Show Info</MudButton>

@code {
    private void ShowSuccess()
    {
        Snackbar.Add("{Entity} created successfully!", Severity.Success, config =>
        {
            config.ShowCloseIcon = false; // Custom config for this snackbar
        });
    }

    private void ShowError()
    {
        Snackbar.Add("Failed to delete {entity}", Severity.Error);
    }

    private void ShowWarning()
    {
        Snackbar.Add("{Entity} capacity almost full", Severity.Warning);
    }

    private void ShowInfo()
    {
        Snackbar.Add("{Entity} updated", Severity.Info);
    }
}
```

### Snackbar Configuration

Configure global Snackbar behavior in `{Project}.Blazor/Program.cs`.

```csharp
// File: {Project}.Blazor/Program.cs
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight; // Position
    config.SnackbarConfiguration.PreventDuplicates = false; // Allow duplicate messages
    config.SnackbarConfiguration.NewestOnTop = true; // Stack new messages on top
    config.SnackbarConfiguration.ShowCloseIcon = true; // Display close icon
    config.SnackbarConfiguration.VisibleStateDuration = 4000; // Duration in ms
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled; // Default appearance
});
```

---

## 9. MudContainer - Layout Container

Used to center and constrain content within a page, providing consistent margins.

```razor
<MudContainer MaxWidth="MaxWidth.Large" Class="my-4"> @* MaxWidth constrains content, my-4 adds vertical margin *@
    <MudText Typo="Typo.h4">{Entities} Overview</MudText>
    @* All your page content goes here *@
</MudContainer>
```

### `MaxWidth` Options

*   `MaxWidth.ExtraSmall` - 600px
*   `MaxWidth.Small` - 960px
*   `MaxWidth.Medium` - 1280px
*   `MaxWidth.Large` - 1920px
*   `MaxWidth.ExtraLarge` - 2560px
*   `MaxWidth.False` - No maximum width (fills available space)

---

## Best Practices for MudBlazor Components

| Component | Best Practice |
|-----------|---------------|
| **MudGrid** | Always use `MudGrid` and `MudItem` for layout, leverage `xs`, `sm`, `md`, `lg` for responsiveness. |
| **MudButton** | Use `Variant.Filled` for primary actions, `Variant.Outlined` for secondary, `Variant.Text` for tertiary. Prefer `Color.Primary` or `Color.Secondary` for main actions. |
| **MudTextField** | Always include `Label`, use `Variant.Outlined`. Leverage `Required`, `RequiredError`, `MaxLength`, `Counter`, and `Validation` for input. |
| **MudSelect** | Use `ToStringFunc` when binding to complex objects. Consider `Clearable` and `MultiSelection` as needed. |
| **MudDialog** | Use `IDialogService` for programmatic control. Centralize common dialogs (e.g., confirmation) into reusable components. |
| **MudTable** | For large datasets, use `ServerData` for efficient pagination, sorting, and filtering. |
| **MudCard** | Great for displaying grouped information. Use `Elevation` and `Class` for styling. |
| **MudSnackbar** | Use `Snackbar.Add` with appropriate `Severity` levels. Configure global settings in `Program.cs`. |
| **MudContainer** | Use with `MaxWidth` to ensure content readability and alignment across screen sizes. |

---

**Related Resources**:
- [component-design.md](component-design.md) - General Blazor component best practices.
- [state-management.md](state-management.md) - Handling state interactions with UI components.
- [common-patterns.md](common-patterns.md) - Full examples of forms, tables, and dialogs.
