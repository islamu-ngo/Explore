// ABOUTME: Minimal event-session context returned to API composition before management mutations.
// ABOUTME: Carries only parent event and tenant identifiers needed for resource authorization.

using System;

namespace Explore.Application.DTOs.EventSession;

public sealed class EventSessionAuthorizationContextDto
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }
    public Guid TenantId { get; set; }
}
