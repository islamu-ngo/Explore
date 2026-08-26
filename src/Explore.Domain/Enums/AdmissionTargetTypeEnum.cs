// ABOUTME: Defines the exact published schedule scopes that can receive admission facts.
// ABOUTME: Stable values distinguish whole-event, event-day, and event-session targets.

namespace Explore.Domain.Enums;

public enum AdmissionTargetTypeEnum
{
    Event = 1,
    EventDay = 2,
    EventSession = 3
}
