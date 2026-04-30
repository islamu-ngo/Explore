<!-- ABOUTME: Checklist for implementing the generic dock layout engine refactor. -->
<!-- ABOUTME: Tracks phased work for shell/workspace docking, resizing, stacking, persistence, and cleanup. -->

# Sidebar Dock Layout Refactor - Task Checklist v2

Last Updated: 2026-04-29

## Status Summary

- Overall status: Plan revised to v2 and session handoff captured, implementation not started.
- Current phase: Phase 1 is next.
- Critical path: Visual freeze -> tokens/docs -> dock engine core -> shell host -> workspace host -> resize -> stacking -> persistence -> hardening -> cleanup.

## Planning And Handoff - Complete

- [x] Create initial `dev/active/sidebar-dock-layout-refactor/` planning package.
- [x] Verify current sidebar files/classes before listing them in the plan.
- [x] Revise plan from explicit shell/workspace state to generic internal dock engine v2.
- [x] Update context file with session handoff notes.
- [x] Update journal with dock engine architecture decision.

Acceptance criteria:

- [x] Plan reflects descriptor-driven generic dock architecture.
- [x] Tasks include resizing, stacking, persistence, mobile, RTL, accessibility, motion, cleanup, and documentation phases.
- [x] Next session can resume from Phase 1 without rediscovering the architecture decision.

## Phase 1: Baseline Tests And Visual Freeze - Not Started

- [ ] Create `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`.
- [ ] Add desktop visual scenario: left nav open, AI closed.
- [ ] Add desktop visual scenario: left nav open, AI open.
- [ ] Add desktop visual scenario: customize panel open, AI open.
- [ ] Add desktop visual scenario: event detail preview open.
- [ ] Add mobile visual scenario: left nav open.
- [ ] Add mobile visual scenario: customize view open.
- [ ] Add mobile visual scenario: event detail preview open.
- [ ] Add stable selectors or data attributes for panel hosts where needed.
- [ ] Add bUnit regression tests for AI availability/toggle behavior.
- [ ] Add bUnit regression tests for customize/detail mutual exclusion.
- [ ] Add bUnit regression tests for detail close reset behavior.
- [ ] Confirm shell landmarks remain covered by `MainLayoutTests`.

Acceptance criteria:

- [ ] Current good UX is captured before layout changes.
- [ ] Event detail panel has visual coverage before migration.
- [ ] Tests are deterministic enough to catch spacing/gap regressions.

## Phase 2: Design Tokens And Dock Contract - Not Started

- [ ] Update `Explore.Blazor/wwwroot/css/tokens.css` with dock width tokens.
- [ ] Add shell defaults: left nav, collapsed nav, AI rail.
- [ ] Add workspace defaults: right panel, inspector overlay, mobile panel.
- [ ] Add panel motion duration/easing tokens.
- [ ] Add semantic z-index tokens for shell, workspace, overlay, and backdrop.
- [ ] Add reduced-motion handling for panel tokens or panel CSS.
- [ ] Create `docs/DOCK_LAYOUT.md`.
- [ ] Document descriptor metadata versus runtime state.
- [ ] Document `DockScope`, `DockSide`, and `DockMode`.
- [ ] Document stack behavior, resize behavior, mobile behavior, snapshot behavior, and persistence behavior.
- [ ] Document the ban on central panel enums and page-level shell compensation.

Acceptance criteria:

- [ ] No new panel width is hardcoded outside tokens/descriptors unless a temporary fallback is justified.
- [ ] `docs/DOCK_LAYOUT.md` is the source of truth for dock layout architecture.

## Phase 3: Dock Engine Core - Not Started

- [ ] Create `Explore.Blazor.Client/Services/Docking/DockPanelId.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockScope.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockSide.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockMode.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockPanelDescriptor.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockPanelState.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockLayoutSnapshot.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/IDockPanelRegistry.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockPanelRegistry.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs`.
- [ ] Register dock services in `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`.
- [ ] Add unit tests for descriptor registration.
- [ ] Add unit tests for duplicate ID handling.
- [ ] Add unit tests for open, close, toggle, mode, resize, and activation.
- [ ] Add unit tests for min/max width clamping.
- [ ] Add unit tests for multiple panels per side.
- [ ] Add unit tests for snapshot creation and restore.

Acceptance criteria:

- [ ] New panels can be modeled without editing central enums.
- [ ] Descriptor metadata and runtime state are separate.
- [ ] At least two panels can exist on the same side in the model.
- [ ] Panel widths can be controlled by state.
- [ ] Layout state can serialize and restore snapshots.
- [ ] Dock engine has no event-specific dependencies.

## Phase 4: Shell Dock Host - Not Started

- [ ] Create `Explore.Blazor.Client/Components/Docking/DockLayoutHost.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockLayoutHost.razor.css`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockSideHost.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockSideHost.razor.css`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockPanelHost.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockPanelHost.razor.css`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockOverlayHost.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockOverlayHost.razor.css`.
- [ ] Create `Explore.Blazor.Client/Components/Shell/AppSideNav.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Shell/AppSideNav.razor.css`.
- [ ] Register `shell.left-nav` descriptor and content.
- [ ] Register `shell.ai-assistant` descriptor and content.
- [ ] Refactor `MainLayout.razor` to use shell `DockLayoutHost`.
- [ ] Refactor `MainLayout.razor.cs` to use dock engine instead of `SidebarState`/`AiAssistantState` where migrated.
- [ ] Refactor `MainLayout.razor.css` to remove `main-layout__main--ai-open`.
- [ ] Update `NavMenu.razor` and `.cs` to toggle dock panels by `DockPanelId`.
- [ ] Preserve skip link, main landmark, header, footer, live regions, and focus-on-navigate.

Acceptance criteria:

- [ ] AI assistant opens without page-level margin compensation.
- [ ] Left nav and AI assistant render through the dock engine.
- [ ] Shell desktop panels are grid tracks.
- [ ] Shell mobile panels are overlays.
- [ ] Current visual shell behavior is preserved.

## Phase 5: Workspace Dock Host - Not Started

- [ ] Create `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs`.
- [ ] Define `events.customize-view` descriptor.
- [ ] Define `events.event-preview` descriptor.
- [ ] Use workspace `DockLayoutHost` in `EventList.razor` or a reusable workspace wrapper.
- [ ] Register EventList dock panel descriptors and content during page lifecycle.
- [ ] Host `EventListCustomizationDrawer` as docked workspace panel.
- [ ] Host event detail preview as inspector/overlay through `DockOverlayHost`.
- [ ] Remove `_customizationDrawerOpen` or reduce it to temporary migration adapter only.
- [ ] Remove `_detailDrawerOpen` or reduce it to temporary migration adapter only.
- [ ] Remove `.event-list__page` negative margin and width expansion.
- [ ] Remove EventList dependency on `RightSidebar`.
- [ ] Preserve EventList settings behavior.
- [ ] Preserve event detail inline registration and tag/category popup behavior.

Acceptance criteria:

- [ ] Customize View is a workspace docked panel on desktop.
- [ ] Event Preview is a workspace inspector/overlay.
- [ ] Customize View and AI rail align with no visible gap.
- [ ] Event detail preview desktop/mobile UX is preserved.
- [ ] Event-specific panels depend on dock engine; dock engine does not depend on event-specific panels.

## Phase 6: Resize Support - Not Started

- [ ] Create `Explore.Blazor.Client/Components/Docking/DockResizeHandle.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockResizeHandle.razor.css`.
- [ ] Add desktop mouse drag resizing.
- [ ] Add desktop touch drag resizing.
- [ ] Enforce descriptor min/max widths through `DockLayoutState.Resize`.
- [ ] Add keyboard resizing with arrow keys.
- [ ] Add faster keyboard resizing with Shift plus arrow.
- [ ] Add ARIA semantics for resize handle.
- [ ] Add tests for min/max enforcement.
- [ ] Add tests for keyboard resize behavior.
- [ ] Add visual coverage for at least one resized panel.

Acceptance criteria:

- [ ] Width changes update `DockPanelState`.
- [ ] Resizing cannot cause layout overflow.
- [ ] Resize handle is keyboard accessible.
- [ ] Visual tests include at least one resized panel.

## Phase 7: Stacking Support - Not Started

- [ ] Create `Explore.Blazor.Client/Components/Docking/DockTabStrip.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Docking/DockTabStrip.razor.css`.
- [ ] Add ordered panel rendering in `DockSideHost`.
- [ ] Add one active panel per side by default.
- [ ] Add tabbed stack rendering for multiple panels on a side.
- [ ] Keep arbitrary split-pane rendering out of scope unless explicitly required.
- [ ] Add tests for two panels on the same side.
- [ ] Add tests for activating side-stack tabs.

Acceptance criteria:

- [ ] Multiple panels per side are represented by the model.
- [ ] `DockSideHost` can render ordered tabs and active panel content.
- [ ] Future modules can add panels without editing shell/workspace enums.

## Phase 8: Persistence And Reset - Not Started

- [ ] Create `Explore.Blazor.Client/Services/Docking/IDockLayoutPersistence.cs`.
- [ ] Create `Explore.Blazor.Client/Services/Docking/LocalStorageDockLayoutPersistence.cs` or project-consistent browser storage adapter.
- [ ] Save `DockLayoutSnapshot` by layout key.
- [ ] Load `DockLayoutSnapshot` by layout key.
- [ ] Add reset-to-default behavior based on descriptors.
- [ ] Keep interface compatible with future user appearance/settings API integration.
- [ ] Add tests for snapshot serialization.
- [ ] Add tests for snapshot restore.
- [ ] Add tests for reset behavior.

Acceptance criteria:

- [ ] User layout preferences can be serialized into a snapshot.
- [ ] System can reset to default layout.
- [ ] Persistence starts local and does not block future server-side settings integration.

## Phase 9: Mobile, RTL, Accessibility, Motion - Not Started

- [ ] Ensure mobile behavior is a descriptor/layout policy, not page CSS.
- [ ] Ensure all mobile side panels are temporary overlays.
- [ ] Ensure all mobile overlays have consistent backdrop tokens.
- [ ] Ensure all mobile overlays lock background scroll.
- [ ] Ensure no panel causes horizontal page overflow.
- [ ] Ensure Escape closes temporary/overlay panels.
- [ ] Ensure focus returns to opener after temporary/overlay panel close.
- [ ] Ensure persistent desktop panels do not trap focus.
- [ ] Ensure temporary panels trap focus or delegate to MudBlazor focus trap behavior.
- [ ] Add `aria-expanded` and `aria-controls` to panel toggles where practical.
- [ ] Add accessible labels/headers to all panel regions.
- [ ] Audit CSS for banned physical direction properties.
- [ ] Test or manually verify RTL shell/workspace layout.
- [ ] Test or manually verify reduced-motion behavior.

Acceptance criteria:

- [ ] Accessibility contract from `docs/ACCESSIBILITY.md` is preserved or improved.
- [ ] Refactored layout is start/end oriented and RTL-ready.
- [ ] Reduced motion disables/minimizes panel animation.

## Phase 10: Cleanup Old Layout Systems - Not Started

- [ ] Remove all references to `SidebarState`.
- [ ] Remove all references to `AiAssistantState`.
- [ ] Remove old service registrations for `SidebarState` and `AiAssistantState`.
- [ ] Remove or deprecate `Explore.Blazor.Client/Components/Common/RightSidebar.razor`.
- [ ] Remove or deprecate `Explore.Blazor.Client/Components/Common/RightSidebar.razor.css`.
- [ ] Remove `main-layout__main--ai-open` CSS.
- [ ] Remove `margin-right: 360px` behavior.
- [ ] Remove EventList negative margin layout hack.
- [ ] Remove duplicate width constants outside tokens/descriptors.
- [ ] Update tests to target dock engine services and hosts.

Acceptance criteria:

- [ ] Old fragmented sidebar mechanisms are gone after migration.
- [ ] No page-specific shell panel compensation remains.

## Phase 11: Documentation, Governance, Verification - Not Started

- [ ] Finalize `docs/DOCK_LAYOUT.md`.
- [ ] Update `docs/BLAZOR.md` with a pointer to `docs/DOCK_LAYOUT.md`.
- [ ] Consider architecture tests that prevent page-level shell compensation.
- [ ] Consider architecture tests that prevent central panel enum regression.
- [ ] Run `dotnet build --configuration Release --verbosity quiet`.
- [ ] Run Blazor client architecture tests.
- [ ] Run accessibility convention tests.
- [ ] Run `Explore.Blazor.Client.Tests`.
- [ ] Run E2E visual tests where environment supports Aspire/Playwright.
- [ ] Document E2E/manual screenshot gaps if environment blocks E2E.
- [ ] Update this context file with final implementation decisions.
- [ ] Update this tasks file as phases complete.

Acceptance criteria:

- [ ] Build and required tests pass.
- [ ] Docs reflect final implementation.
- [ ] Visual coverage proves no gap between customize panel and AI rail.

## Final Acceptance Checklist

- [ ] Opening AI assistant does not require page-level margin changes.
- [ ] Opening Customize View beside AI creates no visible gap.
- [ ] Footer behavior remains correct.
- [ ] Left shell nav remains independent of page scroll.
- [ ] AI assistant remains independent of page scroll.
- [ ] Event detail panel still looks good on desktop and mobile.
- [ ] Mobile sidebars overlay cleanly and do not cause horizontal overflow.
- [ ] No duplicated panel width constants remain.
- [ ] No negative margin layout hacks remain.
- [ ] RTL does not require rewriting the layout.
- [ ] Reduced-motion mode works.
- [ ] Visual tests cover major panel combinations.
- [ ] New panels can be added by registering a descriptor and content, without editing central enums.
- [ ] At least two panels can exist on the same side in the model.
- [ ] Panel widths can be controlled by state, not only static CSS.
- [ ] User layout preferences can be serialized into a snapshot.
- [ ] System can reset to default layout.
- [ ] Future modules can define their own dock panel descriptors.
- [ ] Dock engine does not depend on event-specific concepts.
- [ ] Event-specific panels depend on dock engine, not the other way around.

## Quick Resume

Start with Phase 1. Do not touch the layout implementation until current visual behavior has baseline coverage. Then implement the dock engine core before migrating shell/workspace rendering. The event detail preview remains the highest-risk UX preservation target.
