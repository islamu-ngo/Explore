<!-- ABOUTME: Checklist for implementing the sidebar dock layout refactor. -->
<!-- ABOUTME: Tracks phases, dependencies, acceptance criteria, and verification status. -->

# Sidebar Dock Layout Refactor - Task Checklist

Last Updated: 2026-04-29

## Status Summary

- Overall status: Planning complete, implementation not started.
- Current phase: Phase 1 is next.
- Critical path: Visual freeze -> tokens -> state services -> shell grid -> workspace layout -> detail inspector -> cleanup.

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

## Phase 2: Design Tokens And Layout Contract - Not Started

- [ ] Update `Explore.Blazor/wwwroot/css/tokens.css` with shell width tokens.
- [ ] Add `--isl-shell-left-nav-width`.
- [ ] Add `--isl-shell-left-nav-collapsed-width`.
- [ ] Add `--isl-shell-right-rail-width`.
- [ ] Add `--isl-workspace-right-panel-width`.
- [ ] Add `--isl-workspace-overlay-width`.
- [ ] Add `--isl-mobile-panel-width`.
- [ ] Add panel motion duration/easing tokens.
- [ ] Add semantic z-index tokens for shell, workspace, overlay, and backdrop.
- [ ] Add reduced-motion handling for panel tokens or panel CSS.
- [ ] Document shell/workspace layout contract in `docs/BLAZOR.md` or `docs/SIDEBAR_LAYOUT.md`.

Acceptance criteria:

- [ ] No new panel width is hardcoded outside design tokens unless a temporary fallback is justified.
- [ ] Layout contract explicitly distinguishes shell panels, workspace docked panels, and overlay/inspector panels.

## Phase 3: State Services - Not Started

- [ ] Create `Explore.Blazor.Client/Services/ShellLayoutState.cs`.
- [ ] Add shell state unit tests.
- [ ] Create `Explore.Blazor.Client/Services/WorkspacePanelState.cs`.
- [ ] Add workspace panel state unit tests.
- [ ] Register both services in `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`.
- [ ] Keep `SidebarState` and `AiAssistantState` only temporarily until consumers migrate.

Acceptance criteria:

- [ ] Shell state tracks left nav and AI rail deterministically.
- [ ] Workspace state tracks docked and overlay panels deterministically.
- [ ] State services have no Razor/MudBlazor dependencies.

## Phase 4: Shell Grid Refactor - Not Started

- [ ] Create `Explore.Blazor.Client/Components/Shell/AppSideNav.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Shell/AppSideNav.razor.css`.
- [ ] Move left navigation content out of `MainLayout.razor`.
- [ ] Preserve `nav aria-label="Sidebar navigation"` semantics.
- [ ] Convert desktop left nav to shell grid track.
- [ ] Preserve mobile temporary left nav behavior through centralized state.
- [ ] Update or wrap `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` for shell grid ownership.
- [ ] Remove fixed desktop AI rail positioning.
- [ ] Move AI rail width to `--isl-shell-right-rail-width`.
- [ ] Refactor `MainLayout.razor` to shell grid body.
- [ ] Refactor `MainLayout.razor.cs` to use `ShellLayoutState`.
- [ ] Refactor `MainLayout.razor.css` to remove `main-layout__main--ai-open`.
- [ ] Update `NavMenu.razor` and `.cs` to use `ShellLayoutState`.
- [ ] Preserve skip link, main landmark, header, live regions, and focus-on-navigate.

Acceptance criteria:

- [ ] AI assistant opens without page-level margin compensation.
- [ ] Left nav and AI rail are shell siblings in one grid model.
- [ ] Footer remains correctly placed inside main workspace region.

## Phase 5: Workspace Layout Refactor - Not Started

- [ ] Create `Explore.Blazor.Client/Components/Layout/WorkspaceLayout.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Layout/WorkspaceLayout.razor.css`.
- [ ] Create `Explore.Blazor.Client/Components/Layout/WorkspaceRightPanel.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Layout/WorkspaceRightPanel.razor.css`.
- [ ] Migrate EventList main content into `WorkspaceLayout` main slot.
- [ ] Host `EventListCustomizationDrawer` in `WorkspaceRightPanel`.
- [ ] Replace `_customizationDrawerOpen` with `WorkspacePanelState` or a short-lived adapter.
- [ ] Remove `.event-list__page` negative margin and width expansion.
- [ ] Remove EventList dependency on `RightSidebar`.
- [ ] Verify customize panel and AI rail align with no gap.

Acceptance criteria:

- [ ] Customize View is a desktop workspace grid track.
- [ ] Customize View is a mobile temporary overlay.
- [ ] EventList page no longer compensates for global AI rail width.
- [ ] EventList settings behavior remains unchanged.

## Phase 6: Event Detail Inspector Integration - Not Started

- [ ] Create `Explore.Blazor.Client/Components/Layout/WorkspaceOverlayPanel.razor`.
- [ ] Create `Explore.Blazor.Client/Components/Layout/WorkspaceOverlayPanel.razor.css`.
- [ ] Add Escape-close behavior for active overlay panel.
- [ ] Add focus save/restore for overlay open/close.
- [ ] Add backdrop and scroll-lock behavior.
- [ ] Integrate EventList detail preview as `WorkspaceOverlayPanel` content or as equivalent inspector wrapper.
- [ ] Replace `_detailDrawerOpen` with `WorkspacePanelState` or a short-lived adapter.
- [ ] Preserve inline registration overlay behavior.
- [ ] Preserve tag/category popup reset behavior.
- [ ] Preserve current desktop and mobile visual behavior.

Acceptance criteria:

- [ ] Event detail preview remains overlay/inspector style.
- [ ] Event detail preview does not force EventList grid shrinkage.
- [ ] Desktop and mobile screenshots match or improve current UX.

## Phase 7: Mobile, RTL, Accessibility, Motion - Not Started

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

## Phase 8: Cleanup And Removal - Not Started

- [ ] Remove all references to `SidebarState`.
- [ ] Remove all references to `AiAssistantState`.
- [ ] Remove old service registrations for `SidebarState` and `AiAssistantState`.
- [ ] Remove or deprecate `Explore.Blazor.Client/Components/Common/RightSidebar.razor`.
- [ ] Remove or deprecate `Explore.Blazor.Client/Components/Common/RightSidebar.razor.css`.
- [ ] Remove `main-layout__main--ai-open` CSS.
- [ ] Remove `margin-right: 360px` behavior.
- [ ] Remove EventList negative margin layout hack.
- [ ] Remove duplicate width constants outside `tokens.css`.
- [ ] Update tests to target new state/services/components.

Acceptance criteria:

- [ ] Old fragmented sidebar mechanisms are gone after migration.
- [ ] No page-specific shell panel compensation remains.

## Phase 9: Verification And Documentation - Not Started

- [ ] Run `dotnet build --configuration Release --verbosity quiet`.
- [ ] Run Blazor client architecture tests.
- [ ] Run accessibility convention tests.
- [ ] Run `Explore.Blazor.Client.Tests`.
- [ ] Run E2E visual tests where environment supports Aspire/Playwright.
- [ ] Document E2E/manual screenshot gaps if environment blocks E2E.
- [ ] Update `docs/BLAZOR.md` or `docs/SIDEBAR_LAYOUT.md` with final contract.
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

## Quick Resume

Start with Phase 1. Do not touch the layout implementation until the current visual behavior has baseline coverage. The event detail preview is the most important UX preservation target.
