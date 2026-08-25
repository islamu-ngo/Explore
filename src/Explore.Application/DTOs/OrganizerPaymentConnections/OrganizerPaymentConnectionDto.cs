// ABOUTME: Safe bounded DTO for organizer payment-provider connection local state.
// ABOUTME: Exposes actor/provider readiness and replacement metadata without secrets or raw provider payloads.

namespace Explore.Application.DTOs.OrganizerPaymentConnections;

public sealed record OrganizerPaymentConnectionDto
{
    public int StatusId { get; init; }
    public string? MerchantCountryCode { get; init; }
    public int ChargeCapabilityStateId { get; init; }
    public int RequirementsStateId { get; init; }
    public IReadOnlyList<string> SupportedCurrencyCodes { get; init; } = [];
    public DateTime? LastReadinessObservedAt { get; init; }
}
