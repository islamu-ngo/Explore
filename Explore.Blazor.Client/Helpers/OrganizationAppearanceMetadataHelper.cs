// ABOUTME: Helper for organization-level page appearance stored in Organization.MetadataJson.
// ABOUTME: Enables configurable organization profile visuals without adding dedicated columns for each option.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Explore.Blazor.Client.Helpers;

public sealed class OrganizationAppearanceSettings
{
    public string ProfileImageUrl { get; set; } = string.Empty;
    public string BackgroundColor { get; set; } = string.Empty;
    public string BackgroundMediaUrl { get; set; } = string.Empty;
    public string BackgroundEffect { get; set; } = "None";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(ProfileImageUrl) &&
        string.IsNullOrWhiteSpace(BackgroundColor) &&
        string.IsNullOrWhiteSpace(BackgroundMediaUrl) &&
        (string.IsNullOrWhiteSpace(BackgroundEffect) ||
         BackgroundEffect.Equals("None", StringComparison.OrdinalIgnoreCase));
}

public static class OrganizationAppearanceMetadataHelper
{
    private const string AppearanceNode = "pageAppearance";

    public static OrganizationAppearanceSettings Parse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new OrganizationAppearanceSettings();
        }

        try
        {
            var root = JsonNode.Parse(metadataJson) as JsonObject;
            var appearance = root?[AppearanceNode] as JsonObject;
            if (appearance == null)
            {
                return new OrganizationAppearanceSettings();
            }

            return new OrganizationAppearanceSettings
            {
                ProfileImageUrl = appearance["profileImageUrl"]?.GetValue<string>() ?? string.Empty,
                BackgroundColor = appearance["backgroundColor"]?.GetValue<string>() ?? string.Empty,
                BackgroundMediaUrl = appearance["backgroundMediaUrl"]?.GetValue<string>() ?? string.Empty,
                BackgroundEffect = appearance["backgroundEffect"]?.GetValue<string>() ?? "None"
            };
        }
        catch
        {
            return new OrganizationAppearanceSettings();
        }
    }

    public static string? Upsert(string? metadataJson, OrganizationAppearanceSettings appearance)
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

        if (appearance.IsEmpty)
        {
            root.Remove(AppearanceNode);
        }
        else
        {
            root[AppearanceNode] = new JsonObject
            {
                ["profileImageUrl"] = NullIfWhiteSpace(appearance.ProfileImageUrl),
                ["backgroundColor"] = NullIfWhiteSpace(appearance.BackgroundColor),
                ["backgroundMediaUrl"] = NullIfWhiteSpace(appearance.BackgroundMediaUrl),
                ["backgroundEffect"] = string.IsNullOrWhiteSpace(appearance.BackgroundEffect) ? "None" : appearance.BackgroundEffect.Trim()
            };
        }

        return root.Count == 0 ? null : root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static string BuildBannerStyle(OrganizationAppearanceSettings appearance, string fallbackColorHex)
    {
        var color = string.IsNullOrWhiteSpace(appearance.BackgroundColor)
            ? fallbackColorHex
            : appearance.BackgroundColor.Trim();

        var mediaUrl = appearance.BackgroundMediaUrl?.Trim();
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

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
