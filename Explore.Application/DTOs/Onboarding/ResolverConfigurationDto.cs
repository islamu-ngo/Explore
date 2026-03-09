// ABOUTME: DTO for tenant resolver configuration used by instance admin APIs and future activation UI.
// ABOUTME: Stores only system-level resolver state and avoids tenant-aware settings resolution.

namespace Explore.Application.DTOs.Onboarding;

public class ResolverConfigurationDto
{
    public bool HeaderEnabled { get; set; } = true;

    public bool SubdomainEnabled { get; set; }

    public bool CustomDomainEnabled { get; set; }

    public bool PathEnabled { get; set; } = true;

    public string PathPrefix { get; set; } = "/t";

    public string InstanceBaseDomain { get; set; } = string.Empty;

    public bool AllowTenantCustomDomains { get; set; } = true;
}
