// ABOUTME: Query request for resolving authenticated AI assistant bootstrap capability metadata.
// ABOUTME: Keeps assistant availability and model selection behind the Application layer.

using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Queries;

public sealed record GetAiAssistantBootstrapQuery : IRequest<AiAssistantBootstrapDto>
{
}
