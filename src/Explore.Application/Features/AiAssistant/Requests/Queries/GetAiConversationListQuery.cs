// ABOUTME: Query request for the authenticated user's recent AI assistant conversations.
// ABOUTME: Returns bounded private history metadata without provider secrets or message bodies.

using Explore.Application.Authorization;
using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Queries;

[AuthorizeResource(ResourceKinds.AiConversation, AuthorizationActions.AiConversations.View)]
public sealed class GetAiConversationListQuery : IRequest<IReadOnlyList<AiConversationSummaryDto>>
{
    public int Limit { get; init; } = 20;
}
