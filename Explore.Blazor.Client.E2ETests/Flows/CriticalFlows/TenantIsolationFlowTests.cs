// ABOUTME: Playwright critical-flow scaffold for browser-visible tenant isolation.
// ABOUTME: Documents the tenant A event visibility contract from a tenant B browser context.

using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[ClassDataSource<AppHostFixture, PlaywrightFixture, PostgreSqlContainerFixture>(
    Shared = [SharedType.PerTestSession, SharedType.PerTestSession, SharedType.PerTestSession])]
[ParallelLimiter<BrowserParallelLimit>]
public class TenantIsolationFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright,
    PostgreSqlContainerFixture database)
{
    [Test]
    [Skip("Infrastructure-gated critical flow: requires Docker, Aspire AppHost PostgreSQL override wiring, and deterministic tenant host/header routing.")]
    public async Task TenantIsolationTenantAEventIsHiddenFromTenantBContext()
    {
        await database.ResetAsync();
        var scenario = await SeedTenantIsolationScenarioAsync(database);

        var tenantAPage = await CreateTenantPageAsync(scenario.TenantASlug);
        var tenantBPage = await CreateTenantPageAsync(scenario.TenantBSlug);

        try
        {
            await BrowseEventsAsync(tenantAPage);
            await tenantAPage.GetByText(scenario.TenantAEventTitle, new PageGetByTextOptions { Exact = false })
                .WaitForAsync();

            await BrowseEventsAsync(tenantBPage);
            await Assert.That(await tenantBPage.GetByText(scenario.TenantAEventTitle, new PageGetByTextOptions
            {
                Exact = false
            }).CountAsync()).IsEqualTo(0);
        }
        finally
        {
            await tenantAPage.CloseAsync();
            await tenantBPage.CloseAsync();
        }
    }

    private async Task<IPage> CreateTenantPageAsync(string tenantSlug)
    {
        var page = await playwright.CreatePageAsync();
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["X-Tenant-Slug"] = tenantSlug
        });

        return page;
    }

    private async Task BrowseEventsAsync(IPage page)
    {
        var response = await page.GotoAsync($"{appHost.BlazorBaseUrl}/events");
        await Assert.That(response).IsNotNull();
        await Assert.That(response!.Status).IsEqualTo((int)HttpStatusCode.OK);

        await page.GetByRole(AriaRole.Heading, new PageGetByRoleOptions { Name = "Explore Events" })
            .WaitForAsync();
    }

    private static async Task<TenantIsolationScenarioSeed.Result> SeedTenantIsolationScenarioAsync(
        PostgreSqlContainerFixture database)
    {
        await using var context = database.CreateDbContext();
        return await TenantIsolationScenarioSeed.SeedAsync(context);
    }
}
