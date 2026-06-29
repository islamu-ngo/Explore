// ABOUTME: List DTO for event-session speaker relationship rows.
// ABOUTME: Includes concurrency metadata for admin list update flows.

using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public class EventSessionSpeakerListDto
{
    public Guid Id { get; set; }
    public Guid ConcurrencyStamp { get; set; }
    public Guid ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public Guid EventSessionId { get; set; }
    public string? EventSessionTitle { get; set; }
}
