// ABOUTME: Supported target categories for event reports.
// ABOUTME: Starts with event-level reports while preserving future target extensibility.

namespace Explore.Domain.Enums;

public enum EventReportTargetKind
{
    Event = 1,
    EventSession = 2,
    EventImage = 3,
    EventAgendaItem = 4,
    CustomProperty = 5,
    OrganizerActor = 6
}
