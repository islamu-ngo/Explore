// ABOUTME: Request DTO for assigning an actor as a speaker on an event session.
// ABOUTME: API routes stamp EventSessionId and TenantId from trusted management context.

using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public sealed record CreateEventSessionSpeakerDto
{
    public Guid ActorId { get; init; }
    public Guid EventSessionId { get; init; }
}
