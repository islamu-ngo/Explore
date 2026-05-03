<!-- ABOUTME: Current resume context for the generic dock layout engine refactor. -->
<!-- ABOUTME: Captures implemented dock architecture, verified behavior, remaining risks, and next steps. -->

# Sidebar Dock Layout Refactor - Context v3

Last Updated: 2026-05-02

## Current Status

The sidebar dock refactor is now an implemented dock subsystem, not an early design proposal. The core engine, shell host, EventList workspace panels, responsive dock policy, mobile overlay routing, reverse close animations, and the latest threshold tuning have landed.

The active remaining work is hardening and cleanup:

1. durable visual/geometry evidence;
2. remaining overlay accessibility audit after adding MudBlazor-managed focus trapping;
3. production persistence hydration/autosave;
4. legacy bridge cleanup;
5. RTL/reduced-motion/manual verification;
6. resize/stack visual proof.

The user has explicitly instructed that future work in this folder should stay focused on **sidebars, docks, and overlaying sidepanels** unless they broaden scope. Do not drift into unrelated event/API-contract work.

## Implemented Architecture

```text
MainLayout
  DockLayoutHost Scope=Shell
    DockSideHost Start
      shell.left-nav
    MudMainContent / main#main-content
      Page content
        EventList
          DockLayoutHost Scope=Workspace
            EventList content
            DockSideHost End
              events.customize-view
            DockOverlayHost
              events.event-preview
    DockSideHost End
      shell.ai-assistant
    DockOverlayHost
      shell temporary/mobile/constrained overlays
```

## Implemented Components And Responsibilities

| Area | Canonical files | Current behavior |
|---|---|---|
| Dock state | `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs` | Source of truth for registration, state transitions, responsive policy, viewport width, snapshots, reset, resize, activation, and constrained overlay projection. |
| Shell descriptors | `Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs` | Stable descriptors for `shell.left-nav` and `shell.ai-assistant`. |
| Workspace descriptors | `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs` | Stable descriptors for `events.customize-view` and `events.event-preview`. |
| Shell layout | `Explore.Blazor.Client/Layout/MainLayout.*` | Renders shell dock host, registers shell descriptors, keeps temporary legacy state bridge guarded against feedback loops. |
| Top nav toggles | `Explore.Blazor.Client/Layout/NavMenu.*` | Hamburger/AI toggles operate against `DockLayoutState`, not stale legacy open state. |
| Left navigation content | `Explore.Blazor.Client/Components/Shell/AppSideNav.*` | Owns visible sidebar content and close button. |
| Workspace layout | `Explore.Blazor.Client/Pages/Events/EventList.*` | Uses workspace `DockLayoutHost`; EventList no longer uses `RightSidebar` or legacy preview `MudDrawer`. |
| Dock scope host | `Explore.Blazor.Client/Components/Docking/DockLayoutHost.*` | Grid boundary, MudBlazor viewport observer, actual-width reporting to `DockLayoutState`, and docked-width projection. |
| Docked side host | `Explore.Blazor.Client/Components/Docking/DockSideHost.*` | Desktop docked panel rendering, side-stack support, and retained closing entries for reverse docked close animation. |
| Overlay host | `Explore.Blazor.Client/Components/Docking/DockOverlayHost.*` | Temporary/overlay/inspector/mobile-projected panels, backdrop, scroll lock, MudBlazor focus trap for active overlays, Escape/backdrop close, focus handoff/restore, and reverse close animation. |
| Generic panel host | `Explore.Blazor.Client/Components/Docking/DockPanelHost.*` | Generic panel body only; no generic header chrome. Panel content owns close buttons/headings. |
| Resize handle | `Explore.Blazor.Client/Components/Docking/DockResizeHandle.*` and `wwwroot/js/dock-resize.js` | Keyboard/pointer resize foundation with pointer capture hardening. |
| Persistence | `IDockLayoutPersistence`, `LocalStorageDockLayoutPersistence`, `dock-layout-persistence.js` | Local snapshot persistence foundation exists; production hydration/autosave deferred. |

## Recent Behavior Fixes Landed

### Mobile default and hamburger reliability

- Mobile left nav no longer opens by default after reload.
- Hamburger directly toggles `DockLayoutState` and uses dock state for `aria-expanded`.
- Backdrop/Escape/close button close the same dock state path, avoiding legacy split-brain.
- `SidebarState` remains only as temporary bridge state and no longer forces open when sidebar availability becomes true.

### Close animations

- `DockOverlayHost` keeps closing overlay entries mounted for reverse fade/slide animation before unmount.
- `DockSideHost` keeps closing docked entries mounted for reverse docked close animation before unmount.
- Reduced-motion rules are present.
- `@key` is used for overlay entries to avoid remounting during close animation.

### Generic header removal

- `DockPanelHost` no longer renders `dock-panel-host__header` / `dock-panel-host__title`.
- Panel content owns visible headers/actions.
- `AppSideNav` owns its close button.

### Overlay focus trap

- `DockOverlayHost` wraps temporary/overlay/inspector/mobile-projected panel slots in MudBlazor `MudFocusTrap`.
- The trap uses `DefaultFocus.FirstChild` and is disabled when only closing animation entries remain.
- Persistent docked desktop panels remain rendered by `DockSideHost` and are not focus-trapped.
- bUnit coverage verifies overlay-only trap placement and non-trapping docked panels.

### Responsive threshold tuning

- Hard mobile is only MudBlazor `Breakpoint.Xs` (`<=600px`).
- Center-content minimum threshold is `375px`.
- Left + right docked panels can coexist until center content would drop below `375px`.
- If opening the left/start panel would reduce center content below `375px`, left nav projects to temporary overlay while right panels remain docked/dimmed.
- Low-width/mobile end-side panels are constrained so only one right/end panel remains open when content would be too narrow.

## Latest Verified Evidence

### Automated verification

Latest successful verification after the threshold update:

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
# 965 total, 964 succeeded, 1 known pre-existing skip
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
# 142/142 passed
```

```bash
rtk dotnet build --configuration Release --verbosity quiet
# 23 projects, 0 errors, warnings only
```

### Chrome DevTools evidence

- `1000px`: left nav + EventList Customize View coexist docked; no overlay host active.
- `970px`: Customize View remains docked; hamburger opens left nav as temporary overlay with full-site backdrop; shell start grid track remains `0px`.
- `390px` mobile: left nav defaults closed after reload; explicit hamburger opens temporary overlay with backdrop, close button, and scroll lock.

## Research Notes

Research was refreshed with Context7 and Tavily during the latest planning pass.

| Source | Useful conclusion |
|---|---|
| Context7 `/websites/mudblazor` Drawer docs | Temporary drawers use explicit open state, `OverlayAutoClose`, and close by setting open false. Responsive drawers switch by breakpoint. |
| Context7 MudBlazor Breakpoint docs | `Xs <=600`, `Sm 600-960`, `Md 960-1280`; treating `Sm` as hard mobile was too aggressive for this app. |
| Tavily MudBlazor Drawer docs search | Temporary drawers open above content until section selection or overlay click when overlay autoclose is enabled. |
| Repo `EventFilterBar` | Mobile sidepanel behavior is explicit overlay + temporary drawer + scroll lock + direct close path; dock overlays now mirror this pattern without using `MudDrawer` as host. |

## Remaining Work

### Priority 1 — Visual evidence

- Enable or replace skipped `SidebarLayoutVisualTests`.
- Capture desktop, constrained desktop, mobile, RTL, reduced-motion, Event Preview, Customize, AI, and combined-panel states.
- Add geometry assertions if screenshot storage remains blocked.

### Priority 2 — Overlay accessibility audit

- Re-audit ARIA after generic header removal.
- Verify close button, Escape, backdrop close, and focus restore all remain on one close path.
- Add browser-level keyboard evidence for Tab/Shift+Tab trapping when the Playwright environment is available.

### Priority 3 — Persistence hydration/autosave

- Define layout keys.
- Hydrate after descriptors register.
- Debounce autosave on meaningful layout changes.
- Add reset UI/test hook.
- Persist descriptor state, not effective responsive projection.

### Priority 4 — Legacy cleanup

- Audit and remove or explicitly keep/document:
  - `SidebarState`;
  - `AiAssistantState`;
  - `RightSidebar`;
  - stale CSS selectors;
  - stale negative tests.
- Ensure no legacy state can re-open or desynchronize dock panels.

### Priority 5 — RTL/reduced-motion/resize visual proof

- Run RTL shell/workspace browser pass.
- Verify reduced-motion behavior in browser.
- Add resized-panel visual scenario.
- Verify same-side tab stack visually if/when multiple production panels are active.

## Guardrails For Next Session

- Do not touch unrelated event creation/API-contract work while working this plan.
- Do not restore EventList card-width changes; the 50% DetailedList behavior was confirmed intentional.
- Do not reintroduce EventList `RightSidebar` or preview `MudDrawer` hosts.
- Do not solve dock spacing with negative margins or page-level shell compensation.
- Do not make `DockLayoutState` reference shell or EventList component classes.
- Do not remove bridge services before consumer audit and tests.

## Commands To Run

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

```bash
rtk dotnet build --configuration Release --verbosity quiet
```

When E2E/Aspire screenshot environment is available:

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

## Quick Resume

1. If no live defect is reported, start with visual evidence or focus-trap work.
2. If a live defect is reported, reproduce it in Chrome DevTools first and record geometry before editing.
3. Keep scope to sidebars/docks/overlay panels unless the user explicitly says otherwise.
4. Update `sidebar-dock-layout-refactor-tasks.md` after execution changes.
5. Update `sidebar-dock-layout-refactor-plan.md` only when strategy changes.
