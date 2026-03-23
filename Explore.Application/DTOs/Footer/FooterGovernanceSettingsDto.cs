// ABOUTME: DTO for instance-level footer governance settings (lock flags and defaults).
// ABOUTME: Used in the instance admin UI to control what tenants can override.

namespace Explore.Application.DTOs.Footer;

public class FooterGovernanceSettingsDto
{
    public bool LockTenantTemplate { get; set; }
    public bool LockTenantLinkGroups { get; set; }
    public bool LockTenantSocialLinks { get; set; }
    public bool LockTenantDescription { get; set; }
    public bool LockTenantCopyright { get; set; }
}
