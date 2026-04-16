// ABOUTME: Enum mirror of the EventRegistrationPolicy lookup controlling which registration scopes an organizer allows.
// ABOUTME: Values are stable lookup ids; read alongside EventRegistration intent scopes (Event, Day, SessionSelection).

namespace Explore.Domain.Enums;

public enum EventRegistrationPolicyEnum
{
    WholeEventOnly = 1,
    WholeDayOnly = 2,
    SessionSelectionOnly = 3,
    WholeEventOrDay = 4,
    WholeEventOrSession = 5,
    Flexible = 6
}
