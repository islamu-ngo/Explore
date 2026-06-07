// ABOUTME: Describes how assistant UX should handle incomplete or risky tool proposals.
// ABOUTME: Keeps follow-up behavior advisory so validators and handlers remain source of truth.

namespace Explore.Application.Features.AiAssistant.Tools;

public enum AiToolFollowUpPolicy
{
    None = 1,
    AskClarifyingQuestionBeforeProposal = 2,
    ShowWarningsBeforeConfirmation = 3,
}
