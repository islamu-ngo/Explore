<!-- ABOUTME: Resume context for the generic dock layout engine refactor. -->
<!-- ABOUTME: Captures verified files, decisions, constraints, and next implementation steps. -->

# Sidebar Dock Layout Refactor - Context v2

Last Updated: 2026-04-29

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

- Planning only. No implementation code has been changed.
- Next implementation phase is Phase 1: baseline tests and visual freeze.

### Blockers

- Tavily MCP and context7 MCP were requested but are not exposed in this environment. If available later, use them to confirm MudBlazor and Blazor guidance, but do not restart the architecture unless they reveal a concrete conflict.

## Session Handoff (2026-04-29)

### Current Implementation State

- Implementation has not started.
- The active work is a planning package revision from v1 explicit shell/workspace state to v2 generic internal dock engine.
- No production Blazor/CSS/C# implementation files were changed in this session.
- No tests were run because no implementation code changed.

### Files Modified This Session

| File | Why it changed |
|---|---|
| `dev/active/sidebar-dock-layout-refactor/sidebar-dock-layout-refactor-plan.md` | Rewritten to v2: generic dock engine core, descriptor/state/snapshot model, registry, hosts, resizing, stacking, persistence, and revised phases. |
| `dev/active/sidebar-dock-layout-refactor/sidebar-dock-layout-refactor-context.md` | Updated handoff context, decisions, files to create, and quick resume instructions. |
| `dev/active/sidebar-dock-layout-refactor/sidebar-dock-layout-refactor-tasks.md` | Updated task checklist to v2 phases and added planning/handoff status. |
| `dev/_journal/journal.md` | Added key decision entry for the dock engine architecture pivot. |

### Key Decisions Made This Session

- The target is a generic internal dock engine used by App Shell and Workspace Layout, not only `ShellLayoutState` plus `WorkspacePanelState`.
- The engine must be descriptor-driven with stable `DockPanelId` values and no central enum that must be edited for each future panel.
- Runtime `DockPanelState` must be separate from immutable `DockPanelDescriptor` metadata.
- `DockLayoutSnapshot` is a first-class model for reset, persistence, debugging, tests, and future user preferences.
- Advanced behaviors are planned as explicit phases: resizing, stacking, persistence, mobile/RTL/a11y/motion hardening.
- External/plugin-driven panel loading is intentionally out of scope for the first implementation; registration should remain compile-time and component-owned.
- Event detail preview remains the highest-risk UX preservation target and must stay inspector/overlay style.

### Next Immediate Steps

1. Start Phase 1 by adding visual regression coverage before any layout refactor.
2. Add E2E scenarios in `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs` for desktop/mobile panel combinations.
3. Add bUnit regressions for current AI availability/toggle behavior, customize/detail mutual exclusion, event detail close/reset behavior, and shell landmarks.
4. Only after baseline coverage, start Phase 2 tokens and `docs/DOCK_LAYOUT.md`.
5. Then implement Phase 3 dock engine core before migrating shell or workspace rendering.

### Commands To Run On Restart

```bash
dotnet build --configuration Release --verbosity quiet
```

```bash
dotnet test --project Explore.Blazor.Client.Tests/Explore.Blazor.Client.Tests.csproj --configuration Release --verbosity quiet
```

When E2E/Aspire environment is available:

```bash
dotnet test --project Explore.Blazor.Client.E2ETests/Explore.Blazor.Client.E2ETests.csproj --configuration Release --verbosity quiet
```

### Uncommitted Changes Requiring Attention

- The four documentation files listed above are expected uncommitted planning/handoff changes.
- There are no partial implementation files from this session.

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
| `Explore.Blazor.Client/Layout/MainLayout.razor` | Current shell layout, left `MudDrawer`, `MudMainContent`, footer, AI rail host. |
| `Explore.Blazor.Client/Layout/MainLayout.razor.cs` | Current shell state subscriptions, chrome visibility, AI availability load. |
| `Explore.Blazor.Client/Layout/MainLayout.razor.css` | Current shell CSS, including AI `margin-right: 360px` compensation. |
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

## Files To Create

| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Services/Docking/DockPanelId.cs` | Stable typed panel identifier. |
| `Explore.Blazor.Client/Services/Docking/DockScope.cs` | Shell/workspace scope enum. |
| `Explore.Blazor.Client/Services/Docking/DockSide.cs` | Start/end/bottom side enum. |
| `Explore.Blazor.Client/Services/Docking/DockMode.cs` | Docked/overlay/temporary/inspector/collapsed mode enum. |
| `Explore.Blazor.Client/Services/Docking/DockPanelDescriptor.cs` | Panel metadata contract. |
| `Explore.Blazor.Client/Services/Docking/DockPanelState.cs` | Runtime state contract. |
| `Explore.Blazor.Client/Services/Docking/DockLayoutSnapshot.cs` | Serializable layout snapshot. |
| `Explore.Blazor.Client/Services/Docking/IDockPanelRegistry.cs` | Registry abstraction. |
| `Explore.Blazor.Client/Services/Docking/DockPanelRegistry.cs` | Registry implementation. |
| `Explore.Blazor.Client/Services/Docking/DockLayoutState.cs` | Runtime docking engine. |
| `Explore.Blazor.Client/Services/Docking/IDockLayoutPersistence.cs` | Snapshot persistence abstraction. |
| `Explore.Blazor.Client/Services/Docking/LocalStorageDockLayoutPersistence.cs` | Initial local persistence. |
| `Explore.Blazor.Client/Services/Docking/DockFocusManager.cs` | Focus save/restore policy. |
| `Explore.Blazor.Client/Components/Docking/DockLayoutHost.razor` and `.razor.css` | Scope host and grid boundary. |
| `Explore.Blazor.Client/Components/Docking/DockSideHost.razor` and `.razor.css` | Ordered side host. |
| `Explore.Blazor.Client/Components/Docking/DockPanelHost.razor` and `.razor.css` | Docked panel renderer. |
| `Explore.Blazor.Client/Components/Docking/DockOverlayHost.razor` and `.razor.css` | Overlay/inspector renderer. |
| `Explore.Blazor.Client/Components/Docking/DockResizeHandle.razor` and `.razor.css` | Resize affordance. |
| `Explore.Blazor.Client/Components/Docking/DockTabStrip.razor` and `.razor.css` | Tabbed stack renderer. |
| `Explore.Blazor.Client/Components/Shell/AppSideNav.razor` and `.razor.css` | Extracted shell left nav content. |
| `Explore.Blazor.Client/Components/Shell/AppRightRail.razor` and `.razor.css` | Optional shell right rail wrapper. |
| `Explore.Blazor.Client/Pages/Events/EventDockPanels.cs` | Event module panel descriptors. |
| `docs/DOCK_LAYOUT.md` | Platform dock layout architecture. |

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
dotnet build --configuration Release --verbosity quiet
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.BlazorClientArchitectureTests
```

```bash
dotnet test --project Event.Architecture.Tests/Event.Architecture.Tests.csproj --configuration Release --verbosity quiet --filter FullyQualifiedName~Event.Architecture.Tests.AccessibilityConventionTests
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
2. Start with Phase 1 visual/bUnit regression tests before layout changes.
3. Add tokens and `docs/DOCK_LAYOUT.md`.
4. Implement the generic dock engine core before migrating shell/workspace rendering.
5. Migrate shell panels first: `shell.left-nav`, `shell.ai-assistant`.
6. Migrate EventList workspace panels second: `events.customize-view`, `events.event-preview`.
7. Add resizing, stacking, persistence, and hardening as explicit phases.
8. Update this context file after each phase.
