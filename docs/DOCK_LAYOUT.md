<!-- ABOUTME: Architecture guide for the generic dock layout engine used by shell and workspace UI. -->
<!-- ABOUTME: Documents descriptors, runtime state, snapshots, tokens, and migration guardrails. -->

# Dock Layout Architecture

The dock layout system is a generic internal workbench engine for shell and workspace panels. It is intentionally descriptor-driven: each panel owns a stable `DockPanelId`, immutable metadata, and runtime state without requiring a central enum for every future panel.

## Core model

- `DockPanelDescriptor` is immutable panel metadata: id, scope, side, default mode, title, accessible label, width bounds, order, resize support, close support, and persistence policy.
- `DockPanelState` is runtime state: open/closed, mode, current width, order, and active status.
- `DockPanelEntry` combines descriptor, render content, and current state for host components.
- `DockLayoutSnapshot` captures persistent runtime state for reset, tests, debugging, and future user preference storage.

## Scope, side, and mode

`DockScope.Shell` is for global chrome such as the app nav and AI rail. `DockScope.Workspace` is for page-owned panels such as EventList customization and inspector-style previews.

`DockSide` uses logical values: `Start`, `End`, and `Bottom`. Do not use physical left/right concepts in state or new CSS; renderers must map logical sides to CSS logical properties so RTL remains safe.

`DockMode` models behavior explicitly:

- `Docked`: persistent grid/sidebar track that participates in layout.
- `Overlay`: panel floats over content without reserving layout space.
- `Temporary`: modal/mobile-style panel with backdrop and close affordance.
- `Inspector`: focused preview/details panel; EventList detail remains in this family.
- `Collapsed`: registered but reduced to a compact rail or hidden affordance.

## Registration rules

Register panels through `IDockPanelRegistry.Register(DockPanelDescriptor, RenderFragment)`. Duplicate ids are rejected because persisted state, tests, and ARIA relationships depend on stable uniqueness.

Descriptor capability flags are enforced by the state engine. `Resize` rejects panels whose descriptor has `IsResizable == false`, and `Close` rejects panels whose descriptor has `CanClose == false`. Host components should use these same flags to decide which user affordances to render.

Do not add a central enum for every panel. Shared constants are allowed only when two or more components must reference the same specific id, such as a top-bar toggle and its shell panel host.

## Tokens and styling

Shared widths, z-indexes, backdrop color, and motion live in `Explore.Blazor/wwwroot/css/tokens.css`:

- `--isl-dock-shell-start-width`
- `--isl-dock-shell-start-collapsed-width`
- `--isl-dock-shell-ai-width`
- `--isl-dock-workspace-end-width`
- `--isl-dock-workspace-inspector-width`
- `--isl-dock-mobile-panel-max-width`
- `--isl-dock-z-*`
- `--isl-dock-panel-transition`

Component CSS may provide temporary fallbacks while migrating legacy components, but new hardcoded panel widths should go into descriptors or tokens first.

Inline-end overlay panels must slide toward their actual inline edge. In LTR that means `translateX(100%)`; in RTL it means `translateX(-100%)`. Use `:dir(rtl)` or an equivalent logical-direction mechanism rather than physical left/right selectors.

## Host components

`Explore.Blazor.Client.Components.Docking` contains the first host layer:

- `DockLayoutHost` creates a scope grid with logical start/content/end tracks and a bottom row. It reads open `DockMode.Docked` panels from `DockLayoutState`, observes MudBlazor browser breakpoints, exposes width custom properties from runtime state, and collapses docked inline widths to `0px` on `Xs`/`Sm` breakpoints.
- `DockSideHost` renders ordered open docked panels for one `(DockScope, DockSide)` group on desktop and suppresses docked side-host rendering on mobile so temporary overlay chrome owns panel behavior.
- `DockPanelHost` renders descriptor-driven panel chrome with accessible labels, panel ids, mode/side data attributes, tokenized width, and logical borders.
- `DockOverlayHost` renders open `Overlay`, `Temporary`, and `Inspector` panels separately from persistent grid tracks. On mobile, it also projects open docked side panels into effective `Temporary` render entries while leaving runtime state in its desktop `Docked` mode, so resize/persistence semantics remain stable across breakpoints. Closing overlays remain mounted for the tokenized reverse slide/fade animation before unmounting.
- `DockTabStrip` renders accessible tabs when a side has multiple open docked panels. `DockSideHost` keeps the tabs ordered by runtime state, displays only the active panel content, and routes tab activation back through `DockLayoutState.Activate`.

These hosts now render the production shell and EventList workspace panels. Temporary bridge services remain only where toggles still need compatibility (`SidebarState`, `AiAssistantState`, and page-local EventList adapter booleans); do not remove those bridges until all consumers are audited and tests pass.

`Explore.Blazor.Client.Components.Shell.AppSideNav` is the extracted left navigation panel content. During the migration window it remains hosted by the existing `MainLayout` `MudDrawer`; later shell migration should register the same content as the `shell.left-nav` descriptor content instead of duplicating the navigation tree.

`Explore.Blazor.Client.Components.Shell.ShellDockPanels` owns the shell descriptors for `shell.left-nav` and `shell.ai-assistant`. `MainLayout` currently registers those descriptors and render fragments into `DockLayoutState` while preserving the legacy `MudDrawer` and `AiAssistantRail` rendering path. Legacy `SidebarState` and `AiAssistantState` changes are mirrored into dock state so the future `DockLayoutHost` migration can reuse the same shell descriptors without a second content model.

During the bridge period, hidden-chrome routes such as setup/onboarding/startup must mirror both shell descriptors closed, and layout disposal must unregister shell descriptors. These lifecycle behaviors are covered in `MainLayoutTests` so future host migration does not accidentally leave stale shell panels in scoped dock state.

Top navigation controls remain legacy-rendered during the bridge period. The sidebar and AI buttons still toggle `SidebarState` and `AiAssistantState` so the current `MudDrawer` and `AiAssistantRail` behavior is preserved, but the handlers also mirror through `shell.left-nav` and `shell.ai-assistant` when the shell descriptors are present. This keeps future `DockLayoutHost` migration aligned with the visible shell state without changing production rendering yet.

Before production shell host migration, `MainLayoutTests` must preserve the shell accessibility/navigation contract: the skip link targets `#main-content`, the main landmark keeps `tabindex="-1"`, visible-chrome routes expose header/footer/sidebar navigation landmarks, hidden-chrome routes keep skip/main/live-region anchors while hiding shell chrome, and navigation changes call `IAccessibilityFocusService.FocusOnNavigateAsync()`.

## Resize foundation

`DockResizeHandle` provides the first dormant resize affordance for resizable docked inline panels. `DockPanelHost` renders it only for `DockMode.Docked` panels on `DockSide.Start` or `DockSide.End` whose descriptors set `IsResizable == true`.

The handle is keyboard accessible:

- `ArrowLeft` and `ArrowRight` adjust width by the standard keyboard step.
- Holding `Shift` uses a larger step for faster resizing.
- `Home` requests the descriptor minimum width.
- `End` requests the descriptor maximum width.

Pointer support uses Blazor pointer events for the dormant mouse/touch-capable drag foundation. Pointer movement computes width deltas from the drag start position and sends requested widths through the same descriptor-clamped `DockLayoutState.Resize` path as keyboard resizing. Pointer cancellation ends the drag without applying later movement.

The component exposes `role="separator"`, `aria-orientation="vertical"`, `aria-controls`, and `aria-valuemin`/`aria-valuemax`/`aria-valuenow`. Width changes flow through `DockLayoutState.Resize`, so descriptor `MinWidth`, `MaxWidth`, and `IsResizable` remain the source of truth. Pointer capture is isolated in `/js/dock-resize.js` so drag can continue outside the handle bounds, while the Blazor component filters non-primary and mismatched pointer ids before applying width changes. Component tests cover the module import plus `setPointerCapture`/`releasePointerCapture` calls so the JS bridge remains part of the resize contract.

## Stacking foundation

Multiple docked panels can be registered on the same logical side. In the dormant host foundation, `DockSideHost` renders a `DockTabStrip` when more than one open docked panel exists for a `(DockScope, DockSide)` group. The tab strip uses `role="tablist"` and `role="tab"`, links the active tab to the active panel body with `aria-controls`, renders that body as `role="tabpanel"` labelled by the active tab, and activates panels through `DockLayoutState.Activate`. Keyboard activation moves focus to the newly activated tab through `IAccessibilityFocusService` so roving `tabindex` stays coherent.

The first stack implementation is intentionally tabbed only. Arbitrary split panes, detachable tabs, and complex workbench nesting remain out of scope until a concrete product need appears.

## Snapshots

`DockLayoutSnapshot` stores only panels whose descriptors opt into `PersistState`. During restore, widths are clamped to the current descriptor bounds, non-resizable panels keep their current width, and currently open non-closable panels cannot be closed by imported state. Active state is normalized so each scope/side group with open panels has exactly one active open panel. If an imported snapshot marks multiple panels active in the same group, or opens panels without an active item, the engine keeps or promotes the earliest runtime order and clears the rest.

## Guardrails

- Do not use page-level shell compensation hacks to account for global rails.
- Do not use physical CSS direction properties in new dock CSS; use `inline-start`, `inline-end`, `padding-inline-*`, `margin-inline-*`, and logical borders.
- Do not migrate EventList detail preview until regression coverage exists; it must remain inspector/overlay style.
- Do not remove `SidebarState`, `AiAssistantState`, or `RightSidebar` until all consumers are migrated and tests pass.
- Do not introduce external/plugin panel loading in this phase; registration is compile-time and component-owned.

`Event.Architecture.Tests/DockLayoutArchitectureTests.cs` enforces the descriptor-driven contract by rejecting central panel enums and new page-scoped CSS shell compensation outside known legacy migration debt. Keep this test focused on preventing regressions while the visual baseline gap blocks production shell/workspace host migration.

## Migration path

1. Baseline tests and stable selectors for existing shell/workspace panels.
2. Dock tokens and this architecture document.
3. Generic dock engine and service registration.
4. Shell host migration for left nav and AI rail.
5. Workspace host migration for EventList customization and inspector preview.
6. Resize, stacking, persistence, mobile, RTL, accessibility, motion, and cleanup phases.
