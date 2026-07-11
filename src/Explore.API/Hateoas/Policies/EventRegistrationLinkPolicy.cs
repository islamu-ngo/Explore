// ABOUTME: HATEOAS link policies for event registration detail and collection resources.
// ABOUTME: Emits registration, event/session, and ATProto affordances backed by registered route names.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for EventRegistrationDto (detail view).
/// Provides links for event registration operations.
/// </summary>
public sealed class EventRegistrationDetailLinkPolicy : ILinkPolicy<EventRegistrationDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(EventRegistrationDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventRegistrationById,
            new { id = dto.Id },
            "GET",
            dto.EventTitle ?? dto.EventSessionTitle ?? "Event registration");

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetEventRegistrations,
            null,
            "GET",
            "All registrations");

        // Event session link
        yield return new LinkDefinition(
            "event-session",
            RouteNames.GetEventSessionById,
            new { id = dto.EventSessionId },
            "GET",
            dto.EventSessionTitle);

        // ATProto record link (if federated)
        if (dto.AtprotoRecordId.HasValue)
        {
            yield return new LinkDefinition(
                "atproto-record",
                RouteNames.GetAtprotoRecordEntryById,
                new { id = dto.AtprotoRecordId },
                "GET",
                "ATProto record");
        }

        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateEventRegistration,
            new { id = dto.Id },
            "PATCH",
            "Update registration",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventRegistration, dto);

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteEventRegistration,
            new { id = dto.Id },
            "DELETE",
            "Cancel registration",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.EventRegistration, dto);
    }
}

/// <summary>
/// Link policy for EventRegistrationListDto in collection context.
/// </summary>
public sealed class EventRegistrationCollectionLinkPolicy : ICollectionLinkPolicy<EventRegistrationListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(EventRegistrationListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventRegistrationById,
            new { id = dto.Id },
            "GET",
            dto.EventTitle ?? dto.EventSessionTitle ?? "Event registration");

        // Event session link
        yield return new LinkDefinition(
            "event-session",
            RouteNames.GetEventSessionById,
            new { id = dto.EventSessionId },
            "GET",
            dto.EventSessionTitle);

        // Event link
        yield return new LinkDefinition(
            "event",
            RouteNames.GetEventById,
            new { id = dto.EventId },
            "GET",
            dto.EventTitle);
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateEventRegistration,
            null,
            "POST",
            "Register for event",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(EventRegistrationDto), "event_registration");
    }
}
