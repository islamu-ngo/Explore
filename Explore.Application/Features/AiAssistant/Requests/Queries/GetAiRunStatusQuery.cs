// ABOUTME: Query request for a single AI provider run status in an owned conversation.
// ABOUTME: Supports future polling endpoints without exposing raw provider responses.

using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Queries;

public sealed class GetAiRunStatusQuery : IRequest<AiRunDto?>
{
    public Guid ConversationId { get; init; }
    public Guid RunId { get; init; }
}
