// ABOUTME: Helper for group-level branding settings using Actor-denormalized columns.
// ABOUTME: Builds banner CSS styles from Actor branding properties.

namespace Explore.Blazor.Client.Helpers;

public sealed class GroupBrandingSettings
{
    public string BannerColor { get; set; } = string.Empty;
    public string BannerPictureUri { get; set; } = string.Empty;
    public string BackgroundEffect { get; set; } = "None";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(BannerColor) &&
        string.IsNullOrWhiteSpace(BannerPictureUri) &&
        (string.IsNullOrWhiteSpace(BackgroundEffect) ||
         BackgroundEffect.Equals("None", StringComparison.OrdinalIgnoreCase));
}

public static class GroupBrandingMetadataHelper
{
    public static GroupBrandingSettings FromColumns(
        string? bannerColor, string? bannerPictureUri, string? backgroundEffect)
    {
        return new GroupBrandingSettings
        {
            BannerColor = bannerColor ?? string.Empty,
            BannerPictureUri = bannerPictureUri ?? string.Empty,
            BackgroundEffect = backgroundEffect ?? "None"
        };
    }

    public static string BuildBannerStyle(GroupBrandingSettings branding, string fallbackColorHex)
    {
        var color = string.IsNullOrWhiteSpace(branding.BannerColor)
            ? fallbackColorHex
            : branding.BannerColor.Trim();

        var mediaUrl = branding.BannerPictureUri?.Trim();
        var overlay = branding.BackgroundEffect?.Trim() switch
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
