// ABOUTME: Defines host-neutral tenant effective-configuration models for Control Plane components.
// ABOUTME: Preserves HAL affordances while hiding generated API DTOs from Razor UI flows.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneTenantEffectiveConfiguration(
    Guid TenantId,
    ControlPlaneTenantPlanAssignment? PlanAssignment,
    IReadOnlyList<ControlPlaneTenantEffectiveSetting> Settings,
    IReadOnlyList<ControlPlaneTenantQuotaUsage> Quotas,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;
}

public sealed record ControlPlaneTenantEffectiveSetting(
    string Key,
    string Category,
    string Value,
    int SettingValueTypeId,
    string SettingValueTypeCode,
    string SettingValueTypeName,
    string ValueSource,
    bool IsLocked,
    string? LockSource,
    string? Description,
    bool IsSensitive,
    IReadOnlyList<string> AllowedValues);

public sealed record ControlPlaneTenantQuotaUsage(
    string Key,
    long Limit,
    long Used,
    long Reserved,
    long Quarantined,
    long Available,
    long ObjectCount,
    string? Provider,
    string Source,
    DateTimeOffset? LastRecalculatedAt);
