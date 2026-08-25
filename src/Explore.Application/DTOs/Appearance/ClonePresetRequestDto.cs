// ABOUTME: Input DTO for cloning a preset into a user-owned appearance profile.
// ABOUTME: The system checks for existing clones to avoid duplicates before creating a new profile.

namespace Explore.Application.DTOs.Appearance;

public sealed record ClonePresetRequestDto
{
    /// <summary>Optional name override for the cloned profile. Defaults to the preset's display name.</summary>
    public string? Name { get; init; }
}
