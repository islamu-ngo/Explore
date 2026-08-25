// ABOUTME: Grouped PATCH contract for localization TMS, language, and runtime governance.
// ABOUTME: TMS API keys remain isolated behind the dedicated secret-provider path.

namespace Explore.Application.DTOs.Localization;

public sealed record UpdateLocalizationGovernanceDto
{
    public LocalizationTmsUpdateDto? Tms { get; init; }
    public LocalizationLanguagePolicyUpdateDto? Languages { get; init; }
    public LocalizationRuntimeUpdateDto? Runtime { get; init; }
}

public sealed record LocalizationTmsUpdateDto
{
    public required string Provider { get; init; }
    public string? ApiUrl { get; init; }
    public string? ProjectId { get; init; }
    public string? Component { get; init; }
}

public sealed record LocalizationLanguagePolicyUpdateDto
{
    public required string DefaultLanguage { get; init; }
    public string[] EnabledLanguages { get; init; } = [];
    public required string FallbackLanguage { get; init; }
}

public sealed record LocalizationRuntimeUpdateDto
{
    public bool ClientPickerEnabled { get; init; }
    public bool ForceOfflineMode { get; init; }
}
