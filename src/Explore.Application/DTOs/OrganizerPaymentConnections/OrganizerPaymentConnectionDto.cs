// ABOUTME: Safe bounded DTO for organizer payment-provider connection local state.
// ABOUTME: Exposes actor/provider readiness and replacement metadata without secrets or raw provider payloads.

namespace Explore.Application.DTOs.OrganizerPaymentConnections;

public sealed class OrganizerPaymentConnectionDto
{
    public Guid Id { get; init; }
    public Guid TenantId { get; init; }
    public Guid OrganizerActorId { get; init; }
    public string ProviderCode { get; init; } = string.Empty;
    public string ConnectPlatformId { get; init; } = string.Empty;
    public string ExternalAccountId { get; init; } = string.Empty;
    public int StatusId { get; init; }
    public string? MerchantCountryCode { get; init; }
    public int ChargeCapabilityStateId { get; init; }
    public int RequirementsStateId { get; init; }
    public IReadOnlyList<string> SupportedCurrencyCodes { get; init; } = [];
    public DateTime? LastReadinessObservedAt { get; init; }
    public string? LastReadinessEvidenceRevision { get; init; }
    public Guid? ReplacesConnectionId { get; init; }
    public Guid? ReplacedByConnectionId { get; init; }
    public DateTime? ReplacedAt { get; init; }
    public DateTime? DisabledAt { get; init; }
    public string? DisabledReasonCode { get; init; }
}
