// ABOUTME: Internal command for background AI provider processing of a previously queued run.
// ABOUTME: Keeps request/response queuing separate from long-running provider orchestration.

using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

public sealed class ProcessAiRunCommand : IRequest
{
    public Guid TenantId { get; set; }
    public Guid ConversationId { get; set; }
    public Guid RunId { get; set; }
    public string Mode { get; set; } = AiAssistantInteractionModes.Build;
}
