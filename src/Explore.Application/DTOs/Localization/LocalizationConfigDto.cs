// ABOUTME: DTO representing the current localization configuration for admin display.
// ABOUTME: Combines governance settings (provider, languages, kill-switches) into a single view.

namespace Explore.Application.DTOs.Localization;

public class LocalizationConfigDto
{
    public string DefaultLanguage { get; set; } = "en";
    public string TmsProvider { get; set; } = "None";
    public string? TmsApiUrl { get; set; }
    public string? TmsProjectId { get; set; }
    public string? TmsComponent { get; set; }
    public bool IsConnected { get; set; }
    public List<string> AvailableLanguages { get; set; } = [];
    public List<string> EnabledLanguages { get; set; } = [];
    public string FallbackLanguage { get; set; } = "en";
    public bool ClientPickerEnabled { get; set; } = true;
    public bool ForceOfflineMode { get; set; }
    public bool TmsApiKeyConfigured { get; set; }
}
