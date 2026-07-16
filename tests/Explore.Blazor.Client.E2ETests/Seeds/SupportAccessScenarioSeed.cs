// ABOUTME: Support-access setup for Playwright E2E coverage.
// ABOUTME: Enables instance governance through the AppHost fixture for the bootstrapped administrator.

using Explore.Blazor.Client.Clients;
using Explore.Blazor.Client.E2ETests.Fixtures;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class SupportAccessScenarioSeed
{
    public sealed record Result(
        Guid TenantId,
        string TenantName,
        string TenantSlug,
        Guid AdminUserId);

    public static async Task<Result> SeedAsync(AppHostFixture appHost, IEventApiClient api)
    {
        var user = await api.GetCurrentUserAsync();
        var tenant = (await api.GetTenantsAsync()).Single(candidate => candidate.IsActive == true);

        await appHost.EnableSupportAccessAsync();

        return new Result(
            Required(tenant.Id, "tenant id"),
            tenant.FullName,
            tenant.Slug,
            Required(user.Id, "administrator user id"));
    }

    private static Guid Required(Guid? value, string name) =>
        value is { } result && result != Guid.Empty
            ? result
            : throw new InvalidOperationException($"The API did not return a valid {name}.");
}
