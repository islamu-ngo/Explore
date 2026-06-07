// ABOUTME: Describes human-approval posture for AI tool UX and generated inventories.
// ABOUTME: Does not grant execution authority or replace confirmation command checks.

namespace Explore.Application.Features.AiAssistant.Tools;

public enum AiToolApprovalMode
{
    NoneRequired = 1,
    HumanConfirmationRequired = 2,
}
