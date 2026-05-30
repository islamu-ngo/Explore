// ABOUTME: DTO contracts for AI assistant conversations, messages, runs, references, and actions.
// ABOUTME: Shapes private assistant history without exposing provider secrets or raw infrastructure errors.

namespace Explore.Application.DTOs.Ai;

public sealed class CreateAiConversationRequestDto
{
    public string? Title { get; set; }
    public Guid? ActorId { get; set; }
}

public sealed class SendAiMessageRequestDto
{
    public string Content { get; set; } = string.Empty;
    public string IdempotencyKey { get; set; } = string.Empty;
}

public class AiConversationSummaryDto
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid? ActorId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? Provider { get; set; }
    public string? ModelId { get; set; }
    public string? BlockedReason { get; set; }
    public long LastMessageSequence { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public sealed class AiConversationDto : AiConversationSummaryDto
{
    public IReadOnlyList<AiMessageDto> Messages { get; set; } = [];
    public IReadOnlyList<AiRunDto> Runs { get; set; } = [];
    public IReadOnlyList<AiConversationReferenceDto> References { get; set; } = [];
    public IReadOnlyList<AiProposedActionDto> ProposedActions { get; set; } = [];
}

public sealed class AiMessageDto
{
    public Guid Id { get; set; }
    public long Sequence { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class AiRunDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty;
    public DateTime QueuedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
}

public sealed class AiConversationReferenceDto
{
    public Guid Id { get; set; }
    public string Kind { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class AiProposedActionDto
{
    public Guid Id { get; set; }
    public Guid? MessageId { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public Guid? ConfirmedBy { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public Guid? RejectedBy { get; set; }
    public DateTime? RejectedAt { get; set; }
    public Guid? ResultResourceId { get; set; }
    public string? FailureCode { get; set; }
    public string? FailureMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}
