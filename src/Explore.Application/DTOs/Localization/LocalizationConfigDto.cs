// ABOUTME: DTO representing the current localization configuration for admin display.
// ABOUTME: Combines governance settings (provider, languages, kill-switches) into a single view.

namespace Explore.Application.DTOs.Localization;

public sealed record LocalizationConfigDto
{
    public string DefaultLanguage { get; init; } = "en";
    public string TmsProvider { get; init; } = "None";
    public string? TmsApiUrl { get; init; }
    public string? TmsProjectId { get; init; }
    public string? TmsComponent { get; init; }
    public bool IsConnected { get; init; }
    public List<string> AvailableLanguages { get; init; } = [];
    public List<string> EnabledLanguages { get; init; } = [];
    public string FallbackLanguage { get; init; } = "en";
    public bool ClientPickerEnabled { get; init; } = true;
    public bool ForceOfflineMode { get; init; }
    public bool TmsApiKeyConfigured { get; init; }
}
