// ABOUTME: Unified appearance style builder replacing EventAppearanceMetadataHelper, OrganizationAppearanceMetadataHelper, and GroupBrandingMetadataHelper.
// ABOUTME: Builds CSS inline styles from background color, image, and effect settings for banner/hero sections.

namespace Explore.Blazor.Client.Helpers;

/// <summary>
/// Unified appearance settings for background customization.
/// Used by events, organizations, groups, and users.
/// </summary>
public sealed class AppearanceSettings
{
    public string BackgroundColor { get; set; } = string.Empty;
    public string ImageUri { get; set; } = string.Empty;
    public string BackgroundEffect { get; set; } = "None";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(BackgroundColor) &&
        string.IsNullOrWhiteSpace(ImageUri) &&
        (string.IsNullOrWhiteSpace(BackgroundEffect) ||
         BackgroundEffect.Equals("None", StringComparison.OrdinalIgnoreCase));
}

/// <summary>
/// Builds CSS inline styles for banner/hero sections from appearance settings.
/// Replaces three entity-specific helpers with one unified builder.
/// </summary>
public static class AppearanceStyleBuilder
{
    /// <summary>
    /// Builds a CSS style string from appearance settings.
    /// </summary>
    /// <param name="settings">The appearance settings to apply.</param>
    /// <param name="fallbackColorHex">Fallback color when no background color is set.</param>
    /// <param name="additionalCss">Optional additional CSS properties prepended to the style (e.g., "aspect-ratio: 16/9; position: relative").</param>
    public static string BuildStyle(AppearanceSettings settings, string fallbackColorHex, string? additionalCss = null)
    {
        var color = string.IsNullOrWhiteSpace(settings.BackgroundColor)
            ? fallbackColorHex
            : settings.BackgroundColor.Trim();

        var mediaUrl = settings.ImageUri?.Trim();
        var overlay = ResolveOverlay(settings.BackgroundEffect);

        var prefix = string.IsNullOrWhiteSpace(additionalCss)
            ? string.Empty
            : additionalCss.TrimEnd().TrimEnd(';') + "; ";

        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return $"{prefix}background: {color};";
        }

        var mediaBackground = $"url('{mediaUrl}') center / cover no-repeat";
        if (string.IsNullOrWhiteSpace(overlay))
        {
            return $"{prefix}background: {mediaBackground};";
        }

        return $"{prefix}background: {overlay}, {mediaBackground};";
    }

    /// <summary>
    /// Builds a hero-style CSS string with 16:9 aspect ratio (for event detail/create pages).
    /// </summary>
    public static string BuildHeroStyle(AppearanceSettings settings, string fallbackColorHex)
        => BuildStyle(settings, fallbackColorHex, "aspect-ratio: 16/9; position: relative");

    /// <summary>
    /// Builds a banner-style CSS string without aspect ratio (for profile pages).
    /// </summary>
    public static string BuildBannerStyle(AppearanceSettings settings, string fallbackColorHex)
        => BuildStyle(settings, fallbackColorHex);

    private static string ResolveOverlay(string? effect)
    {
        return effect?.Trim() switch
        {
            "SoftOverlay" => "linear-gradient(rgba(0,0,0,0.24), rgba(0,0,0,0.24))",
            "StrongOverlay" => "linear-gradient(rgba(0,0,0,0.40), rgba(0,0,0,0.40))",
            "Blur" => "linear-gradient(rgba(0,0,0,0.18), rgba(0,0,0,0.18))",
            _ => string.Empty
        };
    }
}
