// ABOUTME: Playwright critical-flow scaffold for browser-visible tenant isolation.
// ABOUTME: Documents the tenant A event visibility contract from a tenant B browser context.

using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[ClassDataSource<AppHostFixture, PlaywrightFixture>(Shared = [SharedType.PerTestSession, SharedType.PerTestSession])]
[NotInParallel("E2EAppHostDb")]
[ParallelLimiter<BrowserParallelLimit>]
public class TenantIsolationFlowTests(
    AppHostFixture appHost,
    PlaywrightFixture playwright)
{
    [Test]
    [Skip("Category: E2E. Removal: enable in the nightly lane when Docker, Aspire AppHost PostgreSQL override wiring, and deterministic tenant host/header routing are available.")]
    public async Task TenantIsolationTenantAEventIsHiddenFromTenantBContext()
    {
        await appHost.ResetDatabaseAsync();
        var scenario = await SeedTenantIsolationScenarioAsync(appHost);

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
            await playwright.ClosePageAsync(tenantAPage, $"{nameof(TenantIsolationTenantAEventIsHiddenFromTenantBContext)}-tenant-a");
            await playwright.ClosePageAsync(tenantBPage, $"{nameof(TenantIsolationTenantAEventIsHiddenFromTenantBContext)}-tenant-b");
        }
    }

    private async Task<IPage> CreateTenantPageAsync(string tenantSlug)
    {
        var page = await playwright.CreatePageAsync($"{nameof(TenantIsolationTenantAEventIsHiddenFromTenantBContext)}-{tenantSlug}");
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
        AppHostFixture appHost)
    {
        await using var context = appHost.CreateDbContext();
        return await TenantIsolationScenarioSeed.SeedAsync(context);
    }
}
