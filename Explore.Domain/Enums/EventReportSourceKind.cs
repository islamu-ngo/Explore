// ABOUTME: Source classifications for reports entering the event-reporting bounded context.
// ABOUTME: Separates user reports from local rules and external provider sync.

namespace Explore.Domain.Enums;

public enum EventReportSourceKind
{
    UserReport = 1,
    LocalRule = 2,
    OspreySignal = 3,
    CoopSync = 4,
    AdminCreated = 5
}
