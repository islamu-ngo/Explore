// ABOUTME: Defines the client-to-BFF purchase-governance boundary for registration UI.
// ABOUTME: Carries only order lineage, access choice, actor selector, and opaque guest capability.

namespace Explore.Blazor.Client.Contracts.Services;

public interface ITicketPurchaseGovernanceService
{
    Task<TicketPurchaseGovernanceSubmission> ReserveAsync(
        Guid eventId,
        Guid orderId,
        int accessMode,
        Guid? requestedPurchaserActorId,
        string? guestCapability,
        bool authenticated,
        CancellationToken cancellationToken);
}

public sealed record TicketPurchaseGovernanceSubmission(
    bool IsSuccess,
    bool SupportsHardCrossOrderCeiling,
    string EnforcementScopeCode);
