// ABOUTME: Defines public purchase-governance input and honest enforcement-scope response fields.
// ABOUTME: Excludes tenant, account, enforcement-key, contact-hash, and quantity authority from JSON input.

using Explore.Domain;

namespace Explore.API.Models;

public sealed record ReserveTicketPurchaseRequest
{
    public TicketPurchaseAccessMode AccessMode { get; init; }
    public Guid? RequestedPurchaserActorId { get; init; }
}

public sealed record TicketPurchaseGovernanceResource
{
    public required Guid OrderId { get; init; }
    public required TicketPurchaseAccessMode AccessMode { get; init; }
    public required bool SupportsHardCrossOrderCeiling { get; init; }
    public required string EnforcementScopeCode { get; init; }
}
