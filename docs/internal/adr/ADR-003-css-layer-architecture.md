ABOUTME: Decision record for adopting CSS @layer cascade architecture in the Blazor frontend.
ABOUTME: Covers the 6-layer hierarchy, MudBlazor override policy, and design token system.

# ADR-003: CSS @layer Cascade Architecture

- **Status:** Accepted
- **Date:** 2026-01
- **Deciders:** Core team

## Context

MudBlazor generates CSS with high specificity that conflicts with custom styles. Blazor CSS isolation (scoped `.razor.css` files) produces unlayered styles that interact unpredictably with component library output. The team needed a deterministic way to control style precedence without resorting to `!important` or specificity hacks.

## Decision

Adopt a 6-layer `@layer` architecture declared in `layers.css`, ordered from lowest to highest priority:

1. **reset** — CSS reset / normalize.
2. **base** — global element defaults.
3. **tokens** — 3-tier design token system (primitive → semantic → component).
4. **mudblazor-overrides** — whitelisted MudBlazor style corrections (drawer, z-index only).
5. **components** — application component styles.
6. **utilities** — single-purpose utility classes.

**Unlayered CSS wins over all layered CSS.** Blazor CSS isolation files and MudThemeProvider output are intentionally unlayered, giving them highest effective priority by design.

### MudBlazor Override Policy

Only drawer container and overlay z-index overrides are permitted in `mudblazor-overrides.css`. No bare `.mud-*` selectors outside this file. Every override must include a comment explaining why it exists.

### Design Tokens

Three tiers in `tokens.css`:

- **Primitive** — raw values (`--isl-space-1` through `--isl-space-16` on a 4px grid, radius, shadows, font families).
- **Semantic** — purpose-bound aliases sourced from MudBlazor palette (`--isl-color-primary` from `--mud-palette-primary`), fluid typography via `clamp()`, accessibility tokens (`--isl-target-min: 24px`, `--isl-focus-ring-width: 2px`).
- **Component** — scoped tokens for specific components (`--isl-card-border-radius`, `--isl-button-padding`).

## Consequences

1. Style conflicts are resolved by layer order, not selector specificity.
2. MudBlazor updates are less likely to break custom styles.
3. New CSS must be placed in the correct layer — wrong layer placement causes subtle bugs.
4. The `mudblazor-overrides.css` whitelist must be maintained as MudBlazor evolves.
5. Accessibility tokens (`prefers-contrast`, `prefers-reduced-motion`) are built into the token layer.

## Related

- [DESIGN_SYSTEM.md](../DESIGN_SYSTEM.md) — full design system reference
- [BLAZOR.md](../BLAZOR.md) — Blazor architecture and styling
- [ADR-004](ADR-004-accessibility-architecture.md) — accessibility architecture
