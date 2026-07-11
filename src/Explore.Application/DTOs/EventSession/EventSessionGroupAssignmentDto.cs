// ABOUTME: Read DTO for a session's assignment to a program section, track, devroom, or stage.
// ABOUTME: Carries explicit join payload so clients can render primary group and ordering without exposing EF entities.

using System;

namespace Explore.Application.DTOs.EventSession;

public class EventSessionGroupAssignmentDto
{
    public Guid EventSessionGroupId { get; set; }

    public required string Name { get; set; }

    public string? Slug { get; set; }

    public string? Color { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }
}
