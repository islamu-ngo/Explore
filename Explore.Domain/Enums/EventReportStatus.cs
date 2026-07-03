// ABOUTME: Review lifecycle states for event reports.
// ABOUTME: Tracks intake, triage, review, resolution, duplicate, escalation, and closure.

namespace Explore.Domain.Enums;

public enum EventReportStatus
{
    Submitted = 1,
    Triaged = 2,
    UnderReview = 3,
    Actioned = 4,
    Dismissed = 5,
    Duplicate = 6,
    Escalated = 7,
    Closed = 8
}
