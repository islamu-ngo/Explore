// ABOUTME: Input DTO for setting the active appearance profile and mode/direction/language overrides.
// ABOUTME: Replaces the old UpdateUserAppearancePreferencesDto with profile-based selection.

namespace Explore.Application.DTOs.Appearance;

public sealed record SetActiveProfileRequestDto
{
    public Guid ProfileId { get; init; }
    public string? ThemeMode { get; init; }
    public string? Direction { get; init; }
    public string? Language { get; init; }
}
