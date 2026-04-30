// ABOUTME: bUnit tests for generic dock host rendering and scope isolation.
// ABOUTME: Verifies the dormant Phase 4 host components before MainLayout migration.

using Explore.Blazor.Client.Components.Docking;
using Explore.Blazor.Client.Contracts.Services.Accessibility;
using Explore.Blazor.Client.Services.Docking;

namespace Explore.Blazor.Client.Tests.Components.Docking;

public sealed class DockHostTests : IDisposable
{
    private readonly BlazorTestContext _ctx;
    private readonly DockLayoutState _dockLayoutState = new();

    public DockHostTests()
    {
        _ctx = new BlazorTestContext();
        _ctx.Services.AddSingleton(_dockLayoutState);
    }

    public void Dispose()
    {
        _ctx.Dispose();
    }

    [Test]
    public async Task DockLayoutHost_RendersChildContentAndOpenDockedPanels()
    {
        var panelId = new DockPanelId("shell.left-nav");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Shell, DockSide.Start, order: 10), CreateContent("Navigation content"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockLayoutHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Shell)
            .AddChildContent("Main content"));

        await Assert.That(cut.Markup).Contains("Main content");
        await Assert.That(cut.Markup).Contains("Navigation content");
        await Assert.That(cut.Find("[data-testid='dock-layout-host']").GetAttribute("style")).Contains("--dock-layout-start-width: 320px;");
        await Assert.That(cut.Find("[data-dock-panel-id='shell.left-nav']").GetAttribute("aria-label")).IsEqualTo("Panel shell.left-nav");
    }

    [Test]
    public async Task DockLayoutHost_RendersOnlyPanelsForRequestedScope()
    {
        var shellPanelId = new DockPanelId("shell.ai-assistant");
        var workspacePanelId = new DockPanelId("workspace.customize");
        _dockLayoutState.Register(CreateDescriptor(shellPanelId, DockScope.Shell, DockSide.End, order: 10), CreateContent("Shell AI"));
        _dockLayoutState.Register(CreateDescriptor(workspacePanelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Workspace customization"));
        _dockLayoutState.Open(shellPanelId);
        _dockLayoutState.Open(workspacePanelId);

        var cut = _ctx.Render<DockLayoutHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Shell)
            .AddChildContent("Shell body"));

        await Assert.That(cut.Markup).Contains("Shell AI");
        await Assert.That(cut.Markup).DoesNotContain("Workspace customization");
    }

    [Test]
    public async Task DockSideHost_WithMultiplePanels_RendersTabStripAndActivePanelOnly()
    {
        var secondPanelId = new DockPanelId("workspace.second");
        var firstPanelId = new DockPanelId("workspace.first");
        _dockLayoutState.Register(CreateDescriptor(secondPanelId, DockScope.Workspace, DockSide.End, order: 20), CreateContent("Second panel"));
        _dockLayoutState.Register(CreateDescriptor(firstPanelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("First panel"));
        _dockLayoutState.Open(secondPanelId);
        _dockLayoutState.Open(firstPanelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var panels = cut.FindAll("[data-testid='dock-panel-host']");
        var tabs = cut.FindAll("[data-testid='dock-tab-strip-tab']");

        await Assert.That(tabs.Count).IsEqualTo(2);
        await Assert.That(tabs[0].GetAttribute("data-dock-tab-panel-id")).IsEqualTo("workspace.first");
        await Assert.That(tabs[1].GetAttribute("data-dock-tab-panel-id")).IsEqualTo("workspace.second");
        await Assert.That(tabs[0].GetAttribute("aria-selected")).IsEqualTo("true");
        await Assert.That(tabs[0].GetAttribute("aria-controls")).IsEqualTo("dock-panel-body-workspace-first");
        await Assert.That(tabs[1].HasAttribute("aria-controls")).IsFalse();
        await Assert.That(panels.Count).IsEqualTo(1);
        await Assert.That(panels[0].GetAttribute("data-dock-panel-id")).IsEqualTo("workspace.first");
        await Assert.That(cut.Find("#dock-panel-body-workspace-first").GetAttribute("role")).IsEqualTo("tabpanel");
        await Assert.That(cut.Find("#dock-panel-body-workspace-first").GetAttribute("aria-labelledby")).IsEqualTo("dock-panel-tab-workspace-first");
        await Assert.That(cut.Markup).Contains("First panel");
        await Assert.That(cut.Markup).DoesNotContain("Second panel");
    }

    [Test]
    public async Task DockTabStrip_ClickingTab_ActivatesPanelAndRendersItsContent()
    {
        var firstPanelId = new DockPanelId("workspace.stack-first");
        var secondPanelId = new DockPanelId("workspace.stack-second");
        _dockLayoutState.Register(CreateDescriptor(firstPanelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("First stack panel"));
        _dockLayoutState.Register(CreateDescriptor(secondPanelId, DockScope.Workspace, DockSide.End, order: 20), CreateContent("Second stack panel"));
        _dockLayoutState.Open(firstPanelId);
        _dockLayoutState.Open(secondPanelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var secondTab = cut.Find("[data-dock-tab-panel-id='workspace.stack-second']");

        await secondTab.ClickAsync(new MouseEventArgs());

        await Assert.That(_dockLayoutState.GetPanel(firstPanelId)?.State.IsActive).IsFalse();
        await Assert.That(_dockLayoutState.GetPanel(secondPanelId)?.State.IsActive).IsTrue();
        await Assert.That(cut.Find("[data-testid='dock-panel-host']").GetAttribute("data-dock-panel-id")).IsEqualTo("workspace.stack-second");
        await Assert.That(cut.Markup).Contains("Second stack panel");
        await Assert.That(cut.Markup).DoesNotContain("First stack panel");
    }

    [Test]
    public async Task DockTabStrip_ArrowKey_ActivatesNextPanel()
    {
        var firstPanelId = new DockPanelId("workspace.stack-key-first");
        var secondPanelId = new DockPanelId("workspace.stack-key-second");
        _dockLayoutState.Register(CreateDescriptor(firstPanelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("First key panel"));
        _dockLayoutState.Register(CreateDescriptor(secondPanelId, DockScope.Workspace, DockSide.End, order: 20), CreateContent("Second key panel"));
        _dockLayoutState.Open(firstPanelId);
        _dockLayoutState.Open(secondPanelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var firstTab = cut.Find("[data-dock-tab-panel-id='workspace.stack-key-first']");

        await firstTab.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowRight" });

        await Assert.That(_dockLayoutState.GetPanel(firstPanelId)?.State.IsActive).IsFalse();
        await Assert.That(_dockLayoutState.GetPanel(secondPanelId)?.State.IsActive).IsTrue();
        await Assert.That(cut.Find("[data-testid='dock-panel-host']").GetAttribute("data-dock-panel-id")).IsEqualTo("workspace.stack-key-second");
    }

    [Test]
    public async Task DockTabStrip_ArrowKey_MovesFocusToActivatedTab()
    {
        var firstPanelId = new DockPanelId("workspace.stack-focus-first");
        var secondPanelId = new DockPanelId("workspace.stack-focus-second");
        _dockLayoutState.Register(CreateDescriptor(firstPanelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("First focus panel"));
        _dockLayoutState.Register(CreateDescriptor(secondPanelId, DockScope.Workspace, DockSide.End, order: 20), CreateContent("Second focus panel"));
        _dockLayoutState.Open(firstPanelId);
        _dockLayoutState.Open(secondPanelId);

        var focusService = _ctx.Services.GetRequiredService<IAccessibilityFocusService>();
        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var firstTab = cut.Find("[data-dock-tab-panel-id='workspace.stack-focus-first']");

        await firstTab.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowRight" });

        await focusService.Received(1).FocusAsync("#dock-panel-tab-workspace-stack-focus-second", preventScroll: true);
        await Assert.That(cut.Find("[data-dock-tab-panel-id='workspace.stack-focus-first']").GetAttribute("tabindex")).IsEqualTo("-1");
        await Assert.That(cut.Find("[data-dock-tab-panel-id='workspace.stack-focus-second']").GetAttribute("tabindex")).IsEqualTo("0");
    }

    [Test]
    public async Task DockOverlayHost_RendersOnlyNonDockedOpenPanels()
    {
        var dockedPanelId = new DockPanelId("workspace.docked");
        var inspectorPanelId = new DockPanelId("workspace.inspector");
        _dockLayoutState.Register(CreateDescriptor(dockedPanelId, DockScope.Workspace, DockSide.End, DockMode.Docked, order: 10), CreateContent("Docked panel"));
        _dockLayoutState.Register(CreateDescriptor(inspectorPanelId, DockScope.Workspace, DockSide.End, DockMode.Inspector, order: 20), CreateContent("Inspector panel"));
        _dockLayoutState.Open(dockedPanelId);
        _dockLayoutState.Open(inspectorPanelId);

        var cut = _ctx.Render<DockOverlayHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace));

        await Assert.That(cut.Markup).Contains("Inspector panel");
        await Assert.That(cut.Markup).DoesNotContain("Docked panel");
        await Assert.That(cut.Find("[data-dock-panel-id='workspace.inspector']").GetAttribute("data-dock-mode")).IsEqualTo("inspector");
    }

    [Test]
    public async Task DockPanelHost_RendersResizeHandleForResizableDockedInlinePanels()
    {
        var panelId = new DockPanelId("workspace.resize");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await Assert.That(handle.GetAttribute("role")).IsEqualTo("separator");
        await Assert.That(handle.GetAttribute("aria-valuemin")).IsEqualTo("240");
        await Assert.That(handle.GetAttribute("aria-valuemax")).IsEqualTo("520");
        await Assert.That(handle.GetAttribute("aria-valuenow")).IsEqualTo("320");
        await Assert.That(handle.GetAttribute("aria-controls")).IsEqualTo("dock-panel-body-workspace-resize");
        await Assert.That(handle.GetAttribute("data-dock-resize-panel-id")).IsEqualTo("workspace.resize");
        await Assert.That(cut.Find("#dock-panel-body-workspace-resize").TextContent).Contains("Resizable panel");
    }

    [Test]
    public async Task DockResizeHandle_KeyboardResize_UpdatesDockPanelWidth()
    {
        var panelId = new DockPanelId("workspace.resize-keyboard");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowLeft" });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(336);
    }

    [Test]
    public async Task DockResizeHandle_StartSideKeyboardResize_UsesOppositeArrowDirection()
    {
        var panelId = new DockPanelId("workspace.resize-start");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.Start, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.Start));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowLeft" });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(304);
    }

    [Test]
    public async Task DockResizeHandle_ShiftKeyboardResize_ClampsToDescriptorBounds()
    {
        var panelId = new DockPanelId("workspace.resize-clamp");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "End" });
        await handle.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "ArrowLeft", ShiftKey = true });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(520);
    }

    [Test]
    public async Task DockResizeHandle_UnsupportedKey_DoesNotRequestResize()
    {
        var panelId = new DockPanelId("workspace.resize-tab");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onkeydown", new KeyboardEventArgs { Key = "Tab" });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(320);
    }

    [Test]
    public async Task DockResizeHandle_PointerDrag_UpdatesEndPanelWidth()
    {
        var panelId = new DockPanelId("workspace.resize-pointer-end");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 400, PointerId = 1, IsPrimary = true });
        await handle.TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 360, PointerId = 1, IsPrimary = true });
        await handle.TriggerEventAsync("onpointerup", new PointerEventArgs { ClientX = 360, PointerId = 1, IsPrimary = true });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(360);
    }

    [Test]
    public async Task DockResizeHandle_PointerDrag_IgnoresNonPrimaryPointers()
    {
        var panelId = new DockPanelId("workspace.resize-pointer-secondary");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 400, PointerId = 1, IsPrimary = false });
        await handle.TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 360, PointerId = 1, IsPrimary = false });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(320);
    }

    [Test]
    public async Task DockResizeHandle_PointerDrag_IgnoresMismatchedPointerId()
    {
        var panelId = new DockPanelId("workspace.resize-pointer-id");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 400, PointerId = 7, IsPrimary = true });
        await handle.TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 360, PointerId = 8, IsPrimary = true });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(320);
    }

    [Test]
    public async Task DockResizeHandle_PointerDrag_InvokesPointerCaptureModule()
    {
        var panelId = new DockPanelId("workspace.resize-pointer-capture");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);
        var module = _ctx.JSInterop.SetupModule("/js/dock-resize.js");
        module.SetupVoid("setPointerCapture", _ => true).SetVoidResult();
        module.SetupVoid("releasePointerCapture", _ => true).SetVoidResult();

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 400, PointerId = 17, IsPrimary = true });
        await handle.TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 360, PointerId = 17, IsPrimary = true });
        await handle.TriggerEventAsync("onpointerup", new PointerEventArgs { ClientX = 360, PointerId = 17, IsPrimary = true });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(360);
        await Assert.That(_ctx.JSInterop.Invocations.Count(invocation => invocation.Identifier == "import")).IsEqualTo(1);
        await Assert.That(module.Invocations.Count(invocation => invocation.Identifier == "setPointerCapture")).IsEqualTo(1);
        await Assert.That(module.Invocations.Count(invocation => invocation.Identifier == "releasePointerCapture")).IsEqualTo(1);
    }

    [Test]
    public async Task DockResizeHandle_PointerDrag_UpdatesStartPanelWidthWithOppositeDirection()
    {
        var panelId = new DockPanelId("workspace.resize-pointer-start");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.Start, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.Start));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 400, PointerId = 1, IsPrimary = true });
        await handle.TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 360, PointerId = 1, IsPrimary = true });
        await handle.TriggerEventAsync("onpointerup", new PointerEventArgs { ClientX = 360, PointerId = 1, IsPrimary = true });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(280);
    }

    [Test]
    public async Task DockResizeHandle_PointerMoveBeforePointerDown_DoesNotResize()
    {
        var panelId = new DockPanelId("workspace.resize-pointer-idle");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 360, PointerId = 1, IsPrimary = true });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(320);
    }

    [Test]
    public async Task DockResizeHandle_PointerCancel_StopsDragResize()
    {
        var panelId = new DockPanelId("workspace.resize-pointer-cancel");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10), CreateContent("Resizable panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));
        var handle = cut.Find("[data-testid='dock-resize-handle']");

        await handle.TriggerEventAsync("onpointerdown", new PointerEventArgs { ClientX = 400, PointerId = 1, IsPrimary = true });
        await handle.TriggerEventAsync("onpointercancel", new PointerEventArgs { ClientX = 400, PointerId = 1, IsPrimary = true });
        await handle.TriggerEventAsync("onpointermove", new PointerEventArgs { ClientX = 360, PointerId = 1, IsPrimary = true });

        await Assert.That(_dockLayoutState.GetPanel(panelId)?.State.Width).IsEqualTo(320);
    }

    [Test]
    public async Task DockPanelHost_DoesNotRenderResizeHandleForNonResizablePanels()
    {
        var panelId = new DockPanelId("workspace.fixed");
        _dockLayoutState.Register(CreateDescriptor(panelId, DockScope.Workspace, DockSide.End, order: 10, isResizable: false), CreateContent("Fixed panel"));
        _dockLayoutState.Open(panelId);

        var cut = _ctx.Render<DockSideHost>(parameters => parameters
            .Add(component => component.Scope, DockScope.Workspace)
            .Add(component => component.Side, DockSide.End));

        await Assert.That(cut.FindAll("[data-testid='dock-resize-handle']").Count).IsEqualTo(0);
    }

    private static DockPanelDescriptor CreateDescriptor(
        DockPanelId id,
        DockScope scope,
        DockSide side,
        DockMode mode = DockMode.Docked,
        int order = 10,
        bool isResizable = true)
    {
        return new DockPanelDescriptor(
            id,
            scope,
            side,
            mode,
            $"Panel {id.Value}",
            $"Panel {id.Value}",
            DefaultWidth: 320,
            MinWidth: 240,
            MaxWidth: 520,
            order,
            IsResizable: isResizable,
            CanClose: true,
            PersistState: true);
    }

    private static RenderFragment CreateContent(string text)
    {
        return builder => builder.AddContent(0, text);
    }
}
