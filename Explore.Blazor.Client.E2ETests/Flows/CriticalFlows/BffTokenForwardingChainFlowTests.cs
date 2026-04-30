// ABOUTME: Playwright critical-flow scaffold for the Blazor BFF token-forwarding chain.
// ABOUTME: Documents cookie-authenticated browser to BFF, YARP, API tenant context, and HAL UI rendering.

using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[ClassDataSource<AppHostFixture, PlaywrightFixture, PostgreSqlContainerFixture>(
    Shared = [SharedType.PerTestSession, SharedType.PerTestSession, SharedType.PerTestSession])]
[ParallelLimiter<BrowserParallelLimit>]
public partial class BffTokenForwardingChainFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright,
    PostgreSqlContainerFixture database)
{
    [Test]
    [Skip("Infrastructure-gated critical flow: requires Docker, Aspire AppHost, Keycloak login seed, and deterministic BFF cookie auth state.")]
    public async Task LoginBffYarpApiTenantHalLinksRenderInBlazor()
    {
        await database.ResetAsync();
        var scenario = await SeedTenantIsolationScenarioAsync(database);

        var page = await playwright.CreatePageAsync();
        try
        {
            await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["X-Tenant-Slug"] = scenario.TenantASlug
            });

            await AssertAnonymousAuthStatusAsync(page);

            // Runtime continuation point once Keycloak login automation is available:
            // drive /login or reuse cookie-only storageState, then assert /auth/status is authenticated.
            // Do not read bearer tokens from browser storage; tokens must remain server-side in the BFF.
            await BrowseEventsThroughBffProxyAsync(page, scenario.TenantAEventTitle);
            await AssertHalDrivenAffordancesRenderAsync(page, scenario.TenantAEventTitle);
        }
        finally
        {
            await page.CloseAsync();
        }
    }

    private async Task AssertAnonymousAuthStatusAsync(IPage page)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/auth/status");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);

        var content = await page.ContentAsync();
        await Assert.That(content).Contains("isAuthenticated");
        await Assert.That(content).Contains("false");
    }

    private async Task BrowseEventsThroughBffProxyAsync(IPage page, string expectedEventTitle)
    {
        var apiResponseTask = page.WaitForResponseAsync(response =>
            response.Url.Contains("/api/event", StringComparison.OrdinalIgnoreCase)
            && response.Status is >= 200 and < 300);

        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/events");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);

        _ = await apiResponseTask;

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Explore Events" })
            .WaitForAsync();
        await page.GetByText(expectedEventTitle, new PageGetByTextOptions { Exact = false })
            .WaitForAsync();
    }

    private static async Task AssertHalDrivenAffordancesRenderAsync(IPage page, string eventTitle)
    {
        await page.GetByText(eventTitle, new PageGetByTextOptions { Exact = false }).ClickAsync();

        await Assert.That(await page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
        {
            Name = "Event Page"
        }).CountAsync()).IsGreaterThanOrEqualTo(1);

        await Assert.That(await page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            NameRegex = ShareEventPattern()
        }).CountAsync()).IsGreaterThanOrEqualTo(1);
    }

    private static async Task<TenantIsolationScenarioSeed.Result> SeedTenantIsolationScenarioAsync(
        PostgreSqlContainerFixture database)
    {
        await using var context = database.CreateDbContext();
        return await TenantIsolationScenarioSeed.SeedAsync(context);
    }

    [GeneratedRegex("Share", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex ShareEventPattern();
}
