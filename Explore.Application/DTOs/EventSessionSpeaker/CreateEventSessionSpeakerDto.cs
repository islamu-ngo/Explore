// ABOUTME: Request DTO for assigning an actor as a speaker on an event session.
// ABOUTME: API routes stamp EventSessionId and TenantId from trusted management context.

using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public class CreateEventSessionSpeakerDto
{
    public Guid ActorId { get; set; }
    public Guid EventSessionId { get; set; }
    public Guid TenantId { get; set; }
}
