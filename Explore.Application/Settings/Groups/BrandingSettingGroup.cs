// ABOUTME: Strongly-typed Branding setting group resolved via batch loading.
// ABOUTME: Groups all tenant-overridable branding settings (display name, logos, CSS).

namespace Explore.Application.Settings.Groups;

using Explore.Application.Contracts.Infrastructure;

/// <summary>
/// Strongly-typed group for white-label branding settings.
/// </summary>
public class BrandingSettingGroup : ISettingGroup
{
    public string DisplayName { get; private set; } = "ISLAMU Explore";
    public string? LogoUrl { get; private set; }
    public string? FaviconUrl { get; private set; }
    public string? CustomCssUrl { get; private set; }

    public static IEnumerable<string> SettingKeys =>
    [
        "branding.display_name", "branding.logo_url",
        "branding.favicon_url", "branding.custom_css_url"
    ];

    public void Populate(IReadOnlyDictionary<string, ResolvedSetting> settings)
    {
        if (settings.TryGetValue("branding.display_name", out var name))
            DisplayName = SettingValueSerializer.Deserialize(name.Value, "ISLAMU Explore");
        if (settings.TryGetValue("branding.logo_url", out var logo))
            LogoUrl = SettingValueSerializer.DeserializeString(logo.Value);
        if (settings.TryGetValue("branding.favicon_url", out var favicon))
            FaviconUrl = SettingValueSerializer.DeserializeString(favicon.Value);
        if (settings.TryGetValue("branding.custom_css_url", out var css))
            CustomCssUrl = SettingValueSerializer.DeserializeString(css.Value);
    }
}
