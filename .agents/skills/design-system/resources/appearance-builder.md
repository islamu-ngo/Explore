ABOUTME: AppearanceStyleBuilder and DialogOptionsFactory utilities.
ABOUTME: Covers appearance settings, background effects, and dialog option presets.

# Appearance Builder and Dialog Presets

## AppearanceStyleBuilder

Located in `Explore.Blazor.Client/Helpers/AppearanceStyleBuilder.cs`.

### AppearanceSettings Model

| Property | Type | Default | Notes |
|----------|------|---------|-------|
| BackgroundColor | string? | null | Hex color (e.g. "#1a1a2e") |
| ImageUri | string? | null | Background image URL |
| BackgroundEffect | string | "None" | One of: None, SoftOverlay, StrongOverlay, Blur |
| IsEmpty | bool | computed | True if no color and no image set |

### Builder Methods

| Method | Extra CSS | Use Case |
|--------|----------|----------|
| `BuildStyle(settings, fallbackHex, additionalCss?)` | None | General purpose |
| `BuildHeroStyle(settings, fallbackHex)` | `aspect-ratio: 16/9` | Hero/banner sections |
| `BuildBannerStyle(settings, fallbackHex)` | Banner-specific | Profile/org banners |

### Background Effects

| Effect | Overlay | Opacity | Description |
|--------|---------|---------|-------------|
| None | — | — | Raw color/image only |
| SoftOverlay | `rgba(0,0,0,...)` | 0.24 | Gentle darkening for readability |
| StrongOverlay | `rgba(0,0,0,...)` | 0.40 | Heavy darkening for light text |
| Blur | `rgba(0,0,0,...)` | 0.18 | Slight overlay (blur applied via CSS) |

## AppearanceEditor Component

Located in `Explore.Blazor.Client/Shared/AppearanceEditor.razor`.

| Parameter | Type | Default | Notes |
|-----------|------|---------|-------|
| BackgroundColor | string | — | Two-way bindable |
| BackgroundEffect | string | — | Two-way bindable |
| ImageUri | string | — | Two-way bindable |
| ShowImageField | bool | true | Toggle image URL input |
| ShowPreview | bool | true | Live preview panel |
| FallbackColor | string | "#f5f5f5" | Preview fallback |

Controls: MudColorPicker (Spectrum mode, 100ms throttle), AppTextField (image URL), MudSelect (effects), AppButton (reset).

## DialogOptionsFactory

Located in `Explore.Blazor.Client/Services/DialogOptionsFactory.cs`.

### Presets

| Preset | MaxWidth | Features |
|--------|----------|----------|
| Small | Small | FullWidth, CloseOnEscape |
| Medium | Medium | FullWidth, CloseOnEscape |
| Confirmation | Small | FullWidth, CloseOnEscape, DialogPosition.Center |
| Editor | Medium | FullWidth, CloseButton, BackdropClick, CloseOnEscape |

**Rule:** Always use a preset — never construct `DialogOptions` manually:

```csharp
// Correct
var options = DialogOptionsFactory.Confirmation();
await DialogService.ShowAsync<ConfirmDialog>("Title", options);

// Wrong — creates inconsistency
var options = new DialogOptions { MaxWidth = MaxWidth.Small };
```

## Related

- `resources/wrapper-components.md` — AppDialogShell for dialog content
- `resources/token-system.md` — tokens used in appearance styles
