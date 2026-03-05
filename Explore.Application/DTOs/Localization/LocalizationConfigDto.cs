// ABOUTME: DTO representing the current localization configuration for admin display.
// ABOUTME: Combines governance settings into a single view of TMS provider state.

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
}
