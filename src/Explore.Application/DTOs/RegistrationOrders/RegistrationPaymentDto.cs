// ABOUTME: Safe authoritative payment projection for purchaser and Studio registration-order surfaces.
// ABOUTME: Excludes provider accounts, request identifiers, idempotency values, capabilities, PII, and raw errors.

namespace Explore.Application.DTOs.RegistrationOrders;

using Explore.Application.Responses;

public sealed record RegistrationPaymentDto
{
    public Guid Id { get; init; }
    public Guid RegistrationOrderId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public bool HostedRedirectAvailable { get; init; }
    public bool RetryAvailable { get; init; }
    public string? FailureCode { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public long RefundedAmountMinor { get; init; }
    public long RefundPendingAmountMinor { get; init; }
    public IReadOnlyList<RegistrationRefundDto> Refunds { get; init; } = [];
    public IReadOnlyList<RegistrationPaymentDisputeDto> Disputes { get; init; } = [];
    public IReadOnlyList<RegistrationMaterialChangeChoiceDto> MaterialChangeChoices { get; init; } = [];
    public bool BuyerRefundRequestAvailable { get; init; }
    public bool OrganizerRefundAvailable { get; init; }
    public long CapturedAmountMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int CurrencyMinorUnitDigits { get; init; }
}

public sealed record RegistrationMaterialChangeChoiceDto
{
    public Guid Id { get; init; }
    public Guid CampaignId { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? DecidedAt { get; init; }
}

public sealed record RegistrationMaterialChangeChoiceRequestDto
{
    public Guid CampaignId { get; init; }
    public string ChoiceCode { get; init; } = string.Empty;
}

public sealed class RegistrationMaterialChangeChoiceCommandResultDto : BaseCommandResponse<Guid>
{
    public RegistrationMaterialChangeChoiceDto? Choice { get; init; }
    public RegistrationRefundDto? Refund { get; init; }
}

public sealed record RegistrationRefundDto
{
    internal bool SettlementRetryAvailable { get; init; }
    public bool ShouldAdvertiseSettlementRetry() => SettlementRetryAvailable;
    public Guid Id { get; init; }
    public string StatusCode { get; init; } = string.Empty;
    public string StatusName { get; init; } = string.Empty;
    public string? FailureCode { get; init; }
    public long AmountMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public int AcceptedRefundPolicyVersion { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastObservedAt { get; init; }
    public DateTime? SucceededAt { get; init; }
}

public sealed record RegistrationPaymentDisputeDto
{
    public Guid Id { get; init; }
    public string StageCode { get; init; } = string.Empty;
    public string StatusCode { get; init; } = string.Empty;
    public long AmountMinor { get; init; }
    public string CurrencyCode { get; init; } = string.Empty;
    public DateTime LastObservedAt { get; init; }
    public DateTime? ResponseDueAt { get; init; }
}

public sealed record RegistrationRefundRequestDto
{
    public long? AmountMinor { get; init; }
    public string ReasonCode { get; init; } = string.Empty;
}

public sealed class RegistrationRefundCommandResultDto : BaseCommandResponse<Guid>
{
    public RegistrationRefundDto? Refund { get; init; }
}

public sealed class RegistrationPaymentCommandResultDto : BaseCommandResponse<Guid>
{
    public RegistrationPaymentDto? Payment { get; init; }
}

public sealed record RegistrationPaymentCheckoutTargetDto
{
    public required string Url { get; init; }
}
