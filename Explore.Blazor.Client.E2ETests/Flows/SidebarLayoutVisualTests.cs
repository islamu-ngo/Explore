// ABOUTME: Playwright visual baseline scaffolding for shell and workspace sidebar combinations.
// ABOUTME: Keeps dock layout migration scenarios explicit until Aspire-backed screenshots are enabled.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows;

[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[ParallelLimiter<BrowserParallelLimit>]
public class SidebarLayoutVisualTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    private static readonly IReadOnlyList<DockVisualScenario> DockResponsiveMatrix =
    [
        new("mobile-390-ltr", Width: 390, Height: 844, Direction: "ltr", ReducedMotion: false),
        new("mobile-390-rtl", Width: 390, Height: 844, Direction: "rtl", ReducedMotion: false),
        new("mobile-390-reduced-motion", Width: 390, Height: 844, Direction: "ltr", ReducedMotion: true),
        new("compact-600-ltr", Width: 600, Height: 900, Direction: "ltr", ReducedMotion: false),
        new("tablet-768-ltr", Width: 768, Height: 900, Direction: "ltr", ReducedMotion: false),
        new("constrained-970-ltr", Width: 970, Height: 900, Direction: "ltr", ReducedMotion: false),
        new("desktop-1280-ltr", Width: 1280, Height: 900, Direction: "ltr", ReducedMotion: false),
        new("wide-1760-ltr", Width: 1760, Height: 1000, Direction: "ltr", ReducedMotion: false)
    ];

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task Desktop_LeftNavOpen_AiClosed_CapturesBaseline()
    {
        var page = await CreateDesktopPageAsync();
        try
        {
            await NavigateToEventsAsync(page);

            await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='shell.left-nav']").CountAsync()).IsGreaterThanOrEqualTo(1);
            await Assert.That(await page.Locator("[data-testid='shell-ai-rail'].ai-rail--open").CountAsync()).IsEqualTo(0);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(Desktop_LeftNavOpen_AiClosed_CapturesBaseline));
        }
    }

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task Desktop_LeftNavOpen_AiOpen_CapturesBaseline()
    {
        var page = await CreateDesktopPageAsync();
        try
        {
            await NavigateToEventsAsync(page);
            await page.Locator("[data-testid='shell-ai-toggle']").ClickAsync();

            await Assert.That(await page.Locator("[data-testid='shell-ai-rail'].ai-rail--open").CountAsync()).IsEqualTo(1);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(Desktop_LeftNavOpen_AiOpen_CapturesBaseline));
        }
    }

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task Desktop_CustomizePanelOpen_AiOpen_CapturesBaseline()
    {
        var page = await CreateDesktopPageAsync();
        try
        {
            await NavigateToEventsAsync(page);
            await page.Locator("[data-testid='shell-ai-toggle']").ClickAsync();
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Customize view" }).ClickAsync();

            await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='events.customize-view']").CountAsync()).IsEqualTo(1);
            await Assert.That(await page.Locator("[data-testid='shell-ai-rail'].ai-rail--open").CountAsync()).IsEqualTo(1);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(Desktop_CustomizePanelOpen_AiOpen_CapturesBaseline));
        }
    }

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task Desktop_EventDetailPreviewOpen_CapturesBaseline()
    {
        var page = await CreateDesktopPageAsync();
        try
        {
            await NavigateToEventsAsync(page);
            await page.Locator(".event-card").First.ClickAsync();

            await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='events.event-preview']").CountAsync()).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(Desktop_EventDetailPreviewOpen_CapturesBaseline));
        }
    }

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task Mobile_LeftNavOpen_CapturesBaseline()
    {
        var page = await CreateMobilePageAsync();
        try
        {
            await NavigateToEventsAsync(page);
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Toggle sidebar navigation" }).ClickAsync();

            await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='shell.left-nav']").CountAsync()).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(Mobile_LeftNavOpen_CapturesBaseline));
        }
    }

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task Mobile_CustomizePanelOpen_CapturesBaseline()
    {
        var page = await CreateMobilePageAsync();
        try
        {
            await NavigateToEventsAsync(page);
            await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Customize view" }).ClickAsync();

            await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='events.customize-view']").CountAsync()).IsEqualTo(1);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(Mobile_CustomizePanelOpen_CapturesBaseline));
        }
    }

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task Mobile_EventDetailPreviewOpen_CapturesBaseline()
    {
        var page = await CreateMobilePageAsync();
        try
        {
            await NavigateToEventsAsync(page);
            await page.Locator(".event-card").First.ClickAsync();

            await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='events.event-preview']").CountAsync()).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            await playwright.ClosePageAsync(page, nameof(Mobile_EventDetailPreviewOpen_CapturesBaseline));
        }
    }

    [Test]
    [Skip("Category: Manual visual. Removal: enable when Aspire, seeded event data, and approved screenshot storage are available for the visual baseline lane.")]
    public async Task DockResponsiveMatrix_CapturesBreakpointsDirectionsAndReducedMotionContract()
    {
        foreach (var scenario in DockResponsiveMatrix)
        {
            var page = await CreateScenarioPageAsync(scenario);
            try
            {
                await NavigateToEventsAsync(page);
                await ApplyScenarioEnvironmentAsync(page, scenario);

                await page.Locator("[data-testid='shell-ai-toggle']").ClickAsync();
                await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions { Name = "Customize view" }).ClickAsync();
                await page.Locator(".event-card").First.ClickAsync();

                await AssertDockMatrixScenarioAsync(page, scenario);
            }
            finally
            {
                await playwright.ClosePageAsync(page, $"dock-responsive-matrix-{scenario.Name}");
            }
        }
    }

    private async Task<IPage> CreateDesktopPageAsync()
    {
        var page = await playwright.CreatePageAsync(nameof(CreateDesktopPageAsync));
        await page.SetViewportSizeAsync(1440, 1000);
        return page;
    }

    private async Task<IPage> CreateMobilePageAsync()
    {
        var page = await playwright.CreatePageAsync(nameof(CreateMobilePageAsync));
        await page.SetViewportSizeAsync(390, 844);
        return page;
    }

    private async Task<IPage> CreateScenarioPageAsync(DockVisualScenario scenario)
    {
        var page = await playwright.CreatePageAsync($"dock-responsive-matrix-{scenario.Name}");
        await page.SetViewportSizeAsync(scenario.Width, scenario.Height);
        return page;
    }

    private async Task NavigateToEventsAsync(IPage page)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/events");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo(200);
    }

    private static async Task ApplyScenarioEnvironmentAsync(IPage page, DockVisualScenario scenario)
    {
        if (scenario.Direction == "rtl")
        {
            await page.EvaluateAsync("() => document.documentElement.setAttribute('dir', 'rtl')");
        }

        if (scenario.ReducedMotion)
        {
            await page.EvaluateAsync(
                """
                () => {
                    const style = document.createElement('style');
                    style.setAttribute('data-testid', 'dock-visual-reduced-motion');
                    style.textContent = `
                        *, *::before, *::after {
                            animation-duration: 1ms !important;
                            animation-iteration-count: 1 !important;
                            scroll-behavior: auto !important;
                            transition-duration: 1ms !important;
                        }`;
                    document.head.appendChild(style);
                }
                """);
        }
    }

    private static async Task AssertDockMatrixScenarioAsync(IPage page, DockVisualScenario scenario)
    {
        await Assert.That(await page.Locator("[data-testid='dock-layout-host'][data-dock-scope='Shell']").CountAsync()).IsGreaterThanOrEqualTo(1);
        await Assert.That(await page.Locator("[data-testid='dock-layout-host'][data-dock-scope='Workspace']").CountAsync()).IsGreaterThanOrEqualTo(1);
        await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='events.customize-view']").CountAsync()).IsGreaterThanOrEqualTo(1);
        await Assert.That(await page.Locator("[data-testid='dock-panel-host'][data-dock-panel-id='events.event-preview']").CountAsync()).IsGreaterThanOrEqualTo(1);

        if (scenario.Direction == "rtl")
        {
            await Assert.That(await page.EvaluateAsync<string>("() => document.documentElement.getAttribute('dir') ?? ''")).IsEqualTo("rtl");
        }

        if (scenario.ReducedMotion)
        {
            await Assert.That(await page.Locator("style[data-testid='dock-visual-reduced-motion']").CountAsync()).IsEqualTo(1);
        }
    }

    private sealed record DockVisualScenario(
        string Name,
        int Width,
        int Height,
        string Direction,
        bool ReducedMotion);
}
