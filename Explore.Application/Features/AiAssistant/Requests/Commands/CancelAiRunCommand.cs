// ABOUTME: Command contract for cancelling a queued or in-progress AI provider run.
// ABOUTME: Keeps run cancellation authenticated and scoped to the owning AI conversation.

using Explore.Application.Authorization;
using Explore.Application.Responses;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Commands;

[AuthorizeResource(ResourceKinds.AiConversation, AuthorizationActions.AiConversations.CancelRun)]
public sealed class CancelAiRunCommand : IRequest<BaseCommandResponse<Guid>>, ISecureRequest
{
    public Guid ConversationId { get; init; }
    public Guid RunId { get; init; }

    string? ISecureRequest.ResourceId => ConversationId == Guid.Empty ? null : ConversationId.ToString();
}
