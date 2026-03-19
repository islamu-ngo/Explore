// ABOUTME: Sub-resource DTO for instance-level domain configuration.
// ABOUTME: Controls base domain, custom domain allowance, and tenant domain lock flags.

namespace Explore.Application.DTOs.Instance;

public class DomainSettingsDto
{
    public string InstanceBaseDomain { get; set; } = string.Empty;
    public bool AllowTenantCustomDomains { get; set; } = true;
    public bool LockTenantSubdomain { get; set; }
    public bool LockTenantCustomDomain { get; set; }
}
