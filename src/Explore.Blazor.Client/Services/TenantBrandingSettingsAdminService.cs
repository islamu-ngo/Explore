// ABOUTME: Tenant-admin BFF service for typed tenant branding settings document reads and replacements.
// ABOUTME: Uses HAL affordances from the API; never falls back to scalar tenant settings rows.

using System.Net;
using Explore.Blazor.Client.Clients;

namespace Explore.Blazor.Client.Services;

public interface ITenantBrandingSettingsAdminService
{
    Task<TenantBrandingSettingsAdminModel> GetAsync(CancellationToken cancellationToken = default);
    Task<TenantBrandingSettingsSaveResult> SaveAsync(TenantBrandingSettingsAdminModel model, CancellationToken cancellationToken = default);
}

public sealed class TenantBrandingSettingsAdminService(
    IEventApiClient api,
    ILogger<TenantBrandingSettingsAdminService> logger) : ITenantBrandingSettingsAdminService
{
    private const string ReplaceLinkRel = "self/replace-settings";

    public async Task<TenantBrandingSettingsAdminModel> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var document = await api.GetTenantBrandingSettingsDocumentAsync(cancellationToken: cancellationToken);
            return TenantBrandingSettingsAdminModel.FromDocument(
                document,
                document._links?.ContainsKey(ReplaceLinkRel) == true);
        }
        catch (ApiException ex) when (ex.StatusCode == (int)HttpStatusCode.NotFound)
        {
            return TenantBrandingSettingsAdminModel.Missing();
        }
        catch (Exception ex) when (IsNotFoundException(ex))
        {
            return TenantBrandingSettingsAdminModel.Missing();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load tenant branding settings document.");
            return TenantBrandingSettingsAdminModel.Failed("Unable to load tenant branding settings.");
        }
    }

    public async Task<TenantBrandingSettingsSaveResult> SaveAsync(
        TenantBrandingSettingsAdminModel model,
        CancellationToken cancellationToken = default)
    {
        if (!model.Exists)
        {
            return TenantBrandingSettingsSaveResult.Failed("Tenant branding settings have not been initialized.");
        }

        if (!model.CanReplace)
        {
            return TenantBrandingSettingsSaveResult.Failed("You do not have permission to replace tenant branding settings.");
        }

        ReplaceTenantBrandingSettingsDocumentDto request = new()
        {
            ExpectedConcurrencyStamp = model.ConcurrencyStamp,
            Payload = new TenantBrandingSettingsPayloadDto
            {
                DisplayName = Normalize(model.DisplayName),
                LogoUrl = Normalize(model.LogoUrl),
                FaviconUrl = Normalize(model.FaviconUrl),
                CustomCssUrl = Normalize(model.CustomCssUrl)
            }
        };

        try
        {
            var document = await api.ReplaceTenantBrandingSettingsDocumentAsync(
                request,
                cancellationToken: cancellationToken);

            TenantBrandingSettingsAdminModel updated = TenantBrandingSettingsAdminModel.FromDocument(
                document,
                document._links?.ContainsKey(ReplaceLinkRel) == true);

            return TenantBrandingSettingsSaveResult.Successful(updated);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to replace tenant branding settings document.");
            return TenantBrandingSettingsSaveResult.Failed("Failed to save tenant branding settings.");
        }
    }


    private static bool IsNotFoundException(Exception exception)
    {
        return exception is ApiException apiException
                && apiException.StatusCode == (int)HttpStatusCode.NotFound
            || exception.InnerException is not null && IsNotFoundException(exception.InnerException)
            || exception.Message.Contains("404", StringComparison.Ordinal);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

}

public sealed class TenantBrandingSettingsAdminModel
{
    public bool Exists { get; set; }
    public bool CanReplace { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public string? DisplayName { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CustomCssUrl { get; set; }
    public string? ErrorMessage { get; set; }

    public static TenantBrandingSettingsAdminModel Missing() => new()
    {
        Exists = false,
        ErrorMessage = "Tenant branding settings have not been initialized."
    };

    public static TenantBrandingSettingsAdminModel Failed(string message) => new()
    {
        Exists = false,
        ErrorMessage = message
    };

    public static TenantBrandingSettingsAdminModel FromDocument(
        HalResourceOfTenantBrandingSettingsDocumentDto document,
        bool canReplace) => new()
        {
            Exists = true,
            CanReplace = canReplace,
            ConcurrencyStamp = document.ConcurrencyStamp,
            DisplayName = document.Payload?.DisplayName,
            LogoUrl = document.Payload?.LogoUrl,
            FaviconUrl = document.Payload?.FaviconUrl,
            CustomCssUrl = document.Payload?.CustomCssUrl
        };
}

public sealed class TenantBrandingSettingsSaveResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public TenantBrandingSettingsAdminModel? Model { get; init; }

    public static TenantBrandingSettingsSaveResult Successful(TenantBrandingSettingsAdminModel model) => new()
    {
        Success = true,
        Message = "Tenant branding settings saved.",
        Model = model
    };

    public static TenantBrandingSettingsSaveResult Failed(string message) => new()
    {
        Success = false,
        Message = message
    };
}
