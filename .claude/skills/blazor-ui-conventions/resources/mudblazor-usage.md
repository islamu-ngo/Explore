ABOUTME: Minimal MudBlazor usage rules for UI consistency.
ABOUTME: Prefer MudBlazor components over raw HTML.

# MudBlazor Usage

## Required Rules
- Use `MudGrid`/`MudItem` for layout and responsive breakpoints.
- Prefer `MudButton`, `MudTextField`, `MudSelect`, `MudDialog`, `MudTable`.
- Use Material icons via `@Icons.Material.*`.

## Defaults
- Primary actions: `Variant.Filled`, `Color.Primary`.
- Secondary actions: `Variant.Outlined` or `Variant.Text`.

## Large Lists
- Use server-side table/pagination for large datasets.

**Related**: `component-design.md`, `common-patterns.md`.
