// ABOUTME: Persists an ordered message in an AI assistant conversation.
// ABOUTME: Stores bounded role/content metadata without provider SDK dependencies.

using Explore.Domain.Interfaces;

namespace Explore.Domain.Ai;

public class AiMessage : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public AiConversation? Conversation { get; set; }
    public long Sequence { get; set; }
    public AiMessageRole Role { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
