// ABOUTME: Helper for event-level appearance settings using dedicated columns on EventDto.
// ABOUTME: Builds hero CSS styles from structured appearance properties.

namespace Explore.Blazor.Client.Helpers;

public sealed class EventAppearanceSettings
{
    public string BackgroundColor { get; set; } = string.Empty;
    public string BackgroundImageUri { get; set; } = string.Empty;
    public string BackgroundEffect { get; set; } = "None";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(BackgroundColor) &&
        string.IsNullOrWhiteSpace(BackgroundImageUri) &&
        (string.IsNullOrWhiteSpace(BackgroundEffect) ||
         BackgroundEffect.Equals("None", StringComparison.OrdinalIgnoreCase));
}

public static class EventAppearanceMetadataHelper
{
    public static EventAppearanceSettings FromColumns(
        string? backgroundColor, string? backgroundImageUri, string? backgroundEffect)
    {
        return new EventAppearanceSettings
        {
            BackgroundColor = backgroundColor ?? string.Empty,
            BackgroundImageUri = backgroundImageUri ?? string.Empty,
            BackgroundEffect = backgroundEffect ?? "None"
        };
    }

    public static string BuildHeroStyle(EventAppearanceSettings appearance, string fallbackColorHex)
    {
        var color = string.IsNullOrWhiteSpace(appearance.BackgroundColor)
            ? fallbackColorHex
            : appearance.BackgroundColor.Trim();

        var mediaUrl = appearance.BackgroundImageUri?.Trim();
        var overlay = appearance.BackgroundEffect?.Trim() switch
        {
            "SoftOverlay" => "linear-gradient(rgba(0,0,0,0.24), rgba(0,0,0,0.24))",
            "StrongOverlay" => "linear-gradient(rgba(0,0,0,0.40), rgba(0,0,0,0.40))",
            "Blur" => "linear-gradient(rgba(0,0,0,0.18), rgba(0,0,0,0.18))",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return $"aspect-ratio: 16/9; position: relative; background: {color};";
        }

        var mediaBackground = $"url('{mediaUrl}') center / cover no-repeat";
        if (string.IsNullOrWhiteSpace(overlay))
        {
            return $"aspect-ratio: 16/9; position: relative; background: {mediaBackground};";
        }

        return $"aspect-ratio: 16/9; position: relative; background: {overlay}, {mediaBackground};";
    }
}
