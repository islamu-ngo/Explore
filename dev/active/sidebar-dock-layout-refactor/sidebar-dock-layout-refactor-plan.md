<!-- ABOUTME: Strategic implementation plan for the generic dock layout engine refactor. -->
<!-- ABOUTME: Defines phased workbench-style docking architecture for shell and workspace side panels. -->

# Sidebar Dock Layout Refactor - Implementation Plan v2

Last Updated: 2026-04-30

## Executive Summary

The Blazor web app currently has a strong visual sidebar experience, but it is implemented through several unrelated mechanisms: a MudBlazor left `MudDrawer`, a custom fixed AI rail, a custom page-level `RightSidebar`, a temporary event detail drawer, page-local booleans, and page-specific CSS compensation. The visible gap between the event-list customization panel and the AI rail is evidence that the layout model is leaking.

The revised target is a generic internal dock engine used by an explicit App Shell and Workspace Layout. This is not an external plugin system and not an uncontrolled abstraction. It is a deliberately designed workbench foundation, closer to Obsidian/Zed/VS Code-style layouts, implemented incrementally so the current good UX is preserved.

The first consumers are the existing panels: shell left navigation, shell AI assistant, EventList Customize View, and EventList detail preview. The event detail preview must remain visually protected as an inspector/overlay panel.

## Strategic Principle

Build an explicit App Shell plus Workspace Layout on top of a generic internal docking engine. Do not implement external/plugin-driven docking yet, but the internal model must support dynamic descriptor registration, resizable panels, stacked panels, overlays, future panel additions, snapshots, and persistence without central enum rewrites.

## Non-Negotiable Constraints

1. No page-specific `margin-right` or `margin-left` compensation for global shell panels.
2. No negative margin escape hatches in EventList layout.
3. No duplicated hardcoded sidebar widths in page CSS or component CSS.
4. No fixed-position persistent desktop panels unless they are true overlays.
5. Desktop persistent panels are CSS grid tracks.
6. Mobile panels are temporary overlays with backdrop, focus restore, and scroll locking.
7. Event detail preview remains inspector/overlay style and must not be degraded.
8. Use logical CSS properties for start/end support and RTL readiness.
9. Add regression coverage before removing the old implementation.
10. No central enum must be edited for every future panel.
11. Every panel must have a stable `DockPanelId`.
12. Every panel must register with a `DockPanelDescriptor`.
13. Runtime panel state must be separate from descriptor metadata.
14. Docked, overlay, temporary, inspector, and collapsed modes must be modeled explicitly.
15. Resizable panel support must be part of the architecture, even if implemented after the first migration.
16. Multiple panels per side must be supported by the data model.
17. Layout snapshots must exist for reset, persistence, debugging, tests, and future user preferences.
18. Mobile behavior must be a policy of the panel/layout, not page-specific CSS.
19. The dock engine must not depend on EventList or event-specific concepts.
20. Event-specific panels depend on the dock engine, not the other way around.

## Research Summary

Local repo findings are authoritative for current behavior. Official documentation and library source were used to validate framework expectations.

| Source | Finding |
|---|---|
| `CLAUDE.md` | Every change must follow repo contribution contract, Clean Architecture boundaries, and verification requirements. |
| `docs/BLAZOR.md` | Scoped services are appropriate for cross-component UI state when URL state is insufficient; CSS isolation and wrappers are preferred. |
| `docs/DESIGN_SYSTEM.md` | Global CSS uses `reset -> base -> tokens -> mudblazor-overrides -> components -> utilities`; drawer/overlay MudBlazor overrides are approved only when documented. |
| `docs/ACCESSIBILITY.md` | Page shell owns skip link, main landmark, sidebar navigation, live regions, focus-on-navigate, logical CSS properties, focus restore, and WCAG 2.2 AA expectations. |
| `docs/BLAZOR_DEV_WORKFLOW.md` | UI work requires full build and visual verification cycle; scoped CSS changes require rebuild. |
| `.claude/skills/blazor-ui-conventions` | MudBlazor v9 APIs, wrapper components, EventCallback flow, and BFF-safe UI rules apply. |
| `.claude/skills/blazor-css-isolation` | Component CSS isolation, BEM, native CSS nesting, and limited `::deep` usage apply. |
| `.claude/skills/design-system` | Layout widths, z-index, and motion tokens belong in token/design-system layer, not page CSS. |
| MudBlazor drawer docs/source | `Persistent` drawers push content, `Responsive` drawers switch behavior, `ClipMode` is evaluated only when drawers are directly inside `MudLayout`, and `@bind-Open` is recommended for self-closing behavior. |
| Microsoft Blazor CSS isolation docs | `.razor.css` scopes styles to component output; `::deep` is needed only for descendants/child component internals. |
| Microsoft Blazor state management docs | Scoped in-memory state containers with `OnChange` are appropriate for per-circuit app state; consumers must unsubscribe and use renderer-safe updates. |
| MDN CSS Grid docs | CSS grid is appropriate for major page regions and explicit track sizing. |

Tooling note: Tavily MCP and context7 MCP were available during the 2026-04-30 implementation sessions. They confirmed the MudBlazor `MudLayout`/`MudDrawer` guidance, drawer variant tradeoffs, and Blazor CSS isolation constraints used by this plan.

## Current State Analysis

All file and class references in this section were re-verified during the 2026-04-30 implementation sessions.

| Concern | Verified file/class | Current behavior |
|---|---|---|
| Main layout markup | `Explore.Blazor.Client/Layout/MainLayout.razor` | Owns `MudThemeProvider`, `MudLayout`, skip link, header, shell `DockLayoutHost`, `MudMainContent`/`main#main-content`, `Footer`, and ARIA live regions; shell left nav and AI render through dock descriptors. |
| Main layout code-behind | `Explore.Blazor.Client/Layout/MainLayout.razor.cs` | Injects temporary shell bridge states plus `DockLayoutState`; registers `shell.left-nav` and `shell.ai-assistant` descriptors, mirrors bridge open state into dock state, refreshes descriptor content after settings changes, and unregisters shell descriptors on disposal. |
| Main layout CSS | `Explore.Blazor.Client/Layout/MainLayout.razor.css` | Shell CSS no longer uses `main-layout__main--ai-open` or hardcoded AI margin compensation; shell panel sizing is owned by dock descriptors/grid tracks. |
| Top nav | `Explore.Blazor.Client/Layout/NavMenu.razor` and `.cs` | Toggles shell bridge state and mirrors actions through `shell.left-nav` and `shell.ai-assistant` dock ids when descriptors are registered. |
| Left sidebar state | `Explore.Blazor.Client/Services/SidebarState.cs` | Tracks `IsOpen` and `HasSidebar`; not a panel registry model. |
| AI rail state | `Explore.Blazor.Client/Services/AiAssistantState.cs` | Tracks `IsOpen` and `IsAvailable`; separate from shell layout state. |
| AI rail component | `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` and `.css` | Supports dock-hosted mode for `shell.ai-assistant`, suppressing legacy backdrop/fixed-position behavior while retaining stable selectors, dock tokens, and RTL-aware slide transforms. |
| Event list page | `Explore.Blazor.Client/Pages/Events/EventList.razor` | Owns the workspace `DockLayoutHost`, main event list content, Customize View panel content, and Event Preview inspector content. |
| Event list code-behind | `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` | Registers `events.customize-view` and `events.event-preview`, mirrors temporary page booleans into dock state, and enforces customize/preview mutual exclusion through the dock engine. |
| Event list CSS | `Explore.Blazor.Client/Pages/Events/EventList.razor.css` | Legacy width-expansion escape has been removed; preview styling is retargeted to dock panel hosts with logical selectors. |
| Generic right sidebar | `Explore.Blazor.Client/Components/Common/RightSidebar.razor` and `.css` | Legacy sticky/fixed panel remains for any other consumers, but EventList no longer depends on it. |
| Dock engine foundation | `Explore.Blazor.Client/Services/Docking/*` | Implemented descriptor-driven state, scoped registry, snapshot normalization, capability enforcement, and tests. |
| Dock host foundation | `Explore.Blazor.Client/Components/Docking/*` | Host components render registered docked/overlay panels; shell rendering and EventList workspace rendering are migrated. `DockLayoutHost` observes browser viewport changes, feeds actual width into `DockLayoutState`, collapses docked widths to `0px` on mobile, and routes mobile docked panels through `DockOverlayHost` as effective temporary overlays. `DockOverlayHost` provides shared backdrop, scroll lock, Escape close, focus handoff, focus restore, and reverse close animations for overlay/temporary/inspector/mobile-docked modes. `DockLayoutState` owns generic responsive policy for start-panel retraction and constrained/mobile one-end-panel behavior across scopes. Start/end docked panels have keyboard-accessible pointer-drag resize foundation with pointer-capture hardening and JSInterop invocation coverage, and multiple panels on one side render through a tabbed stack foundation with coherent tabpanel linkage and keyboard focus movement. |
| Shell accessibility contract | `Explore.Blazor.Client.Tests/Layout/MainLayoutTests.cs` | Covers skip link, main landmark, header/footer/sidebar landmarks, dock-hosted shell panels, ARIA live regions, hidden-chrome accessibility anchors, and focus-on-navigate after production host migration. |
| Dock governance guardrails | `Event.Architecture.Tests/DockLayoutArchitectureTests.cs` | Prevents central dock panel enum regressions and new page-scoped shell compensation outside known legacy migration debt. |
| Customize view content | `Explore.Blazor.Client/Pages/Events/Components/EventListCustomizationDrawer.razor` and `.cs/.css` | Good content component; layout wrapper is page-specific. |
| Event detail preview | `EventList.razor` | Uses `events.event-preview` as a workspace inspector through `DockOverlayHost`; `_detailDrawerOpen` remains only as a temporary backdrop/card-selection adapter. |

## Implementation Inventory

This inventory is updated as slices land so future sessions do not rediscover or recreate completed files.

| File | Purpose | Status |
|---|---|---|
| `Explore.Blazor.Client/Services/Docking/DockPanelId.cs` | Stable typed panel identifier. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockScope.cs` | Shell/workspace scope enum. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockSide.cs` | Start/end/bottom side enum. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockMode.cs` | Docked/overlay/temporary/inspector/collapsed mode enum. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockPanelDescriptor.cs` | Panel metadata contract. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockPanelState.cs` | Runtime state contract. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockLayoutSnapshot.cs` | Serializable layout state snapshot. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockPanelEntry.cs` | Descriptor/content/state registry entry. | Implemented |
| `Explore.Blazor.Client/Services/Docking/IDockPanelRegistry.cs` | Controlled panel registration abstraction. | Implemented |
| `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs` | Runtime state engine, registry implementation, and orchestration. | Implemented |
| `Explore.Blazor.Client/Components/Docking/DockLayoutHost.razor` and `.razor.css` | Scope host, viewport-aware grid boundary, and mobile docked-width policy. | Implemented and production-rendered for shell/workspace; actual viewport width drives responsive dock policy; mobile docked widths collapse to `0px` through viewport policy |
| `Explore.Blazor.Client/Components/Docking/DockSideHost.razor` and `.razor.css` | Ordered side panel host. | Implemented and suppresses docked side-host rendering on mobile so overlay chrome owns temporary behavior |
| `Explore.Blazor.Client/Components/Docking/DockPanelHost.razor` and `.razor.css` | Individual docked/overlay panel renderer. | Implemented as dormant host foundation |
| `Explore.Blazor.Client/Components/Docking/DockOverlayHost.razor` and `.razor.css` | Overlay/inspector/temporary panel renderer with shared backdrop, scroll lock, Escape close, focus handoff, focus restore, mobile docked-panel projection behavior, and reverse close animations. | Implemented and hardened as Phase 9 host-level overlay/mobile/motion foundation |
| `Explore.Blazor.Client/Components/Docking/DockTabStrip.razor` and `.razor.css` | Accessible tab strip for multiple open docked panels on one side, including tabpanel linkage and focus movement on keyboard activation. | Implemented as dormant stack foundation |
| `Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs` | Shell-owned stable ids and descriptors for left nav and AI assistant. | Implemented behind legacy rendering with hidden-chrome/disposal and NavMenu toggle bridge coverage |
| `Explore.Blazor.Client/Services/Docking/IDockLayoutPersistence.cs` | Snapshot load/save/delete abstraction for layout-keyed dock snapshots. | Implemented as Phase 8 non-rendering slice |
| `Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs` | Initial client-side persistence implementation behind the approved JS interop boundary. | Implemented as Phase 8 non-rendering slice |
| `Explore.Blazor.Client/Services/Docking/DockFocusManager.cs` | Optional future extraction if focus policy outgrows `DockOverlayHost`. | Deferred; `DockOverlayHost` currently owns first-slice focus save/restore directly through `IAccessibilityFocusService` |
| `Explore.Blazor.Client/Components/Docking/DockResizeHandle.razor` and `.razor.css` | Keyboard-accessible and pointer-drag resize affordance wired through `DockLayoutState.Resize`, including pointer capture, pointer identity filtering, and bUnit coverage for the pointer-capture JS helper. | Implemented as dormant resize foundation |
| `Explore.Blazor.Client/Components/Shell/AppSideNav.razor` and `.razor.css` | Extracted shell left nav panel content while preserving the legacy `MudDrawer` host. | Implemented behind legacy rendering and registered as `shell.left-nav` content |
| `Explore.Blazor.Client/Components/Shell/AppRightRail.razor` and `.razor.css` | Optional shell right rail wrapper for AI assistant content. | Future phase |
| `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs` | Event module dock panel descriptors for `events.customize-view` and `events.event-preview`. | Implemented |
| `docs/DOCK_LAYOUT.md` | Platform-level dock layout architecture document. | Implemented |
| `Event.Architecture.Tests/DockLayoutArchitectureTests.cs` | Governance tests for descriptor-driven dock contracts. | Implemented for central enum and page compensation guardrails |

## Proposed Future State

### Architecture Shape

```text
MainLayout / AppShellLayout
  DockLayoutHost Scope=Shell
    TopBar
    ShellBody
      DockSideHost Side=Start
        shell.left-nav
      MainWorkspaceRegion
        Page content
          DockLayoutHost Scope=Workspace LayoutKey=route/module
            WorkspaceMainContent
            DockSideHost Side=End
              events.customize-view
            DockOverlayHost
              events.event-preview
      DockSideHost Side=End
        shell.ai-assistant
```

### Dock Engine Responsibilities

| Component/service | Responsibility |
|---|---|
| `DockPanelDescriptor` | Immutable metadata: what the panel is and how it should default. |
| `DockPanelState` | Runtime state: open/closed, mode, width, order, active state. |
| `IDockPanelRegistry` | Controlled descriptor/content registration abstraction. |
| `DockLayoutState` | Runtime orchestration, registry implementation, open/close/toggle/resize/activate/snapshot. |
| `DockLayoutHost` | Provides shell or workspace layout boundary and grid variables. |
| `DockSideHost` | Renders ordered panels for a scope/side and supports the dormant tabbed stack policy. |
| `DockPanelHost` | Renders a docked/collapsed panel. |
| `DockOverlayHost` | Renders overlay, temporary, and inspector panels. |
| `DockResizeHandle` | Width resizing and keyboard-accessible resizing. |
| `DockFocusManager` | Focus save, restore, and active overlay focus behavior. |
| `IDockLayoutPersistence` | Snapshot persistence abstraction. |

### Core Model

```csharp
public sealed record DockPanelId(string Value);

public enum DockScope
{
    Shell,
    Workspace
}

public enum DockSide
{
    Start,
    End,
    Bottom
}

public enum DockMode
{
    Docked,
    Overlay,
    Temporary,
    Inspector,
    Collapsed
}

public sealed record DockPanelDescriptor(
    DockPanelId Id,
    DockScope Scope,
    DockSide Side,
    DockMode DefaultMode,
    string Title,
    string AriaLabel,
    int DefaultWidth,
    int MinWidth,
    int MaxWidth,
    int Order,
    bool IsResizable,
    bool CanClose,
    bool PersistState);

public sealed record DockPanelState(
    DockPanelId Id,
    bool IsOpen,
    DockMode Mode,
    int Width,
    int Order,
    bool IsActive);

public sealed record DockLayoutSnapshot(
    string LayoutKey,
    IReadOnlyList<DockPanelState> Panels,
    DateTimeOffset UpdatedAt);
```

### Registry And Runtime State

```csharp
public interface IDockPanelRegistry
{
    void Register(DockPanelDescriptor descriptor, RenderFragment content);
    void Unregister(DockPanelId id);
    IReadOnlyList<DockPanelDescriptor> GetPanels(DockScope scope, DockSide side);
}
```

```csharp
public sealed class DockLayoutState
{
    public event Action? Changed;

    public void Register(DockPanelDescriptor descriptor);
    public void Unregister(DockPanelId id);
    public void Open(DockPanelId id);
    public void Close(DockPanelId id);
    public void Toggle(DockPanelId id);
    public void SetMode(DockPanelId id, DockMode mode);
    public void Resize(DockPanelId id, int width);
    public void Activate(DockPanelId id);
    public DockLayoutSnapshot CreateSnapshot(string layoutKey);
    public void RestoreSnapshot(DockLayoutSnapshot snapshot);
    public IReadOnlyList<DockPanelState> GetPanels(DockScope scope, DockSide side);
}
```

### Descriptor Examples

```csharp
public static class EventDockPanels
{
    public static readonly DockPanelDescriptor CustomizeView = new(
        Id: new DockPanelId("events.customize-view"),
        Scope: DockScope.Workspace,
        Side: DockSide.End,
        DefaultMode: DockMode.Docked,
        Title: "Customize View",
        AriaLabel: "Customize event list view",
        DefaultWidth: 320,
        MinWidth: 280,
        MaxWidth: 480,
        Order: 100,
        IsResizable: true,
        CanClose: true,
        PersistState: true);

    public static readonly DockPanelDescriptor EventPreview = new(
        Id: new DockPanelId("events.event-preview"),
        Scope: DockScope.Workspace,
        Side: DockSide.End,
        DefaultMode: DockMode.Inspector,
        Title: "Event Preview",
        AriaLabel: "Event details preview",
        DefaultWidth: 420,
        MinWidth: 360,
        MaxWidth: 640,
        Order: 200,
        IsResizable: true,
        CanClose: true,
        PersistState: false);
}
```

## CSS Layout Model

Use grid tracks for persistent desktop panels:

```css
.dock-layout-host {
    display: grid;
    grid-template-columns:
        var(--dock-start-width, 0px)
        minmax(0, 1fr)
        var(--dock-end-width, 0px);
    min-block-size: calc(100dvh - var(--mud-appbar-height));
    transition: grid-template-columns var(--isl-motion-panel-duration) var(--isl-motion-panel-easing);
}
```

Use overlay transforms for temporary/mobile/inspector panels:

```css
.dock-overlay-host__panel {
    position: fixed;
    inset-block-start: var(--mud-appbar-height);
    inset-block-end: 0;
    inset-inline-end: 0;
    inline-size: var(--dock-overlay-width, var(--isl-dock-overlay-width));
    transform: translateX(100%);
    transition: transform var(--isl-motion-panel-duration) var(--isl-motion-panel-easing);
}
```

## Clean Architecture Impact

| Layer | Expected changes |
|---|---|
| Domain | None. |
| Application | None. |
| Persistence | None. |
| Infrastructure | None. |
| API | None. |
| Blazor Client | Primary implementation: docking services, layout components, tokens, EventList integration, tests. |
| Blazor Server/BFF | No auth/BFF behavior changes expected. |

## Implementation Phases

### Phase 1: Baseline Tests And Visual Freeze

Purpose: protect current UX before refactoring.

Tasks:

- Create `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`.
- Capture desktop states: left nav open and AI closed, left nav open and AI open, customize panel open and AI open, event detail preview open.
- Capture mobile states: left nav open, customize view open, event detail preview open.
- Add stable panel selectors or data attributes as needed.
- Add bUnit regressions for current AI availability, customize/detail mutual exclusion, event detail close/reset, and shell landmarks.

Acceptance criteria:

- Current good UX is frozen before layout changes.
- Event detail preview has visual coverage before migration.
- Visual tests can catch gaps between customize panel and AI rail.

Effort: L.

### Phase 2: Design Tokens And Dock Contract

Purpose: define the platform-level contract before behavior changes.

Tasks:

- Update `Explore.Blazor/wwwroot/css/tokens.css` with dock width, z-index, and motion tokens.
- Add shell defaults: left nav, collapsed nav, AI rail.
- Add workspace defaults: right panel, inspector overlay, mobile panel.
- Add motion tokens and reduced-motion handling.
- Create `docs/DOCK_LAYOUT.md`.
- Document descriptors, state, scope, side, mode, stack behavior, resize behavior, mobile behavior, persistence, and reset behavior.

Acceptance criteria:

- Widths and motion are token-driven.
- `docs/DOCK_LAYOUT.md` becomes the source of truth for panel architecture.
- The contract explicitly bans central panel enums and page-level shell compensation.

Effort: M.

### Phase 3: Dock Engine Core

Purpose: implement the generic internal model without rendering migration yet.

Tasks:

- Create `DockPanelId`, `DockScope`, `DockSide`, `DockMode`.
- Create `DockPanelDescriptor`, `DockPanelState`, `DockLayoutSnapshot`.
- Create `IDockPanelRegistry` and `DockPanelRegistry`.
- Create `DockLayoutState` with register, unregister, open, close, toggle, mode, resize, activate, snapshot, and restore methods.
- Register services in `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`.
- Add unit tests for descriptor registration, duplicate ID handling, open/close/toggle, resize min/max clamping, activation, multiple panels per side, and snapshot restore.

Acceptance criteria:

- New panels can be modeled without editing central enums.
- Descriptor metadata and runtime state are separate.
- At least two panels can exist on the same side in the model.
- Panel widths can be controlled by state.
- Layout state can serialize and restore snapshots.
- Dock engine has no event-specific dependencies.

Effort: L.

### Phase 4: Shell Dock Host

Purpose: migrate shell left nav and AI assistant onto the dock engine.

Tasks:

- Create `DockLayoutHost`, `DockSideHost`, `DockPanelHost`, and `DockOverlayHost` initial versions.
- Extract left nav content into `AppSideNav`. *(Implemented and rendered through `shell.left-nav`.)*
- Convert AI assistant into `shell.ai-assistant` descriptor and content. *(Implemented and rendered through `DockLayoutHost`.)*
- Convert left navigation into `shell.left-nav` descriptor and content. *(Implemented and rendered through `DockLayoutHost`.)*
- Refactor `MainLayout.razor`, `.razor.cs`, and `.razor.css` to use shell `DockLayoutHost`. *(Implemented and verified.)*
- Update `NavMenu` to toggle dock panels by `DockPanelId`.
- Remove shell AI `margin-right: 360px` compensation.
- Preserve skip link, header, main landmark, footer, live regions, and focus-on-navigate.

Acceptance criteria:

- AI assistant opens without page-level margin changes.
- Left nav and AI assistant render through the dock engine.
- Shell desktop panels are grid tracks.
- Shell mobile panels are overlays.
- Current visual shell behavior is preserved.

Effort: XL.

### Phase 5: Workspace Dock Host

Purpose: migrate EventList page panels onto workspace dock host.

Tasks:

- Add workspace use of `DockLayoutHost` inside `EventList.razor` or a reusable workspace wrapper.
- Create `EventDockPanels` descriptors for `events.customize-view` and `events.event-preview`.
- Register EventList panel descriptors and content during page lifecycle.
- Host `EventListCustomizationDrawer` as a docked workspace panel.
- Host event detail preview as inspector/overlay through `DockOverlayHost`.
- Remove `_customizationDrawerOpen` and `_detailDrawerOpen` or reduce them to temporary adapters during migration only.
- Remove EventList negative margin and width expansion CSS.
- Remove EventList dependency on `RightSidebar`.

Acceptance criteria:

- Customize View is a workspace docked panel on desktop.
- Event Preview is a workspace inspector/overlay.
- Customize View and AI rail align with no visible gap.
- Event detail preview desktop/mobile UX is preserved.
- EventList settings behavior remains unchanged.

Effort: XL.

### Phase 6: Resize Support

Purpose: make panel widths state-driven and user-adjustable.

Tasks:

- Create `DockResizeHandle.razor` and `.razor.css`.
- Add pointer-event drag resizing for desktop mouse/touch-capable pointers.
- Add pointer capture hardening so drag continues outside handle bounds.
- Enforce descriptor min/max widths in `DockLayoutState.Resize`.
- Add keyboard resizing: arrow keys adjust width, Shift plus arrow adjusts faster.
- Add ARIA slider semantics where appropriate: `aria-valuemin`, `aria-valuemax`, `aria-valuenow`, labelled resize handle.
- Add tests for min/max enforcement and keyboard resize behavior.

Acceptance criteria:

- Width changes update `DockPanelState`.
- Resizing cannot cause layout overflow.
- Resize handle is keyboard accessible.
- Visual tests include at least one resized panel.

Effort: L.

### Phase 7: Stacking Support

Purpose: support multiple panels on one side without forcing all to render as separate full-width siblings.

Tasks:

- Add side-host stacking policy to `DockSideHost`.
- Implement initial tabbed stack rendering through `DockTabStrip`.
- Support ordered panels and one active panel per side by default.
- Keep split-stack rendering as a future extension unless explicitly needed.
- Add tests with two panels registered on the same side.

Acceptance criteria:

- Multiple panels per side are represented by the model.
- `DockSideHost` can render ordered tabs and active panel content.
- Future modules can add panels without editing shell/workspace enums.

Effort: L.

### Phase 8: Persistence And Reset — Implemented As Non-Rendering Slice

Purpose: support local user layout preference and future cross-device settings integration.

Tasks:

- Create `IDockLayoutPersistence`. **Done.**
- Implement `LocalStorageDockLayoutPersistence` or project-consistent browser storage adapter. **Done under `Services/Interop` to satisfy JS interop architecture boundaries.**
- Save/load/delete `DockLayoutSnapshot` by layout key. **Done with schema-versioned local storage envelope.**
- Add reset behavior to restore descriptor defaults. **Done via `DockLayoutState.ResetToDefaults()`.**
- Design interface so later integration with user appearance/settings API is possible. **Done: callers depend on `IDockLayoutPersistence`, not local storage directly.**
- Add tests for snapshot serialization, corrupt/unsupported snapshots, unknown-panel restore, width clamping, and reset. **Done.**

Acceptance criteria:

- User layout preferences can be serialized into a snapshot.
- System can reset to default layout.
- Persistence starts local and does not block future server-side settings integration.
- Production auto-hydration/autosave remains intentionally deferred until `DockLayoutHost` owns visible shell/workspace rendering.

Effort: M-L.

### Phase 9: Mobile, RTL, Accessibility, And Motion Hardening

Purpose: make the dock engine enterprise-grade across devices and languages.

Tasks:

- Ensure mobile behavior is a descriptor/layout policy, not page CSS.
- Ensure temporary panels use consistent backdrop and scroll locking.
- Route mobile docked panels through temporary overlay chrome instead of CSS-hidden side hosts.
- Ensure Escape closes the active temporary/overlay panel.
- Ensure focus returns to opener after close.
- Ensure closing overlays animate out with the reverse of their opening transform before unmounting.
- Ensure responsive policy closes start panels earlier when right-side panels consume content width, and keeps one end-side panel open on constrained/mobile widths.
- Ensure temporary overlays trap focus or use MudBlazor behavior that traps focus.
- Ensure persistent docked panels do not trap focus.
- Add `aria-expanded`, `aria-controls`, labels, and region semantics.
- Audit all new CSS for logical properties.
- Add reduced-motion behavior to all panel transitions.
- Test or manually verify RTL and reduced motion.

Acceptance criteria:

- Mobile sidebars overlay cleanly and do not cause horizontal overflow.
- RTL does not require rewriting layout code.
- Reduced-motion mode works.
- Accessibility contract from `docs/ACCESSIBILITY.md` is preserved or improved.

Effort: L.

### Phase 10: Cleanup Old Layout Systems

Purpose: remove fragmented implementations only after migration is proven.

Tasks:

- Remove references to `SidebarState` and `AiAssistantState`.
- Remove old state service registrations.
- Remove or deprecate `RightSidebar` after all usages migrate.
- Remove `main-layout__main--ai-open` and `margin-right: 360px` behavior.
- Remove EventList negative margin and width expansion CSS.
- Remove duplicated width constants outside tokens/descriptors.
- Update tests to target dock engine services and hosts.

Acceptance criteria:

- Old fragmented sidebar mechanisms are gone.
- No page-specific shell panel compensation remains.
- No duplicated panel width constants remain outside approved locations.

Effort: M.

### Phase 11: Documentation, Governance, And Verification

Purpose: document the platform-level dock architecture and prove the refactor.

Tasks:

- Finalize `docs/DOCK_LAYOUT.md`.
- Update `docs/BLAZOR.md` with a pointer to `docs/DOCK_LAYOUT.md`.
- Update this dev-docs package after implementation decisions.
- Architecture tests prevent new page-level shell compensation outside known legacy migration debt and central panel enum regressions.
- Run build, Blazor client tests, architecture tests, accessibility tests, and feasible E2E visual tests.

Required verification commands:

```bash
rtk dotnet build --configuration Release --verbosity quiet
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

Acceptance criteria:

- Build passes.
- Blazor client tests pass.
- Architecture/accessibility tests pass.
- E2E visual tests pass or environment blockers are documented with manual screenshots.
- Dock architecture is documented as a reusable platform subsystem.

Effort: M.

## Success Metrics

| Metric | Target |
|---|---|
| Visual gap between customize panel and AI rail | 0 unexpected gap in desktop test case. |
| Page-level AI compensation | 0 occurrences. |
| Negative margin EventList layout hacks | 0 occurrences. |
| Duplicated panel width constants | 0 outside tokens/descriptors and justified fallbacks. |
| Event detail preview regression | No visual or interaction regression in desktop/mobile tests. |
| New panel extensibility | New panels register descriptor and content without editing central enums. |
| Multi-panel model | At least two panels can exist on the same side in state and registry. |
| Resize state | Panel widths are controlled by `DockPanelState`. |
| Snapshot support | Layout state can serialize, restore, and reset. |
| Engine independence | Dock engine has no event-specific concepts. |
| RTL readiness | No new banned physical CSS properties in refactored dock CSS. |
| Reduced motion | Panel animations disabled/minimized under reduced motion. |
| Test status | Build, bUnit, architecture/accessibility, and feasible E2E tests pass. |

## Required Resources And Dependencies

| Resource | Why needed |
|---|---|
| MudBlazor v9 behavior | Temporary mobile drawers and overlay behaviors may still use MudBlazor. |
| Playwright/E2E environment | Required to freeze and verify visual panel combinations. |
| Accessibility services | `IAccessibilityFocusService` is required for focus save/restore around overlays. |
| Design tokens | Widths, motion, z-index, and overlay tokens centralize layout behavior. |
| Existing EventList settings workflows | Customize panel content logic must be preserved while moving layout ownership. |
| Browser storage or JS interop | Initial snapshot persistence needs local client storage. |

## Risk Assessment And Mitigation

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Event detail preview UX regresses | Medium | High | Freeze visual tests first; migrate as inspector/overlay, not docked panel. |
| Generic engine becomes uncontrolled | Medium | High | Keep compile-time descriptor registration; do not implement external plugin loading. |
| Scope grows too large | Medium | High | Implement core, hosts, resizing, stacking, persistence in separate phases. |
| E2E environment is unstable | High | Medium | Add bUnit/state tests and document manual visual verification when E2E cannot run. |
| RTL issues appear late | Medium | Medium | Use logical properties from first CSS change; audit with architecture/accessibility tests. |
| Footer behavior breaks | Medium | High | Keep footer in main workspace region and add screenshots for short/long pages. |
| Persistence conflicts with future settings API | Low | Medium | Depend on `IDockLayoutPersistence` abstraction and start with local storage only. |

## Final Acceptance Criteria

- [ ] Opening AI assistant does not require page-level margin changes.
- [ ] Opening Customize View beside AI creates no visible gap.
- [ ] Footer behavior remains correct for page-level panels.
- [ ] Left shell nav remains independent of page scroll.
- [ ] AI assistant remains independent of page scroll.
- [ ] Event detail panel still looks good on desktop and mobile.
- [ ] Mobile sidebars overlay cleanly and do not cause horizontal overflow.
- [ ] No duplicated panel width constants remain.
- [ ] No negative margin layout hacks remain.
- [ ] RTL does not require rewriting the layout.
- [ ] Reduced-motion mode works.
- [ ] Visual tests cover major panel combinations.
- [ ] New panels can be added by registering a descriptor and content without editing central enums.
- [ ] At least two panels can exist on the same side in the model.
- [ ] Panel widths can be controlled by state, not only static CSS.
- [ ] User layout preferences can be serialized into a snapshot.
- [ ] The system can reset to default layout.
- [ ] Future modules can define their own dock panel descriptors.
- [ ] The dock engine does not depend on event-specific concepts.
- [ ] Event-specific panels depend on the dock engine, not the other way around.

## Potential Risks & Unknowns

The highest-risk area remains proving the migrated shell/workspace overlays visually across desktop, mobile, and RTL after moving shell panels, Customize View, and Event Preview into dock hosts. Mobile docked panels now route through temporary overlay chrome at `Xs`/`Sm` breakpoints, but enabled desktop/mobile visual evidence is still needed before removing temporary adapters. The second highest risk is allowing the generic docking engine to become an uncontrolled plugin system. Keep registration compile-time and component-owned for now, while designing the internal model for future extensibility.
