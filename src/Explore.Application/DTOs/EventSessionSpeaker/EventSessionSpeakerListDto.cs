// ABOUTME: List DTO for event-session speaker relationship rows.
// ABOUTME: Includes concurrency metadata for admin list update flows.

using System;

namespace Explore.Application.DTOs.EventSessionSpeaker;

public sealed record EventSessionSpeakerListDto
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
