// ABOUTME: DTO for a footer link group column containing ordered links.
// ABOUTME: Maps from TenantFooterLinkGroup entity; includes child links for rendering.

namespace Explore.Application.DTOs.Footer;

public class FooterLinkGroupDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public IReadOnlyList<FooterLinkItemDto> Links { get; set; } = [];
}
