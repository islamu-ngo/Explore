// ABOUTME: Carries the bounded ticket-purchase governance result consumed by registration UI.
// ABOUTME: Exposes only success, enforcement scope, and hard-ceiling support.

namespace Explore.Blazor.Client.Contracts.Services;

public sealed record TicketPurchaseGovernanceSubmission(
    bool IsSuccess,
    bool SupportsHardCrossOrderCeiling,
    string EnforcementScopeCode);
