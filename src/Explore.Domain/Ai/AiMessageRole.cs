// ABOUTME: Defines the trusted role values used by persisted AI assistant messages.
// ABOUTME: Separates user, assistant, system, and tool content before provider adaptation.

namespace Explore.Domain.Ai;

public enum AiMessageRole
{
    System = 1,
    User = 2,
    Assistant = 3,
    Tool = 4
}
