// ABOUTME: Captures the safe validation result for one AI plan-preview step.
// ABOUTME: Includes risk, approval, warning, and next-action metadata for proposal UI rendering.

using Explore.Application.Features.AiAssistant.Tools;
using Explore.Domain.Ai;

namespace Explore.Application.Features.AiAssistant.Plans;

public sealed record AiPlanValidatedStep(
    string StepId,
    AiProposedActionKind Kind,
    string ToolName,
    AiPlanStepStatus Status,
    AiToolRiskClass RiskClass,
    AiToolApprovalMode ApprovalMode,
    bool CanRequestConfirmation,
    bool ExecutionAuthorityGranted,
    string? FailureCode,
    string? FailureMessage,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<string> NextActions);
