// ABOUTME: Read model for the multi-tenant instance control-plane overview.
// ABOUTME: Exposes safe operational summary fields without provider secrets or browser-side authority.

namespace Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneOverviewDto
{
    public string Version { get; set; } = string.Empty;
    public string DeploymentMode { get; set; } = string.Empty;
    public string? PublicOrigin { get; set; }
    public string? AdminOrigin { get; set; }
    public string? InstanceBaseDomain { get; set; }
    public int TotalTenantCount { get; set; }
    public int ActiveTenantCount { get; set; }
    public IReadOnlyList<ControlPlaneTenantStatusCountDto> TenantStatusCounts { get; set; } = [];
    public IReadOnlyList<ControlPlaneProviderSummaryDto> ProviderSummaries { get; set; } = [];
    public IReadOnlyList<ControlPlaneWarningDto> Warnings { get; set; } = [];
}

public sealed class ControlPlaneTenantStatusCountDto
{
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
}

public sealed class ControlPlaneProviderSummaryDto
{
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool Configured { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
}

public sealed class ControlPlaneWarningDto
{
    public string Code { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
