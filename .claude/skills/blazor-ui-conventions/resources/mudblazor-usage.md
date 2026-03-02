ABOUTME: MudBlazor v9 usage rules — modern aesthetic with clean, minimal UI.
ABOUTME: Prefer MudBlazor components over raw HTML; covers v9 API changes and styling defaults.

# MudBlazor Usage (v9)

## Required Rules
- Use `MudGrid`/`MudItem` for layout and responsive breakpoints.
- Prefer `MudButton`, `MudTextField`, `MudSelect`, `MudDialog`, `MudTable`.
- Use Material icons via `@Icons.Material.*`.
- New v9 components available: `MudFabMenu`, `MudSplitPanel`, `MudHotkey`, `MudExitPrompt`.

## Modern Aesthetic Defaults (Apply to All Components)

### Buttons
- **Primary CTA**: `Variant.Filled`, `Color.Primary`, `Elevation="0"` (flat, no shadow).
- **Secondary actions**: `Variant.Outlined`, `Color.Default` — subtle border, no fill.
- **Tertiary/text actions**: `Variant.Text`, `Color.Default` — borderless, minimal.
- **Destructive**: `Variant.Filled`, `Color.Error` — reserved for delete/remove only.
- **Icon buttons**: `Size.Small` or `Size.Medium` — never oversized.
- **Spacing**: `Class="ml-2"` between adjacent buttons; `Class="gap-3 d-flex"` for button groups.

### Cards
- `Elevation="0"` with `Class="border rounded-lg"` for outlined cards (preferred).
- Or `Elevation="1"` max for subtle lift. Never `Elevation > 2` for content cards.
- Padding: `pa-4` to `pa-6` inside `MudCardContent`.
- `ContentPadding="true"` (v9) for default card body padding.

### Text Fields / Inputs
- `Variant.Outlined` as the standard input variant (clean bordered look).
- `Margin.Dense` for compact forms; `Margin.Normal` for spacious forms.
- `Immediate="true"` for search/filter inputs.
- Always pair with label and helper text for clarity.

### Tables & DataGrids
- `Hover` enabled, `Striped` disabled (clean rows, highlight on hover).
- `Elevation="0"` with border wrapper: `<MudPaper Elevation="0" Class="border rounded-lg overflow-hidden">`.
- Dense mode for data-heavy tables; normal for user-facing lists.
- v9 `ServerData` requires `CancellationToken` parameter.

### Dialogs
- `MaxWidth.Small` to `MaxWidth.Medium` — avoid full-width dialogs.
- Always include clear title, body, and action buttons (primary right, cancel left).
- `DialogOptions.CloseOnNavigation = true` for navigable apps.

### Navigation / Drawer
- Clean white/surface background (not colored).
- `Elevation="0"` on drawer; use `border-right` for separation.
- Active item: subtle background highlight, not bold color fill.

### Snackbars
- `Severity` for semantic coloring (Info, Success, Warning, Error).
- Keep messages concise (one line). Use `SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomCenter`.

### General Rules
- **Elevation budget**: Most components should be `Elevation="0"` (flat). Floating elements (menus, popovers, tooltips) can use `1`.
- **Borders over shadows**: Use `Class="border"` for visual separation instead of elevation/shadow.
- **Spacing consistency**: `gap-3` or `gap-4` between sibling elements; `pa-4` for containers.
- **Colors**: Use neutral tones for most UI; accent (`Color.Primary`) only for interactive/call-to-action elements.
- **Dense mode**: Prefer `Dense` for data-heavy views; normal for user-facing content.

## MudGlobal Removal — Wrapper Component Pattern

Since `MudGlobal` defaults are removed in v9, create thin wrapper components for repeated defaults:

```razor
@* AppButton.razor — shared button defaults *@
<MudButton Variant="Variant.Filled" Color="Color.Primary" Elevation="0"
           DisableElevation="true" Class="@Class" Style="@Style"
           @attributes="AdditionalAttributes">
    @ChildContent
</MudButton>

@code {
    [Parameter] public string? Class { get; set; }
    [Parameter] public string? Style { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public IDictionary<string, object>? AdditionalAttributes { get; set; }
}
```

```razor
@* AppCard.razor — shared card defaults *@
<MudCard Elevation="0" Class="@($"border rounded-lg {Class}")">
    @ChildContent
</MudCard>

@code {
    [Parameter] public string? Class { get; set; }
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
```

## v9 Breaking API Changes (Must Follow)

### DialogService
- `ShowMessageBox` → **`ShowMessageBoxAsync`** (old method removed).
- `Show<T>()` → **`ShowAsync<T>()`**; `ShowForm<T>()` → **`ShowFormAsync<T>()`**.
- `Close()` → **`CloseAsync()`**.
- `DefaultFocus` moved from `MudGlobal.DialogDefaults` to `MudDialogProvider` or `DialogOptions`.

### MudFileUpload
- `<ActivatorContent>` → **`<CustomContent Context="fileUpload">`**.
- Must explicitly call `OnClick="@fileUpload.OpenFilePickerAsync"` on inner button (no longer auto-activates).
- New features: built-in drag-and-drop (`DragAndDrop="true"`), default file list, `GetFilenames()`, `RemoveFile()`.

### MudMenu ActivatorContent
- `ActivatorContent` now provides a **`MenuContext`** parameter.
- Must explicitly call `context.ToggleAsync` / `context.OpenAsync` / `context.CloseAsync` on event handlers.

### MudSelect
- `SelectedValues` type changed from `ICollection<T>` to **`IReadOnlyCollection<T>`**.
- `Clear` → **`ClearAsync`**; `Open` now supports `@bind-Open`.

### MudTabs Class Renames
- `TabPanelClass` → **`TabButtonsClass`**; `PanelClass` → **`TabPanelsClass`**.
- `MudTabPanel` has new `PanelClass` property for panel-specific styling.

### MudLink
- `Typo` default changed from `Typo.body1` to **`Typo.inherit`**. Add `Typo="Typo.body1"` explicitly where needed.

### MudSnackbar
- Snackbars with action buttons **require interaction by default** (won't auto-dismiss). Set `RequireInteraction="false"` to restore old behavior.

### Popover
- Modal default changed from `true` to **`false`**. Set `Modal="true"` explicitly if needed.
- `OverflowBehavior` default is now **`FlipAlways`**. Configure via `PopoverOptions` in `AddMudServices`.

### Range / DateRange
- Now **immutable** — no setters on `Start`/`End`. Create new instances instead of mutating.

## Large Lists
- Use server-side table/pagination for large datasets.

**Related**: `component-design.md`, `common-patterns.md`.
