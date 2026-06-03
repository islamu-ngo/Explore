// ABOUTME: Lightweight AI reference search result used for bounded event reference discovery.
// ABOUTME: Exposes safe event metadata only; full event body/content stays out of AI reference results.

namespace Explore.Application.DTOs.Ai;

public sealed record AiReferenceSearchResultDto(
    string Kind,
    Guid ReferenceId,
    string DisplayName,
    string? Summary,
    DateOnly? FirstSessionDate,
    DateOnly? LastSessionDate,
    string? EventStatus,
    string? Visibility,
    string? Format);
