// ABOUTME: Input DTO for updating user-scoped appearance preferences (theme, direction, language).
// ABOUTME: Language is persisted here for v1 delivery speed — see plan D3 for the follow-up UserPreferences split.

namespace Explore.Application.DTOs.Appearance;

public class UpdateUserAppearancePreferencesDto
{
    public string ThemeMode { get; set; } = "system";
    public string Direction { get; set; } = "auto";
    public string Language { get; set; } = "en";
}
