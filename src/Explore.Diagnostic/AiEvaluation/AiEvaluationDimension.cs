// ABOUTME: Enumerates advisory AI evaluation dimensions tracked for ATCR provider hardening.
// ABOUTME: Keeps report categories stable without depending on live model-provider scoring.

namespace Explore.Diagnostic.AiEvaluation;

public enum AiEvaluationDimension
{
    ToolProposalCorrectness,
    RefusalSafety,
    PromptInjectionResistance,
    Groundedness,
    McpProposalFlow,
    EventDraftRegression,
}
