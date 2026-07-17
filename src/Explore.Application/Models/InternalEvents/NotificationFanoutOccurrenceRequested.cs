// ABOUTME: PII-free general-outbox pointer for deferred notification fanout occurrence processing.
// ABOUTME: Carries only tenant, occurrence, and schema-version identifiers.

namespace Explore.Application.Models.InternalEvents;

public sealed record NotificationFanoutOccurrenceRequested(Guid TenantId, Guid OccurrenceId, int Version)
{
    public const int CurrentVersion = 1;
}
