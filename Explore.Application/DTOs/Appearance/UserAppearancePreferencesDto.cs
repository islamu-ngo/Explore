// ABOUTME: DTO representing the effective appearance preferences for the authenticated user.
// ABOUTME: Carries the resolved theme mode and selected theme reference for BFF/runtime consumption.

namespace Explore.Application.DTOs.Appearance;

public class UserAppearancePreferencesDto
{
    public string ThemeMode { get; set; } = "system";
    public string Direction { get; set; } = "auto";
    public Guid? DefaultThemeId { get; set; }
}
