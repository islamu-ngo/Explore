// ABOUTME: DTO for a single link within a footer link group column.
// ABOUTME: Maps from TenantFooterLink entity for public/tenant consumption.

namespace Explore.Application.DTOs.Footer;

public class FooterLinkItemDto
{
    public Guid Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public bool OpenInNewTab { get; set; }
    public bool IsActive { get; set; }
    public int Order { get; set; }
}
