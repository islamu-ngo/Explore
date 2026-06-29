// ABOUTME: Grouped update DTO for event-session speaker link mutations.
// ABOUTME: Nullable groups allow callers to update the session side or actor side independently.

using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public class UpdateEventSessionSpeakerDto
{
    public UpdateEventSessionSpeakerSessionDto? Session { get; set; }
    public UpdateEventSessionSpeakerActorDto? Actor { get; set; }
}

public class UpdateEventSessionSpeakerSessionDto
{
    public Guid EventSessionId { get; set; }
}

public class UpdateEventSessionSpeakerActorDto
{
    public Guid ActorId { get; set; }
}
