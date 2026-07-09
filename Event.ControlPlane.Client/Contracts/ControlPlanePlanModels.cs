// ABOUTME: Defines host-neutral tenant-plan governance models for the shared control-plane client.
// ABOUTME: Preserves HAL affordances while keeping Razor components independent from generated API DTOs.

namespace Event.ControlPlane.Client.Contracts;

public sealed record ControlPlaneTenantPlanList(
    IReadOnlyList<ControlPlaneTenantPlanSummary> Items,
    int TotalCount,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;

    public static ControlPlaneTenantPlanList Empty() => new([], 0);
}

public sealed record ControlPlaneTenantPlanSummary(
    Guid Id,
    string Key,
    string DisplayName,
    string? Description,
    int LatestVersionNumber,
    int? PublishedVersionNumber,
    decimal PriceAmount,
    string CurrencyCode,
    string BillingPeriod,
    bool IsActiveForProvisioning,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;
}

public sealed record ControlPlaneTenantPlanDetail(
    Guid Id,
    string Key,
    string DisplayName,
    string? Description,
    IReadOnlyList<ControlPlaneTenantPlanVersion> Versions,
    IReadOnlyDictionary<string, ControlPlaneHalLink>? Links = null) : IControlPlaneHalResource
{
    public IReadOnlyDictionary<string, ControlPlaneHalLink> Links { get; init; } = Links ?? ControlPlaneHal.EmptyLinks;
}

public sealed record ControlPlaneTenantPlanVersion(
    Guid Id,
    int VersionNumber,
    int StatusId,
    string StatusCode,
    decimal PriceAmount,
    string CurrencyCode,
    string BillingPeriod,
    bool IsActiveForProvisioning,
    IReadOnlyList<ControlPlaneTenantPlanSetting> Settings,
    IReadOnlyList<ControlPlaneTenantPlanQuota> Quotas);

public sealed record ControlPlaneTenantPlanSetting(
    string Key,
    string JsonValue,
    bool IsLocked);

public sealed record ControlPlaneTenantPlanQuota(
    string Key,
    long Limit);

public sealed record ControlPlaneTenantPlanDraft(
    string Key,
    string Name,
    ControlPlaneTenantPlanPricing Pricing,
    bool IsActiveForProvisioning,
    IReadOnlyList<ControlPlaneTenantPlanSettingOverride> SettingOverrides,
    IReadOnlyList<ControlPlaneTenantPlanQuotaLimit> QuotaLimits);

public sealed record ControlPlaneTenantPlanPricing(
    decimal Amount,
    string CurrencyCode,
    string BillingPeriod);

public sealed record ControlPlaneTenantPlanSettingOverride(
    string Key,
    string JsonValue,
    bool IsLocked);

public sealed record ControlPlaneTenantPlanQuotaLimit(
    string Key,
    long Limit);

public sealed record ControlPlaneTenantPlanEffectiveConfiguration(
    IReadOnlyList<ControlPlaneTenantPlanEffectiveSetting> Settings);

public sealed record ControlPlaneTenantPlanEffectiveSetting(
    string Key,
    string JsonValue,
    bool IsLocked);

public sealed record ControlPlaneTenantPlanValidationResult(
    IReadOnlyList<ControlPlaneTenantPlanValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;
}

public sealed record ControlPlaneTenantPlanValidationError(
    string Code,
    string Target,
    string Message);

public sealed record ControlPlaneTenantPlanDiffResult(
    IReadOnlyList<ControlPlaneTenantPlanSettingChange> SettingChanges);

public sealed record ControlPlaneTenantPlanSettingChange(
    string Key,
    ControlPlaneTenantPlanChangeType ChangeType,
    string? BeforeValue,
    string? AfterValue,
    bool LockChanged);

public sealed record ControlPlaneTenantPlanAssignment(
    Guid Id,
    Guid TenantId,
    Guid PlanId,
    string PlanKey,
    Guid PlanVersionId,
    int VersionNumber,
    int StatusId,
    string StatusCode,
    DateTimeOffset AssignedAt,
    Guid? AssignedByUserId);

public enum ControlPlaneTenantPlanExistingAssignmentPolicy
{
    LeaveExistingTenantsPinned = 0,
    MoveExistingTenantsToPublishedVersion = 1
}

public enum ControlPlaneTenantPlanChangeType
{
    Added = 0,
    Changed = 1
}
