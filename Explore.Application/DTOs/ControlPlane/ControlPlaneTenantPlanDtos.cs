// ABOUTME: Control-plane tenant plan DTOs for SaaS tier plan management.
// ABOUTME: Exposes bounded pricing, version, settings, quota, and assignment metadata.

namespace Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneTenantPlanListItemDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int LatestVersionNumber { get; set; }
    public int? PublishedVersionNumber { get; set; }
    public decimal PriceAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public bool IsActiveForProvisioning { get; set; }
}

public sealed class ControlPlaneTenantPlanDetailDto
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public IReadOnlyList<ControlPlaneTenantPlanVersionDto> Versions { get; set; } = [];
}

public sealed class ControlPlaneTenantPlanVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public decimal PriceAmount { get; set; }
    public string CurrencyCode { get; set; } = string.Empty;
    public string BillingPeriod { get; set; } = string.Empty;
    public bool IsActiveForProvisioning { get; set; }
    public IReadOnlyList<ControlPlaneTenantPlanSettingDto> Settings { get; set; } = [];
    public IReadOnlyList<ControlPlaneTenantPlanQuotaDto> Quotas { get; set; } = [];
}

public sealed class ControlPlaneTenantPlanSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string JsonValue { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
}

public sealed class ControlPlaneTenantPlanQuotaDto
{
    public string Key { get; set; } = string.Empty;
    public long Limit { get; set; }
}

public sealed class ControlPlaneTenantPlanAssignmentDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid PlanId { get; set; }
    public string PlanKey { get; set; } = string.Empty;
    public Guid PlanVersionId { get; set; }
    public int VersionNumber { get; set; }
    public int StatusId { get; set; }
    public string StatusCode { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; }
    public Guid AssignedByUserId { get; set; }
}
