// ABOUTME: Request contract for creating a program section, track, devroom, or stage within an event.
// ABOUTME: TenantId is intentionally omitted; handlers derive it from the parent event under tenant filters.

namespace Explore.Application.DTOs.EventSessionGroup;

public sealed record CreateEventSessionGroupRequestDto
{
    public Guid EventId { get; init; }
    public required string Name { get; init; }
    public string? Slug { get; init; }
    public string? Description { get; init; }
    public Guid? LocationId { get; init; }
    public Guid? RoomId { get; init; }
    public string? Color { get; init; }
    public int SortOrder { get; init; }
    public bool IsPublished { get; init; }
}
