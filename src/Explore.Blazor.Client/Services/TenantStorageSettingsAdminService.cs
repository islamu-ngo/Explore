// ABOUTME: Tenant-admin BFF service for provider-neutral tenant storage settings.
// ABOUTME: Preserves HAL edit affordance semantics using generated API models directly.

using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface ITenantStorageSettingsAdminService
{
    Task<HalResourceOfTenantStorageSettingsDto> GetAsync(CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> PatchPolicyAsync(HalResourceOfTenantStorageSettingsDto settings, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> PatchS3Async(HalResourceOfTenantStorageSettingsDto settings, CancellationToken cancellationToken = default);
    Task<BaseCommandResponseOfGuid> PatchS3CredentialsAsync(HalResourceOfTenantStorageSettingsDto settings, CancellationToken cancellationToken = default);
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

    public Task<BaseCommandResponseOfGuid> PatchPolicyAsync(
        HalResourceOfTenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default) =>
        PatchAsync(settings, settings.ToPolicyPatchRequest(), "policy", cancellationToken);

    public Task<BaseCommandResponseOfGuid> PatchS3Async(
        HalResourceOfTenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default) =>
        PatchAsync(settings, settings.ToS3PatchRequest(), "S3", cancellationToken);

    public Task<BaseCommandResponseOfGuid> PatchS3CredentialsAsync(
        HalResourceOfTenantStorageSettingsDto settings,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(settings.S3AccessKeyId)
            || string.IsNullOrWhiteSpace(settings.S3SecretAccessKey))
        {
            return Task.FromResult(new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Both S3 credential values are required."
            });
        }

        return PatchAsync(settings, settings.ToS3CredentialsPatchRequest(), "S3 credentials", cancellationToken);
    }

    private async Task<BaseCommandResponseOfGuid> PatchAsync(
        HalResourceOfTenantStorageSettingsDto settings,
        PatchTenantStorageSettingsDto request,
        string group,
        CancellationToken cancellationToken)
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
                request,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to patch tenant storage {Group} settings.", group);
            return new BaseCommandResponseOfGuid
            {
                Success = false,
                Message = "Tenant storage settings save failed.",
                Errors = [ex.Message]
            };
        }
    }
}
