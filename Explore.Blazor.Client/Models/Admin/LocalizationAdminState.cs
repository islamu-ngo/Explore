// ABOUTME: View-model for the InstanceLocalizationSection — binds to form fields, tracks dirty/loading state.
// ABOUTME: Maps to/from client-side LocalizationGovernancePayload + NSwag-generated LocalizationConfigDto.

using Explore.Blazor.Client.Clients;
using Explore.Domain.Common.Localization;

namespace Explore.Blazor.Client.Models.Admin;

public sealed class LocalizationAdminState
{
    public string DefaultLanguage { get; set; } = "en";
    public string TmsProvider { get; set; } = "none";
    public string? TmsApiUrl { get; set; }
    public string? TmsProjectId { get; set; }
    public string? TmsComponent { get; set; }
    public List<string> EnabledLanguages { get; set; } = new() { "en", "fr", "ar" };
    public string FallbackLanguage { get; set; } = "en";
    public bool ClientPickerEnabled { get; set; } = true;
    public bool ForceOfflineMode { get; set; }

    public bool TmsApiKeyConfigured { get; set; }
    public string? TmsApiKey { get; set; }

    public bool IsLoading { get; set; }
    public bool IsSaving { get; set; }
    public bool IsTesting { get; set; }
    public bool IsSavingKillSwitches { get; set; }
    public string? LastTestResult { get; set; }
    public bool LastTestSucceeded { get; set; }

    public bool BundlePathWritable { get; set; }
    public string? BundlePathReason { get; set; }
    public string? BundlePathTarget { get; set; }
    public bool BundleHealthLoaded { get; set; }
    public Dictionary<string, bool> ExportingLanguages { get; set; } = new();

    public bool IsOffline => string.Equals(TmsProvider, "none", StringComparison.OrdinalIgnoreCase);
    public bool IsTolgee => string.Equals(TmsProvider, "tolgee", StringComparison.OrdinalIgnoreCase);
    public bool IsWeblate => string.Equals(TmsProvider, "weblate", StringComparison.OrdinalIgnoreCase);

    public void LoadFrom(LocalizationConfigDto config)
    {
        DefaultLanguage = CultureRegistry.Contains(config.DefaultLanguage) ? config.DefaultLanguage : "en";
        TmsProvider = (config.TmsProvider ?? "None").Trim().ToLowerInvariant();
        TmsApiUrl = config.TmsApiUrl;
        TmsProjectId = config.TmsProjectId;
        TmsComponent = config.TmsComponent;

        // Governance fields — read from AdditionalProperties until NSwag regeneration adds typed properties.
        var extras = config.AdditionalProperties;

        if (TryGetJsonList(extras, "enabledLanguages") is { Count: > 0 } langs)
        {
            EnabledLanguages = langs;
        }
        if (TryGetJsonString(extras, "fallbackLanguage") is { Length: > 0 } fb && CultureRegistry.Contains(fb))
        {
            FallbackLanguage = fb;
        }
        ClientPickerEnabled = TryGetJsonBool(extras, "clientPickerEnabled") ?? true;
        ForceOfflineMode = TryGetJsonBool(extras, "forceOfflineMode") ?? false;
        TmsApiKeyConfigured = TryGetJsonBool(extras, "tmsApiKeyConfigured") ?? false;
    }

    private static string? TryGetJsonString(IDictionary<string, object>? dict, string key)
    {
        if (dict is null || !dict.TryGetValue(key, out var val)) return null;
        if (val is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.String)
            return je.GetString();
        return val?.ToString();
    }

    private static bool? TryGetJsonBool(IDictionary<string, object>? dict, string key)
    {
        if (dict is null || !dict.TryGetValue(key, out var val)) return null;
        if (val is System.Text.Json.JsonElement je)
        {
            return je.ValueKind switch
            {
                System.Text.Json.JsonValueKind.True => true,
                System.Text.Json.JsonValueKind.False => false,
                _ => null
            };
        }
        return val is bool b ? b : null;
    }

    private static List<string>? TryGetJsonList(IDictionary<string, object>? dict, string key)
    {
        if (dict is null || !dict.TryGetValue(key, out var val)) return null;
        if (val is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            var result = new List<string>();
            foreach (var item in je.EnumerateArray())
            {
                if (item.ValueKind == System.Text.Json.JsonValueKind.String && item.GetString() is { } s)
                    result.Add(s);
            }
            return result;
        }
        return null;
    }

    public LocalizationGovernancePayload ToPayload() => new()
    {
        DefaultLanguage = DefaultLanguage,
        TmsProvider = TmsProvider,
        TmsApiUrl = TmsApiUrl,
        TmsProjectId = TmsProjectId,
        TmsComponent = TmsComponent,
        EnabledLanguages = EnabledLanguages.ToArray(),
        FallbackLanguage = FallbackLanguage,
        ClientPickerEnabled = ClientPickerEnabled,
        ForceOfflineMode = ForceOfflineMode
    };

    public string? ValidateSynchronously()
    {
        if (EnabledLanguages.Count == 0)
            return "At least one language must be enabled.";
        if (!EnabledLanguages.Contains(DefaultLanguage, StringComparer.OrdinalIgnoreCase))
            return "Default language must be one of the enabled languages.";
        if (!EnabledLanguages.Contains(FallbackLanguage, StringComparer.OrdinalIgnoreCase))
            return "Fallback language must be one of the enabled languages.";
        if (!IsOffline && string.IsNullOrWhiteSpace(TmsApiUrl))
            return "TMS API URL is required when a TMS provider is selected.";
        if (!IsOffline && string.IsNullOrWhiteSpace(TmsProjectId))
            return "TMS Project ID is required when a TMS provider is selected.";
        if (IsWeblate && string.IsNullOrWhiteSpace(TmsComponent))
            return "Weblate requires a component slug.";
        return null;
    }
}
