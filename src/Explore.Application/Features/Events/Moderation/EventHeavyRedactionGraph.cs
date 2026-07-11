// ABOUTME: Domain-entity graph required to irreversibly redact event-owned content during heavy moderation.
// ABOUTME: Keeps the Application redaction service independent from EF Core while avoiding DTO-shaped repository results.

using Explore.Domain;

namespace Explore.Application.Features.Events.Moderation;

public sealed record EventHeavyRedactionGraph(
    Event Event,
    IReadOnlyList<EventSession> Sessions,
    IReadOnlyList<EventDay> Days,
    IReadOnlyList<EventAgendaItem> AgendaItems,
    IReadOnlyList<EventSessionAgendaItem> SessionAgendaItems,
    IReadOnlyList<EventSessionGroup> SessionGroups,
    IReadOnlyList<EventCustomPropertyDefinition> EventCustomPropertyDefinitions,
    IReadOnlyList<EventCustomPropertyProjection> EventCustomPropertyProjections,
    IReadOnlyList<EventSessionCustomPropertyDefinition> SessionCustomPropertyDefinitions,
    IReadOnlyList<EventSessionCustomPropertyProjection> SessionCustomPropertyProjections,
    IReadOnlyList<StorageObject> ImageStorageObjects);
