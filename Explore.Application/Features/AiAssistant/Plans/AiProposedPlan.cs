// ABOUTME: Represents a proposal-only AI plan preview for one tenant-scoped conversation.
// ABOUTME: Keeps multi-step assistant suggestions outside execution until existing confirmation flows run.

namespace Explore.Application.Features.AiAssistant.Plans;

public sealed record AiProposedPlan(
    Guid TenantId,
    Guid ConversationId,
    IReadOnlyList<AiProposedPlanStep> Steps);
