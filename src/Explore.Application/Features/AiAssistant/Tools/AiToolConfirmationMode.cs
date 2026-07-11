// ABOUTME: Defines confirmation posture for AI tools before any mutation can execute.
// ABOUTME: Makes mutating tool confirmation explicit in registry metadata instead of UI assumptions.

namespace Explore.Application.Features.AiAssistant.Tools;

public enum AiToolConfirmationMode
{
    Required = 1,
    NotRequired = 2
}
