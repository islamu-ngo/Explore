// ABOUTME: Client-side payload mirroring server UpdateLocalizationGovernanceDto until NSwag regenerates from swagger.
// ABOUTME: Matches the JSON shape on the wire; replaced by the generated client type when swagger is refreshed.

namespace Explore.Blazor.Client.Models.Admin;

public sealed class LocalizationGovernancePayload
{
    public string DefaultLanguage { get; set; } = "en";
    public string TmsProvider { get; set; } = "none";
    public string? TmsApiUrl { get; set; }
    public string? TmsProjectId { get; set; }
    public string? TmsComponent { get; set; }
    public string[] EnabledLanguages { get; set; } = ["en", "fr", "ar"];
    public string FallbackLanguage { get; set; } = "en";
    public bool ClientPickerEnabled { get; set; } = true;
    public bool ForceOfflineMode { get; set; }
}
