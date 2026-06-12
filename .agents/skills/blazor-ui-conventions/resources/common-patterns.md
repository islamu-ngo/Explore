ABOUTME: UI patterns for forms, dialogs, file upload, and tables (MudBlazor v9).
ABOUTME: Modern aesthetic patterns with clean, minimal component styling.

# Common Patterns (v9)

## Forms
- Use MudBlazor components + validation.
- Show inline errors and disable submit while saving.
- `MudForm` now supports `OnEnterPressed` event callback.
- `Error` and `ErrorId` are now two-way bindable (`@bind-Error`, `@bind-ErrorId`).
- **Styling**: `Variant.Outlined` inputs, `Margin.Dense` for compact forms, generous `gap-4` between fields.

### Modern Form Pattern
```razor
<MudPaper Elevation="0" Class="border rounded-lg pa-6">
    <MudForm @ref="_form" @bind-IsValid="_isValid">
        <MudStack Spacing="4">
            <MudTextField T="string" Label="Name" @bind-Value="_name"
                          Variant="Variant.Outlined" Required />
            <MudTextField T="string" Label="Email" @bind-Value="_email"
                          Variant="Variant.Outlined" InputType="InputType.Email" />
            <MudStack Row Spacing="3" Justify="Justify.FlexEnd">
                <MudButton Variant="Variant.Text" Color="Color.Default"
                           OnClick="Cancel">Cancel</MudButton>
                <MudButton Variant="Variant.Filled" Color="Color.Primary"
                           Elevation="0" Disabled="@(!_isValid)"
                           OnClick="Submit">Save</MudButton>
            </MudStack>
        </MudStack>
    </MudForm>
</MudPaper>
```

## Dialogs
- Use MudDialog for confirmations and form entry.
- Always return a clear result (ok/cancel + optional payload).
- **v9**: Use `ShowMessageBoxAsync` (not `ShowMessageBox` — removed).
- **v9**: Use `ShowAsync<T>()` (not `Show<T>()` — removed).
- **v9**: `DialogOptions` supports `CloseOnNavigation` to auto-close dialogs on navigation.
- **Styling**: `MaxWidth.Small` default, clean title + body + right-aligned actions.

### Modern Dialog Pattern
```razor
<MudDialog>
    <TitleContent>
        <MudText Typo="Typo.h6">@Title</MudText>
    </TitleContent>
    <DialogContent>
        <MudText Typo="Typo.body2" Class="text-secondary">@Message</MudText>
    </DialogContent>
    <DialogActions>
        <MudButton Variant="Variant.Text" Color="Color.Default"
                   OnClick="Cancel">Cancel</MudButton>
        <MudButton Variant="Variant.Filled" Color="Color.Primary"
                   Elevation="0" OnClick="Submit">Confirm</MudButton>
    </DialogActions>
</MudDialog>
```

## File Upload
- **v9**: Use `<CustomContent Context="fileUpload">` (not `<ActivatorContent>` — removed).
- Must call `fileUpload.OpenFilePickerAsync` explicitly on the trigger button's `OnClick`.
- New: built-in drag-and-drop via `DragAndDrop="true"`, default file list rendering.

## Snackbars
- **v9**: Snackbars with action buttons require interaction by default (won't auto-dismiss).
- Set `RequireInteraction="false"` explicitly if auto-dismiss is needed with actions.

## Tables & Lists
- Prefer `MudTable` for CRUD lists.
- Provide empty/loading states and deterministic sort.
- **Styling**: Wrap in `<MudPaper Elevation="0" Class="border rounded-lg overflow-hidden">`.

### Modern Table Pattern
```razor
<MudPaper Elevation="0" Class="border rounded-lg overflow-hidden">
    <MudTable Items="@_items" Hover Dense
              HeaderClass="mud-theme-primary-lighten"
              Loading="@_loading" LoadingProgressColor="Color.Primary">
        <HeaderContent>
            <MudTh>Name</MudTh>
            <MudTh>Status</MudTh>
            <MudTh Style="width: 80px"></MudTh>
        </HeaderContent>
        <RowTemplate>
            <MudTd>@context.Name</MudTd>
            <MudTd><MudChip T="string" Size="Size.Small" Variant="Variant.Outlined"
                            Color="@GetStatusColor(context)">@context.Status</MudChip></MudTd>
            <MudTd>
                <MudIconButton Icon="@Icons.Material.Outlined.MoreVert" Size="Size.Small" />
            </MudTd>
        </RowTemplate>
        <NoRecordsContent>
            <MudStack AlignItems="AlignItems.Center" Class="pa-8">
                <MudIcon Icon="@Icons.Material.Outlined.SearchOff" Size="Size.Large"
                         Color="Color.Default" Class="mb-2" />
                <MudText Typo="Typo.body2" Color="Color.Secondary">No records found</MudText>
            </MudStack>
        </NoRecordsContent>
    </MudTable>
</MudPaper>
```

### Modern Card List Pattern
```razor
<MudGrid Spacing="4">
    @foreach (var item in _items)
    {
        <MudItem xs="12" sm="6" md="4">
            <MudCard Elevation="0" Class="border rounded-lg h-100">
                <MudCardContent Class="pa-4">
                    <MudText Typo="Typo.subtitle1">@item.Title</MudText>
                    <MudText Typo="Typo.body2" Color="Color.Secondary" Class="mt-1">
                        @item.Description
                    </MudText>
                </MudCardContent>
                <MudCardActions Class="pa-4 pt-0">
                    <MudButton Variant="Variant.Text" Color="Color.Primary" Size="Size.Small">
                        View
                    </MudButton>
                </MudCardActions>
            </MudCard>
        </MudItem>
    }
</MudGrid>
```

## Empty States
- Always provide a visual empty state (icon + message + optional action).
- Use `MudStack` for centered alignment with `pa-8` spacing.

## Loading States
- Use `MudProgressLinear` at page/section top for data fetching.
- Use `MudSkeleton` for placeholder content shapes.
- Never leave the user staring at a blank screen.

## Errors
- Catch and surface user-friendly messages; log exceptions.
- Use `MudAlert` with `Severity.Error` for inline errors; `Snackbar` for transient messages.

## Related
- [mudblazor-usage.md](mudblazor-usage.md)
