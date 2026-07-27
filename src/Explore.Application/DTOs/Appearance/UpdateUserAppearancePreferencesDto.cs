// ABOUTME: Grouped PATCH contract for current-user appearance localization preferences.
// ABOUTME: Theme mode and active profile remain owned by their focused operations.

namespace Explore.Application.DTOs.Appearance;

public class UpdateUserAppearancePreferencesDto
{
    public UpdateAppearanceLocalizationDto? Localization { get; set; }
}

public sealed class UpdateAppearanceLocalizationDto
{
    public string? Direction { get; set; }
    public string? Language { get; set; }
}
