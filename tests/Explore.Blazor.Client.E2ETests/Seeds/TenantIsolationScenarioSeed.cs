// ABOUTME: API-driven tenant-isolation scenario for Playwright E2E coverage.
// ABOUTME: Creates two tenants and publishes an event only in tenant A through generated contracts.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class TenantIsolationScenarioSeed
{
    public sealed record Result(
        string TenantASlug,
        string TenantBSlug,
        string TenantAEventTitle);

    public static async Task<Result> SeedAsync(
        IEventApiClient instanceApi,
        Func<string, IEventApiClient> tenantApiFactory)
    {
        await WebhookManagementScenarioSeed.EnableMultiTenantRoutingAsync(instanceApi);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var tenantASlug = $"tenant-a-{suffix}";
        var tenantBSlug = $"tenant-b-{suffix}";
        await CreateTenantAsync(instanceApi, "Tenant A", tenantASlug);
        await CreateTenantAsync(instanceApi, "Tenant B", tenantBSlug);

        var tenantAApi = tenantApiFactory(tenantASlug);
        var title = $"Tenant A Private Event {suffix}";
        await EventApiScenario.CreatePublishedEventAsync(
            tenantAApi,
            title,
            $"tenant-a-event-{suffix}");

        return new Result(tenantASlug, tenantBSlug, title);
    }

    private static async Task CreateTenantAsync(IEventApiClient api, string name, string slug)
    {
        var response = await api.CreateTenantAsync(new CreateTenantDto
        {
            FullName = name,
            Slug = slug,
            IsActive = true,
            AssignCurrentUserAsTenantAdmin = true
        });
        EnsureSuccess(response, $"creating tenant {slug}");
    }

    private static void EnsureSuccess(BaseCommandResponseOfGuid response, string operation)
    {
        if (response.Success != true)
        {
            throw new InvalidOperationException($"API failed while {operation}: {response.Message}");
        }
    }
}
