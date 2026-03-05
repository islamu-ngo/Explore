// ABOUTME: Helper for organization-level page appearance using Actor-denormalized columns.
// ABOUTME: Builds banner CSS styles from Actor appearance properties (background, banner).

namespace Explore.Blazor.Client.Helpers;

public sealed class OrganizationAppearanceSettings
{
    public string BackgroundColor { get; set; } = string.Empty;
    public string BannerPictureUri { get; set; } = string.Empty;
    public string BackgroundEffect { get; set; } = "None";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(BackgroundColor) &&
        string.IsNullOrWhiteSpace(BannerPictureUri) &&
        (string.IsNullOrWhiteSpace(BackgroundEffect) ||
         BackgroundEffect.Equals("None", StringComparison.OrdinalIgnoreCase));
}

public static class OrganizationAppearanceMetadataHelper
{
    public static OrganizationAppearanceSettings FromColumns(
        string? backgroundColor, string? bannerPictureUri, string? backgroundEffect)
    {
        return new OrganizationAppearanceSettings
        {
            BackgroundColor = backgroundColor ?? string.Empty,
            BannerPictureUri = bannerPictureUri ?? string.Empty,
            BackgroundEffect = backgroundEffect ?? "None"
        };
    }

    public static string BuildBannerStyle(OrganizationAppearanceSettings appearance, string fallbackColorHex)
    {
        var color = string.IsNullOrWhiteSpace(appearance.BackgroundColor)
            ? fallbackColorHex
            : appearance.BackgroundColor.Trim();

        var mediaUrl = appearance.BannerPictureUri?.Trim();
        var overlay = appearance.BackgroundEffect?.Trim() switch
        {
            "SoftOverlay" => "linear-gradient(rgba(0,0,0,0.22), rgba(0,0,0,0.22))",
            "StrongOverlay" => "linear-gradient(rgba(0,0,0,0.40), rgba(0,0,0,0.40))",
            "Blur" => "linear-gradient(rgba(0,0,0,0.18), rgba(0,0,0,0.18))",
            _ => string.Empty
        };

        if (string.IsNullOrWhiteSpace(mediaUrl))
        {
            return $"background: {color};";
        }

        var mediaBackground = $"url('{mediaUrl}') center / cover no-repeat";
        if (string.IsNullOrWhiteSpace(overlay))
        {
            return $"background: {mediaBackground};";
        }

        return $"background: {overlay}, {mediaBackground};";
    }
}
