<!-- ABOUTME: Resume context for the generic dock layout engine refactor. -->
<!-- ABOUTME: Captures verified files, decisions, constraints, and next implementation steps. -->

# Sidebar Dock Layout Refactor - Context v2

Last Updated: 2026-04-30

## Session Progress

### Completed

- Read `.claude/commands/dev-docs.md` and created the initial three-file planning package under `dev/active/sidebar-dock-layout-refactor/`.
- Loaded relevant skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `clean-architecture-rules`, and `agentic-research`.
- Read key repo docs: `CLAUDE.md`, `docs/ACCESSIBILITY.md`, `docs/BLAZOR_DEV_WORKFLOW.md`, `docs/GOVERNANCE.md`, and `dev/active/README.md`.
- Verified existing sidebar/layout files and classes with `Glob` and `Grep` before referencing them.
- Verified old proposed explicit-only classes did not exist: `ShellLayoutState`, `WorkspacePanelState`, `WorkspaceLayout`, `AppShellLayout`, `AppSideNav`, `AppRightRail`, `WorkspaceRightPanel`, and `WorkspaceOverlayPanel`.
- Consulted official docs/web sources available in this runtime: MudBlazor Drawer docs/source, Microsoft Blazor CSS isolation, Microsoft Blazor state management, and MDN CSS Grid.
- Updated the plan to v2 based on CTO feedback: the target is now a generic internal dock engine used by shell and workspace layouts, not only explicit shell/workspace state services.
- Per `/dev-docs-update`, captured session handoff and journaled the major architecture decision.

### In Progress

- Implementation has started with safe foundation and baseline-test slices: generic dock engine models/state, DI registration, dock host components, dormant tabbed stack rendering, extracted shell side-navigation content, shell descriptor registration, production shell `DockLayoutHost` rendering, EventList workspace customize-view `DockLayoutHost` rendering, shell bridge lifecycle hardening, NavMenu dock-id toggle mirroring, shell accessibility/navigation contract hardening, dormant keyboard/pointer resize handle foundation with pointer-capture hardening and JSInterop invocation coverage, dock governance architecture guardrails, dock design tokens/docs, stable shell/workspace selectors, low-risk logical CSS hardening, descriptor capability enforcement, RTL-aware inline-end slide animations, snapshot active-state normalization, Phase 8 persistence/reset, and Phase 1 bUnit/E2E scenario coverage.
- Current implementation phase is Phase 1 baseline coverage plus Phase 2/3/4/5/6/7/8/11 foundation setup; production shell rendering now uses the dock host and the first workspace rendering slice (`events.customize-view`) is complete. Event preview workspace migration has not started.

### Blockers

- Tavily MCP and context7 MCP were available in the 2026-04-30 session and confirmed the MudBlazor `MudLayout`/`MudDrawer` guidance, drawer variant tradeoffs, and Blazor CSS isolation constraints. No concrete conflict with the v2 architecture was found.
- Oracle reviewed the next-slice choice on 2026-04-30 and initially recommended implementing Phase 8 persistence/reset as a non-rendering dock subsystem slice. After the user explicitly requested continued implementation without backward compatibility constraints, Oracle recommended a narrow production shell `DockLayoutHost` migration; that shell slice has now landed and passed focused closeout review. The first workspace `DockLayoutHost` slice has also landed for EventList Customize View; event preview migration remains the active next risk area.

## Session Handoff (2026-04-30)

### Current Implementation State

- Implementation started with a non-rendering foundation slice; the shell host portion is now production-rendered through `DockLayoutHost`.
- The active code path now includes `Explore.Blazor.Client.Services.Docking` primitives and scoped `DockLayoutState`/`IDockPanelRegistry` registration.
- `DockLayoutState` now enforces `CanClose`/`IsResizable` across direct mutations and snapshot restore, normalizes restored snapshots so each open scope/side group has exactly one active open panel, and prevents transient multi-active notifications during `Open`.
- `Explore.Blazor.Client.Components.Docking` now contains host components (`DockLayoutHost`, `DockSideHost`, `DockPanelHost`, `DockOverlayHost`) plus a `DockTabStrip` stack renderer that renders ordered tabs and active panel content for multiple open panels on the same side. These hosts now render the production shell and EventList customize-view workspace panel; EventList event preview migration remains future work. The stack renderer uses coherent `tablist`/`tab`/`tabpanel` ARIA relationships and moves focus to keyboard-activated tabs through the accessibility focus service.
- `Explore.Blazor.Client.Components.Docking.DockResizeHandle` now provides a dormant keyboard-accessible and pointer-drag resize affordance for resizable docked inline panels; it uses a small JS pointer-capture helper for drag robustness, filters non-primary and mismatched pointer ids, and `DockPanelHost` wires it to `DockLayoutState.Resize` for start/end docked panels only. bUnit coverage now proves the `/js/dock-resize.js` module import plus `setPointerCapture` and `releasePointerCapture` calls.
- `Explore.Blazor.Client.Components.Shell.AppSideNav` owns the left navigation content and is now rendered through the `shell.left-nav` dock descriptor.
- `Explore.Blazor.Client.Components.Shell.ShellDockPanels` owns stable shell descriptor ids for `shell.left-nav` and `shell.ai-assistant`; `MainLayout` registers their content into `DockLayoutState` and renders them through the shell `DockLayoutHost`.
- `MainLayout` still mirrors `SidebarState` and `AiAssistantState` open/close changes into `DockLayoutState` as a temporary toggle bridge, but the production shell panel hosts are dock-rendered rather than `MudDrawer`/fixed AI margin compensation.
- `NavMenu` still uses the legacy shell state services as the public toggle bridge, but the visible shell panel rendering flows through `shell.left-nav` and `shell.ai-assistant` dock ids.
- `MainLayoutTests` now covers hidden-chrome routes closing the mirrored shell dock panels and `MainLayout.Dispose()` unregistering shell descriptors, reducing lifecycle risk before host migration.
- Phase 8 persistence/reset is implemented as a non-rendering dock subsystem slice: `IDockLayoutPersistence` abstracts snapshot load/save/delete, `LocalStorageDockLayoutPersistence` stores schema-versioned layout snapshots by layout key behind browser-only JS interop, and `DockLayoutState.ResetToDefaults()` restores currently registered panels to descriptor defaults without wiring production shell hydration yet.
- `LocalStorageDockLayoutPersistence` lives under `Explore.Blazor.Client/Services/Interop/` because `Event.Architecture.Tests` enforces that direct `IJSRuntime` usage stays in approved interop/http boundaries or `*Interop`-suffixed files. The first implementation under `Services/Docking` failed `Rule_1_07_Services_MustNotUse_IJSRuntime_OutsideInterop`; moving the implementation behind the approved interop namespace fixed the architecture boundary without changing the `IDockLayoutPersistence` abstraction.
- `AiAssistantRail` supports dock-hosted mode (`HostedInDock=true`) so shell AI renders inside `DockPanelHost` without a fixed rail backdrop or page-level margin compensation. `RightSidebar` remains available for legacy consumers, but EventList no longer uses it for Customize View.
- Workspace rendering now uses `EventDockPanels.CustomizeView` and `DockLayoutHost Scope=Workspace` for the EventList customization panel. `_customizationDrawerOpen` remains only as a temporary migration adapter mirrored into `DockLayoutState`; the event preview drawer still uses the legacy `_detailDrawerOpen`/temporary MudDrawer path. Legacy `SidebarState`/`AiAssistantState` remain only as temporary shell toggle bridge services.
- Tests were added for dock state behavior, DI registration, dock host rendering/scope isolation/ordering/overlay filtering, tabbed side-stack rendering/click activation/keyboard activation/focus movement/tabpanel linkage, resize handle ARIA/keyboard/pointer capture JSInterop invocation/pointer identity/clamping/cancellation/non-resizable behavior, AppSideNav rendering, shell descriptor registration/state mirroring, shell hidden-chrome/disposal lifecycle behavior, NavMenu dock-id toggle mirroring, shell accessibility/navigation contracts (skip link, landmarks, live regions, focus-on-navigate), shell AI availability/toggle behavior, EventList customize/detail mutual exclusion, EventList detail close/reset behavior, snapshot serialization/corrupt-data handling, unknown-panel restore handling, width clamping, and descriptor-default reset behavior.
- Architecture guardrails now prevent new central dock panel enums and page-scoped CSS shell compensation outside known legacy EventList migration debt.
- `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs` now lists the required desktop/mobile visual baseline scenarios, but they are skipped until Aspire-backed seed data and screenshot storage are available.
- Latest verification for the workspace customize-view slice passed: `Explore.Blazor.Client.Tests` 919 total/918 passed with 1 documented pre-existing skip (`ErrorState_RendersRetryButton_WhenOnRetryProvided`), `Event.Architecture.Tests` 131/131, and `rtk dotnet build --configuration Release --verbosity quiet` completed 23 projects with 0 errors. Build warnings remain known package/analyzer warnings.

### Files Modified Across The Active Dock Refactor Slices

| Area | Files | Why they changed |
|---|---|---|
| Dock engine | `Explore.Blazor.Client/Services/Docking/*`, `Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs`, `Explore.Blazor.Client/wwwroot/js/dock-layout-persistence.js` | Added descriptor-driven panel ids, descriptors, runtime state, snapshots, registry abstraction, scoped `DockLayoutState` orchestration, descriptor-default reset behavior, and schema-versioned local snapshot persistence behind the approved JS interop boundary. |
| Dock hosts | `Explore.Blazor.Client/Components/Docking/*` | Added dormant host/grid/side/panel/overlay renderers, tabbed side-stack renderer, resize handle, pointer-capture helper integration, and stable element id helpers. |
| Shell bridge/host | `Explore.Blazor.Client/Components/Shell/AppSideNav.*`, `ShellDockPanels.cs`, `AiAssistantRail.*`, `Explore.Blazor.Client/Layout/MainLayout.*`, `NavMenu.*`, `Footer.*` | Extracted shell nav content, registered shell descriptors, migrated production shell panels to `DockLayoutHost`, removed the legacy drawer/AI/footer compensation path, and kept top-nav toggles bridged by `DockPanelId`. |
| Workspace host | `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs`, `EventList.razor`, `EventList.razor.cs`, `EventList.razor.css` | Added event workspace descriptors, rendered EventList content through a workspace `DockLayoutHost`, hosted Customize View through `events.customize-view`, removed EventList `RightSidebar` dependency, and removed the page negative margin/width expansion hack. |
| Tests | `Explore.Blazor.Client.Tests/**`, `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs`, `Event.Architecture.Tests/DockLayoutArchitectureTests.cs` | Added dock state/DI/host/resize/stack/shell bridge/accessibility/EventList regression coverage, skipped visual scenario scaffolding, and architecture guardrails. |
| Docs | `docs/DOCK_LAYOUT.md`, `dev/active/sidebar-dock-layout-refactor/*`, `dev/_journal/journal.md` | Kept the implementation plan, task checklist, handoff context, architecture reference, and durable lessons aligned with completed slices and deferred migration gaps. |

### Key Decisions Made This Session

- The target is a generic internal dock engine used by App Shell and Workspace Layout, not only `ShellLayoutState` plus `WorkspacePanelState`.
- The engine must be descriptor-driven with stable `DockPanelId` values and no central enum that must be edited for each future panel.
- Runtime `DockPanelState` must be separate from immutable `DockPanelDescriptor` metadata.
- `DockLayoutSnapshot` is a first-class model for reset, persistence, debugging, tests, and future user preferences.
- Phase 8 persistence remains opt-in and non-rendering for now: snapshots are serialized locally by layout key, but production shell hydration/autosave is intentionally deferred until the dock hosts own visible rendering.
- JS interop implementations that touch `IJSRuntime` must live under `Services/Interop`, `Services/Http`, or follow the architecture-approved interop naming pattern; keep persistence abstractions in `Services/Docking` and browser storage implementations behind that boundary.
- Advanced behaviors are planned as explicit phases: resizing, stacking, persistence, mobile/RTL/a11y/motion hardening.
- External/plugin-driven panel loading is intentionally out of scope for the first implementation; registration should remain compile-time and component-owned.
- Event detail preview remains the highest-risk UX preservation target and must stay inspector/overlay style.

### Next Immediate Steps

1. Continue Phase 5 workspace migration by moving event detail preview to `events.event-preview` through `DockOverlayHost` without degrading current desktop/mobile UX.
2. Enable or manually run the scaffolded E2E visual scenarios once Aspire seed data and screenshot storage are available.
3. Continue mobile overlay hardening: scroll lock, Escape close, focus restore/trap behavior, and RTL/manual checks for shell/workspace overlays.
4. Preserve the bUnit baseline/accessibility contract tests and architecture guardrails before cleanup.

### Commands To Run On Restart

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

```bash
rtk dotnet build --configuration Release --verbosity quiet
```

If these commands regenerate package-lock churn from the local .NET SDK, restore the generated lockfile diffs unless dependencies intentionally changed:

```bash
git diff -- .claude/hooks/packages.lock.json Explore.Blazor.Client.Tests/packages.lock.json Explore.Blazor.Client/packages.lock.json Explore.Blazor.IntegrationTests/packages.lock.json Explore.Blazor/packages.lock.json
```

When E2E/Aspire environment is available:

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

### Uncommitted Changes Requiring Attention

- Sidebar dock implementation files, tests, tokens, and docs are expected uncommitted changes from the foundation slices.
- Phase 8-specific uncommitted files include `Explore.Blazor.Client/Services/Docking/IDockLayoutPersistence.cs`, `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs`, `Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs`, `Explore.Blazor.Client/wwwroot/js/dock-layout-persistence.js`, `Explore.Blazor.Client/Properties/AssemblyInfo.cs`, `Explore.Blazor.Client/Extensions/ServiceCollectionExtensions.cs`, `Explore.Blazor.Client.Tests/Services/Docking/DockLayoutStateTests.cs`, and `Explore.Blazor.Client.Tests/Services/Docking/LocalStorageDockLayoutPersistenceTests.cs`.
- Unrelated pre-existing edits remain outside this work and should not be attributed to the dock refactor: `Event.API.IntegrationTests/Fixtures/AuthenticatedWebApplicationFactory.cs`, `Explore.Blazor/Extensions/BffAuthEndpoints.cs`, and `dev/active/api-contract-stabilization/api-contract-stabilization-action-inventory.md`.

## Core Decision

Build a generic internal dock engine that powers explicit shell and workspace layout hosts.

This replaces the v1 final-state idea of only using `ShellLayoutState` and `WorkspacePanelState`. Facade/controller helpers can still exist for ergonomics, but the underlying model must be descriptor-driven and generic.

## Approved Model

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

## Architectural Boundaries

| Scope | Owns | Scroll behavior |
|---|---|---|
| Shell | Top app bar, left app navigation, global AI rail, global mobile overlays | Outside page scroll |
| Workspace | Page content, customize view, page-specific right panels, event detail inspector overlay | Inside page/workspace rules |

## Important Decisions

| Decision | Rationale |
|---|---|
| Use a generic internal dock engine | Future panels should not require central enum rewrites or another layout refactor. |
| Keep registration controlled and compile-time for now | Generic engine must not become uncontrolled plugin loading. |
| Separate descriptor metadata from runtime state | Descriptor answers what the panel is; state answers what is happening now. |
| Model snapshots now | Enables reset, persistence, tests, debugging, and future user preferences. |
| Use CSS grid tracks for persistent desktop panels | Prevents page-specific margin compensation and sibling gaps. |
| Use overlay transforms for temporary/mobile/inspector panels | Matches expected UX for inspectors and mobile drawers. |
| Keep event detail preview as inspector/overlay | Current event detail UX is strong and must not be forced into docked behavior. |
| Centralize widths and motion in tokens/descriptors | Avoids duplicated hardcoded widths and inconsistent animation. |
| Use logical CSS properties | Required by `docs/ACCESSIBILITY.md` and RTL support. |

## Core Types To Create

```csharp
public sealed record DockPanelId(string Value);

public enum DockScope { Shell, Workspace }

public enum DockSide { Start, End, Bottom }

public enum DockMode { Docked, Overlay, Temporary, Inspector, Collapsed }

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

## Verified Existing Files

| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Layout/MainLayout.razor` | Current shell layout with shell `DockLayoutHost`, `MudMainContent`, footer, and live regions. |
| `Explore.Blazor.Client/Layout/MainLayout.razor.cs` | Current shell descriptor registration, chrome visibility, AI availability load, and temporary shell state bridge. |
| `Explore.Blazor.Client/Layout/MainLayout.razor.css` | Current shell CSS without AI page-margin compensation. |
| `Explore.Blazor.Client/Layout/NavMenu.razor` | Current top nav and sidebar/AI toggle buttons. |
| `Explore.Blazor.Client/Layout/NavMenu.razor.cs` | Current nav state/service dependencies. |
| `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor` | Current AI assistant content and close behavior. |
| `Explore.Blazor.Client/Components/Shell/AiAssistantRail.razor.css` | Current fixed AI rail layout. |
| `Explore.Blazor.Client/Components/Common/RightSidebar.razor` | Current generic page-level right sidebar wrapper. |
| `Explore.Blazor.Client/Components/Common/RightSidebar.razor.css` | Current sticky desktop/fixed mobile page-level sidebar CSS. |
| `Explore.Blazor.Client/Pages/Events/EventList.razor` | Current EventList page, detail drawer, customize panel host. |
| `Explore.Blazor.Client/Pages/Events/EventList.razor.cs` | Current EventList page state including `_detailDrawerOpen` and `_customizationDrawerOpen`. |
| `Explore.Blazor.Client/Pages/Events/EventList.razor.css` | Current page layout CSS with negative right margin and width expansion. |
| `Explore.Blazor.Client/Pages/Events/Components/EventListCustomizationDrawer.razor` | Customize view content component to preserve. |
| `Explore.Blazor.Client/Services/SidebarState.cs` | Current left sidebar state service to replace. |
| `Explore.Blazor.Client/Services/AiAssistantState.cs` | Current AI rail state service to replace. |
| `Explore.Blazor/wwwroot/css/tokens.css` | Global design tokens to extend with dock width, z-index, and motion tokens. |
| `Explore.Blazor.Client.Tests/Layout/MainLayoutTests.cs` | Existing layout bUnit tests. |
| `Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs` | Existing EventList tests. |
| `Explore.Blazor.Client.Tests/Components/Event/EventListCustomizationDrawerTests.cs` | Existing customization drawer content tests. |
| `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs` | Existing Playwright/E2E test fixture entry point. |

## Implementation Inventory And Remaining Files

Most foundation files originally listed here have now been implemented. `DockLayoutState` is the registry implementation; a separate `DockPanelRegistry.cs` was intentionally not created.

| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Services/Docking/IDockLayoutPersistence.cs` | Snapshot persistence abstraction; implemented as Phase 8 non-rendering slice. |
| `Explore.Blazor.Client/Services/Interop/LocalStorageDockLayoutPersistence.cs` | Initial client-side persistence behind the approved JS interop boundary; implemented as Phase 8 non-rendering slice. |
| `Explore.Blazor.Client/Services/Docking/DockFocusManager.cs` | Overlay focus save/restore policy; future hardening phase. |
| `Explore.Blazor.Client/Components/Shell/AppRightRail.razor` and `.razor.css` | Optional shell right rail wrapper if AI rail content needs extraction before production host migration. |
| `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs` | Event module descriptors for `events.customize-view` and `events.event-preview`; future Phase 5. |

## Implementation Guardrails

- Do not implement external/plugin-driven panel loading now.
- Do not use central `WorkspacePanel` or shell-specific panel enums as the final extension model.
- Do not redesign `EventListCustomizationDrawer`; move the layout wrapper around it.
- Do not redesign event detail preview; integrate it as inspector/overlay while preserving behavior.
- Do not use page CSS to compensate for shell AI rail width.
- Do not add new physical CSS direction properties in dock layout CSS.
- Do not remove `SidebarState`, `AiAssistantState`, or `RightSidebar` until all consumers are migrated and tests pass.

## Verification Commands

```bash
rtk dotnet build --configuration Release --verbosity quiet
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet
```

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

E2E/visual tests require Aspire/Playwright environment:

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

## Quick Resume

1. Read `sidebar-dock-layout-refactor-plan.md`.
2. Do not duplicate completed foundation work: dock engine core, dormant hosts, resizing, stacking, shell bridge, and Phase 8 persistence/reset are already implemented as non-rendering slices.
3. Production shell rendering is complete; EventList Customize View workspace rendering is complete; event preview workspace rendering remains incomplete.
4. Resume with the latest incomplete safe slice: Phase 5 event preview migration, visual resized-panel coverage, full mobile stack behavior, or overlay scroll-lock/Escape/focus-restore hardening.
5. Do not recreate shell migration; `shell.left-nav` and `shell.ai-assistant` already render through the dock engine.
6. Migrate EventList event preview next: `events.event-preview`.
7. Preserve the EventList detail preview as inspector/overlay; it remains the highest-risk UX preservation target.
8. Update this context file after each phase.
