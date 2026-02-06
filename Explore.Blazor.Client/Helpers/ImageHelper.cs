// ABOUTME: Shared helper for generating placeholder image URLs when no actual image is available.
// ABOUTME: Replaces 6 duplicate placeholder image URL generation patterns across the codebase.

namespace Explore.Blazor.Client.Helpers;

/// <summary>
/// Provides consistent placeholder image URL generation.
/// Uses placehold.co for all placeholder images (consistent service).
/// </summary>
public static class ImageHelper
{
    private const string PlaceholderService = "https://placehold.co";
    private const int MaxTitleLength = 30;

    /// <summary>
    /// Returns the event's featured image URL or a placeholder with the event title and color.
    /// </summary>
    /// <param name="featuredImageUri">The actual image URI, if available.</param>
    /// <param name="title">The event title for placeholder text.</param>
    /// <param name="colorHex">Hex color code (without #). Defaults to gray.</param>
    /// <param name="width">Placeholder width. Defaults to 600.</param>
    /// <param name="height">Placeholder height. Defaults to 400.</param>
    public static string GetEventImageUrl(
        string? featuredImageUri,
        string title,
        string? colorHex = null,
        int width = 600,
        int height = 400)
    {
        if (!string.IsNullOrEmpty(featuredImageUri))
            return featuredImageUri;

        var color = colorHex ?? EventColorHelper.DefaultColor;
        var truncatedTitle = title.Length > MaxTitleLength
            ? title[..MaxTitleLength] + "..."
            : title;
        var encodedTitle = Uri.EscapeDataString(truncatedTitle);
        return $"{PlaceholderService}/{width}x{height}/{color}/ffffff?text={encodedTitle}";
    }

    /// <summary>
    /// Returns an organization placeholder image with the org name.
    /// </summary>
    public static string GetOrganizationPlaceholder(
        string? profileImageUri,
        string name,
        int size = 240)
    {
        if (!string.IsNullOrEmpty(profileImageUri))
            return profileImageUri;

        var truncatedName = name.Length > 15
            ? name[..15] + "..."
            : name;
        var encodedName = Uri.EscapeDataString(truncatedName);
        return $"{PlaceholderService}/{size}x{size}/2196F3/FFFFFF?text={encodedName}";
    }
}
