// ABOUTME: Query contract for bounded AI reference search across tenant-visible event references.
// ABOUTME: Keeps provider prompt candidates lightweight and leaves authorization/data shaping in Application handlers.

using Explore.Application.DTOs.Ai;
using MediatR;

namespace Explore.Application.Features.AiAssistant.Requests.Queries;

public sealed record SearchAiReferencesQuery : IRequest<IReadOnlyList<AiReferenceSearchResultDto>>
{
    public string SearchTerm { get; init; } = string.Empty;
    public int Limit { get; init; } = 10;
}
