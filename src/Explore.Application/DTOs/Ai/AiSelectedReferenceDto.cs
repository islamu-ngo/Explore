// ABOUTME: Minimal selected-reference contract for future AI prompt packing.
// ABOUTME: Carries only bounded display metadata so prompt context cannot leak full event content.

namespace Explore.Application.DTOs.Ai;

public sealed record AiSelectedReferenceDto(
    string Kind,
    Guid ReferenceId,
    string DisplayName,
    string? Summary);
