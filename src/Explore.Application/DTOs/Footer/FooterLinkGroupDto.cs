// ABOUTME: DTO for a footer link group column containing ordered links.
// ABOUTME: Maps from TenantFooterLinkGroup entity; includes child links for rendering.

namespace Explore.Application.DTOs.Footer;

public sealed record FooterLinkGroupDto
{
    public Guid Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public int Order { get; init; }
    public IReadOnlyList<FooterLinkItemDto> Links { get; init; } = [];
}
