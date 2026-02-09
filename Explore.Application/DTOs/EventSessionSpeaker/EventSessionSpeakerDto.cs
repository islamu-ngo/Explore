using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public class EventSessionSpeakerDto
{
    public Guid Id { get; set; }
    public Guid ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public Guid EventSessionId { get; set; }
    public string? EventSessionTitle { get; set; }
    public Guid TenantId { get; set; }
}
