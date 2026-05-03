<!-- ABOUTME: Current implementation plan for the generic dock layout engine refactor. -->
<!-- ABOUTME: Tracks the landed dock architecture, remaining hardening work, and objective next steps. -->

# Sidebar Dock Layout Refactor - Implementation Plan v3

Last Updated: 2026-05-02

## Executive Summary

The dock refactor is no longer a proposed migration. The core dock engine, production shell host, EventList workspace panels, responsive routing, and overlay motion foundation have landed. The old v2 plan mixed completed work with future-state sketches and is no longer safe to follow.

The current goal is to finish hardening and cleanup around the implemented descriptor-driven dock system:

- preserve the generic engine boundary;
- eliminate remaining legacy bridge state only after consumer audit;
- prove visual behavior across desktop, constrained desktop, tablet, mobile, RTL, and reduced-motion;
- finish accessibility details around overlay ARIA and browser-level keyboard proof;
- keep future panel additions descriptor-driven and page-independent.

This plan intentionally ignores backward compatibility with the old layout because the project is in development mode. It does **not** authorize unrelated event/API changes.

## Current Architecture Snapshot

### Implemented Dock Architecture

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
      shell temporary/mobile overlays
```

### Implemented Responsibilities

| Area | Current implementation |
|---|---|
| Dock state | `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs` owns registration, open/close/toggle, resize, activation, snapshots, reset, viewport width, responsive policy, and constrained-panel decisions. |
| Panel descriptors | `ShellDockPanels.cs` and `EventDockPanels.cs` own stable ids and descriptor metadata. There is no central panel enum. |
| Shell host | `MainLayout.razor` renders shell panels through `DockLayoutHost`; `SidebarState`/`AiAssistantState` remain temporary bridge services only. |
| Workspace host | `EventList.razor` renders Customize View and Event Preview through workspace dock descriptors. EventList no longer uses `RightSidebar` or legacy preview `MudDrawer`. |
| Responsive policy | `DockLayoutState` allows left + right docked panels until center content would drop below `375px`; explicit start panels then project to temporary overlay while right panels remain docked. Hard mobile is MudBlazor `Breakpoint.Xs` only. |
| Overlay lifecycle | `DockOverlayHost` owns backdrop, scroll lock, MudBlazor focus trapping for active overlays, Escape/backdrop close, focus save/restore, focus handoff, temporary projection, and reverse close animations before unmount. |
| Docked close lifecycle | `DockSideHost` keeps closing docked panels mounted long enough to animate out before unmount. |
| Generic panel chrome | `DockPanelHost` no longer renders a generic header. Panel content owns its own chrome/actions. |
| Left nav mobile UX | `AppSideNav` owns the close button; `NavMenu` toggles `DockLayoutState` directly to avoid legacy split-brain. |
| Persistence | `IDockLayoutPersistence` and `LocalStorageDockLayoutPersistence` exist but production hydration/autosave remains deferred. |

## Non-Negotiable Constraints

1. Do not reintroduce page-level shell compensation (`margin-right`, negative margins, or width expansion hacks).
2. Do not reintroduce `RightSidebar` or `MudDrawer` as EventList layout hosts.
3. Do not add dock engine dependencies on EventList, shell components, API, Application, Persistence, or Domain layers.
4. Do not add a central enum that must change for every future dock panel.
5. Keep widths in descriptors/tokens or explicitly justified constants; do not duplicate layout widths across page CSS.
6. Use logical CSS (`inline`, `block`, `start`, `end`) for dock layout and overlay motion.
7. Temporary/mobile panels must have backdrop, scroll lock, Escape/backdrop close, focus restore, and reduced-motion behavior.
8. Docked desktop panels must be grid tracks; temporary/inspector/mobile-projected panels must be overlays.
9. Panel content owns close buttons and headers; `DockPanelHost` remains generic chrome only.
10. Visual claims must be backed by Chrome/Playwright/manual evidence, not just bUnit state tests.

## Research Inputs Refreshed 2026-05-02

| Source | Relevant guidance |
|---|---|
| MudBlazor Drawer docs via Context7 (`/websites/mudblazor`) | Temporary drawers are explicit state (`@bind-Open`) + overlay autoclose; responsive drawers switch behavior at configured breakpoints; hamburger toggles must be direct and reliable. |
| MudBlazor Breakpoint docs via Context7 | `Xs` is phone-sized (`<=600px`), `Sm` is tablet (`600-960px`), `Md` is laptop/tablet (`960-1280px`). Do not treat `Sm` as hard mobile unless UX requires it. |
| Tavily search against MudBlazor docs | Temporary drawers open above content until section selection or overlay click when overlay autoclose is enabled. |
| Repo `EventFilterBar` mobile pattern | Mobile sidepanel uses explicit overlay, scroll lock, temporary drawer semantics, state transfer on breakpoint changes, and a direct close path. Dock overlays should mirror this behavior without wrapping in MudDrawer. |
| Project design-system skills | Global cross-component selectors belong in the component layer when CSS isolation cannot express the relationship; component CSS should stay BEM/logical and use `::deep` only for child component roots/internal surfaces. |

## What Is Done

### Completed Slices

| Slice | Status |
|---|---|
| Dock core model (`DockPanelId`, `DockScope`, `DockSide`, `DockMode`, descriptor/state/snapshot) | Done |
| Scoped `DockLayoutState` / `IDockPanelRegistry` | Done |
| Dock host components (`DockLayoutHost`, `DockSideHost`, `DockPanelHost`, `DockOverlayHost`) | Done |
| Shell migration (`shell.left-nav`, `shell.ai-assistant`) | Done |
| Workspace migration (`events.customize-view`, `events.event-preview`) | Done |
| EventList negative-margin removal | Done |
| AI page-level margin compensation removal | Done |
| Footer/dock connection fixes | Done |
| Shell side panels independent of page scroll | Done |
| Event Preview full-viewport overlay stacking | Done |
| Mobile docked-panel projection to overlay chrome | Done |
| Width-aware responsive policy | Done |
| Reverse close animations for overlays and docked panels | Done |
| Generic `dock-panel-host__header` removal | Done |
| AppSideNav close button | Done |
| Resize handle foundation | Done |
| Tabbed same-side stack foundation | Done |
| Local snapshot persistence/reset foundation | Done |
| MudBlazor-managed focus trap for temporary overlays | Done |
| Architecture guardrails for dock contract | Done |
| bUnit state/host/bridge coverage | Done |

### Latest Verification Baseline

The latest successful dock-threshold verification after the 375px policy update:

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

Latest Chrome evidence:

- `1000px` desktop-ish width: left nav + EventList Customize View coexist docked; no overlay host is active.
- `970px` constrained desktop width: Customize View remains docked; explicit hamburger opens left nav as temporary overlay with full-site backdrop; shell start grid track remains `0px`.
- `390px` mobile: left nav defaults closed after reload; explicit hamburger opens it as a temporary overlay with backdrop, close button, and scroll lock.

## Current Risks And Gaps

| Risk/gap | Severity | Current evidence | Required next action |
|---|---:|---|---|
| Browser-level overlay keyboard proof incomplete | Medium | Active overlays now use MudBlazor `MudFocusTrap` and bUnit placement tests verify docked panels are non-trapping. | Add Playwright/manual Tab/Shift+Tab evidence when the browser evidence workflow is available. |
| Visual baselines still mostly manual | High | Chrome DevTools evidence exists for key regressions; E2E visual scenarios are scaffolded but skipped. | Enable screenshot baseline capture under Aspire/Playwright or document manual screenshot set. |
| Legacy bridge services remain | Medium | `SidebarState`/`AiAssistantState` still exist for compatibility bridge; `RightSidebar` remains for possible non-EventList consumers. | Audit consumers, then remove/deprecate bridge state and unused right sidebar component. |
| Persistence not hydrated in production | Medium | Local snapshot persistence exists, but host auto-load/autosave intentionally deferred. | Add layout-key hydration/autosave once visual behavior is stable. |
| RTL proof incomplete | Medium | CSS uses logical properties and RTL keyframes, but full manual/browser verification is pending. | Run RTL browser pass for shell/workspace/start/end overlays. |
| Reduced-motion proof incomplete | Medium | CSS reduced-motion rules exist; automated proof is incomplete. | Add reduced-motion browser/CSS assertion or manual evidence. |
| Resized-panel visual overflow not proven | Medium | Resize state/keyboard/pointer tests exist; no visual overflow baseline. | Add resized shell/workspace panel visual scenario. |
| Plan/docs drift risk | Medium | Tasks/context have historical detail and can drift. | Keep this plan as strategic source; update tasks/context only as execution logs. |

## Revised Implementation Roadmap

### Phase A — Stabilize Dock UX Evidence

Purpose: turn recent manual Chrome fixes into durable evidence.

Tasks:

- Enable or replace skipped `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs` scenarios.
- Capture at least these states:
  - desktop: left only;
  - desktop: left + AI;
  - desktop: left + Customize;
  - desktop: Customize + AI;
  - constrained desktop: Customize docked + left overlay;
  - mobile: left overlay;
  - mobile: AI overlay;
  - mobile: Customize overlay;
  - Event Preview inspector overlay;
  - reduced-motion mode;
  - RTL shell/workspace.
- Store screenshots or document manual evidence paths in the active task docs.
- Add a small geometry assertion suite if screenshot storage remains blocked.

Acceptance criteria:

- Visual gap regressions and overlay stacking regressions can be caught without relying on memory.
- Manual-only evidence is explicitly marked with date, viewport, and panel combination.

### Phase B — Focus Trap And Accessibility Completion

Purpose: complete the temporary overlay accessibility contract.

Tasks:

- Keep the MudBlazor-provided `MudFocusTrap` around active temporary/overlay panels only.
- Keep persistent docked desktop panels non-trapping.
- Verify close button, Escape, backdrop click, and focus restore all use one close path.
- Add browser/manual tests for tab cycling when the Playwright evidence path is available.
- Audit ARIA after generic header removal:
  - panel regions keep `aria-label`;
  - tabbed stacks keep `tablist`/`tab`/`tabpanel` relationships;
  - content-owned headings/buttons provide visible labels where needed.

Acceptance criteria:

- Keyboard users cannot tab behind an active temporary overlay.
- Closing an overlay restores focus to a sensible opener/fallback.
- No panel relies on removed generic header ids.

### Phase C — Production Dock Persistence

Purpose: wire the already-implemented persistence abstraction into visible hosts safely.

Tasks:

- Define layout keys:
  - shell layout key;
  - EventList workspace layout key;
  - future route/module workspace key convention.
- Hydrate snapshots after descriptors register.
- Autosave on meaningful layout changes with debounce.
- Reset shell/workspace layout to descriptor defaults from UI/test hook.
- Ensure corrupt/unknown snapshots remain non-fatal.
- Decide whether responsive projection state is persisted (recommended: persist descriptor state, not effective responsive projection).

Acceptance criteria:

- User width/open/order preferences survive reload.
- Reset returns to current descriptor defaults.
- Mobile temporary projection does not persist as a different desktop mode.

### Phase D — Legacy Bridge Cleanup

Purpose: remove fragmented pre-dock state once consumers are proven migrated.

Tasks:

- Audit all references to:
  - `SidebarState`;
  - `AiAssistantState`;
  - `RightSidebar`;
  - old drawer/sidebar CSS selectors;
  - old page compensation classes.
- Replace remaining public toggles with `DockLayoutState` operations or a thin shell/workspace facade.
- Remove bridge subscriptions from `MainLayout` after `NavMenu`/AI controls are fully dock-native.
- Remove service registrations that are no longer used.
- Delete `RightSidebar` only after no non-EventList consumers remain.

Acceptance criteria:

- No legacy state can re-open or desynchronize a dock panel.
- No stale selectors (`workspace-right-sidebar`, `event-list__detail-drawer`, `main-layout__main--ai-open`) remain except intentional negative tests.
- Tests target dock ids and dock state as source of truth.

### Phase E — Responsive Policy Hardening

Purpose: make threshold behavior explicit, tested, and maintainable.

Tasks:

- Move responsive thresholds into named constants or design tokens documented in `docs/DOCK_LAYOUT.md`.
- Keep the current behavior unless evidence says otherwise:
  - hard mobile: `Breakpoint.Xs`;
  - center-content floor: `375px`;
  - non-mobile start panels project to overlay only when end panels would leave less than `375px` center content;
  - end panels remain docked unless hard-mobile or exclusivity policy closes another end panel.
- Add browser/Playwright tests for threshold transitions if possible.
- Verify AI + Customize + left nav combinations across 970/1000/1280/1760px.
- Add a short “how to tune thresholds” note for future maintainers.

Acceptance criteria:

- Threshold behavior is documented with examples.
- Tests fail if the system reverts to the old too-early 960px exclusivity.
- Hamburger can open left nav as overlay over a docked right panel when content would be too narrow.

### Phase F — Resize And Stacking Visual Finish

Purpose: complete visual proof for already-implemented advanced foundations.

Tasks:

- Add visual scenario for resized left nav and resized Customize View.
- Verify resized widths do not overflow at desktop/constrained widths.
- Verify tabbed same-side stack with two panels when available.
- Decide whether the tabbed stack needs production UX polish before exposing multiple same-side panels to users.

Acceptance criteria:

- Resize foundation is not just unit-tested; it is visually safe.
- Same-side stack ARIA remains correct under visual use.

## Objective Cleanup Checklist

Before declaring the refactor complete, all of these must be true:

- [ ] `Explore.Blazor.Client.Tests` passes except explicitly documented unrelated/pre-existing skips.
- [ ] `Event.Architecture.Tests` passes.
- [ ] `rtk dotnet build --configuration Release --verbosity quiet` passes with no errors.
- [ ] E2E/manual visual evidence exists for major desktop, constrained, and mobile panel combinations.
- [ ] Focus trapping is implemented or explicitly delegated to a proven component.
- [ ] `SidebarState` and `AiAssistantState` are removed or documented as intentional facades.
- [ ] `RightSidebar` is removed or documented with active non-EventList consumers.
- [ ] No page-level shell compensation or negative EventList layout hacks remain.
- [ ] No generic dock header chrome remains.
- [ ] New panel registration requires only descriptor + content registration, not central enum edits.
- [ ] Responsive thresholds are documented and tested.
- [ ] RTL and reduced-motion checks are complete.

## Files To Treat As Canonical For Future Work

| File | Use it for |
|---|---|
| `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs` | Runtime source of truth, responsive policy, snapshots, state transitions. |
| `Explore.Blazor.Client/Components/Docking/DockLayoutHost.razor` | Scope boundary, viewport observer, grid width projection. |
| `Explore.Blazor.Client/Components/Docking/DockSideHost.razor` | Docked desktop rendering and docked close-retention lifecycle. |
| `Explore.Blazor.Client/Components/Docking/DockOverlayHost.razor` | Temporary/overlay/inspector/mobile projection lifecycle. |
| `Explore.Blazor.Client/Components/Docking/DockPanelHost.razor` | Generic panel body host only; content owns chrome. |
| `Explore.Blazor.Client/Components/Shell/ShellDockPanels.cs` | Shell descriptor ids/defaults. |
| `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs` | EventList workspace descriptor ids/defaults. |
| `Explore.Blazor/wwwroot/css/tokens.css` | Dock width/z-index/motion tokens. |
| `Explore.Blazor/wwwroot/css/components.css` | Approved global cross-component layout rules where CSS isolation cannot target relationships. |
| `docs/DOCK_LAYOUT.md` | Public architecture reference that should mirror this plan after each phase. |

## Do Not Do

- Do not modify unrelated event creation/API-contract work while continuing this plan.
- Do not restore the EventList card width probe; the 50% DetailedList card behavior was confirmed intentional.
- Do not use `MudDrawer` as the new EventList or shell dock host.
- Do not solve visual gaps with negative margins.
- Do not make `DockLayoutState` reference EventList or Shell component classes directly.
- Do not remove legacy bridge services until all consumers and tests are migrated.

## Quick Resume

1. Start with **Phase A** unless the user reports a specific live dock/sidebar defect.
2. If a live defect is reported, reproduce it in Chrome DevTools first and record geometry/evidence before editing.
3. Keep changes limited to dock/sidebar/overlay files unless the user explicitly broadens scope.
4. Run at minimum:

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

```bash
rtk dotnet build --configuration Release --verbosity quiet
```

5. Update this plan only when strategy changes; update the task checklist/context files for execution logs.
