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

## Wrapper Component Catalog

Wrappers in `Explore.Blazor.Client/Components/Common/` replace `MudGlobal` defaults (removed in v9). All use `[Parameter(CaptureUnmatchedValues = true)]` + `@attributes` for pass-through.

| Wrapper | Wraps | Key Defaults |
|---------|-------|-------------|
| `AppButton` | `MudButton` | `Variant.Filled`, `Color.Primary`, `Elevation=0` |
| `AppCard` | `MudCard` | `Elevation=0` (border via `.app-card` CSS) |
| `AppTextField<T>` | `MudTextField<T>` | `Variant.Outlined` |
| `AppIconButton` | `MudIconButton` | (transparent pass-through) |
| `AppDialogShell` | N/A | Structural shell: `Title`, `HeaderContent`, `ChildContent`, `ActionsContent` |

Override any default by passing the parameter explicitly: `<AppButton Variant="Variant.Outlined">`.

### DialogOptionsFactory

Static presets in `Explore.Blazor.Client/Services/DialogOptionsFactory.cs`:

```csharp
DialogOptionsFactory.Small()          // CloseOnEscapeKey, MaxWidth.Small, FullWidth
DialogOptionsFactory.Medium()         // CloseOnEscapeKey, MaxWidth.Medium, FullWidth
DialogOptionsFactory.Confirmation()   // Small + Position.Center
DialogOptionsFactory.Editor()         // MaxWidth.Medium, FullWidth, CloseButton, BackdropClick
```

Use instead of inline `new DialogOptions { ... }` for consistency.

### MudDialogProvider Configuration

Both `MainLayout.razor` and `SetupLayout.razor` configure:
```razor
<MudDialogProvider DefaultFocus="DefaultFocus.FirstChild" />
```

Both `Program.cs` files configure popover transitions:
```csharp
builder.Services.AddMudServices(config =>
{
    config.PopoverOptions.Duration = TimeSpan.FromMilliseconds(300);
});
```

## v9 Breaking API Changes

See [v9-migration.md](v9-migration.md) for the complete v9 breaking changes reference. Key items: `ShowAsync<T>()` replaces `Show<T>()`, `<CustomContent>` replaces `<ActivatorContent>` in file uploads, Range/DateRange are immutable.

## Large Lists
- Use server-side table/pagination for large datasets.

**Related**: `component-design.md`, `common-patterns.md`.
