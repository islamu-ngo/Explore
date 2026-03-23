// ABOUTME: Aggregate DTO that combines footer settings and link groups for public rendering.
// ABOUTME: Returned by GetFooterConfigQuery; contains everything the Footer.razor needs.

namespace Explore.Application.DTOs.Footer;

public class FooterConfigDto
{
    public FooterSettingsDto Settings { get; set; } = new();
    public IReadOnlyList<FooterLinkGroupDto> LinkGroups { get; set; } = [];
}
