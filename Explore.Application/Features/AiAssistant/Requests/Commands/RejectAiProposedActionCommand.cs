// ABOUTME: Command contract for rejecting an AI-proposed action without side effects.
// ABOUTME: Uses AI conversation authorization metadata while handlers enforce tenant and user ownership.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

[AuthorizeResource(ResourceKinds.AiConversation, AuthorizationActions.AiConversations.RejectAction)]
public sealed class RejectAiProposedActionCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid ProposedActionId { get; set; }

    string? ISecureRequest.ResourceId => ProposedActionId == Guid.Empty ? null : ProposedActionId.ToString();
}
