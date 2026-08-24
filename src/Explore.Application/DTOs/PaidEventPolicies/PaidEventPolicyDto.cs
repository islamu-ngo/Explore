// ABOUTME: DTO contracts for instance and tenant paid-event policy revisions.
// ABOUTME: Exposes provider-neutral policy ceilings without leaking persistence row entities.

using System.Text.Json.Serialization;

namespace Explore.Application.DTOs.PaidEventPolicies;

public sealed class PaidEventPolicyDto
{
    [JsonIgnore]
    public Guid Id { get; init; }

    [JsonIgnore]
    public Guid? TenantId { get; init; }
    public int VersionNumber { get; init; }
    public bool IsActive { get; init; }
    public bool IsPaymentsEnabled { get; init; }
    public bool RequiresLocalVerification { get; init; }
    public IReadOnlyList<int> AllowedOrganizerKindIds { get; init; } = [];
    public IReadOnlyList<string> AllowedCurrencyCodes { get; init; } = [];
    public string? DefaultCurrencyCode { get; init; }
    public IReadOnlyList<int> RefundProtectionIds { get; init; } = [];
    public IReadOnlyList<PaidEventPolicyCurrencyRiskLimitDto> CurrencyRiskLimits { get; init; } = [];
    public bool RequiresFirstPaidEventReview { get; init; }
    public int? FarFutureReviewThresholdDays { get; init; }
}

public sealed class TenantPaidEventPolicyConfigurationDto
{
    [JsonIgnore]
    public Guid TenantId { get; init; }
    public PaidEventPolicyDto ActiveInstanceCeiling { get; init; } = default!;
    public PaidEventPolicyDto? ActiveTenantOverride { get; init; }
    public PaidEventPolicyDto EffectivePolicy { get; init; } = default!;
}

public sealed class PaidEventPolicyCurrencyRiskLimitDto
{
    public string CurrencyCode { get; init; } = string.Empty;
    public long? PerEventSalesCeilingMinor { get; init; }
    public int? PerEventSalesCountCeiling { get; init; }
    public long? RollingOrganizerSalesCeilingMinor { get; init; }
    public int? RollingOrganizerSalesCountCeiling { get; init; }
    public int? RollingOrganizerWindowDays { get; init; }
    public long? HighValueReviewThresholdMinor { get; init; }
}

public sealed class RevisePaidEventPolicyDto
{
    public bool IsPaymentsEnabled { get; init; }
    public IReadOnlyList<int> AllowedOrganizerKindIds { get; init; } = [];
    public bool RequiresLocalVerification { get; init; }
    public IReadOnlyList<string> AllowedCurrencyCodes { get; init; } = [];
    public string? DefaultCurrencyCode { get; init; }
    public IReadOnlyList<int> RefundProtectionIds { get; init; } = [];
    public IReadOnlyList<PaidEventPolicyCurrencyRiskLimitDto> CurrencyRiskLimits { get; init; } = [];
    public bool RequiresFirstPaidEventReview { get; init; }
    public int? FarFutureReviewThresholdDays { get; init; }
}
