// ABOUTME: Client-side model for admin footer governance settings form.
// ABOUTME: Mirrors footer settings structure for two-way binding in Blazor admin UI.

namespace Explore.Blazor.Client.Models.Footer;

public sealed class FooterGovernanceSettingsModel
{
    public bool Enabled { get; set; }
    public string Template { get; set; } = "Default";
    public bool ShowDescription { get; set; }
    public string? DescriptionText { get; set; }
    public bool ShowSocialLinks { get; set; }
    public string? SocialLinksJson { get; set; }
    public string? CopyrightText { get; set; }
    public bool ShowCookieSettingsLink { get; set; }
}
