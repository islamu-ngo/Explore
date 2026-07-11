// ABOUTME: Defines preview-only lifecycle states for AI proposed plan steps.
// ABOUTME: Separates review and confirmation readiness from side-effect execution.

namespace Explore.Application.Features.AiAssistant.Plans;

public enum AiPlanStepStatus
{
    Proposed = 1,
    RequiresClarification = 2,
    ReadyForConfirmation = 3,
    Blocked = 4,
    Confirmed = 5,
    Executing = 6,
    Executed = 7,
    Failed = 8
}
