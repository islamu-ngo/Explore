// ABOUTME: Query request for an authenticated user's AI assistant conversation detail.
// ABOUTME: Handlers must preserve tenant filters and user ownership before returning history.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Queries;

[AuthorizeResource(ResourceKinds.AiConversation, AuthorizationActions.AiConversations.View)]
public sealed record GetAiConversationDetailQuery : IRequest<AiConversationDto?>, ISecureRequest
{
    public Guid ConversationId { get; init; } = default;

    string? ISecureRequest.ResourceId => ConversationId == Guid.Empty ? null : ConversationId.ToString();
}
