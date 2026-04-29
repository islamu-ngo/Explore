// ABOUTME: DTO for the fully resolved appearance state returned to the client.
// ABOUTME: Carries the active profile, source provenance, effective theme data, and capabilities so the UI never guesses.

namespace Explore.Application.DTOs.Appearance;

public sealed class ResolvedAppearanceDto
{
    public Guid? ActiveProfileId { get; set; }
    public Guid? SourcePresetId { get; set; }
    public string? SourcePresetKey { get; set; }

    /// <summary>
    /// How the resolved appearance was determined. Maps to AppearanceResolutionSource enum values.
    /// </summary>
    public string ResolutionSource { get; set; } = default!;

    public string ThemeMode { get; set; } = "system";

    /// <summary>
    /// Nullable because the server cannot fully resolve System mode —
    /// the Blazor client is the runtime authority for the final dark/light decision.
    /// </summary>
    public bool? ServerEffectiveDarkMode { get; set; }

    public string Direction { get; set; } = "auto";
    public string Language { get; set; } = "en";

    public ResolvedThemeDto Theme { get; set; } = default!;

    public AppearanceCapabilitiesDto Capabilities { get; set; } = default!;
}