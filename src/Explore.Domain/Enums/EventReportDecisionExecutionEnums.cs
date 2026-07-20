// ABOUTME: Stable lifecycle and enforcement-receipt identifiers for report-decision execution.
// ABOUTME: Separates side-effect claiming from atomic business and notification completion.

namespace Explore.Domain.Enums;

public enum EventReportDecisionExecutionState
{
    Requested = 1,
    InProgress = 2,
    CompletionPending = 3,
    Completed = 4
}

public enum EventReportDecisionEnforcementReceiptKind
{
    None = 0,
    NoAction = 1,
    LightModeration = 2,
    HeavyRedaction = 3,
    OrganizerWarning = 4,
    NonTerminal = 5
}
