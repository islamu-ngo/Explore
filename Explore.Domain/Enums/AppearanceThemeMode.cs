// ABOUTME: Theme mode selection covering all user-facing appearance pillars.
// ABOUTME: Light/Dark/HighContrast variants map to ServerEffectiveDarkMode; System and Custom require client-side resolution.

namespace Explore.Domain.Enums;

public enum AppearanceThemeMode
{
    Light,
    Dark,
    System,
    LightHighContrast,
    DarkHighContrast,
    Custom
}