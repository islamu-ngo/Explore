// ABOUTME: Command contract for confirming an AI-proposed action before tool execution.
// ABOUTME: Uses AI conversation authorization metadata while handlers enforce tenant and user ownership.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

[AuthorizeResource(ResourceKinds.AiConversation, AuthorizationActions.AiConversations.ConfirmAction)]
public sealed class ConfirmAiProposedActionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid ProposedActionId { get; set; }

    string? ISecureRequest.ResourceId => ProposedActionId == Guid.Empty ? null : ProposedActionId.ToString();
}
