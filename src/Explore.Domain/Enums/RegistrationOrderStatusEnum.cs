// ABOUTME: Enum mirror for stable RegistrationOrderStatus lookup identities.
// ABOUTME: Includes the explicit terminal rejected outcome for organizer approval workflows.

namespace Explore.Domain.Enums;

public enum RegistrationOrderStatusEnum
{
    Draft = 1,
    AwaitingIdentity = 2,
    AwaitingParticipantDetails = 3,
    AwaitingRequirements = 4,
    ReadyForCheckout = 5,
    AwaitingPayment = 6,
    AwaitingApproval = 7,
    Waitlisted = 8,
    Confirmed = 9,
    Rejected = 10,
    Expired = 11,
    Cancelled = 12,
    NeedsReconciliation = 13
}
