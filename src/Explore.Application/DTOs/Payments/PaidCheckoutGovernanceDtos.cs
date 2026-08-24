// ABOUTME: Defines bounded operator contracts for durable sale controls, audits, and paid Checkout reviews.
// ABOUTME: Excludes secrets, connected-account identifiers, and startup-owned official/activation mutation fields.

namespace Explore.Application.DTOs.Payments;

public sealed class PaidCheckoutSaleControlDto
{
    public Guid TenantId { get; init; }
    public Guid? EventId { get; init; }
    public bool IsStopped { get; init; }
    public bool ResumeReviewPending { get; init; }
    public long Version { get; init; }
    public IReadOnlyList<PaidCheckoutSaleControlAuditDto> AuditTrail { get; init; } = [];
}

public sealed class PaidCheckoutSaleControlAuditDto
{
    public int Sequence { get; init; }
    public required string ActionCode { get; init; }
    public required string ReasonCode { get; init; }
    public DateTime OccurredAt { get; init; }
}

public sealed class PaidCheckoutSaleControlMutationDto
{
    public string ReasonCode { get; init; } = string.Empty;
}

public sealed class PaidCheckoutResumeReviewDto
{
    public bool Approved { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}

public sealed class PaidCheckoutReviewApprovalDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid EventId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int TriggerId { get; init; }
    public long? MaximumOrderAmountMinor { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
}

public sealed class RequestPaidCheckoutReviewDto
{
    public int TriggerId { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public long? MaximumOrderAmountMinor { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}

public sealed class DecidePaidCheckoutReviewDto
{
    public bool Approved { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}
