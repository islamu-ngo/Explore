// ABOUTME: Admin input DTO for localization governance changes — TMS config + kill-switches + enabled languages.
// ABOUTME: Secrets (API keys) are NOT carried here; they are rotated through a dedicated SecretProvider path.

namespace Explore.Application.DTOs.Localization;

public class UpdateLocalizationGovernanceDto
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
