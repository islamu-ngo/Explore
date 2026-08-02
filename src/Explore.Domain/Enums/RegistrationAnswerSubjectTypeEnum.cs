// ABOUTME: Defines stable subject kinds for atomic registration answers.
// ABOUTME: Distinguishes booking, purchaser, participant, ticket, and session-selection identity.

namespace Explore.Domain.Enums;

public enum RegistrationAnswerSubjectTypeEnum
{
    RegistrationOrder = 1,
    Purchaser = 2,
    Participant = 3,
    TicketAssignment = 4,
    SessionSelection = 5
}
