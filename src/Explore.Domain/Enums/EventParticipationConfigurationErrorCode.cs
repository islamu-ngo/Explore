// ABOUTME: Machine-readable error codes for invalid event participation identifiers and combinations.
// ABOUTME: Enables callers to handle Domain validation failures without parsing exception messages.

namespace Explore.Domain.Enums;

public enum EventParticipationConfigurationErrorCode
{
    EventIdRequired = 1,
    TenantIdRequired = 2,
    UnknownParticipationHandlingMode = 3,
    UnknownAdvanceRegistrationObligation = 4,
    UnknownIdentityAccessMode = 5,
    UnknownGuestRecoveryPolicy = 6,
    AdvanceRegistrationObligationNotAllowed = 7,
    IdentityAccessModeRequired = 8,
    IdentityAccessModeMustBeAbsent = 9,
    GuestRecoveryPolicyRequired = 10,
    GuestRecoveryPolicyMustBeAbsent = 11,
    GuestRecoveryPolicyNotAllowed = 12
}
