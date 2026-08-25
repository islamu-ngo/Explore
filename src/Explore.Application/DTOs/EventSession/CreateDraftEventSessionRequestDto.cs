// ABOUTME: Request payload for creating an unscheduled draft event session.
// ABOUTME: Keeps draft sessions structurally valid while schedule and publication fields remain lifecycle-gated.

namespace Explore.Application.DTOs.EventSession;

public sealed record CreateDraftEventSessionRequestDto
{
    public Guid EventId { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public int SortOrder { get; init; }
}
