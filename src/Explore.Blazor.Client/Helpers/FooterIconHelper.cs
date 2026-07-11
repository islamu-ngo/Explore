// ABOUTME: Static helper providing social media platform-to-icon resolution for footer templates.
// ABOUTME: Maps platform identifiers to MudBlazor icon constants (Facebook, Twitter/X, Instagram, etc.).

namespace Explore.Blazor.Client.Helpers;

using MudBlazor;

public static class FooterIconHelper
{
    public static string GetSocialIcon(string platform) => platform.ToLowerInvariant() switch
    {
        "facebook" => Icons.Custom.Brands.Facebook,
        "twitter" or "x" => Icons.Custom.Brands.Twitter,
        "instagram" => Icons.Custom.Brands.Instagram,
        "linkedin" => Icons.Custom.Brands.LinkedIn,
        "youtube" => Icons.Custom.Brands.YouTube,
        "github" => Icons.Custom.Brands.GitHub,
        _ => Icons.Material.Filled.Link,
    };
}
