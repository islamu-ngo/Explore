// ABOUTME: Tenant-admin BFF service for provider-neutral tenant storage settings.
// ABOUTME: Preserves HAL edit affordance semantics using generated API models directly.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface ITenantStorageSettingsAdminService
{
    Task<HalResourceOfTenantStorageSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> SaveAsync(HalResourceOfTenantStorageSettingsDto settings, CancellationToken cancellationToken = default);
}

public sealed class TenantStorageSettingsAdminService(
    IEventApiClient api,
    ILogger<TenantStorageSettingsAdminService> logger) : ITenantStorageSettingsAdminService
{
    public async Task<HalResourceOfTenantStorageSettingsDto> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await api.GetTenantStorageSettingsAsync(cancellationToken: cancellationToken);
            return response.InitializeForEditing();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load tenant storage settings.");
            return new HalResourceOfTenantStorageSettingsDto().InitializeForEditing();
        }
    }

    public async Task<BaseCommandResponseOfGuid> SaveAsync(
        HalResourceOfTenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        if (!settings.IsEditable())
        {
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "The API did not expose a tenant storage edit affordance."
            };
        }

        try
        {
            return await api.PatchTenantStorageSettingsAsync(
                settings.ToPatchRequest(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save tenant storage settings.");
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Tenant storage settings save failed.",
                Errors = [ex.Message]
            };
        }
    }
}
