// ABOUTME: Playwright visual baseline scaffolding for shell and workspace sidebar combinations.
// ABOUTME: Keeps dock layout migration scenarios explicit until Aspire-backed screenshots are enabled.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows;

[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[ParallelLimiter<BrowserParallelLimit>]
public class SidebarLayoutVisualTests(AppHostFixture appHost, PlaywrightFixture playwright)
{
    [Test]
    [Skip("Infrastructure-gated visual baseline: requires Aspire, seeded event data, and approved screenshot storage.")]
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
            await page.CloseAsync();
        }
    }

    [Test]
    [Skip("Infrastructure-gated visual baseline: requires Aspire, seeded event data, and approved screenshot storage.")]
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
            await page.CloseAsync();
        }
    }

    [Test]
    [Skip("Infrastructure-gated visual baseline: requires Aspire, seeded event data, and approved screenshot storage.")]
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
            await page.CloseAsync();
        }
    }

    [Test]
    [Skip("Infrastructure-gated visual baseline: requires Aspire, seeded event data, and approved screenshot storage.")]
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
            await page.CloseAsync();
        }
    }

    [Test]
    [Skip("Infrastructure-gated visual baseline: requires Aspire, seeded event data, and approved screenshot storage.")]
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
            await page.CloseAsync();
        }
    }

    [Test]
    [Skip("Infrastructure-gated visual baseline: requires Aspire, seeded event data, and approved screenshot storage.")]
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
            await page.CloseAsync();
        }
    }

    [Test]
    [Skip("Infrastructure-gated visual baseline: requires Aspire, seeded event data, and approved screenshot storage.")]
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
            await page.CloseAsync();
        }
    }

    private async Task<IPage> CreateDesktopPageAsync()
    {
        var page = await playwright.CreatePageAsync();
        await page.SetViewportSizeAsync(1440, 1000);
        return page;
    }

    private async Task<IPage> CreateMobilePageAsync()
    {
        var page = await playwright.CreatePageAsync();
        await page.SetViewportSizeAsync(390, 844);
        return page;
    }

    private async Task NavigateToEventsAsync(IPage page)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/events");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo(200);
    }
}
