// ABOUTME: Control-plane DTOs for per-tenant effective configuration governance.
// ABOUTME: Combines resolved settings, plan assignment, and quota usage without leaking secrets.

namespace Explore.Application.DTOs.ControlPlane;

using System.Text.Json.Serialization;
using Explore.Application.Hateoas;

public sealed class ControlPlaneTenantEffectiveConfigurationDto
{
    public Guid TenantId { get; set; }
    public ControlPlaneTenantPlanAssignmentDto? PlanAssignment { get; set; }
    public IReadOnlyList<ControlPlaneTenantEffectiveSettingDto> Settings { get; set; } = [];
    public IReadOnlyList<ControlPlaneTenantQuotaUsageDto> Quotas { get; set; } = [];
}

public sealed class ControlPlaneTenantEffectiveSettingDto
{
    public string Key { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int SettingValueTypeId { get; set; }
    public string SettingValueTypeCode { get; set; } = string.Empty;
    public string SettingValueTypeName { get; set; } = string.Empty;
    public string ValueSource { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public string? LockSource { get; set; }
    public string? Description { get; set; }
    public bool IsSensitive { get; set; }
    public IReadOnlyList<string> AllowedValues { get; set; } = [];

    [JsonPropertyName("_links")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, HalLink>? Links { get; set; }
}

public sealed class ControlPlaneTenantQuotaUsageDto
{
    public string Key { get; set; } = string.Empty;
    public long Limit { get; set; }
    public long Used { get; set; }
    public long Reserved { get; set; }
    public long Quarantined { get; set; }
    public long Available { get; set; }
    public long ObjectCount { get; set; }
    public string? Provider { get; set; }
    public string Source { get; set; } = string.Empty;
    public DateTime? LastRecalculatedAt { get; set; }
}
