// ABOUTME: Centralizes exhaustive legal transitions for the pre-payment registration-order state machine.
// ABOUTME: Keeps rejected, confirmed, expired, and cancelled orders terminal before persistence executes transitions.

using Explore.Domain.Enums;

namespace Explore.Domain.Services.Registration;

public static class RegistrationOrderRules
{
    public static bool IsTerminal(RegistrationOrderStatusEnum status) => status is
        RegistrationOrderStatusEnum.Confirmed or
        RegistrationOrderStatusEnum.Rejected or
        RegistrationOrderStatusEnum.Expired or
        RegistrationOrderStatusEnum.Cancelled;

    public static bool IsTerminalForCurrentWorkstream(RegistrationOrderStatusEnum status) =>
        IsTerminal(status) || status == RegistrationOrderStatusEnum.AwaitingPayment;

    public static RegistrationOrderStatusEnum GetCheckoutDestination(long totalDueMinor)
    {
        if (totalDueMinor < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalDueMinor));
        }

        return totalDueMinor == 0
            ? RegistrationOrderStatusEnum.Confirmed
            : RegistrationOrderStatusEnum.AwaitingPayment;
    }

    public static bool CanTransition(RegistrationOrderStatusEnum current, RegistrationOrderStatusEnum desired)
    {
        if (!Enum.IsDefined(current) || !Enum.IsDefined(desired))
        {
            return false;
        }

        return current == desired || (current, desired) switch
        {
            (RegistrationOrderStatusEnum.Draft, RegistrationOrderStatusEnum.AwaitingIdentity or RegistrationOrderStatusEnum.AwaitingParticipantDetails or RegistrationOrderStatusEnum.AwaitingRequirements or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled) => true,
            (RegistrationOrderStatusEnum.AwaitingIdentity, RegistrationOrderStatusEnum.AwaitingParticipantDetails or RegistrationOrderStatusEnum.AwaitingRequirements or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled or RegistrationOrderStatusEnum.NeedsReconciliation) => true,
            (RegistrationOrderStatusEnum.AwaitingParticipantDetails, RegistrationOrderStatusEnum.AwaitingIdentity or RegistrationOrderStatusEnum.AwaitingRequirements or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled or RegistrationOrderStatusEnum.NeedsReconciliation) => true,
            (RegistrationOrderStatusEnum.AwaitingRequirements, RegistrationOrderStatusEnum.AwaitingIdentity or RegistrationOrderStatusEnum.AwaitingParticipantDetails or RegistrationOrderStatusEnum.ReadyForCheckout or RegistrationOrderStatusEnum.AwaitingApproval or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled or RegistrationOrderStatusEnum.NeedsReconciliation) => true,
            (RegistrationOrderStatusEnum.ReadyForCheckout, RegistrationOrderStatusEnum.AwaitingRequirements or RegistrationOrderStatusEnum.AwaitingPayment or RegistrationOrderStatusEnum.AwaitingApproval or RegistrationOrderStatusEnum.Waitlisted or RegistrationOrderStatusEnum.Confirmed or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled or RegistrationOrderStatusEnum.NeedsReconciliation) => true,
            (RegistrationOrderStatusEnum.AwaitingPayment, RegistrationOrderStatusEnum.Cancelled or RegistrationOrderStatusEnum.NeedsReconciliation) => true,
            (RegistrationOrderStatusEnum.AwaitingApproval, RegistrationOrderStatusEnum.ReadyForCheckout or RegistrationOrderStatusEnum.Confirmed or RegistrationOrderStatusEnum.Rejected or RegistrationOrderStatusEnum.Waitlisted or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled or RegistrationOrderStatusEnum.NeedsReconciliation) => true,
            (RegistrationOrderStatusEnum.Waitlisted, RegistrationOrderStatusEnum.AwaitingApproval or RegistrationOrderStatusEnum.Confirmed or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled or RegistrationOrderStatusEnum.NeedsReconciliation) => true,
            (RegistrationOrderStatusEnum.NeedsReconciliation, RegistrationOrderStatusEnum.AwaitingIdentity or RegistrationOrderStatusEnum.AwaitingParticipantDetails or RegistrationOrderStatusEnum.AwaitingRequirements or RegistrationOrderStatusEnum.ReadyForCheckout or RegistrationOrderStatusEnum.AwaitingPayment or RegistrationOrderStatusEnum.AwaitingApproval or RegistrationOrderStatusEnum.Waitlisted or RegistrationOrderStatusEnum.Confirmed or RegistrationOrderStatusEnum.Rejected or RegistrationOrderStatusEnum.Expired or RegistrationOrderStatusEnum.Cancelled) => true,
            _ => false
        };
    }

    public static void EnsureCanTransition(RegistrationOrderStatusEnum current, RegistrationOrderStatusEnum desired)
    {
        if (!CanTransition(current, desired))
        {
            throw new InvalidOperationException($"Registration order cannot transition from {current} to {desired}.");
        }
    }
}
