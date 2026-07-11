// ABOUTME: API-client critical flow for tenant isolation in the full Aspire stack.
// ABOUTME: Verifies that tenant A event data is hidden from tenant B through generated contracts.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.E2ETests.Fixtures;
using Explore.Blazor.Client.E2ETests.Seeds;

namespace Explore.Blazor.Client.E2ETests.Flows.CriticalFlows;

[Category(E2ETestCategories.E2E)]
[ClassDataSource<AppHostFixture>(Shared = SharedType.PerClass)]
[NotInParallel("E2EAppHostDb")]
public class TenantIsolationFlowTests(AppHostFixture appHost)
{
    [Test]
    public async Task TenantIsolationTenantAEventIsHiddenFromTenantBContext()
    {
        var scenario = await SeedTenantIsolationScenarioAsync(appHost);

        var adminTokens = await appHost.GetTestAdminTokensAsync();
        var tenantAEvents = await GetTenantEventsAsync(adminTokens.AccessToken, scenario.TenantASlug);
        await Assert.That(tenantAEvents.Any(candidate => candidate.Title == scenario.TenantAEventTitle)).IsTrue();

        var tenantBEvents = await GetTenantEventsAsync(adminTokens.AccessToken, scenario.TenantBSlug);
        await Assert.That(tenantBEvents.Any(candidate => candidate.Title == scenario.TenantAEventTitle)).IsFalse();
    }

    private async Task<IReadOnlyCollection<HalResourceOfEventListDto>> GetTenantEventsAsync(
        string accessToken,
        string tenantSlug)
    {
        var response = await appHost.CreateApiClient(accessToken, tenantSlug).GetEventsAsync(pageSize: 100);
        return response._embedded?.Items?.ToArray() ?? [];
    }

    private static async Task<TenantIsolationScenarioSeed.Result> SeedTenantIsolationScenarioAsync(
        AppHostFixture appHost)
    {
        var adminTokens = await appHost.GetTestAdminTokensAsync();
        var instanceApi = appHost.CreateApiClient(adminTokens.AccessToken);
        return await TenantIsolationScenarioSeed.SeedAsync(
            instanceApi,
            tenantSlug => appHost.CreateApiClient(adminTokens.AccessToken, tenantSlug));
    }
}
