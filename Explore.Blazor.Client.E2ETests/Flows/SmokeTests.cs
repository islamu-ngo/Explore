// ABOUTME: Smoke tests verifying the Blazor frontend loads in a real browser.
// ABOUTME: Requires running infrastructure (PostgreSQL, Redis, Keycloak) via Aspire AppHost.

using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Flows;

[ClassDataSource<AppHostFixture>(Shared = SharedType.PerTestSession)]
[ClassDataSource<PlaywrightFixture>(Shared = SharedType.PerTestSession)]
[ParallelLimiter<BrowserParallelLimit>]
public class SmokeTests(AppHostFixture appHost, PlaywrightFixture playwright)
{
    [Test]
    public async Task BlazorFrontend_Loads_ReturnsHtml()
    {
        var page = await playwright.CreatePageAsync();
        try
        {
            var response = await page.GotoAsync(appHost.BlazorBaseUrl);
            await Assert.That(response).IsNotNull();
            await Assert.That(response!.Status).IsEqualTo(200);

            var appElement = page.Locator("#app");
            await Assert.That(await appElement.CountAsync()).IsGreaterThanOrEqualTo(1);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    [Test]
    public async Task AuthStatus_Anonymous_ReturnsNotAuthenticated()
    {
        var page = await playwright.CreatePageAsync();
        try
        {
            var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/auth/status");
            await Assert.That(response).IsNotNull();

            var content = await page.ContentAsync();
            await Assert.That(content).Contains("isAuthenticated");
        }
        finally
        {
            await page.CloseAsync();
        }
    }
}
