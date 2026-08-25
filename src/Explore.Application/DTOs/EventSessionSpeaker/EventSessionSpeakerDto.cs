// ABOUTME: Detail DTO for an event-session speaker relationship row.
// ABOUTME: Exposes concurrency metadata so clients can submit strong update preconditions.

using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public sealed record EventSessionSpeakerDto
{
    public Guid Id { get; init; }
    public Guid ConcurrencyStamp { get; init; }
    public Guid ActorId { get; init; }
    public string? ActorDisplayName { get; init; }
    public Guid EventSessionId { get; init; }
    public string? EventSessionTitle { get; init; }
    public Guid EventId { get; init; }
    public Guid TenantId { get; init; }
}
