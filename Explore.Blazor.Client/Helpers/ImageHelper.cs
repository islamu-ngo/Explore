// ABOUTME: Shared helper for generating local fallback images when no actual image is available.
// ABOUTME: Keeps production pages independent from external placeholder image services.

using System.Net;

namespace Explore.Blazor.Client.Helpers;

/// <summary>
/// Provides consistent local fallback image generation without external network dependencies.
/// </summary>
public static class ImageHelper
{
    private const string DefaultTextColor = "ffffff";
    private const string DefaultOrganizationColor = "2196F3";
    private const int MaxTitleLength = 30;
    private const int MaxOrganizationNameLength = 15;

    /// <summary>
    /// Returns the event's featured image URL or a local SVG fallback with the event title and color.
    /// </summary>
    /// <param name="featuredImageUri">The actual image URI, if available.</param>
    /// <param name="title">The event title for fallback text.</param>
    /// <param name="colorHex">Hex color code (without #). Defaults to gray.</param>
    /// <param name="width">Fallback width. Defaults to 600.</param>
    /// <param name="height">Fallback height. Defaults to 400.</param>
    public static string GetEventImageUrl(
        string? featuredImageUri,
        string title,
        string? colorHex = null,
        int width = 600,
        int height = 400)
    {
        if (!string.IsNullOrEmpty(featuredImageUri))
            return featuredImageUri;

        var color = NormalizeHexColor(colorHex, EventColorHelper.DefaultColor);
        var truncatedTitle = title.Length > MaxTitleLength
            ? title[..MaxTitleLength] + "..."
            : title;

        return CreateSvgDataUri(width, height, color, truncatedTitle, DefaultTextColor);
    }

    /// <summary>
    /// Returns an organization image URI or a local SVG fallback with the organization name.
    /// </summary>
    public static string GetOrganizationPlaceholder(
        string? profileImageUri,
        string name,
        int size = 240)
    {
        if (!string.IsNullOrEmpty(profileImageUri))
            return profileImageUri;

        var truncatedName = name.Length > MaxOrganizationNameLength
            ? name[..MaxOrganizationNameLength] + "..."
            : name;

        return CreateSvgDataUri(size, size, DefaultOrganizationColor, truncatedName, DefaultTextColor);
    }

    private static string CreateSvgDataUri(int width, int height, string backgroundColor, string text, string textColor)
    {
        var safeWidth = Math.Max(1, width);
        var safeHeight = Math.Max(1, height);
        var fontSize = Math.Clamp(Math.Min(safeWidth, safeHeight) / 7, 18, 34);
        var encodedText = WebUtility.HtmlEncode(string.IsNullOrWhiteSpace(text) ? "Event" : text.Trim());

        var svg = $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"{safeWidth}\" height=\"{safeHeight}\" viewBox=\"0 0 {safeWidth} {safeHeight}\" role=\"img\" aria-label=\"{encodedText}\"><rect width=\"100%\" height=\"100%\" fill=\"#{backgroundColor}\"/><foreignObject width=\"100%\" height=\"100%\"><div xmlns=\"http://www.w3.org/1999/xhtml\" style=\"width:100%;height:100%;display:flex;align-items:center;justify-content:center;text-align:center;color:#{textColor};font-family:Inter,Segoe UI,Arial,sans-serif;font-size:{fontSize}px;font-weight:600;padding:24px;box-sizing:border-box;overflow:hidden;word-break:break-word;line-height:1.3;\">{encodedText}</div></foreignObject></svg>";
        return $"data:image/svg+xml;utf8,{Uri.EscapeDataString(svg)}";
    }

    private static string NormalizeHexColor(string? colorHex, string fallback)
    {
        var color = string.IsNullOrWhiteSpace(colorHex) ? fallback : colorHex.Trim().TrimStart('#');
        return IsValidHexColor(color) ? color : fallback;
    }

    private static bool IsValidHexColor(string color) =>
        (color.Length == 3 || color.Length == 6) && color.All(Uri.IsHexDigit);
}
