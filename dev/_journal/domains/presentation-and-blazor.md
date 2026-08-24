<!-- ABOUTME: Domain journal for Blazor UI, MudBlazor components, CSS isolation, and HAL affordances. -->
<!-- ABOUTME: Captures durable findings on Blazor WASM/SSR, dock layout, and frontend architecture. -->

# Presentation & Blazor Knowledge Ledger

> **Scope**: `Explore.Blazor`, `Explore.Blazor.Client`, MudBlazor v9, HAL affordance gating, and Dock Layout engine.

---

## 1. Architectural Decisions

- **Complete Isolation of Blazor Layer**: `Explore.Blazor` and `Explore.Blazor.Client` must never reference Domain, Application, Infrastructure, or Persistence projects. All backend communication flows through the generated `IEventApiClient` contract.
- **HAL Affordances as Single Source of Truth for UI**: Client components gate action buttons (Edit, Delete, Publish) by checking `_links` presence on DTOs, never via local claim/role checks.
- **Internal Descriptor-Driven Dock Engine**: Layout panels use stable `DockPanelId` identifiers, sealed value-object stack policies (`DockPanelStackStrategy.Tabbed`), and scoped snapshot persistence (`CreateSnapshot(layoutKey, DockScope scope)`).
- **Deterministic Local SVG Fallbacks**: Avoid third-party placeholder image services (e.g. `placehold.co`). Use `ImageHelper` to render inline data-URI SVG placeholders with sanitized colors and encoded text.

---

## 2. Technical Insights & Patterns

- **Blazor CSS Isolation with `::deep`**: When parent overlay styles target child component internals across boundary lines, use `::deep` (e.g. `.dock-overlay-host__slot ::deep .dock-panel-host`) with `@media (prefers-reduced-motion: reduce)` and RTL-safe selectors.
- **`Microsoft.AspNetCore.WebUtilities` Unavailable in WASM**: `QueryHelpers.ParseQuery()` is absent in Blazor InteractiveWebAssembly. Use `string.Split` and `Uri.UnescapeDataString` for client-side query string parsing.
- **`MudToggleGroup` Value Binding Conflict**: Do not combine `@bind-Value` and `ValueChanged` on `MudToggleGroup` (RZ10010 duplicate parameter error). Use explicit `Value="..." ValueChanged="@Handler"` when custom change logic is required.
- **Dock Tab Semantics & Keyboard Focus**: Only the active tab owns `aria-controls`; the active panel body renders `role="tabpanel"` and `aria-labelledby` targeting the tab. Keyboard navigation routes through `IAccessibilityFocusService.FocusAsync`.
- **Browser Local Storage Interop Boundary**: Browser persistence code must live behind the approved interop boundary in `Explore.Blazor.Client/Services/Interop/` (e.g. `LocalStorageDockLayoutPersistence.cs`) rather than directly inside domain/state classes.

---

## 3. Failed Approaches & Lessons

- **Central `Panel` Enums**: Modeling dock panels or layout policies as central enums with `Panel` in their type name violates architecture tests. Use static sealed value-object records instead.
- **Deleting Razor Pages without Cleaning Route Registrations**: When deleting Razor pages, remove matching route declarations in `Routes.razor` in the same commit to avoid broken type errors.
