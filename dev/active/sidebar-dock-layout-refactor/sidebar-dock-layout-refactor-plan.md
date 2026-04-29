<!-- ABOUTME: Strategic implementation plan for rebuilding sidebar and sidepanel layout architecture. -->
<!-- ABOUTME: Defines explicit App Shell plus Workspace Layout refactor phases, constraints, tests, and acceptance criteria. -->

# Sidebar Dock Layout Refactor - Implementation Plan

Last Updated: 2026-04-29

## Executive Summary

The Blazor web app currently has a visually strong sidebar experience, but the implementation is fragmented across unrelated mechanisms: a MudBlazor left `MudDrawer`, a custom fixed AI rail, a custom page-level `RightSidebar`, a temporary event detail drawer, page-local booleans, and page-specific CSS compensation. This works in selected combinations but is already leaking through visible gaps and brittle layout math.

The approved refactor is a layout subsystem rebuild, not a generic docking framework. The target is an explicit, typed, maintainable App Shell plus Workspace Layout system that preserves the current good UX while removing hacks, duplicated widths, independent panel state services, and page-specific compensation.

The implementation must preserve the event detail preview UX. The event detail panel is an overlay/inspector experience, not a persistent docked workspace panel. The customize-view panel is a persistent workspace dock on desktop and temporary overlay on mobile.

## Non-Negotiable Constraints

1. No page-specific `margin-right` or `margin-left` compensation for global shell panels.
2. No negative margin escape hatches in EventList layout.
3. No duplicated hardcoded sidebar widths in page CSS or component CSS.
4. No fixed-position persistent desktop panels unless they are true overlays.
5. No independent state service per panel type after migration.
6. Desktop persistent panels are CSS grid tracks.
7. Mobile panels are temporary overlays with backdrop, focus restore, and scroll locking.
8. Event detail preview remains overlay/inspector style and must not be degraded.
9. Use logical CSS properties for start/end support and RTL readiness.
10. Add regression coverage before removing the old implementation.

## Research Summary

Local repo findings are authoritative for current behavior. Official documentation and library source were used to validate framework expectations.

Verified local sources:

| Source | Finding |
|---|---|
| `CLAUDE.md` | Every change must follow repo contribution contract, Clean Architecture boundaries, and verification requirements. |
| `docs/BLAZOR.md` | Scoped services are appropriate for cross-component UI state when URL state is insufficient; CSS isolation and wrappers are preferred. |
| `docs/DESIGN_SYSTEM.md` | Global CSS uses `reset -> base -> tokens -> mudblazor-overrides -> components -> utilities`; drawer/overlay MudBlazor overrides are the only approved global `.mud-*` exceptions. |
| `docs/ACCESSIBILITY.md` | Page shell owns skip link, main landmark, sidebar navigation, live regions, focus-on-navigate, logical CSS properties, focus restore, and WCAG 2.2 AA expectations. |
| `docs/BLAZOR_DEV_WORKFLOW.md` | UI work requires full build and visual verification cycle; scoped CSS changes require rebuild. |
| `.claude/skills/blazor-ui-conventions` | MudBlazor v9 APIs, wrapper components, EventCallback flow, and BFF-safe UI rules apply. |
| `.claude/skills/blazor-css-isolation` | Component CSS isolation, BEM, native CSS nesting, and limited `::deep` usage apply. |
| `.claude/skills/design-system` | Layout widths, z-index, and motion tokens belong in token/design-system layer, not page CSS. |

External docs findings:

| Source | Finding |
|---|---|
| MudBlazor drawer docs/source | `Persistent` drawers push content, `Responsive` drawers switch behavior, `ClipMode` is evaluated only when drawers are directly inside `MudLayout`, and `@bind-Open` is recommended for self-closing behavior. |
| Microsoft Blazor CSS isolation docs | `.razor.css` scopes styles to component output; `::deep` is needed only for descendants/child component internals. |
| Microsoft Blazor state management docs | Scoped in-memory state containers with `OnChange` are appropriate for per-circuit app state; consumers must unsubscribe and use renderer-safe updates. |
| MDN CSS Grid docs | CSS grid is appropriate for major page regions and explicit track sizing. |

Tooling note: Tavily MCP and context7 MCP were requested but are not exposed in this runtime. Re-run research through those MCPs if they become available, but do not block this plan because local repo docs and official docs already establish the required architecture direction.

## Current State Analysis

All file and class references in this section were verified by search on 2026-04-29.

### Existing Shell Layout

| Concern | Verified file/class | Current behavior |
|---|---|---|
| Main layout markup | `Explore.Blazor.Client/Layout/MainLayout.razor` | Owns `MudThemeProvider`, `MudLayout`, header, left `MudDrawer`, `MudMainContent`, `Footer`, and `AiAssistantRail`. |
| Main layout code-behind | `Explore.Blazor.Client/Layout/MainLayout.razor.cs` | Injects `SidebarState` and `AiAssistantState`; sets sidebar availability based on chrome visibility. |
| Main layout CSS | `Explore.Blazor.Client/Layout/MainLayout.razor.css` | Uses `main-layout__main--ai-open` to add `margin-right: 360px`; relies on MudDrawer margin behavior for left nav. |
| Top nav | `Explore.Blazor.Client/Layout/NavMenu.razor` and `.cs` | Toggles `SidebarState` and `AiAssistantState`; renders AI button when available. |
| Left sidebar state | `Explore.Blazor.Client/Services/SidebarState.cs` | Tracks `IsOpen` and `HasSidebar`; not a true page panel registration model. |
| AI rail state | `Explore.Blazor.Client/Services/AiAssistantState.cs` | Tracks `IsOpen` and `IsAvailable`; separate from shell layout state. |
| AI rail component | `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` and `.css` | Custom fixed right rail, hard-coded `width: 360px`, physical `right`, backdrop on mobile. |

### Existing Event List Workspace Layout

| Concern | Verified file/class | Current behavior |
|---|---|---|
| Event list page | `Explore.Blazor.Client/Pages/Events/EventList.razor` | Owns detail drawer, main event list, customization sidebar, and page-specific state. |
| Event list code-behind | `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` | Uses `_detailDrawerOpen` and `_customizationDrawerOpen`; manually enforces mutual exclusion. |
| Event list CSS | `Explore.Blazor.Client/Pages/Events/EventList.razor.css` | Uses `width: calc(100% + var(--layout-padding-inline))` and negative right margin to escape parent padding. |
| Generic right sidebar | `Explore.Blazor.Client/Components/Common/RightSidebar.razor` and `.css` | Custom sticky desktop panel and fixed mobile overlay; not aware of shell AI rail. |
| Customize view content | `Explore.Blazor.Client/Pages/Events/Components/EventListCustomizationDrawer.razor` and `.cs/.css` | Good content component but layout wrapper is page-specific. |
| Event detail preview | `EventList.razor` | Uses `MudOverlay` plus `MudDrawer` temporary right drawer; visually strong and should be preserved. |

### Existing Test Surface

| Concern | Verified file/class | Current coverage |
|---|---|---|
| Main layout tests | `Explore.Blazor.Client.Tests/Layout/MainLayoutTests.cs` | Existing bUnit coverage for landmarks/chrome behavior. |
| Nav menu tests | `Explore.Blazor.Client.Tests/Layout/NavMenuAdminTests.cs` | Existing NavMenu behavior coverage. |
| Event list tests | `Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs` | Existing event list loading/empty state coverage. |
| Customize drawer tests | `Explore.Blazor.Client.Tests/Components/Event/EventListCustomizationDrawerTests.cs` | Existing content-level tests for customization drawer. |
| E2E smoke tests | `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs` | Playwright fixture exists, but visual regression coverage for panel combinations does not yet exist. |
| Architecture tests | `Event.Architecture.Tests/BlazorClientArchitectureTests.cs` and `AccessibilityConventionTests.cs` | Existing enforcement surface for Blazor and accessibility conventions. |

### Missing Components To Create

Search confirmed these do not currently exist and must be created during the refactor:

| Missing concept | Required task |
|---|---|
| `ShellLayoutState` | Create a scoped shell layout state service. |
| `WorkspacePanelState` | Create a scoped workspace panel state service. |
| `AppShellLayout` | Refactor/replace `MainLayout` markup with explicit shell grid structure. |
| `AppSideNav` | Extract shell left navigation content from `MainLayout`. |
| `AppRightRail` | Replace AI fixed rail layout wrapper with shell right rail component. |
| `WorkspaceLayout` | Create reusable page workspace layout with main and right panel slots. |
| `WorkspaceRightPanel` | Create page-level docked/overlay panel wrapper. |
| `WorkspaceOverlayPanel` | Create overlay/inspector panel wrapper for event detail preview behavior. |
| Layout tokens | Extend `Explore.Blazor/wwwroot/css/tokens.css` with sidebar width, z-index, breakpoint, and motion tokens. |

## Proposed Future State

### Architecture Shape

The target hierarchy is explicit and typed:

```text
MainLayout / AppShellLayout
  TopBar
  ShellBody
    AppSideNav
    MainWorkspaceRegion
      Page content
        WorkspaceLayout
          WorkspaceMainContent
          WorkspaceRightPanel
          WorkspaceOverlayPanel when active
    AppRightRail
```

### Shell Scope

Shell scope owns UI outside route/page scrolling:

| Shell responsibility | Target owner |
|---|---|
| Sticky header and top navigation | `MainLayout` plus `NavMenu` initially, optional extracted `AppTopBar` later. |
| Left app navigation | `AppSideNav`, controlled by `ShellLayoutState`. |
| Global AI assistant | `AppRightRail` plus `AiAssistantRail` content, controlled by `ShellLayoutState`. |
| Mobile shell nav overlay | `AppSideNav` mobile mode or temporary MudDrawer with centralized state. |
| Global shell dimensions | CSS tokens in `tokens.css`. |

### Workspace Scope

Workspace scope owns page-specific panels inside the current page:

| Workspace responsibility | Target owner |
|---|---|
| Event list main content | `WorkspaceLayout` main slot inside `EventList.razor`. |
| Customize view | `WorkspaceRightPanel` containing `EventListCustomizationDrawer`. |
| Event detail preview | `WorkspaceOverlayPanel` or equivalent inspector wrapper preserving current `MudDrawer` UX. |
| Page panel state | `WorkspacePanelState`. |
| Page panel dimensions | CSS tokens in `tokens.css`. |

### State Model

Do not build a generic dynamic registry first. Use explicit state services:

```csharp
public sealed class ShellLayoutState
{
    public bool IsLeftNavOpen { get; private set; }
    public bool IsAiRailOpen { get; private set; }
    public bool IsAiRailAvailable { get; private set; }
    public event Action? Changed;

    public void ToggleLeftNav();
    public void SetLeftNavOpen(bool isOpen);
    public void SetAiRailAvailable(bool isAvailable);
    public void ToggleAiRail();
    public void OpenAiRail();
    public void CloseAiRail();
}
```

```csharp
public enum WorkspacePanel
{
    None,
    CustomizeView,
    EventPreview
}

public sealed class WorkspacePanelState
{
    public WorkspacePanel DockedPanel { get; private set; }
    public WorkspacePanel OverlayPanel { get; private set; }
    public event Action? Changed;

    public void OpenRightPanel(WorkspacePanel panel);
    public void CloseRightPanel();
    public void OpenOverlayPanel(WorkspacePanel panel);
    public void CloseOverlayPanel();
    public void CloseAll();
}
```

### CSS Model

Use grid tracks for desktop persistent panels:

```css
.app-shell__body {
    display: grid;
    grid-template-columns:
        var(--isl-shell-left-width-active)
        minmax(0, 1fr)
        var(--isl-shell-right-width-active);
    min-height: calc(100dvh - var(--mud-appbar-height));
    transition: grid-template-columns var(--isl-motion-panel-duration) var(--isl-motion-panel-easing);
}

.workspace-layout {
    display: grid;
    grid-template-columns: minmax(0, 1fr) var(--isl-workspace-right-width-active);
    transition: grid-template-columns var(--isl-motion-panel-duration) var(--isl-motion-panel-easing);
}
```

Use overlay transforms for temporary/mobile/inspector panels:

```css
.workspace-overlay-panel {
    position: fixed;
    inset-block-start: var(--mud-appbar-height);
    inset-block-end: 0;
    inset-inline-end: 0;
    inline-size: var(--isl-workspace-overlay-width);
    transform: translateX(100%);
    transition: transform var(--isl-motion-panel-duration) var(--isl-motion-panel-easing);
}
```

For RTL, implementation must use start/end semantics and logical CSS properties. Physical `left`, `right`, `margin-left`, and `margin-right` must not be introduced except in approved third-party overrides.

## Layer Impact

| Clean Architecture layer | Expected changes |
|---|---|
| Domain | None. This is UI layout only. |
| Application | None. No CQRS commands/queries required. |
| Persistence | None. No EF Core migrations. |
| Infrastructure | None. |
| API | None. No API contract changes. |
| Blazor Client | Main implementation surface: layout components, state services, CSS tokens, EventList integration, tests. |
| Blazor Server/BFF | No auth/BFF behavior changes expected. Only static CSS asset consumption remains as-is. |

## Implementation Phases

### Phase 1: Baseline Tests And Visual Freeze

Purpose: protect the current good UX before refactoring.

#### Task 1.1: Add panel visual regression scenarios

- File: create `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`.
- Acceptance criteria:
- [ ] Desktop scenario captures left nav open and AI closed.
- [ ] Desktop scenario captures left nav open and AI open.
- [ ] Desktop scenario captures customize panel open and AI open.
- [ ] Desktop scenario captures event detail preview open.
- [ ] Mobile scenario captures left nav open.
- [ ] Mobile scenario captures customize view open.
- [ ] Mobile scenario captures event detail preview open.
- [ ] Tests use stable selectors or data attributes added intentionally for panel hosts.
- Dependencies: existing Playwright fixtures in `Explore.Blazor.Client.E2ETests`.
- Effort: L.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`.

#### Task 1.2: Add state and component regression tests for existing behavior

- Files: update or add tests near `Explore.Blazor.Client.Tests/Layout/MainLayoutTests.cs`, `Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs`, and `Explore.Blazor.Client.Tests/Components/Event/EventListCustomizationDrawerTests.cs`.
- Acceptance criteria:
- [ ] Test AI availability gates the AI toggle and rail.
- [ ] Test customize drawer open closes detail preview in current behavior before refactor.
- [ ] Test event detail preview close resets inline overlays/popups.
- [ ] Test shell landmarks remain present.
- Dependencies: none.
- Effort: M.
- Related skills: `blazor-ui-conventions`.

### Phase 2: Design Tokens And Layout Contract

Purpose: centralize dimensions, motion, and layering before behavior changes.

#### Task 2.1: Extend global layout tokens

- File: update `Explore.Blazor/wwwroot/css/tokens.css`.
- Acceptance criteria:
- [ ] Add shell tokens: `--isl-shell-left-nav-width`, `--isl-shell-left-nav-collapsed-width`, `--isl-shell-right-rail-width`.
- [ ] Add workspace tokens: `--isl-workspace-right-panel-width`, `--isl-workspace-overlay-width`, `--isl-mobile-panel-width`.
- [ ] Add motion tokens: `--isl-motion-panel-duration`, `--isl-motion-panel-easing`.
- [ ] Add z-index semantic tokens for shell panels, workspace panels, overlays, and backdrops.
- [ ] Add `@media (prefers-reduced-motion: reduce)` handling for panel motion tokens or equivalent component-level handling.
- [ ] Existing design token comments and 3-tier structure remain intact.
- Dependencies: none.
- Effort: S.
- Related skills: `design-system`, `blazor-css-isolation`.

#### Task 2.2: Define layout contract documentation

- File: create or update a concise section in `docs/BLAZOR.md` or create `docs/SIDEBAR_LAYOUT.md` if the content is too large.
- Acceptance criteria:
- [ ] Documents shell scope versus workspace scope.
- [ ] Documents persistent desktop panels as grid tracks.
- [ ] Documents temporary/mobile/inspector panels as overlays.
- [ ] Documents the ban on page-level compensation for shell panels.
- [ ] Documents event detail preview as overlay/inspector, not docked panel.
- Dependencies: Task 2.1.
- Effort: S.
- Related skills: `design-system`, `blazor-ui-conventions`.

### Phase 3: State Services

Purpose: consolidate panel state without over-generalizing into a plugin docking framework.

#### Task 3.1: Create `ShellLayoutState`

- File: create `Explore.Blazor.Client/Services/ShellLayoutState.cs`.
- Acceptance criteria:
- [ ] File starts with two `ABOUTME:` lines.
- [ ] Tracks left nav open state.
- [ ] Tracks AI rail open and availability state.
- [ ] Exposes one `Changed` event.
- [ ] Does not reference Razor components or MudBlazor types.
- [ ] Avoids redundant `Changed` notifications when values do not change.
- [ ] Unit tests cover all state transitions.
- Dependencies: none.
- Effort: M.
- Related skills: `blazor-ui-conventions`, `clean-architecture-rules`.

#### Task 3.2: Create `WorkspacePanelState`

- File: create `Explore.Blazor.Client/Services/WorkspacePanelState.cs`.
- Acceptance criteria:
- [ ] File starts with two `ABOUTME:` lines.
- [ ] Tracks one docked workspace panel and one overlay panel.
- [ ] Provides explicit methods for customize view and event preview workflows through enum values, not arbitrary string IDs.
- [ ] Supports closing all workspace panels on route changes when needed.
- [ ] Unit tests cover mutual exclusion rules: docked customize panel and overlay event preview can be controlled deterministically.
- Dependencies: none.
- Effort: M.
- Related skills: `blazor-ui-conventions`, `clean-architecture-rules`.

#### Task 3.3: Register new state services

- File: update `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`.
- Acceptance criteria:
- [ ] Register `ShellLayoutState` scoped.
- [ ] Register `WorkspacePanelState` scoped.
- [ ] Keep old `SidebarState` and `AiAssistantState` temporarily only until migration is complete.
- [ ] Add removal tasks for old services in Phase 7.
- Dependencies: Tasks 3.1 and 3.2.
- Effort: S.
- Related skills: `blazor-ui-conventions`.

### Phase 4: Shell Grid Refactor

Purpose: move shell left nav and AI rail into one predictable grid system.

#### Task 4.1: Extract shell left navigation content

- File: create `Explore.Blazor.Client/Components/Shell/AppSideNav.razor` and `.razor.css`.
- Source to migrate from: `Explore.Blazor.Client/Layout/MainLayout.razor` left `MudDrawer` content.
- Acceptance criteria:
- [ ] Navigation markup is extracted without changing visible labels or HAL/tenant link behavior.
- [ ] Component exposes parameters/callbacks or consumes `ShellLayoutState` consistently.
- [ ] Uses `nav aria-label="Sidebar navigation"` or preserves equivalent landmark semantics.
- [ ] CSS uses BEM and logical properties.
- [ ] Desktop mode is in grid flow, not fixed drawer flow.
- [ ] Mobile mode remains temporary overlay or MudDrawer-backed while using centralized state.
- Dependencies: Task 3.1.
- Effort: L.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

#### Task 4.2: Convert AI rail into shell right rail

- Files: update or replace `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` and `.razor.css`; create `AppRightRail` only if separation improves clarity.
- Acceptance criteria:
- [ ] AI rail width comes from `--isl-shell-right-rail-width`.
- [ ] Persistent desktop AI rail is a shell grid track, not `position: fixed` with main-content margin compensation.
- [ ] Mobile AI rail remains temporary overlay with backdrop.
- [ ] Uses logical properties (`inset-inline-end`, `border-inline-start`) where positioning is needed.
- [ ] Existing placeholder content and close behavior are preserved.
- Dependencies: Tasks 2.1 and 3.1.
- Effort: M.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

#### Task 4.3: Refactor `MainLayout` to shell grid

- Files: update `Explore.Blazor.Client/Layout/MainLayout.razor`, `.razor.cs`, and `.razor.css`.
- Acceptance criteria:
- [ ] Shell body uses CSS grid tracks for left nav, main workspace, and AI rail.
- [ ] Remove `main-layout__main--ai-open` margin-right compensation.
- [ ] Remove direct dependence on `AiAssistantState` after AI migration.
- [ ] `NavMenu` uses `ShellLayoutState` for toggles.
- [ ] Footer remains inside the main workspace region and is not pushed by shell AI rail incorrectly.
- [ ] Skip link, main landmark, header landmark, live regions, and focus-on-navigate behavior remain intact.
- Dependencies: Tasks 4.1 and 4.2.
- Effort: XL.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

### Phase 5: Workspace Layout Refactor

Purpose: make page-specific panels siblings inside a workspace grid, not strangers compensating for shell panels.

#### Task 5.1: Create `WorkspaceLayout`

- Files: create `Explore.Blazor.Client/Components/Layout/WorkspaceLayout.razor` and `.razor.css`.
- Acceptance criteria:
- [ ] Provides `MainContent`, `RightPanel`, and optional `OverlayPanel` render fragments.
- [ ] Desktop persistent right panel is a grid track.
- [ ] Mobile right panel renders as temporary overlay.
- [ ] Uses CSS variables for active right-panel width.
- [ ] Does not know EventList-specific concepts.
- [ ] Uses BEM and logical CSS properties.
- Dependencies: Task 2.1.
- Effort: L.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

#### Task 5.2: Create `WorkspaceRightPanel`

- Files: create `Explore.Blazor.Client/Components/Layout/WorkspaceRightPanel.razor` and `.razor.css`.
- Acceptance criteria:
- [ ] Provides labelled complementary region semantics.
- [ ] Supports header/body/footer slots or child content without forcing content layout.
- [ ] Does not trap focus in desktop docked mode.
- [ ] Supports mobile overlay close/backdrop behavior through `WorkspaceLayout` or explicit callbacks.
- [ ] Width uses `--isl-workspace-right-panel-width`.
- Dependencies: Task 5.1.
- Effort: M.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`.

#### Task 5.3: Migrate EventList customize view into `WorkspaceLayout`

- Files: update `Explore.Blazor.Client/Pages/Events/EventList.razor`, `.razor.cs`, and `.razor.css`.
- Acceptance criteria:
- [ ] `EventListCustomizationDrawer` is hosted inside `WorkspaceRightPanel`.
- [ ] `_customizationDrawerOpen` is replaced by `WorkspacePanelState` or a thin page adapter to it.
- [ ] Remove `.event-list__page` negative right margin and width expansion.
- [ ] Remove dependency on `RightSidebar` for customize view.
- [ ] Customize panel and AI rail align with no gap when both are open.
- [ ] Existing customization setting behavior remains unchanged.
- Dependencies: Tasks 3.2, 5.1, and 5.2.
- Effort: XL.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

### Phase 6: Event Detail Inspector Integration

Purpose: preserve and normalize the strongest existing panel UX.

#### Task 6.1: Create workspace overlay/inspector wrapper

- Files: create `Explore.Blazor.Client/Components/Layout/WorkspaceOverlayPanel.razor` and `.razor.css`.
- Acceptance criteria:
- [ ] Overlay panel slides from logical end side.
- [ ] Backdrop is consistent with design tokens.
- [ ] Escape closes active overlay.
- [ ] Focus is saved before opening and restored after close.
- [ ] Background scroll locks for temporary overlay mode.
- [ ] Persistent workspace docked panels remain unaffected.
- Dependencies: Tasks 2.1 and 3.2.
- Effort: L.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

#### Task 6.2: Integrate EventList detail preview as inspector overlay

- Files: update `Explore.Blazor.Client/Pages/Events/EventList.razor` and `.razor.cs`.
- Acceptance criteria:
- [ ] Preserve current desktop visual behavior of event detail preview.
- [ ] Preserve current mobile behavior or improve only where tests prove no regression.
- [ ] `_detailDrawerOpen` is replaced by `WorkspacePanelState` or kept only as temporary adapter during migration.
- [ ] Inline registration overlay and tag/category popup reset behavior remains intact.
- [ ] The detail preview does not force EventList grid shrinkage unless explicitly required later.
- [ ] Detail preview has clear close, backdrop, focus, and Escape behavior.
- Dependencies: Task 6.1.
- Effort: XL.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

### Phase 7: Mobile, RTL, Accessibility, And Motion Hardening

Purpose: make the new layout enterprise-grade rather than only visually correct on desktop.

#### Task 7.1: Mobile panel policy

- Files: update new shell/workspace layout components and CSS.
- Acceptance criteria:
- [ ] All mobile panels are temporary overlays.
- [ ] Backdrops are consistent and token-driven.
- [ ] Mobile overlays do not cause horizontal page overflow.
- [ ] Background scroll is locked for active temporary overlay panels.
- [ ] AI rail is hidden behind the AI button on mobile, not permanently docked.
- Dependencies: Phases 4, 5, and 6.
- Effort: L.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`.

#### Task 7.2: Accessibility pass

- Files: update all new panel components and relevant tests.
- Acceptance criteria:
- [ ] `Escape` closes the active temporary/overlay panel.
- [ ] Focus returns to the opener after temporary/overlay panel close.
- [ ] Temporary panels trap focus or use MudBlazor behavior that traps focus.
- [ ] Persistent panels do not trap focus.
- [ ] Toggle buttons use `aria-expanded` and `aria-controls` where practical.
- [ ] Panels have accessible labels or labelled headers.
- [ ] Keyboard tab order remains logical with multiple panels open.
- Dependencies: Phases 4, 5, and 6.
- Effort: L.
- Related skills: `blazor-ui-conventions`, `blazor-css-isolation`.

#### Task 7.3: RTL and logical CSS audit

- Files: new/changed `.razor.css`, `MainLayout.razor.css`, `EventList.razor.css`, `AiAssistantRail.razor.css`.
- Acceptance criteria:
- [ ] No new physical `left`, `right`, `margin-left`, `margin-right`, `border-left`, or `border-right` in component CSS except justified third-party/MudBlazor override contexts.
- [ ] Start/end panel semantics work under `MudRTLProvider`.
- [ ] Existing physical properties removed where part of the sidebar refactor.
- Dependencies: Phases 4, 5, and 6.
- Effort: M.
- Related skills: `blazor-css-isolation`, `design-system`.

#### Task 7.4: Reduced motion pass

- Files: `tokens.css`, panel `.razor.css` files.
- Acceptance criteria:
- [ ] `prefers-reduced-motion: reduce` disables or minimizes panel transitions.
- [ ] Backdrop opacity changes do not animate in reduced motion.
- [ ] Tests or documented manual checks cover reduced motion.
- Dependencies: Task 2.1 and panel CSS implementation.
- Effort: S.
- Related skills: `design-system`, `blazor-css-isolation`.

### Phase 8: Cleanup And Removal

Purpose: remove old fragmented systems after migration is proven.

#### Task 8.1: Remove old independent sidebar state services

- Files: remove or stop registering `Explore.Blazor.Client/Services/SidebarState.cs` and `Explore.Blazor.Client/Services/AiAssistantState.cs` after all consumers are migrated.
- Acceptance criteria:
- [ ] No references remain to `SidebarState`.
- [ ] No references remain to `AiAssistantState`.
- [ ] `ServiceCollectionExtensions.cs` registers only the new layout state services for this concern.
- [ ] Tests updated to use `ShellLayoutState`.
- Dependencies: Phases 4 and 5.
- Effort: M.
- Related skills: `blazor-ui-conventions`, `clean-architecture-rules`.

#### Task 8.2: Remove old `RightSidebar`

- Files: remove or deprecate `Explore.Blazor.Client/Components/Common/RightSidebar.razor` and `.razor.css` after all usages are migrated.
- Acceptance criteria:
- [ ] No references remain to `RightSidebar`.
- [ ] Workspace panels use `WorkspaceRightPanel`.
- [ ] Shared component folder remains focused on generic wrappers, not layout-specific panels.
- Dependencies: Phase 5.
- Effort: S.
- Related skills: `blazor-ui-conventions`.

#### Task 8.3: Remove layout hacks

- Files: `MainLayout.razor.css`, `EventList.razor.css`, new layout CSS.
- Acceptance criteria:
- [ ] Remove `main-layout__main--ai-open` and `margin-right: 360px` behavior.
- [ ] Remove `.event-list__page` negative margin and width expansion.
- [ ] No page-level hardcoded AI/customization widths remain.
- [ ] No duplicate sidebar width constants remain outside `tokens.css` and intentional component fallback values.
- Dependencies: Phases 4 and 5.
- Effort: M.
- Related skills: `design-system`, `blazor-css-isolation`.

### Phase 9: Verification And Documentation

Purpose: prove the refactor preserves UX and improves maintainability.

#### Task 9.1: Run automated verification

- Commands:
- [ ] `dotnet build --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.BlazorClientArchitectureTests`
- [ ] `dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AccessibilityConventionTests`
- [ ] `dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet`
- [ ] `dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet` where environment supports Aspire/Playwright.
- Acceptance criteria:
- [ ] Build passes.
- [ ] Blazor client tests pass.
- [ ] Architecture/accessibility tests pass.
- [ ] E2E visual tests pass or are documented as environment-blocked with screenshots manually captured.
- Dependencies: all implementation phases.
- Effort: M.
- Related skills: `blazor-ui-conventions`, `design-system`.

#### Task 9.2: Update docs and dev context

- Files: update this task package and final docs.
- Acceptance criteria:
- [ ] `sidebar-dock-layout-refactor-context.md` reflects final decisions.
- [ ] `sidebar-dock-layout-refactor-tasks.md` is updated with completed tasks.
- [ ] `docs/BLAZOR.md` or `docs/SIDEBAR_LAYOUT.md` documents the final layout contract.
- [ ] Any new component usage examples are included where useful.
- Dependencies: all implementation phases.
- Effort: S.
- Related skills: `blazor-ui-conventions`, `design-system`.

## Success Metrics

| Metric | Target |
|---|---|
| Visual gap between customize panel and AI rail | 0 unexpected gap in desktop test case. |
| Page-level AI compensation | 0 occurrences. |
| Negative margin event list layout hacks | 0 occurrences. |
| Duplicated panel width constants | 0 outside design tokens and justified fallbacks. |
| Event detail preview regression | No visual or interaction regression in desktop/mobile tests. |
| RTL readiness | No new banned physical CSS properties in refactored panel CSS. |
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

## Effort Estimate

| Phase | Effort |
|---|---|
| Phase 1: Baseline tests | L |
| Phase 2: Tokens and contract | S-M |
| Phase 3: State services | M |
| Phase 4: Shell grid | XL |
| Phase 5: Workspace layout | XL |
| Phase 6: Detail inspector integration | XL |
| Phase 7: Mobile/RTL/a11y/motion hardening | L |
| Phase 8: Cleanup | M |
| Phase 9: Verification/docs | M |

Total estimate: XL multi-session refactor. Expect 4-7 focused implementation sessions depending on test/E2E environment readiness.

## Risk Assessment And Mitigations

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| Event detail preview UX regresses | Medium | High | Freeze visual tests first; migrate as overlay/inspector, not docked panel. |
| MudDrawer behavior conflicts with custom shell grid | Medium | Medium | Use MudDrawer only for temporary/mobile overlays where appropriate; do not rely on MudLayout margins for desktop persistent panels. |
| E2E environment is unstable | High | Medium | Add bUnit/state tests and document manual visual verification when E2E cannot run. |
| RTL issues appear late | Medium | Medium | Use logical properties from first CSS change; audit with architecture/accessibility tests. |
| Layout grid breaks footer behavior | Medium | High | Keep footer in main workspace region and add screenshots for short and long pages. |
| Over-abstraction creeps in | Medium | Medium | Keep explicit `ShellLayoutState` and `WorkspacePanelState`; avoid dynamic descriptor registry in this phase. |
| Existing page padding assumptions break | Medium | Medium | Remove negative margin hacks only after `WorkspaceLayout` owns gutters and right panel placement. |

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

## Potential Risks & Unknowns

The highest-risk area is preserving the event detail preview while moving it into the new overlay/inspector model. Its current behavior is visually strong because it uses a dedicated `MudOverlay` plus `MudDrawer` pattern; replacing that too aggressively could degrade desktop and mobile UX. Treat that migration as integration, not redesign. The second biggest unknown is whether the current E2E environment can reliably run the visual regression set; if not, manual screenshots plus bUnit/state tests must temporarily bridge the gap.
