// ABOUTME: Helper for storing and retrieving event-level appearance settings inside Event.MetadataJson.
// ABOUTME: Keeps visual customization schema stable without requiring immediate table expansion.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Explore.Blazor.Client.Helpers;

public sealed class EventAppearanceSettings
{
    public string BackgroundColor { get; set; } = string.Empty;
    public string BackgroundMediaUrl { get; set; } = string.Empty;
    public string BackgroundEffect { get; set; } = "None";

    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(BackgroundColor) &&
        string.IsNullOrWhiteSpace(BackgroundMediaUrl) &&
        (string.IsNullOrWhiteSpace(BackgroundEffect) ||
         BackgroundEffect.Equals("None", StringComparison.OrdinalIgnoreCase));
}

public static class EventAppearanceMetadataHelper
{
    private const string AppearanceNode = "appearance";

    public static EventAppearanceSettings Parse(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return new EventAppearanceSettings();
        }

        try
        {
            var root = JsonNode.Parse(metadataJson) as JsonObject;
            var appearance = root?[AppearanceNode] as JsonObject;
            if (appearance == null)
            {
                return new EventAppearanceSettings();
            }

            return new EventAppearanceSettings
            {
                BackgroundColor = appearance["backgroundColor"]?.GetValue<string>() ?? string.Empty,
                BackgroundMediaUrl = appearance["backgroundMediaUrl"]?.GetValue<string>() ?? string.Empty,
                BackgroundEffect = appearance["backgroundEffect"]?.GetValue<string>() ?? "None"
            };
        }
        catch
        {
            return new EventAppearanceSettings();
        }
    }

    public static string? Upsert(string? metadataJson, EventAppearanceSettings appearance)
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
                ["backgroundColor"] = NullIfWhiteSpace(appearance.BackgroundColor),
                ["backgroundMediaUrl"] = NullIfWhiteSpace(appearance.BackgroundMediaUrl),
                ["backgroundEffect"] = string.IsNullOrWhiteSpace(appearance.BackgroundEffect) ? "None" : appearance.BackgroundEffect.Trim()
            };
        }

        return root.Count == 0 ? null : root.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
    }

    public static string BuildHeroStyle(EventAppearanceSettings appearance, string fallbackColorHex)
    {
        var color = string.IsNullOrWhiteSpace(appearance.BackgroundColor)
            ? fallbackColorHex
            : appearance.BackgroundColor.Trim();

        var mediaUrl = appearance.BackgroundMediaUrl?.Trim();
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

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
