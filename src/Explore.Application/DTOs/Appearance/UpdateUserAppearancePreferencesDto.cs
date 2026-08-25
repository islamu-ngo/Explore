// ABOUTME: Grouped PATCH contract for current-user appearance localization preferences.
// ABOUTME: Theme mode and active profile remain owned by their focused operations.

namespace Explore.Application.DTOs.Appearance;

public sealed record UpdateUserAppearancePreferencesDto
{
    public UpdateAppearanceLocalizationDto? Localization { get; init; }
}

public sealed record UpdateAppearanceLocalizationDto
{
    public string? Direction { get; init; }
    public string? Language { get; init; }
}
