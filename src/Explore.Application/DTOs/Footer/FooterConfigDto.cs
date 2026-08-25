// ABOUTME: Aggregate DTO that combines footer settings and link groups for public rendering.
// ABOUTME: Returned by GetFooterConfigQuery; contains everything the Footer.razor needs.

namespace Explore.Application.DTOs.Footer;

public sealed record FooterConfigDto
{
    public FooterSettingsDto Settings { get; init; } = new();
    public IReadOnlyList<FooterLinkGroupDto> LinkGroups { get; init; } = [];
}
