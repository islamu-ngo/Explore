<!-- ABOUTME: Current task checklist for the generic dock layout engine refactor. -->
<!-- ABOUTME: Tracks completed dock/sidebar slices and remaining hardening tasks. -->

# Sidebar Dock Layout Refactor - Task Checklist v3

Last Updated: 2026-05-02

## Status Summary

- Overall status: core dock refactor is implemented and verified at bUnit/build/architecture level.
- Current workstream: hardening, visual proof, persistence wiring, bridge cleanup, RTL/reduced-motion verification.
- Scope warning: keep future work focused on sidebars, docks, and overlaying sidepanels unless explicitly broadened.

## Completed Implementation Slices

### Planning and architecture

- [x] Create active planning package under `dev/active/sidebar-dock-layout-refactor/`.
- [x] Replace stale v2 implementation plan with current v3 plan.
- [x] Refresh Context7/Tavily MudBlazor responsive drawer guidance.
- [x] Record current architecture and remaining risks.

### Dock engine core

- [x] Implement `DockPanelId`.
- [x] Implement `DockScope`.
- [x] Implement `DockSide`.
- [x] Implement `DockMode`.
- [x] Implement `DockPanelDescriptor`.
- [x] Implement `DockPanelState`.
- [x] Implement `DockPanelEntry`.
- [x] Implement `DockLayoutSnapshot`.
- [x] Implement `IDockPanelRegistry`.
- [x] Implement `DockLayoutState` as registry and runtime state source.
- [x] Register dock services in DI.
- [x] Add state/registration/snapshot/reset tests.

### Shell migration

- [x] Implement `ShellDockPanels` descriptors.
- [x] Extract shell nav content into `AppSideNav`.
- [x] Add `AppSideNav` close button.
- [x] Render shell through `DockLayoutHost`.
- [x] Render `shell.left-nav` through dock engine.
- [x] Render `shell.ai-assistant` through dock engine.
- [x] Remove legacy left `MudDrawer` host from `MainLayout`.
- [x] Remove standalone fixed AI margin compensation.
- [x] Keep shell side panels independent of page scroll.
- [x] Update `NavMenu` to toggle `DockLayoutState` directly.
- [x] Guard temporary legacy bridge state to avoid feedback loops.

### Workspace migration

- [x] Implement `EventDockPanels` descriptors.
- [x] Render EventList through workspace `DockLayoutHost`.
- [x] Render `events.customize-view` as workspace dock panel.
- [x] Render `events.event-preview` as workspace inspector/overlay.
- [x] Remove EventList dependency on `RightSidebar`.
- [x] Remove EventList preview `MudDrawer` host.
- [x] Remove EventList negative margin / width expansion hack.
- [x] Preserve EventList customize/detail mutual exclusion.
- [x] Preserve inline registration and tag/category popup behavior inside preview.

### Overlay, responsiveness, and motion

- [x] Implement full-viewport `DockOverlayHost` stacking over app chrome.
- [x] Remove `isolation: isolate` blocker from dock host overlay stacking.
- [x] Add shared overlay backdrop.
- [x] Add overlay scroll lock/unlock.
- [x] Add Escape close.
- [x] Add backdrop close.
- [x] Add focus save/restore.
- [x] Add initial focus handoff to active panel.
- [x] Add reverse close animations for overlay/temporary/inspector panels.
- [x] Add reverse close animations for docked desktop panels.
- [x] Add reduced-motion handling for dock motion.
- [x] Route mobile docked panels through temporary overlay chrome.
- [x] Treat only `Breakpoint.Xs` as hard mobile.
- [x] Tune content-width threshold to `375px`.
- [x] Allow left + right docked panels until center content would drop below `375px`.
- [x] Project explicit start panel to overlay when right docked panel would leave less than `375px` center content.
- [x] Keep only one end-side panel when content is constrained/mobile.
- [x] Ensure mobile left nav defaults closed.
- [x] Ensure hamburger opens mobile left nav with one click.
- [x] Ensure backdrop/close button closes mobile left nav reliably.
- [x] Remove generic `dock-panel-host__header` chrome.
- [x] Add MudBlazor-managed focus trap for active temporary/overlay dock panels.
- [x] Keep closing overlay animations non-trapping once no active overlay remains.

### Resize, stacking, persistence foundations

- [x] Implement `DockResizeHandle`.
- [x] Add pointer capture helper.
- [x] Add keyboard resize support.
- [x] Clamp resize to descriptor min/max.
- [x] Add resize bUnit/JSInterop coverage.
- [x] Implement same-side tab stack foundation.
- [x] Add ARIA tab/tablist/tabpanel coverage.
- [x] Implement `IDockLayoutPersistence`.
- [x] Implement local storage persistence behind approved interop boundary.
- [x] Implement descriptor-default reset.
- [x] Add persistence/reset/corrupt snapshot tests.

### Tests and guardrails

- [x] Add dock state tests.
- [x] Add dock host rendering tests.
- [x] Add overlay lifecycle tests.
- [x] Add focus-trap placement tests for overlay-only behavior.
- [x] Add mobile routing tests.
- [x] Add responsive threshold tests.
- [x] Add AppSideNav close-button test.
- [x] Add generic header removal test.
- [x] Add architecture guardrail against central panel enum regressions.
- [x] Add architecture guardrail against page-level shell compensation regressions.
- [x] Scaffold visual scenario tests.

## Latest Verification

- [x] `Explore.Blazor.Client.Tests`: 965 total / 964 passed / 1 known pre-existing skip.
- [x] `Event.Architecture.Tests`: 142/142 passed.
- [x] `rtk dotnet build --configuration Release --verbosity quiet`: 23 projects / 0 errors / warnings only.
- [x] Chrome `1000px`: left + Customize coexist docked.
- [x] Chrome `970px`: left projects to temporary overlay while Customize remains docked.
- [x] Chrome `390px`: left defaults closed and opens as temporary overlay.

## Remaining Tasks

### Phase A — Visual evidence

- [ ] Enable `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs` or replace with stable geometry tests.
- [ ] Capture desktop left-only screenshot.
- [ ] Capture desktop left + AI screenshot.
- [ ] Capture desktop left + Customize screenshot.
- [ ] Capture desktop Customize + AI screenshot.
- [ ] Capture constrained desktop Customize docked + left overlay screenshot.
- [ ] Capture mobile left overlay screenshot.
- [ ] Capture mobile AI overlay screenshot.
- [ ] Capture mobile Customize overlay screenshot.
- [ ] Capture Event Preview inspector overlay screenshot.
- [ ] Capture reduced-motion scenario.
- [ ] Capture RTL shell/workspace scenario.

### Phase B — Focus trap/accessibility

- [x] Choose focus-trap implementation strategy: MudBlazor `MudFocusTrap`, matching project accessibility guidance and avoiding custom JS.
- [x] Trap tab focus inside active temporary/overlay panels.
- [x] Keep persistent docked desktop panels non-trapping.
- [x] Add bUnit coverage for overlay focus-trap placement and non-trapping docked panels.
- [ ] Re-audit ARIA after generic header removal.
- [ ] Verify close button, Escape, backdrop click, and focus restore use one close path.

### Phase C — Persistence wiring

- [ ] Define shell layout key.
- [ ] Define EventList workspace layout key.
- [ ] Define future route/module workspace layout key convention.
- [ ] Hydrate snapshots after descriptors register.
- [ ] Debounce autosave after meaningful layout changes.
- [ ] Add reset control or test hook for shell/workspace layout.
- [ ] Confirm responsive effective projection is not persisted as desktop mode.

### Phase D — Legacy bridge cleanup

- [ ] Audit all `SidebarState` references.
- [ ] Audit all `AiAssistantState` references.
- [ ] Audit all `RightSidebar` references.
- [ ] Audit old drawer/sidebar CSS selectors.
- [ ] Replace remaining public toggles with dock operations or a thin facade.
- [ ] Remove bridge subscriptions from `MainLayout` when safe.
- [ ] Remove unused service registrations when safe.
- [ ] Delete or document `RightSidebar` after consumer audit.

### Phase E — Responsive policy documentation and proof

- [ ] Document `375px` content threshold in `docs/DOCK_LAYOUT.md`.
- [ ] Document hard-mobile `Breakpoint.Xs` decision in `docs/DOCK_LAYOUT.md`.
- [ ] Add maintainer guidance for tuning responsive thresholds.
- [ ] Add Playwright/geometry tests for 970/1000/1280/1760px if possible.
- [ ] Verify AI + Customize + left nav combinations at 970/1000/1280/1760px in automated or recorded manual evidence.

### Phase F — Resize and stack visual proof

- [ ] Add visual scenario for resized left nav.
- [ ] Add visual scenario for resized Customize View.
- [ ] Verify resized panels do not overflow at constrained widths.
- [ ] Verify same-side tab stack visually with two panels.
- [ ] Decide whether tab stack requires production UX polish before broad use.

## Final Acceptance Checklist

- [ ] All required tests/build pass with only explicitly documented unrelated/pre-existing skips.
- [ ] Visual evidence exists for major panel combinations.
- [x] Focus trap is implemented or delegated to a proven component.
- [ ] RTL verification is complete.
- [ ] Reduced-motion verification is complete.
- [ ] No page-level shell compensation remains.
- [ ] No EventList negative margin hacks remain.
- [ ] No generic dock header chrome remains.
- [ ] Responsive thresholds are documented and tested.
- [ ] Legacy bridge services are removed or explicitly documented as intentional facades.
- [ ] New panel registration remains descriptor + content only.

## Quick Resume

1. If no live defect is reported, start with Phase A visual evidence or Phase B focus trap.
2. If the user reports a live defect, reproduce it in Chrome DevTools before editing.
3. Keep scope to sidebars/docks/overlay panels.
4. Do not modify unrelated event/API work.
5. Update this checklist after each implementation slice.
