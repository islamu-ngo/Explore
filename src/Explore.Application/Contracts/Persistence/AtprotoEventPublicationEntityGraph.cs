// ABOUTME: Complete tenant-scoped entity graph required to build one public ATProto event record.
// ABOUTME: Keeps repositories entity-first while Application owns privacy filtering and immutable snapshot mapping.

using Explore.Domain;

namespace Explore.Application.Contracts.Persistence;

public sealed record AtprotoEventPublicationEntityGraph(
    Event Event,
    IReadOnlyList<EventLocation> EventLocations,
    IReadOnlyList<EventSession> Sessions,
    IReadOnlyList<EventDay> Days,
    IReadOnlyList<EventSessionGroup> SessionGroups,
    IReadOnlyList<EventSessionGroupSession> SessionGroupSessions,
    IReadOnlyList<EventAgendaItem> AgendaItems,
    IReadOnlyList<EventSessionAgendaItem> SessionAgendaItems,
    IReadOnlyList<EventCategories> Categories,
    IReadOnlyList<EventTags> Tags,
    IReadOnlyList<EventSessionCategory> SessionCategories,
    IReadOnlyList<EventSessionTag> SessionTags,
    IReadOnlyList<EventSessionLanguage> SessionLanguages,
    IReadOnlyList<EventSessionSpeaker> SessionSpeakers,
    IReadOnlyList<EventCustomPropertyDefinition> CustomPropertyDefinitions,
    IReadOnlyList<EventSessionCustomPropertyDefinition> SessionCustomPropertyDefinitions);
