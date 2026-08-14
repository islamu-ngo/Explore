// ABOUTME: DTO contracts for instance and tenant paid-event policy revisions.
// ABOUTME: Exposes provider-neutral policy ceilings without leaking persistence row entities.

namespace Explore.Application.DTOs.PaidEventPolicies;

public sealed class PaidEventPolicyDto
{
    public Guid Id { get; init; }
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

public sealed class PaidEventPolicyCurrencyRiskLimitDto
{
    public string CurrencyCode { get; init; } = string.Empty;
    public long? PerEventSalesCeilingMinor { get; init; }
    public long? RollingOrganizerSalesCeilingMinor { get; init; }
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
