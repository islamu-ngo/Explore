// ABOUTME: Enum mirror for stable registration-requirement subject lookup identities.
// ABOUTME: Defines order, ticket, participant, lead-booker, child, and session applicability.

namespace Explore.Domain.Enums;

public enum RegistrationRequirementSubjectTypeEnum
{
    AllOrders = 1,
    SpecificTicketType = 2,
    EveryParticipant = 3,
    LeadBookerOnly = 4,
    ChildParticipants = 5,
    SpecificSessionSelection = 6
}
