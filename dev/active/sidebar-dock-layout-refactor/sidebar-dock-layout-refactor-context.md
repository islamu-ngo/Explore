<!-- ABOUTME: Resume context for the sidebar dock layout refactor planning package. -->
<!-- ABOUTME: Captures verified files, decisions, constraints, and next implementation steps. -->

# Sidebar Dock Layout Refactor - Context

Last Updated: 2026-04-29

## Session Progress

### Completed

- Read `.claude/commands/dev-docs.md` and created this three-file planning package under `dev/active/sidebar-dock-layout-refactor/`.
- Loaded relevant skills: `blazor-ui-conventions`, `blazor-css-isolation`, `design-system`, `clean-architecture-rules`, and `agentic-research`.
- Read key repo docs: `CLAUDE.md`, `docs/ACCESSIBILITY.md`, `docs/BLAZOR_DEV_WORKFLOW.md`, `docs/GOVERNANCE.md`, and existing `dev/active/README.md` guidance.
- Verified existing sidebar/layout files and classes with `Glob` and `Grep` before referencing them in the plan.
- Verified missing future classes/components: `ShellLayoutState`, `WorkspacePanelState`, `WorkspaceLayout`, `AppShellLayout`, `AppSideNav`, `AppRightRail`, `WorkspaceRightPanel`, and `WorkspaceOverlayPanel` do not exist yet.
- Consulted official docs/web sources available in this runtime: MudBlazor Drawer docs/source, Microsoft Blazor CSS isolation, Microsoft Blazor state management, and MDN CSS Grid.

### In Progress

- Planning only. No implementation code has been changed.

### Blockers

- Tavily MCP and context7 MCP were requested but are not exposed in this environment. If available in a later session, use them to confirm MudBlazor drawer behavior and CSS grid/logical property guidance, but do not restart the architecture direction unless they reveal a concrete conflict.

## Core Decision

Build an explicit App Shell plus Workspace Layout subsystem, not a generic plugin docking engine.

Approved model:

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

## Architectural Boundaries

| Scope | Owns | Scroll behavior |
|---|---|---|
| Shell | Top app bar, left app navigation, global AI rail, global mobile overlays | Outside page scroll |
| Workspace | Page content, customize view, page-specific right panels, event detail inspector overlay | Inside page/workspace rules |

## Important Design Decisions

| Decision | Rationale |
|---|---|
| Use CSS grid tracks for persistent desktop panels | Prevents page-specific margin compensation and guarantees sibling alignment. |
| Use overlay transforms for temporary/mobile/inspector panels | Matches MudBlazor and native UI expectations for temporary panels. |
| Keep event detail preview as overlay/inspector | Current event detail UX is strong and should not be forced into docked grid behavior. |
| Keep state explicit | `ShellLayoutState` and `WorkspacePanelState` are sufficient; no dynamic descriptor registry yet. |
| Centralize widths and motion in tokens | Avoids duplicated hardcoded widths and inconsistent animations. |
| Use logical CSS properties | Required by `docs/ACCESSIBILITY.md` and RTL support. |

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
| `Explore.Blazor/wwwroot/css/tokens.css` | Global design tokens to extend with layout width, z-index, and motion tokens. |
| `Explore.Blazor.Client.Tests/Layout/MainLayoutTests.cs` | Existing layout bUnit tests. |
| `Explore.Blazor.Client.Tests/Pages/Event/EventListTests.cs` | Existing EventList tests. |
| `Explore.Blazor.Client.Tests/Components/Event/EventListCustomizationDrawerTests.cs` | Existing customization drawer content tests. |
| `Explore.Blazor.Client.E2ETests/Flows/SmokeTests.cs` | Existing Playwright/E2E test fixture entry point. |

## Files To Create

These were verified missing and should be created during implementation:

| File | Purpose |
|---|---|
| `Explore.Blazor.Client/Services/ShellLayoutState.cs` | Scoped shell layout state for left nav and AI rail. |
| `Explore.Blazor.Client/Services/WorkspacePanelState.cs` | Scoped workspace panel state for docked and overlay page panels. |
| `Explore.Blazor.Client/Components/Shell/AppSideNav.razor` | Extracted shell left navigation component. |
| `Explore.Blazor.Client/Components/Shell/AppSideNav.razor.css` | Isolated BEM CSS for shell left nav. |
| `Explore.Blazor.Client/Components/Shell/AppRightRail.razor` | Optional explicit shell right rail wrapper if `AiAssistantRail` should remain content-only. |
| `Explore.Blazor.Client/Components/Shell/AppRightRail.razor.css` | Optional CSS for shell right rail wrapper. |
| `Explore.Blazor.Client/Components/Layout/WorkspaceLayout.razor` | Reusable workspace grid host. |
| `Explore.Blazor.Client/Components/Layout/WorkspaceLayout.razor.css` | Workspace grid and responsive panel CSS. |
| `Explore.Blazor.Client/Components/Layout/WorkspaceRightPanel.razor` | Docked workspace right panel primitive. |
| `Explore.Blazor.Client/Components/Layout/WorkspaceRightPanel.razor.css` | Isolated panel CSS. |
| `Explore.Blazor.Client/Components/Layout/WorkspaceOverlayPanel.razor` | Overlay/inspector panel primitive for detail preview. |
| `Explore.Blazor.Client/Components/Layout/WorkspaceOverlayPanel.razor.css` | Overlay panel transform/backdrop CSS. |
| `Explore.Blazor.Client.E2ETests/Flows/SidebarLayoutVisualTests.cs` | Visual regression scenarios for panel combinations. |

## Essential Interface Signatures

Use these as a starting point, adjusting only if implementation details require it.

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

## Implementation Guardrails

- Do not introduce a dynamic dock descriptor registry in this phase.
- Do not redesign `EventListCustomizationDrawer`; move the layout wrapper around it.
- Do not redesign the event detail preview; integrate it as overlay/inspector while preserving behavior.
- Do not use page CSS to compensate for shell AI rail width.
- Do not add new physical CSS direction properties in refactored layout CSS.
- Do not remove `SidebarState`, `AiAssistantState`, or `RightSidebar` until all consumers are migrated and tests pass.

## Verification Commands

Run these during/after implementation:

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

1. Read `dev/active/sidebar-dock-layout-refactor/sidebar-dock-layout-refactor-plan.md`.
2. Start with Phase 1 visual/bUnit regression tests before layout changes.
3. Add design tokens in `Explore.Blazor/wwwroot/css/tokens.css`.
4. Create `ShellLayoutState` and `WorkspacePanelState` with tests.
5. Migrate shell AI/left nav first, then EventList customize panel, then event detail overlay.
6. Update this context file after each phase.
