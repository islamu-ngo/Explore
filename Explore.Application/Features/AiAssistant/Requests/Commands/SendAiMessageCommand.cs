// ABOUTME: Command contract for sending a guarded user message into an AI conversation run.
// ABOUTME: Returns the queued run id while provider calls and proposed actions remain Application-controlled.

using Explore.Application.DTOs.Ai;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

public sealed class SendAiMessageCommand : IRequest<BaseCommandResponse<Guid>>
{
    public Guid ConversationId { get; set; }
    public SendAiMessageRequestDto Message { get; set; } = new();
}
