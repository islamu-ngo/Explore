// ABOUTME: Request payload for creating an unscheduled draft event session.
// ABOUTME: Keeps draft sessions structurally valid while schedule and publication fields remain lifecycle-gated.

namespace Explore.Application.DTOs.EventSession;

public sealed class CreateDraftEventSessionRequestDto
{
    public Guid EventId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public int SortOrder { get; set; }
}
