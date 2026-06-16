// ABOUTME: HATEOAS link policies for event session detail and collection resources.
// ABOUTME: Emits only event session affordances backed by registered API route names.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for EventSessionDto (detail view).
/// </summary>
public sealed class EventSessionDetailLinkPolicy : ILinkPolicy<EventSessionDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(EventSessionDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventSessionById,
            new { id = dto.Id },
            "GET",
            dto.Title ?? "Session details");

        // Parent event link
        yield return new LinkDefinition(
            "event",
            RouteNames.GetEventById,
            new { id = dto.EventId },
            "GET",
            dto.EventTitle);

        // Location link (if has location)
        if (dto.LocationId.HasValue)
        {
            yield return new LinkDefinition(
                "location",
                RouteNames.GetLocationById,
                new { id = dto.LocationId },
                "GET",
                dto.LocationFullName);
        }

        // Agenda items link
        yield return new LinkDefinition(
            "agenda-items",
            RouteNames.GetEventSessionAgendaItems,
            new { sessionId = dto.Id },
            "GET",
            "Session agenda");

        foreach (var assignment in dto.SessionGroups)
        {
            yield return new LinkDefinition(
                LinkRelations.SessionGroups,
                RouteNames.GetEventSessionGroupById,
                new { id = assignment.EventSessionGroupId },
                "GET",
                assignment.Name);
        }

        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateEventSession,
            new { id = dto.Id },
            "PUT",
            "Update session",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSession, dto);

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteEventSession,
            new { id = dto.Id },
            "DELETE",
            "Delete session",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.EventSession, dto);
    }
}

/// <summary>
/// Link policy for EventSessionListDto (collection items).
/// </summary>
public sealed class EventSessionCollectionLinkPolicy : ICollectionLinkPolicy<EventSessionListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(EventSessionListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventSessionById,
            new { id = dto.Id },
            "GET",
            dto.Title ?? "Session");

        // Parent event link
        yield return new LinkDefinition(
            "event",
            RouteNames.GetEventById,
            new { id = dto.EventId },
            "GET",
            dto.EventTitle);

        // Location link (if has location)
        if (dto.LocationId.HasValue)
        {
            yield return new LinkDefinition(
                "location",
                RouteNames.GetLocationById,
                new { id = dto.LocationId },
                "GET",
                dto.LocationFullName);
        }

        foreach (var assignment in dto.SessionGroups)
        {
            yield return new LinkDefinition(
                LinkRelations.SessionGroups,
                RouteNames.GetEventSessionGroupById,
                new { id = assignment.EventSessionGroupId },
                "GET",
                assignment.Name);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateEventSession,
            null,
            "POST",
            "Create new session",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(EventSessionDto), "event_session");
    }
}
