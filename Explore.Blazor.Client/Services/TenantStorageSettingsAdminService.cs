// ABOUTME: Tenant-admin BFF service for provider-neutral tenant storage settings.
// ABOUTME: Preserves HAL edit affordance semantics while mapping generated API DTOs into UI models.

using Explore.Blazor.Client.Clients;
using Refit;

namespace Explore.Blazor.Client.Services;

public interface ITenantStorageSettingsApi
{
    [Get("/api/tenant/settings/storage")]
    Task<IApiResponse<HalResourceOfTenantStorageSettingsDto>> GetAsync(CancellationToken cancellationToken);

    [Put("/api/tenant/settings/storage")]
    Task<IApiResponse<InstanceCommandResponseModel>> UpdateAsync([Body] TenantStorageSettingsDto settings, CancellationToken cancellationToken);
}

public interface ITenantStorageSettingsAdminService
{
    Task<TenantStorageSettingsModel> GetAsync(CancellationToken cancellationToken = default);
    Task<InstanceCommandResponseModel> SaveAsync(TenantStorageSettingsModel model, CancellationToken cancellationToken = default);
}

public sealed class TenantStorageSettingsAdminService(
    ITenantStorageSettingsApi api,
    ILogger<TenantStorageSettingsAdminService> logger) : ITenantStorageSettingsAdminService
{
    public async Task<TenantStorageSettingsModel> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await api.GetAsync(cancellationToken);
            if (!response.IsSuccessStatusCode || response.Content is null)
            {
                return TenantStorageSettingsModel.Failed("Unable to load tenant storage settings.");
            }

            return TenantStorageSettingsModel.FromHal(response.Content);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load tenant storage settings.");
            return TenantStorageSettingsModel.Failed("Unable to load tenant storage settings.");
        }
    }

    public async Task<InstanceCommandResponseModel> SaveAsync(
        TenantStorageSettingsModel model,
        CancellationToken cancellationToken = default)
    {
        if (!model.IsEditable)
        {
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "The API did not expose a tenant storage edit affordance."
            };
        }

        try
        {
            var response = await api.UpdateAsync(model.ToDto(), cancellationToken);
            if (response.IsSuccessStatusCode && response.Content is not null)
            {
                return response.Content;
            }

            return new InstanceCommandResponseModel
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Message = response.Error?.Content ?? $"Tenant storage settings save failed with status {(int)response.StatusCode}."
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save tenant storage settings.");
            return new InstanceCommandResponseModel
            {
                Success = false,
                Message = "Tenant storage settings save failed.",
                Errors = [ex.Message]
            };
        }
    }
}
