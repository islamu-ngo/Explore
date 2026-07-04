// ABOUTME: Read models for the multi-tenant control-plane domain and DNS checklist.
// ABOUTME: Exposes operator guidance without performing external DNS lookups or leaking infrastructure secrets.

namespace Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneDomainOverviewDto
{
    public string? PublicOrigin { get; set; }
    public string? PublicPlatformHost { get; set; }
    public string? InstanceBaseDomain { get; set; }
    public string? WildcardTenantHost { get; set; }
    public string? AdminOrigin { get; set; }
    public string? AdminHost { get; set; }
    public bool AllowTenantCustomDomains { get; set; }
    public bool LockTenantSubdomain { get; set; }
    public bool LockTenantCustomDomain { get; set; }
    public IReadOnlyList<ControlPlaneDnsRecordDto> DnsRecords { get; set; } = [];
    public IReadOnlyList<ControlPlaneWarningDto> Warnings { get; set; } = [];
}

public sealed class ControlPlaneDnsRecordDto
{
    public string Purpose { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Target { get; set; } = string.Empty;
    public bool Required { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Guidance { get; set; } = string.Empty;
}
