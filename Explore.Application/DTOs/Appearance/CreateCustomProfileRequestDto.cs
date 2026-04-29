// ABOUTME: Input DTO for creating a fully custom user appearance profile from natural + brand color inputs.
// ABOUTME: The palette generation engine expands two colors into full 18-token light and dark palettes.

namespace Explore.Application.DTOs.Appearance;

public sealed class CreateCustomProfileRequestDto
{
    public required string Name { get; set; }
    public string ThemeMode { get; set; } = "system";

    /// <summary>The natural/neutral color (determines surface, background, text, etc.).</summary>
    public required string NaturalColor { get; set; }

    /// <summary>The brand/accent color (determines primary, secondary, appbar, etc.).</summary>
    public required string BrandColor { get; set; }
}