# MudBlazor Styling with Isolation

Combine MudBlazor component `Class` parameters with isolated CSS and BEM names.

## Pattern

```razor
<MudCard Class="event-card event-card--upcoming">
    <MudCardHeader Class="event-card__header" />
    <MudCardContent Class="event-card__body" />
</MudCard>
```

```css
.event-card { border-radius: 12px; }
.event-card--upcoming { border-left: 4px solid var(--mud-palette-success); }
.event-card__header { background: var(--mud-palette-surface-variant); }
```

## Guidance

- Prefer MudBlazor properties first (`Color`, `Variant`, `Size`, etc.).
- Add classes for app-specific design language.
- Use `::deep` only for internals that are otherwise unreachable.
