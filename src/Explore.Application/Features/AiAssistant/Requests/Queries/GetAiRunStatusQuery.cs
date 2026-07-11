// ABOUTME: Query request for a single AI provider run status in an owned conversation.
// ABOUTME: Supports future polling endpoints without exposing raw provider responses.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Queries;

[AuthorizeResource(ResourceKinds.AiConversation, AuthorizationActions.AiConversations.View)]
public sealed class GetAiRunStatusQuery : IRequest<AiRunDto?>, ISecureRequest
{
    public Guid ConversationId { get; init; }
    public Guid RunId { get; init; }

    string? ISecureRequest.ResourceId => ConversationId == Guid.Empty ? null : ConversationId.ToString();
}
