// ABOUTME: Persists a domain reference attached to an AI conversation for prompt context and audit.
// ABOUTME: Keeps reference identity tenant-scoped and typed before the Application layer builds prompts.

using Explore.Domain.Interfaces;

namespace Explore.Domain.Ai;

public class AiConversationReference : ITenantEntity
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public AiConversation? Conversation { get; set; }
    public AiReferenceKind Kind { get; set; }
    public Guid ReferenceId { get; set; }
    public required string DisplayName { get; set; }
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
    public Guid? CreatedBy { get; set; }
}
