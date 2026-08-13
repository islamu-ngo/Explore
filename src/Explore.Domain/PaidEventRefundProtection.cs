// ABOUTME: Names the mandatory paid-event refund protection situations from ADR-022 and I-VSD.
// ABOUTME: Keeps Phase 16.1 refund floors as policy facts without modeling refund workflows.

namespace Explore.Domain;

public enum PaidEventRefundProtection
{
    OrganizerCancellationFullRefund = 1,
    MaterialChangeBuyerChoiceOrFullRefund = 2,
    DuplicateOrIncorrectChargeFullRefund = 3,
    SubstantialNonDeliveryRemedy = 4,
    AttendeeBuyerChangeTermsDisclosedSubjectToLaw = 5,
    CardDisputeRightsNotWaived = 6,
    CancelledEventPlatformAmountsRefundedByDefault = 7
}
