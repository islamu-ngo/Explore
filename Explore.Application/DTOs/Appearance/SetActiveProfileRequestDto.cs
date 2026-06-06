// ABOUTME: Input DTO for setting the active appearance profile and mode/direction/language overrides.
// ABOUTME: Replaces the old UpdateUserAppearancePreferencesDto with profile-based selection.

namespace Explore.Application.DTOs.Appearance;

public sealed class SetActiveProfileRequestDto
{
    public Guid ProfileId { get; set; }
    public string? ThemeMode { get; set; }
    public string? Direction { get; set; }
    public string? Language { get; set; }
}
