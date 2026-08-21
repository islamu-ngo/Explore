// ABOUTME: Safe authoritative payment projection for purchaser and Studio registration-order surfaces.
// ABOUTME: Excludes provider accounts, request identifiers, idempotency values, capabilities, PII, and raw errors.

namespace Explore.Application.DTOs.RegistrationOrders;

using Explore.Application.Responses;

public sealed class RegistrationPaymentDto
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
}

public sealed class RegistrationPaymentCommandResultDto : BaseCommandResponse<Guid>
{
    public RegistrationPaymentDto? Payment { get; init; }
}

public sealed class RegistrationPaymentCheckoutTargetDto
{
    public required string Url { get; init; }
}
