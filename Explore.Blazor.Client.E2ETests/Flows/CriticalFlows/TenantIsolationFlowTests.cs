// ABOUTME: Playwright critical flow for API tenant isolation in the full Aspire stack.
// ABOUTME: Documents that tenant A event data is hidden from tenant B API context.

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
    public async Task TenantIsolationTenantAEventIsHiddenFromTenantBContext()
    {
        await appHost.ResetDatabaseAsync();
        var scenario = await SeedTenantIsolationScenarioAsync(appHost);

        var tenantAPage = await CreateTenantPageAsync(scenario.TenantASlug);
        var tenantBPage = await CreateTenantPageAsync(scenario.TenantBSlug);

        try
        {
            var tenantAEvents = await GetTenantEventsPayloadAsync(tenantAPage, scenario.TenantASlug);
            await Assert.That(tenantAEvents).Contains(scenario.TenantAEventTitle);

            var tenantBEvents = await GetTenantEventsPayloadAsync(tenantBPage, scenario.TenantBSlug);
            await Assert.That(tenantBEvents).DoesNotContain(scenario.TenantAEventTitle);
        }
        finally
        {
            await playwright.ClosePageAsync(tenantAPage, $"{nameof(TenantIsolationTenantAEventIsHiddenFromTenantBContext)}-tenant-a");
            await playwright.ClosePageAsync(tenantBPage, $"{nameof(TenantIsolationTenantAEventIsHiddenFromTenantBContext)}-tenant-b");
        }
    }

    private async Task<IPage> CreateTenantPageAsync(string tenantSlug)
    {
        return await playwright.CreatePageAsync($"{nameof(TenantIsolationTenantAEventIsHiddenFromTenantBContext)}-{tenantSlug}");
    }

    private async Task<string> GetTenantEventsPayloadAsync(IPage page, string tenantSlug)
    {
        var response = await page.Context.APIRequest.GetAsync(
            $"{appHost.ApiBaseUrl}/api/Event",
            new APIRequestContextOptions
            {
                Headers = new Dictionary<string, string>
                {
                    ["X-Tenant-Slug"] = tenantSlug
                }
            });
        await Assert.That(response).IsNotNull();
        await Assert.That(response.Status).IsEqualTo((int)HttpStatusCode.OK);

        return await response.TextAsync();
    }

    private static async Task<TenantIsolationScenarioSeed.Result> SeedTenantIsolationScenarioAsync(
        AppHostFixture appHost)
    {
        await using var context = appHost.CreateDbContext();
        return await TenantIsolationScenarioSeed.SeedAsync(context);
    }
}
