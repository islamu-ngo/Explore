<!-- ABOUTME: Checklist for implementing the generic dock layout engine refactor. -->
<!-- ABOUTME: Tracks phased work for shell/workspace docking, resizing, stacking, persistence, and cleanup. -->

# Sidebar Dock Layout Refactor - Task Checklist v2

Last Updated: 2026-04-30

## Status Summary

- Overall status: Implementation started with dock engine foundation, dock host components, dormant tabbed stack rendering, extracted shell side-navigation content, shell descriptor registration, production shell `DockLayoutHost` migration, EventList workspace `DockLayoutHost` migration for Customize View and Event Preview, responsive dock policy with width-aware start-panel retraction, mobile docked-panel routing through overlay chrome, reverse close animations, shell bridge hidden-chrome/disposal hardening, NavMenu dock-id toggle mirroring, shell accessibility/navigation contract hardening, dormant keyboard/pointer resize handle foundation with pointer-capture hardening and JSInterop invocation coverage, dock governance architecture guardrails, dock tokens/docs, stable selector/logical CSS hardening, descriptor capability enforcement, RTL-aware slide hardening, snapshot normalization, Phase 8 local persistence/reset, and Phase 1 bUnit/E2E baseline scaffolding.
- Current phase: Phase 1 baseline coverage plus Phase 2/3/4/5/6/7/9/11 foundation slices are in progress; Phase 4 shell host migration is production-rendered and verified; Phase 5 workspace docking has landed for both `events.customize-view` and `events.event-preview`; Phase 8 is implemented as a non-rendering subsystem slice; Phase 9 host-level overlay hardening has landed for `DockOverlayHost` panels, mobile docked panels route through `DockOverlayHost` as temporary overlays, reverse close animations are implemented, and the dock state applies width-aware responsive retraction/exclusive-right-panel policy.
- Critical path: Visual freeze -> tokens/docs -> dock engine core -> shell host -> workspace host -> resize -> stacking -> persistence -> hardening -> cleanup. Phase 4 shell host migration, Phase 5 workspace host slices, and Phase 9 overlay/mobile docked-panel hardening plus responsive retraction/close-animation slices have landed; Phase 8 persistence is implemented early by Oracle recommendation, but production hydration/autosave remains deferred until final visual coverage.

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

## Phase 1: Baseline Tests And Visual Freeze - In Progress

- [x] Create `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`.
- [x] Add desktop visual scenario: left nav open, AI closed.
- [x] Add desktop visual scenario: left nav open, AI open.
- [x] Add desktop visual scenario: customize panel open, AI open.
- [x] Add desktop visual scenario: event detail preview open.
- [x] Add mobile visual scenario: left nav open.
- [x] Add mobile visual scenario: customize view open.
- [x] Add mobile visual scenario: event detail preview open.
- [x] Add stable selectors or data attributes for panel hosts where needed.
- [x] Add bUnit regression tests for AI availability/toggle behavior.
- [x] Add bUnit regression tests for customize/detail mutual exclusion.
- [x] Add bUnit regression tests for detail close reset behavior.
- [x] Confirm shell landmarks remain covered by `MainLayoutTests`.

Acceptance criteria:

- [ ] Current good UX is captured before layout changes. *(E2E scenarios are scaffolded but skipped until Aspire screenshot storage/seeding is enabled.)*
- [ ] Event detail panel has visual coverage before migration. *(Scenario scaffold exists, but screenshot/manual baseline is not established yet.)*
- [ ] Tests are deterministic enough to catch spacing/gap regressions. *(Needs enabled screenshot comparison in E2E environment.)*

## Phase 2: Design Tokens And Dock Contract - In Progress

- [x] Update `Explore.Blazor/wwwroot/css/tokens.css` with dock width tokens.
- [x] Add shell defaults: left nav, collapsed nav, AI rail.
- [x] Add workspace defaults: right panel, inspector overlay, mobile panel.
- [x] Add panel motion duration/easing tokens.
- [x] Add semantic z-index tokens for shell, workspace, overlay, and backdrop.
- [x] Add reduced-motion handling for panel tokens or panel CSS.
- [x] Create `docs/DOCK_LAYOUT.md`.
- [x] Document descriptor metadata versus runtime state.
- [x] Document `DockScope`, `DockSide`, and `DockMode`.
- [x] Document resize behavior, RTL inline-end motion, snapshot behavior, and persistence boundaries.
- [x] Document initial host component behavior for docked and overlay panels.
- [x] Document stack behavior for the dormant tabbed stack foundation.
- [ ] Document full mobile host behavior once mobile host migration exists.
- [x] Document the ban on central panel enums and page-level shell compensation.

Acceptance criteria:

- [ ] No new panel width is hardcoded outside tokens/descriptors unless a temporary fallback is justified.
- [ ] `docs/DOCK_LAYOUT.md` is the source of truth for dock layout architecture.

## Phase 3: Dock Engine Core - In Progress

- [x] Create `Explore.Blazor.Client/Services/Docking/DockPanelId.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/DockScope.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/DockSide.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/DockMode.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/DockPanelDescriptor.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/DockPanelState.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/DockLayoutSnapshot.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/IDockPanelRegistry.cs`.
- [x] Create `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs`.
- [x] Register dock services in `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`.
- [x] Add unit tests for descriptor registration.
- [x] Add unit tests for duplicate ID handling.
- [x] Add unit tests for open, close, toggle, mode, resize, and activation.
- [x] Add unit tests for min/max width clamping.
- [x] Add unit tests for multiple panels per side.
- [x] Add unit tests for snapshot creation and restore.

Acceptance criteria:

- [x] New panels can be modeled without editing central enums.
- [x] Descriptor metadata and runtime state are separate.
- [x] At least two panels can exist on the same side in the model.
- [x] Panel widths can be controlled by state.
- [x] Layout state can serialize and restore snapshots.
- [x] Dock engine has no event-specific dependencies.

## Phase 4: Shell Dock Host - In Progress

- [x] Create `Explore.Blazor.Client/Components/Docking/DockLayoutHost.razor`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockLayoutHost.razor.css`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockSideHost.razor`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockSideHost.razor.css`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockPanelHost.razor`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockPanelHost.razor.css`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockOverlayHost.razor`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockOverlayHost.razor.css`.
- [x] Add bUnit coverage for dormant dock host rendering, scope isolation, side ordering, and overlay filtering.
- [x] Create `Explore.Blazor.Client/Components/Shell/AppSideNav.razor`.
- [x] Create `Explore.Blazor.Client/Components/Shell/AppSideNav.razor.css`.
- [x] Add bUnit coverage for extracted AppSideNav core links, brand label, community link toggle, and tenant link ordering/target attributes.
- [x] Register `shell.left-nav` descriptor and content behind legacy rendering.
- [x] Register `shell.ai-assistant` descriptor and content behind legacy rendering.
- [x] Add bUnit coverage for shell descriptor registration and legacy state mirroring.
- [x] Add bUnit coverage for hidden-chrome routes closing shell dock panels behind legacy rendering.
- [x] Add bUnit coverage for `MainLayout` unregistering shell descriptors on disposal.
- [x] Refactor `MainLayout.razor` to use shell `DockLayoutHost`.
- [x] Refactor `MainLayout.razor.cs` to use dock engine as the production shell host while keeping `SidebarState`/`AiAssistantState` as a temporary toggle bridge.
- [x] Refactor `MainLayout.razor.css` to remove `main-layout__main--ai-open`.
- [x] Update `NavMenu.razor` and `.cs` to mirror toggle actions through shell `DockPanelId` values while legacy rendering remains active.
- [x] Add bUnit coverage for NavMenu sidebar and AI toggle actions mirroring shell dock state.
- [x] Preserve skip link, main landmark, header, footer, live regions, and focus-on-navigate with bUnit shell contract coverage before host migration.

Acceptance criteria:

- [x] AI assistant opens without page-level margin compensation.
- [x] Left nav and AI assistant render through the dock engine.
- [x] Dormant host foundation can render desktop docked panels as grid tracks when supplied registered state.
- [x] Dormant host foundation can render non-docked overlay/temporary/inspector panels separately from grid tracks.
- [x] Legacy left drawer content is extracted into a shell component without replacing the legacy `MudDrawer` host.
- [x] Shell descriptors are registered behind legacy rendering and mirror `SidebarState`/`AiAssistantState` open state.
- [x] Hidden-chrome routes and layout disposal keep the bridge state lifecycle safe before host migration.
- [x] Top-nav shell toggles mirror actions through `shell.left-nav` and `shell.ai-assistant` dock ids without replacing legacy rendering.
- [x] Shell accessibility/navigation contract is covered before host migration: skip link, main landmark, visible chrome landmarks, hidden-chrome anchors/live regions, and focus-on-navigate.
- [x] Shell desktop panels are migrated to grid tracks.
- [x] Shell mobile panels are migrated to overlays.
- [x] Current visual shell behavior is preserved by bUnit/architecture/build verification; enabled E2E screenshot baselines remain pending.

## Phase 5: Workspace Dock Host - In Progress

- [x] Create `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs`.
- [x] Define `events.customize-view` descriptor.
- [x] Define `events.event-preview` descriptor.
- [x] Use workspace `DockLayoutHost` in `EventList.razor` or a reusable workspace wrapper.
- [x] Register EventList customize-view dock panel descriptor and content during page lifecycle.
- [x] Host `EventListCustomizationDrawer` as docked workspace panel.
- [x] Host event detail preview as inspector/overlay through `DockOverlayHost`.
- [x] Remove `_customizationDrawerOpen` or reduce it to temporary migration adapter only.
- [x] Remove `_detailDrawerOpen` or reduce it to temporary migration adapter only.
- [x] Remove `.event-list__page` negative margin and width expansion.
- [x] Remove EventList dependency on `RightSidebar`.
- [x] Preserve EventList settings behavior.
- [x] Preserve event detail inline registration and tag/category popup behavior.

Acceptance criteria:

- [x] Customize View is a workspace docked panel on desktop.
- [x] Event Preview is a workspace inspector/overlay.
- [ ] Customize View and AI rail align with no visible gap in enabled visual screenshots.
- [x] Event detail preview desktop/mobile behavior is preserved by moving the existing preview content into `events.event-preview` while keeping `_detailDrawerOpen` as a temporary backdrop/card-selection adapter.
- [x] Event-specific panels depend on dock engine; dock engine does not depend on event-specific panels.

## Phase 6: Resize Support - In Progress

- [x] Create `Explore.Blazor.Client/Components/Docking/DockResizeHandle.razor`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockResizeHandle.razor.css`.
- [x] Add dormant pointer-event drag resizing foundation for mouse/touch-capable pointers.
- [x] Add pointer capture hardening so drag continues outside the handle bounds.
- [x] Add primary-pointer and pointer-id filtering for drag safety.
- [x] Enforce descriptor min/max widths through `DockLayoutState.Resize`.
- [x] Add keyboard resizing with arrow keys.
- [x] Add faster keyboard resizing with Shift plus arrow.
- [x] Add ARIA semantics for resize handle.
- [x] Add tests for min/max enforcement.
- [x] Add tests for keyboard resize behavior.
- [x] Add tests for pointer drag direction, cancellation, and idle pointer movement.
- [x] Add tests for non-primary and mismatched pointer drag filtering.
- [x] Add bUnit coverage for `/js/dock-resize.js` import plus `setPointerCapture`/`releasePointerCapture` invocation.
- [ ] Add visual coverage for at least one resized panel.

Acceptance criteria:

- [x] Keyboard width changes update `DockPanelState` for dormant docked inline panels.
- [x] Pointer drag width changes update `DockPanelState` through the same descriptor-clamped resize path.
- [x] Pointer capture and pointer identity checks are in place for dormant pointer resizing.
- [x] Pointer-capture helper import, capture, and release calls are covered by bUnit.
- [ ] Resizing cannot cause layout overflow.
- [x] Resize handle is keyboard accessible for the dormant keyboard foundation.
- [ ] Visual tests include at least one resized panel.

## Phase 7: Stacking Support - In Progress

- [x] Create `Explore.Blazor.Client/Components/Docking/DockTabStrip.razor`.
- [x] Create `Explore.Blazor.Client/Components/Docking/DockTabStrip.razor.css`.
- [x] Add ordered panel rendering in `DockSideHost`.
- [x] Add one active panel per side by default.
- [x] Add tabbed stack rendering for multiple panels on a side.
- [x] Keep arbitrary split-pane rendering out of scope unless explicitly required.
- [x] Add tests for two panels on the same side.
- [x] Add tests for activating side-stack tabs.
- [x] Add keyboard activation coverage for side-stack tabs.
- [x] Add ARIA tabpanel linkage and focus-move coverage for side-stack keyboard activation.

Acceptance criteria:

- [x] Multiple panels per side are represented by the model.
- [x] `DockSideHost` can render ordered tabs and active panel content for the dormant host foundation.
- [x] Stacked panels expose coherent tablist/tab/tabpanel relationships and move focus to keyboard-activated tabs.
- [ ] Future modules can add panels without editing shell/workspace enums.

## Phase 8: Persistence And Reset - Implemented As Non-Rendering Slice

- [x] Create `Explore.Blazor.Client/Services/Docking/IDockLayoutPersistence.cs`.
- [x] Create `Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs` or project-consistent browser storage adapter.
- [x] Save `DockLayoutSnapshot` by layout key.
- [x] Load `DockLayoutSnapshot` by layout key.
- [x] Add reset-to-default behavior based on descriptors.
- [x] Keep interface compatible with future user appearance/settings API integration.
- [x] Add tests for snapshot serialization.
- [x] Add tests for snapshot restore.
- [x] Add tests for reset behavior.

Acceptance criteria:

- [x] User layout preferences can be serialized into a snapshot.
- [x] System can reset to default layout.
- [x] Persistence starts local and does not block future server-side settings integration.

## Phase 9: Mobile, RTL, Accessibility, Motion - In Progress

- [x] Ensure mobile behavior is a descriptor/layout policy, not page CSS.
- [x] Ensure all mobile side panels are temporary overlays.
- [x] Ensure all mobile overlays have consistent backdrop tokens for current AI rail and right sidebar components.
- [x] Ensure `DockOverlayHost` overlay/temporary/inspector panels lock background scroll through the shared host boundary.
- [ ] Ensure no panel causes horizontal page overflow.
- [x] Ensure Escape closes `DockOverlayHost` temporary/overlay/inspector panels.
- [x] Ensure focus returns to opener after `DockOverlayHost` temporary/overlay/inspector panel close.
- [ ] Ensure persistent desktop panels do not trap focus.
- [ ] Ensure temporary panels trap focus or delegate to MudBlazor focus trap behavior.
- [x] Add `aria-expanded` and `aria-controls` to panel toggles where practical.
- [x] Add accessible labels/headers to all panel regions touched by the foundation slice.
- [x] Audit CSS for banned physical direction properties through architecture tests.
- [x] Harden current inline-end slide animations for RTL with `:dir(rtl)` transforms.
- [x] Test reduced-motion behavior through component CSS and architecture/build verification.
- [ ] Test or manually verify full RTL shell/workspace layout after host migration.

Phase 9 verification notes:

- [x] `DockOverlayHost` now renders a tokenized backdrop, locks body scroll, saves/restores focus, moves focus into the active dock panel, and closes the active closeable overlay on Escape or backdrop click.
- [x] bUnit coverage verifies overlay filtering, backdrop close, Escape close, focus save/restore, focus handoff, and scroll lock/unlock calls.
- [x] `DockLayoutHost` observes MudBlazor viewport changes and feeds actual viewport width into `DockLayoutState`; docked inline widths collapse to `0px` on mobile instead of relying on CSS compensation.
- [x] `DockSideHost` suppresses mobile docked side-host rendering; `DockOverlayHost` projects open mobile `DockMode.Docked` panels to effective `DockMode.Temporary` render entries while preserving desktop runtime state.
- [x] `DockLayoutState` applies generic responsive policy: start panels auto-close when open right-side panels constrain the content width; low-width/mobile layouts keep only one end-side panel open across shell/workspace scopes.
- [x] `DockOverlayHost` keeps closing panels mounted for reverse slide/fade animations before unmounting, while preserving scroll lock/focus restore until animation completion.
- [x] bUnit coverage verifies mobile docked panels route through overlay chrome, render as temporary panels, omit resize handles, lock scroll, receive focus handoff, keep mobile left nav open when explicitly requested, auto-close left nav when a right panel opens, enforce one right panel on narrow/mobile widths, and keep closing panels mounted with closing classes before unmount.

Acceptance criteria:

- [x] Accessibility contract from `docs/ACCESSIBILITY.md` is preserved or improved for `DockOverlayHost` overlay/temporary/inspector panels.
- [x] Mobile side panels route through temporary overlay chrome without reserving grid width.
- [ ] Refactored layout is start/end oriented and RTL-ready. *(Host overlay positioning uses logical properties; full manual RTL verification remains pending.)*
- [x] Reduced motion disables/minimizes panel animation.

## Phase 10: Cleanup Old Layout Systems - Not Started

- [ ] Remove all references to `SidebarState`.
- [ ] Remove all references to `AiAssistantState`.
- [ ] Remove old service registrations for `SidebarState` and `AiAssistantState`.
- [ ] Remove or deprecate `Explore.Blazor.Client/Components/Common/RightSidebar.razor`.
- [ ] Remove or deprecate `Explore.Blazor.Client/Components/Common/RightSidebar.razor.css`.
- [x] Remove `main-layout__main--ai-open` CSS.
- [x] Remove `margin-right: 360px` behavior.
- [x] Remove EventList negative margin layout hack.
- [ ] Remove duplicate width constants outside tokens/descriptors.
- [ ] Update tests to target dock engine services and hosts.

Acceptance criteria:

- [ ] Old fragmented sidebar mechanisms are gone after migration.
- [ ] No page-specific shell panel compensation remains.

## Phase 11: Documentation, Governance, Verification - In Progress

- [ ] Finalize `docs/DOCK_LAYOUT.md`.
- [ ] Update `docs/BLAZOR.md` with a pointer to `docs/DOCK_LAYOUT.md`.
- [x] Add architecture tests that prevent new page-level shell compensation outside known legacy migration debt.
- [x] Add architecture tests that prevent central dock panel enum regression.
- [x] Run `rtk dotnet build --configuration Release --verbosity quiet` for the Phase 8 foundation slice. *(Passed: 23 projects, 0 errors, 151 pre-existing/noisy warnings.)*
- [x] Run Blazor client architecture tests for the governance foundation slice.
- [x] Run architecture/accessibility convention project for the Phase 8 foundation slice. *(Passed: `Event.Architecture.Tests` 131/131.)*
- [x] Run `Explore.Blazor.Client.Tests` for the Phase 8 foundation slice. *(Passed: 895/896, 1 documented pre-existing skip.)*
- [ ] Run E2E visual tests where environment supports Aspire/Playwright.
- [ ] Document E2E/manual screenshot gaps if environment blocks E2E.
- [x] Update this context file with current foundation-slice implementation decisions and handoff notes.
- [x] Update this tasks file as foundation slices complete.

Acceptance criteria:

- [x] Governance guardrails protect the descriptor-driven dock contract before production host migration.
- [x] Build and required tests pass for the current shell/workspace/mobile routing slices. *Latest: dock-related client tests pass inside the full suite; current full `Explore.Blazor.Client.Tests` result is 937 total/931 passed/5 unrelated failures/1 pre-existing skip, `Event.Architecture.Tests` 135/135, `rtk dotnet build` 23 projects/0 errors/138 known warnings.*
- [ ] Docs reflect final implementation.
- [ ] Visual coverage proves no gap between customize panel and AI rail.

## Final Acceptance Checklist

- [x] Opening AI assistant does not require page-level margin changes.
- [ ] Opening Customize View beside AI creates no visible gap.
- [x] Footer behavior remains correct.
- [x] Left shell nav remains independent of page scroll.
- [x] AI assistant remains independent of page scroll.
- [x] Event detail panel renders through the workspace inspector and preserves covered interaction behavior; enabled visual screenshot proof remains pending.
- [x] Mobile sidebars route through overlay chrome without grid-track width; enabled/manual overflow proof remains pending.
- [ ] No duplicated panel width constants remain.
- [x] No EventList negative margin layout hacks remain.
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

Resume from the latest incomplete phase rather than restarting Phase 1. Production shell rendering now uses `DockLayoutHost` and passed focused closeout review; Phase 5 workspace rendering hosts EventList Customize View through `events.customize-view` and Event Preview through `events.event-preview`; Phase 9 mobile routing sends open docked side panels through `DockOverlayHost` as temporary overlays on `Xs`/`Sm` breakpoints, responsive policy now retracts start panels as right-side panels consume content width, narrow/mobile layouts enforce one right-side panel, and overlay close animations reverse the opening motion before unmount. Phase 8 persistence/reset is already implemented as a non-rendering subsystem slice; do not duplicate it. The current safe next work is focus-trap strategy, enabled E2E/manual visual baseline capture, full RTL/manual verification, or cleanup of remaining temporary bridge services/components once all consumers are confirmed migrated. The remaining full client-test failures are unrelated onboarding/profile/Cerbos tests and should be handled separately.
