// ABOUTME: API-driven support-access setup for Playwright E2E coverage.
// ABOUTME: Uses generated contracts to enable tenant governance for the bootstrapped administrator.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.E2ETests.Seeds;

public static class SupportAccessScenarioSeed
{
    public sealed record Result(
        Guid TenantId,
        string TenantName,
        string TenantSlug,
        Guid AdminUserId);

    public static async Task<Result> SeedAsync(IEventApiClient api)
    {
        var user = await api.GetCurrentUserAsync();
        var tenant = (await api.GetTenantsAsync()).Single(candidate => candidate.IsActive == true);

        await UpdateSettingAsync(api, "support_access.enabled", "true");
        await UpdateSettingAsync(api, "support_access.require_ticket_reference", "true");
        await UpdateSettingAsync(api, "support_access.allow_write_mode", "false");
        await UpdateSettingAsync(api, "support_access.max_read_only_minutes", "30");
        await UpdateSettingAsync(api, "support_access.max_write_minutes", "10");
        await UpdateSettingAsync(api, "support_access.one_active_session_per_actor", "true");

        return new Result(
            Required(tenant.Id, "tenant id"),
            tenant.FullName,
            tenant.Slug,
            Required(user.Id, "administrator user id"));
    }

    private static async Task UpdateSettingAsync(IEventApiClient api, string key, string value)
    {
        var response = await api.UpdateTenantSettingAsync(key, new UpdateSettingValueDto { Value = value });
        EnsureSuccess(response, $"updating {key}");
    }

    private static Guid Required(Guid? value, string name) =>
        value is { } result && result != Guid.Empty
            ? result
            : throw new InvalidOperationException($"The API did not return a valid {name}.");

    private static void EnsureSuccess(BaseCommandResponseOfGuid response, string operation)
    {
        if (response.Success != true)
        {
            throw new InvalidOperationException($"API failed while {operation}: {response.Message}");
        }
    }
}
