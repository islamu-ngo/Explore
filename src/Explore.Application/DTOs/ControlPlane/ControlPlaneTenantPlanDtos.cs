// ABOUTME: Control-plane tenant plan DTOs for SaaS tier plan management.
// ABOUTME: Exposes bounded pricing, version, settings, quota, and assignment metadata.

namespace Explore.Application.DTOs.ControlPlane;

using System.Text.Json.Serialization;
using Explore.Application.Hateoas;

public sealed record ControlPlaneTenantPlanListItemDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int LatestVersionNumber { get; init; }
    public int? PublishedVersionNumber { get; init; }
    public decimal PriceAmount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string BillingPeriod { get; init; } = string.Empty;
    public bool IsActiveForProvisioning { get; init; }
}

public sealed record ControlPlaneTenantPlanDetailDto
{
    public Guid Id { get; init; }
    public string Key { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string? Description { get; init; }
    public IReadOnlyList<ControlPlaneTenantPlanVersionDto> Versions { get; init; } = [];
}

public sealed record ControlPlaneTenantPlanVersionDto
{
    public Guid Id { get; init; }
    public int VersionNumber { get; init; }
    public int StatusId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public decimal PriceAmount { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public string BillingPeriod { get; init; } = string.Empty;
    public bool IsActiveForProvisioning { get; init; }
    public IReadOnlyList<ControlPlaneTenantPlanSettingDto> Settings { get; init; } = [];
    public IReadOnlyList<ControlPlaneTenantPlanQuotaDto> Quotas { get; init; } = [];

    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, HalLink>? Links { get; set; }
}

public sealed record ControlPlaneTenantPlanSettingDto
{
    public string Key { get; init; } = string.Empty;
    public string JsonValue { get; init; } = string.Empty;
    public bool IsLocked { get; init; }
}

public sealed record ControlPlaneTenantPlanQuotaDto
{
    public string Key { get; init; } = string.Empty;
    public long Limit { get; init; }
}

public sealed record ControlPlaneTenantPlanAssignmentDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid PlanId { get; init; }
    public string PlanKey { get; init; } = string.Empty;
    public Guid PlanVersionId { get; init; }
    public int VersionNumber { get; init; }
    public int StatusId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public DateTime AssignedAt { get; init; }
    public Guid? AssignedByUserId { get; init; }
}
