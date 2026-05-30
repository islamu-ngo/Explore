// ABOUTME: Query request for an authenticated user's AI assistant conversation detail.
// ABOUTME: Handlers must preserve tenant filters and user ownership before returning history.

using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Queries;

public sealed class GetAiConversationDetailQuery : IRequest<AiConversationDto?>
{
    public Guid ConversationId { get; init; }
}
