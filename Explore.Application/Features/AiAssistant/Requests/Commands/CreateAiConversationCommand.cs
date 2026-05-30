// ABOUTME: Command request for creating a private AI assistant conversation shell.
// ABOUTME: Creation is gated by tenant AI settings and does not call a provider.

using Explore.Application.DTOs.Ai;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

public sealed class CreateAiConversationCommand : IRequest<BaseCommandResponse<Guid>>
{
    public CreateAiConversationRequestDto Conversation { get; set; } = new();
}
