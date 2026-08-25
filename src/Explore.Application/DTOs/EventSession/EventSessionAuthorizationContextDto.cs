// ABOUTME: Minimal event-session context returned to API composition before management mutations.
// ABOUTME: Carries only parent event and tenant identifiers needed for resource authorization.

using System;

namespace Explore.Application.DTOs.EventSession;

public sealed record EventSessionAuthorizationContextDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public Guid TenantId { get; init; }
}
