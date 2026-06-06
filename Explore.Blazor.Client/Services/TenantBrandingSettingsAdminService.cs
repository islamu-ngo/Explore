// ABOUTME: Tenant-admin BFF service for typed tenant branding settings document reads and replacements.
// ABOUTME: Uses HAL affordances from the API; never falls back to scalar tenant settings rows.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

using Refit;

namespace Explore.Blazor.Client.Services;

public interface ITenantBrandingSettingsApi
{
    [Get("/api/tenant/settings/documents/branding")]
    Task<IApiResponse<TenantBrandingSettingsDocumentResponse>> GetAsync(CancellationToken cancellationToken);

    [Put("/api/tenant/settings/documents/branding")]
    Task<IApiResponse<TenantBrandingSettingsDocumentResponse>> ReplaceAsync([Body] TenantBrandingSettingsReplaceRequest request, CancellationToken cancellationToken);
}

public interface ITenantBrandingSettingsAdminService
{
    Task<TenantBrandingSettingsAdminModel> GetAsync(CancellationToken cancellationToken = default);
    Task<TenantBrandingSettingsSaveResult> SaveAsync(TenantBrandingSettingsAdminModel model, CancellationToken cancellationToken = default);
}

public sealed class TenantBrandingSettingsAdminService(
    ITenantBrandingSettingsApi api,
    ILogger<TenantBrandingSettingsAdminService> logger) : ITenantBrandingSettingsAdminService
{
    private const string ReplaceLinkRel = "self/replace-settings";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<TenantBrandingSettingsAdminModel> GetAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await api.GetAsync(cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return TenantBrandingSettingsAdminModel.Missing();
            }

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Failed to load tenant branding settings document. StatusCode={StatusCode}",
                    response.StatusCode);
                return TenantBrandingSettingsAdminModel.Failed("Unable to load tenant branding settings.");
            }

            var document = response.Content;

            return document is null
                ? TenantBrandingSettingsAdminModel.Failed("Tenant branding settings response was empty.")
                : TenantBrandingSettingsAdminModel.FromDocument(document, HasHalLink(document.AdditionalProperties, ReplaceLinkRel));
        }
        catch (ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
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

        TenantBrandingSettingsReplaceRequest request = new()
        {
            ExpectedConcurrencyStamp = model.ConcurrencyStamp,
            Payload = new TenantBrandingSettingsPayloadModel
            {
                DisplayName = Normalize(model.DisplayName),
                LogoUrl = Normalize(model.LogoUrl),
                FaviconUrl = Normalize(model.FaviconUrl),
                CustomCssUrl = Normalize(model.CustomCssUrl)
            }
        };

        try
        {
            var response = await api.ReplaceAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                string message = ReadFailureMessage(response);
                return TenantBrandingSettingsSaveResult.Failed(message);
            }

            var document = response.Content;

            if (document is null)
            {
                return TenantBrandingSettingsSaveResult.Failed("Tenant branding settings response was empty after save.");
            }

            TenantBrandingSettingsAdminModel updated = TenantBrandingSettingsAdminModel.FromDocument(
                document,
                HasHalLink(document.AdditionalProperties, ReplaceLinkRel));

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
        return exception is ApiException { StatusCode: HttpStatusCode.NotFound }
            || exception.InnerException is not null && IsNotFoundException(exception.InnerException)
            || exception.Message.Contains("404", StringComparison.Ordinal);
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string ReadFailureMessage(IApiResponse response)
    {
        TenantBrandingCommandResponse? command = null;
        if (response.Error?.Content != null)
        {
            try
            {
                command = JsonSerializer.Deserialize<TenantBrandingCommandResponse>(response.Error.Content, JsonOptions);
            }
            catch { /* Ignored */ }
        }

        if (!string.IsNullOrWhiteSpace(command?.Message))
        {
            return command.Message;
        }

        if (command?.Errors is { Count: > 0 })
        {
            return string.Join(" ", command.Errors.Where(error => !string.IsNullOrWhiteSpace(error)));
        }

        return $"Tenant branding settings save failed with status {(int)response.StatusCode}.";
    }

    private static bool HasHalLink(IDictionary<string, JsonElement>? additionalProperties, string rel)
    {
        if (additionalProperties is null || !additionalProperties.TryGetValue("_links", out JsonElement links))
        {
            return false;
        }

        return links.ValueKind == JsonValueKind.Object && links.TryGetProperty(rel, out _);
    }
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
        TenantBrandingSettingsDocumentResponse document,
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

public sealed class TenantBrandingSettingsDocumentResponse
{
    public string DocumentKey { get; set; } = string.Empty;
    public int SchemaVersion { get; set; }
    public string DefaultsVersion { get; set; } = string.Empty;
    public TenantBrandingSettingsPayloadModel? Payload { get; set; }
    public string Source { get; set; } = string.Empty;
    public Guid SourceScopeId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public Guid ConcurrencyStamp { get; set; }

    [JsonExtensionData]
    public IDictionary<string, JsonElement>? AdditionalProperties { get; set; }
}

public sealed class TenantBrandingSettingsPayloadModel
{
    public string? DisplayName { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? CustomCssUrl { get; set; }
}

public sealed class TenantBrandingSettingsReplaceRequest
{
    public required Guid ExpectedConcurrencyStamp { get; init; }
    public required TenantBrandingSettingsPayloadModel Payload { get; init; }
}

public sealed class TenantBrandingCommandResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = [];
}
