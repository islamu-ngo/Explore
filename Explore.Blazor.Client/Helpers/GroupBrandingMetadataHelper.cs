// ABOUTME: Helper for group-level branding settings stored in Group.MetadataJson.
// ABOUTME: Supports profile image and banner theming without dedicated schema columns.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Explore.Blazor.Client.Helpers;

public sealed class GroupBrandingSettings
{
    public string PictureUrl { get; set; } = string.Empty;
    public string BannerColor { get; set; } = string.Empty;
    public string BannerMediaUrl { get; set; } = string.Empty;
    public string BannerEffect { get; set; } = "None";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(PictureUrl) &&
        string.IsNullOrWhiteSpace(BannerColor) &&
        string.IsNullOrWhiteSpace(BannerMediaUrl) &&
        (string.IsNullOrWhiteSpace(BannerEffect) ||
         BannerEffect.Equals("None", StringComparison.OrdinalIgnoreCase));
}

public static class GroupBrandingMetadataHelper
{
    private const string BrandingNode = "groupBranding";

    public static GroupBrandingSettings Parse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new GroupBrandingSettings();
        }

        try
        {
            var root = JsonNode.Parse(metadataJson) as JsonObject;
            var branding = root?[BrandingNode] as JsonObject;
            if (branding == null)
            {
                return new GroupBrandingSettings();
            }

            return new GroupBrandingSettings
            {
                PictureUrl = branding["pictureUrl"]?.GetValue<string>() ?? string.Empty,
                BannerColor = branding["bannerColor"]?.GetValue<string>() ?? string.Empty,
                BannerMediaUrl = branding["bannerMediaUrl"]?.GetValue<string>() ?? string.Empty,
                BannerEffect = branding["bannerEffect"]?.GetValue<string>() ?? "None"
            };
        }
        catch
        {
            return new GroupBrandingSettings();
        }
    }

    public static string? Upsert(string? metadataJson, GroupBrandingSettings branding)
    {
        JsonObject root;
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            root = new JsonObject();
        }
        else
        {
            root = JsonNode.Parse(metadataJson) as JsonObject ?? new JsonObject();
        }

        if (branding.IsEmpty)
        {
            root.Remove(BrandingNode);
        }
        else
        {
            root[BrandingNode] = new JsonObject
            {
                ["pictureUrl"] = NullIfWhiteSpace(branding.PictureUrl),
                ["bannerColor"] = NullIfWhiteSpace(branding.BannerColor),
                ["bannerMediaUrl"] = NullIfWhiteSpace(branding.BannerMediaUrl),
                ["bannerEffect"] = string.IsNullOrWhiteSpace(branding.BannerEffect) ? "None" : branding.BannerEffect.Trim()
            };
        }

        return root.Count == 0 ? null : root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static string BuildBannerStyle(GroupBrandingSettings branding, string fallbackColorHex)
    {
        var color = string.IsNullOrWhiteSpace(branding.BannerColor)
            ? fallbackColorHex
            : branding.BannerColor.Trim();

        var mediaUrl = branding.BannerMediaUrl?.Trim();
        var overlay = branding.BannerEffect?.Trim() switch
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

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
