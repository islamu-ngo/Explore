// ABOUTME: Reporter identity categories for event-report intake records.
// ABOUTME: Distinguishes human, anonymous, system, and admin-created reports.

namespace Explore.Domain.Enums;

public enum EventReporterKind
{
    AuthenticatedUser = 1,
    Anonymous = 2,
    System = 3,
    Admin = 4
}
