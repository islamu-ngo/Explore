// ABOUTME: Local moderation queue states for report cases.
// ABOUTME: Separates open, assigned, waiting, decision-ready, and closed case work.

namespace Explore.Domain.Enums;

public enum EventReportCaseStatus
{
    Open = 1,
    Assigned = 2,
    WaitingExternal = 3,
    WaitingReporter = 4,
    DecisionReady = 5,
    Closed = 6
}
