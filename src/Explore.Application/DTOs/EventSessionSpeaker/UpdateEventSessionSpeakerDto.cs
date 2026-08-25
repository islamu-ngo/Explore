// ABOUTME: Grouped update DTO for event-session speaker link mutations.
// ABOUTME: Nullable groups allow callers to update the session side or actor side independently.

using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public sealed record UpdateEventSessionSpeakerDto
{
    public UpdateEventSessionSpeakerSessionDto? Session { get; init; }
    public UpdateEventSessionSpeakerActorDto? Actor { get; init; }
}

public sealed record UpdateEventSessionSpeakerSessionDto
{
    public Guid EventSessionId { get; init; }
}

public sealed record UpdateEventSessionSpeakerActorDto
{
    public Guid ActorId { get; init; }
}
