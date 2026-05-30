ABOUTME: MudBlazor v9 breaking API changes consolidated in one reference.
ABOUTME: Covers DialogService, FileUpload, Menu, Select, Tabs, Link, Snackbar, Popover, Range, CssBuilder.

# MudBlazor v9 Breaking API Changes

These changes are mandatory when writing or modifying MudBlazor code. The old APIs are **removed** in v9.

## DialogService
- `ShowMessageBox` → **`ShowMessageBoxAsync`** (old method removed).
- `Show<T>()` → **`ShowAsync<T>()`**; `ShowForm<T>()` → **`ShowFormAsync<T>()`**.
- `Close()` → **`CloseAsync()`**.
- `DefaultFocus` moved from `MudGlobal.DialogDefaults` to `MudDialogProvider` or `DialogOptions`.

## MudFileUpload
- `<ActivatorContent>` → **`<CustomContent Context="fileUpload">`** (removed).
- Must explicitly call `OnClick="@fileUpload.OpenFilePickerAsync"` on inner button.
- New: built-in drag-and-drop (`DragAndDrop="true"`), default file list, `GetFilenames()`, `RemoveFile()`.

## MudMenu
- `ActivatorContent` now provides a **`MenuContext`** parameter.
- Must call `context.ToggleAsync` / `context.OpenAsync` / `context.CloseAsync` on event handlers.

## MudSelect
- `SelectedValues` type: `ICollection<T>` → **`IReadOnlyCollection<T>`**.
- `Clear` → **`ClearAsync`**; `Open` now supports `@bind-Open`.

## MudTabs
- `TabPanelClass` → **`TabButtonsClass`**; `PanelClass` → **`TabPanelsClass`**.
- `MudTabPanel` has new `PanelClass` property for panel-specific styling.

## MudLink
- `Typo` default: `Typo.body1` → **`Typo.inherit`**. Add `Typo="Typo.body1"` explicitly where needed.

## MudSnackbar
- Snackbars with action buttons **require interaction by default** (won't auto-dismiss).
- Set `RequireInteraction="false"` to restore old behavior.

## Popover
- Modal default: `true` → **`false`**. Set `Modal="true"` explicitly if needed.
- `OverflowBehavior` default is now **`FlipAlways`**. Configure via `PopoverOptions` in `AddMudServices`.

## Range And DateRange
- Now **immutable** — no setters on `Start`/`End`. Create new instances instead of mutating.

## CssBuilder And StyleBuilder
- Now `readonly struct`. Use `new CssBuilder()` or `CssBuilder.Default()` — **never** `default(CssBuilder)` (throws NRE).

## Theme And Global Changes
- `PaletteLight` / `PaletteDark` types unified to **`Palette`**. Replace `new PaletteLight()` with `new Palette()`.
- `ObserveSystemThemeChange` → **`ObserveSystemDarkModeChange`**.
- All `MudGlobal` defaults **removed**. Set via wrapper components, `MudTheme`, or explicit parameters.
- `PopoverOptions` configured in `AddMudServices(config => { ... })`.
- MudSwitch/CheckBox/Radio render content inside `<span>` — update `::deep` selectors targeting child text.
- MudDrawer uses CSS `transition` instead of `animation`.

## Related
- [mudblazor-usage.md](mudblazor-usage.md) — component usage rules
- [common-patterns.md](common-patterns.md) — UI pattern examples
- [mudblazor-styling.md](../../blazor-css-isolation/resources/mudblazor-styling.md) — styling patterns
