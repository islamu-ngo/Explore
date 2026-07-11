// ABOUTME: Command contract for creating an AI proposed action from a governed tool payload.
// ABOUTME: External adapters use this to persist proposals without executing mutating side effects.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

[AuthorizeResource(ResourceKinds.AiConversation, AuthorizationActions.AiConversations.ProposeAction)]
public sealed class ProposeAiToolActionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid ConversationId { get; set; }
    public string ToolName { get; set; } = string.Empty;
    public string PayloadJson { get; set; } = string.Empty;
    public string? Summary { get; set; }

    string? ISecureRequest.ResourceId => ConversationId == Guid.Empty ? null : ConversationId.ToString();
}
