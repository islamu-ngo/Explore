// ABOUTME: Enum mirror of the RegistrationScope lookup for whole-event, day, and session-selection workflow vocabulary.
// ABOUTME: Values are stable lookup ids consumed by registration policy rules and legacy lookup displays.

namespace Explore.Domain.Enums;

public enum RegistrationScopeEnum
{
    Event = 1,
    Day = 2,
    SessionSelection = 3
}
