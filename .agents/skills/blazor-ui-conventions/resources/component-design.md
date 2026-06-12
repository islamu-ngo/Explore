ABOUTME: Blazor component design rules — modern aesthetic + MudBlazor v9.
ABOUTME: Covers structure, parameters, lifecycle, v9 converter/ParameterState, and visual guidelines.

# Component Design (v9)

## Core Rules
- Keep component state private; expose only `[Parameter]` inputs and `EventCallback` outputs.
- Prefer composition (child components) over large monoliths.
- Use code-behind (`.razor.cs`) only when logic is non-trivial.
- Initialize lists/objects to avoid nulls; use `?` for optional data.

## Parameters & Events
- Parameters are one-way input; child never mutates parent state directly.
- Use `EventCallback<T>` for child → parent changes.
- Avoid two-way binding on complex models unless necessary.

## Visual Design Rules

### Component Structure
- Every page/section should follow: **header → content → actions** (top to bottom).
- Use `MudStack` for vertical flow (`Spacing="4"`); `MudStack Row` for horizontal groups.
- Wrap content sections in `<MudPaper Elevation="0" Class="border rounded-lg pa-4">`.

### Elevation & Depth
- **Flat by default**: `Elevation="0"` for cards, papers, tables.
- **Elevation budget**: max `1` for floating elements (menus, popovers). Never exceed `2`.
- Separate sections with subtle borders (`Class="border"`) not shadows.
- Use `Outlined` variant for inputs and secondary buttons.

### Spacing & Density
- `pa-4` (16px) as the default container padding.
- `gap-3` or `gap-4` between sibling elements (use `MudStack Spacing`).
- `mt-6` / `mb-6` for major section breaks.
- Use `Dense` for data tables and admin grids; normal for public-facing pages.

### Color Usage
- Primary color: **CTAs and active states only**. Don't paint entire surfaces.
- Backgrounds: off-white (`#fafafa`) / dark (`#0f0f17`) — never pure white or black.
- Text: near-black for body (`#111827`), muted gray for secondary (`#6b7280`).
- Status: use `MudChip` with `Variant.Outlined` + `Size.Small` for status indicators.

### Interactive States
- Hover: subtle background shift (4% opacity change). No color jumps.
- Focus: visible ring/outline for accessibility.
- Active: use `Color.Primary` highlight on selected items.
- Disabled: reduced opacity (`opacity: 0.5`), no cursor pointer.
- Transitions: 150ms ease for hover/focus. Smooth, not distracting.

### Icons & Imagery
- Prefer `Icons.Material.Outlined` (lighter weight) over `Icons.Material.Filled`.
- Icon buttons: `Size.Small` for inline actions; `Size.Medium` for standalone.
- Empty states: centered icon + text + optional action button.

## v9 Converter System (Custom Components Only)
- Old `Converter<T>` / `Converter<T,U>` classes are **removed**. Use `IConverter<TInput, TOutput>` / `IReversibleConverter<TInput, TOutput>` interfaces.
- Inline converters: use `Conversions.From(...)` factory instead of `new Converter<T> { SetFunc = ..., GetFunc = ... }`.
- Components inheriting `MudFormComponent` must implement `GetDefaultConverter()` instead of setting converter in constructor.
- Access active converter via `GetConverter()` method (not `Converter` property, which may be null).

## v9 ParameterState Analyzers
- MUD0010: Don't read `[ParameterState]` properties directly — use `_state.Value`.
- MUD0011: Don't write to `[ParameterState]` properties — use `_state.SetValueAsync()`.
- MUD0012: Don't access ParameterState properties externally — use `GetState()`.
- These only apply to custom components inheriting MudBlazor base classes.

## Lifecycle Notes
- `OnInitialized{Async}` can run twice with prerendering.
- Put side effects behind guards; prefer `OnAfterRenderAsync(firstRender)` when needed.

## Layout & CSS
- Use `.razor.css` (CSS isolation) for component-specific styles.
- Only use `::deep` for styling internal elements of MudBlazor components.
- Always wrap MudBlazor components in a `<div>` before applying `::deep` selectors.

## Related
- [state-management.md](state-management.md)
- [render-modes.md](render-modes.md)
