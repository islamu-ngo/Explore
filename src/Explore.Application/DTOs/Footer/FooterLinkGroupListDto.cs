// ABOUTME: Lightweight list DTO for footer link groups in admin list views.
// ABOUTME: Does not include child links to keep list payloads small.

namespace Explore.Application.DTOs.Footer;

public class FooterLinkGroupListDto
{
    public Guid Id { get; set; }
    public Guid? TenantId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; }
    public int LinkCount { get; set; }
}
