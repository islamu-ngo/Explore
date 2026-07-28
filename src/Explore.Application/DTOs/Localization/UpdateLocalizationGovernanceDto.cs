// ABOUTME: Grouped PATCH contract for localization TMS, language, and runtime governance.
// ABOUTME: TMS API keys remain isolated behind the dedicated secret-provider path.

namespace Explore.Application.DTOs.Localization;

public sealed class UpdateLocalizationGovernanceDto
{
    public LocalizationTmsUpdateDto? Tms { get; set; }
    public LocalizationLanguagePolicyUpdateDto? Languages { get; set; }
    public LocalizationRuntimeUpdateDto? Runtime { get; set; }
}

public sealed class LocalizationTmsUpdateDto
{
    public required string Provider { get; set; }
    public string? ApiUrl { get; set; }
    public string? ProjectId { get; set; }
    public string? Component { get; set; }
}

public sealed class LocalizationLanguagePolicyUpdateDto
{
    public required string DefaultLanguage { get; set; }
    public string[] EnabledLanguages { get; set; } = [];
    public required string FallbackLanguage { get; set; }
}

public sealed class LocalizationRuntimeUpdateDto
{
    public bool ClientPickerEnabled { get; set; }
    public bool ForceOfflineMode { get; set; }
}
