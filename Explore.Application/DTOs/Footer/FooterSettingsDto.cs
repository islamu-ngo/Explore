// ABOUTME: DTO for the resolved tenant footer settings (settings-layer values only).
// ABOUTME: Used internally by FooterConfigDto to carry the scalar/json setting values.

namespace Explore.Application.DTOs.Footer;

public class FooterSettingsDto
{
    public bool Enabled { get; set; }
    public string Template { get; set; } = "standard-3-col";
    public bool ShowDescription { get; set; }
    public string DescriptionText { get; set; } = string.Empty;
    public bool ShowSocialLinks { get; set; }
    public IReadOnlyList<FooterSocialLinkDto> SocialLinks { get; set; } = [];
    public string CopyrightText { get; set; } = string.Empty;
    public bool ShowCookieSettingsLink { get; set; }
}
