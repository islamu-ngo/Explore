// ABOUTME: Full detail DTO for a footer link group including all child links.
// ABOUTME: Returned by GetFooterLinkGroupDetailsQuery for admin edit views.

namespace Explore.Application.DTOs.Footer;

public class FooterLinkGroupDetailsDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public IReadOnlyList<FooterLinkItemDto> Links { get; set; } = [];
}
