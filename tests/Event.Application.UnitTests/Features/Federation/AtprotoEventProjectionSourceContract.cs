// ABOUTME: Reflects the explicit Domain source-type allowlist behind the exhaustive ATProto event projection manifest.
// ABOUTME: Makes any added or omitted public source property fail until its disposition is independently reviewed.

using System.Collections.Immutable;
using System.Reflection;
using Explore.Application.Contracts.LocationPrivacy;
using Explore.Domain;

namespace Event.Application.UnitTests.Features.Federation;

internal static class AtprotoEventProjectionSourceContract
{
    private static readonly ImmutableArray<(Type Type, string Prefix)> SourceTypes =
    [
        (typeof(Explore.Domain.Event), "Event"),
        (typeof(Actor), "Event.Actor"),
        (typeof(ActorPii), "Event.Actor.Pii"),
        (typeof(Organization), "Event.Actor.Organization"),
        (typeof(OrganizationPii), "Event.Actor.Organization.Pii"),
        (typeof(Group), "Event.Actor.Group"),
        (typeof(Explore.Domain.EventSeries), "Event.EventSeries"),
        (typeof(EventIslamicAspect), "Event.IslamicAspect"),
        (typeof(EventTechAspect), "Event.TechAspect"),
        (typeof(EventLocation), "EventLocation"),
        (typeof(Location), "Location"),
        (typeof(LocationPii), "LocationPii"),
        (typeof(LocationRoom), "LocationRoom"),
        (typeof(EventLocationDisclosureResult), "EventLocationDisclosureResult"),
        (typeof(EventLocationDisclosureValues), "EventLocationDisclosureResult.Values"),
        (typeof(EventDay), "EventDay"),
        (typeof(EventSession), "EventSession"),
        (typeof(EventSessionIslamicAspect), "EventSession.IslamicAspect"),
        (typeof(EventSessionGroup), "EventSessionGroup"),
        (typeof(EventSessionGroupSession), "EventSessionGroupSession"),
        (typeof(EventAgendaItem), "EventAgendaItem"),
        (typeof(EventSessionAgendaItem), "EventSessionAgendaItem"),
        (typeof(EventCategories), "EventCategoryLink"),
        (typeof(EventTags), "EventTagLink"),
        (typeof(Category), "Event.Category"),
        (typeof(Tag), "Event.Tag"),
        (typeof(EventSessionCategory), "EventSessionCategoryLink"),
        (typeof(EventSessionTag), "EventSessionTagLink"),
        (typeof(EventSessionLanguage), "EventSessionLanguageLink"),
        (typeof(Language), "EventSession.Language"),
        (typeof(EventSessionSpeaker), "EventSession.Speaker"),
        (typeof(Actor), "EventSession.Speaker.Actor"),
        (typeof(ActorPii), "EventSession.Speaker.Actor.Pii"),
        (typeof(EventCustomPropertyDefinition), "EventCustomPropertyDefinition"),
        (typeof(EventCustomPropertyOption), "EventCustomPropertyOption"),
        (typeof(EventCustomPropertyValue), "EventCustomPropertyValue"),
        (typeof(EventSessionCustomPropertyDefinition), "EventSessionCustomPropertyDefinition"),
        (typeof(EventSessionCustomPropertyOption), "EventSessionCustomPropertyOption"),
        (typeof(EventSessionCustomPropertyValue), "EventSessionCustomPropertyValue"),
        (typeof(StorageObject), "StorageObject")
    ];

    private static readonly ImmutableArray<string> ConditionalPolicyPaths =
    [
        "EventCustomPropertyDefinition.ExposureLevel!=Public",
        "EventSessionCustomPropertyDefinition.ExposureLevel!=Public",
        "StorageObject.Visibility!=public_image",
        "StorageObject.LifecycleState!=active"
    ];

    public static ImmutableArray<string> SourcePaths { get; } =
    [
        .. SourceTypes.SelectMany(source => source.Type
            .GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Select(property => $"{source.Prefix}.{property.Name}")),
        .. ConditionalPolicyPaths
    ];
}
