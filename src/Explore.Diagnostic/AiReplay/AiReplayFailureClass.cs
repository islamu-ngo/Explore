// ABOUTME: Classifies deterministic AI replay scenario failures for triage reports.
// ABOUTME: Avoids content-bearing diagnostics while preserving actionable failure categories.

namespace Explore.Diagnostic.AiReplay;

public enum AiReplayFailureClass
{
    None = 0,
    CatalogAuthorization = 1,
    ProposalValidation = 2,
    Recovery = 3,
    Redaction = 4,
    SideEffectSafety = 5
}
