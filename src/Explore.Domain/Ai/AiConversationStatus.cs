// ABOUTME: Defines lifecycle states for persisted AI assistant conversations.
// ABOUTME: Keeps conversation availability explicit for persistence, API, and HAL policy decisions.

namespace Explore.Domain.Ai;

public enum AiConversationStatus
{
    Active = 1,
    Running = 2,
    Blocked = 3,
    Archived = 4
}
