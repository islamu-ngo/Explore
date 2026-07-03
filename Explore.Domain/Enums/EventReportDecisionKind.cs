// ABOUTME: Decision outcomes available to report reviewers.
// ABOUTME: Keeps review results separate from existing moderation enforcement records.

namespace Explore.Domain.Enums;

public enum EventReportDecisionKind
{
    NoViolation = 1,
    Duplicate = 2,
    NeedsMoreInfo = 3,
    Escalate = 4,
    LightModerate = 5,
    HeavyRedact = 6,
    WarnOrganizer = 7
}
