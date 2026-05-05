namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Event;
using Explore.Application.DTOs.EventRegistration;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;

/// <summary>
/// Link policy for EventDto (detail view).
/// Generates links based on event state and user authorization.
/// </summary>
public sealed class EventDetailLinkPolicy : ILinkPolicy<EventDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(EventDto dto, ClaimsPrincipal? user)
    {
        // Self link - always included
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventById,
            new { id = dto.Id },
            "GET",
            "Event details");

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetEvents,
            null,
            "GET",
            "All events");

        // Sessions link - event has sessions
        yield return new LinkDefinition(
            LinkRelations.Sessions,
            RouteNames.GetEventSessions,
            new { eventId = dto.Id },
            "GET",
            $"Event sessions ({dto.SessionCount ?? 0})");

        yield return new LinkDefinition(
            LinkRelations.SessionGroups,
            RouteNames.GetEventSessionGroupsByEvent,
            new { eventId = dto.Id },
            "GET",
            "Program sections");


        // Categories link
        yield return new LinkDefinition(
            "categories",
            RouteNames.GetEventCategories,
            new { eventId = dto.Id },
            "GET",
            "Event categories");

        // Tags link
        yield return new LinkDefinition(
            "tags",
            RouteNames.GetEventTags,
            new { eventId = dto.Id },
            "GET",
            "Event tags");

        // Aspect links - conditionally included based on available aspects
        if (dto.AvailableAspects?.Contains("Islamic") == true || dto.IslamicAspect != null)
        {
            yield return new LinkDefinition(
                "islamic-aspect",
                RouteNames.GetEventIslamicAspect,
                new { id = dto.Id },
                "GET",
                "Islamic aspect details");
        }
        else
        {
            // Even if aspect doesn't exist, provide link to create it
            yield return new LinkDefinition(
                "islamic-aspect:create",
                RouteNames.UpsertEventIslamicAspect,
                new { id = dto.Id },
                "PUT",
                "Add Islamic aspect",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Event, dto);
        }

        if (dto.AvailableAspects?.Contains("Tech") == true || dto.TechAspect != null)
        {
            yield return new LinkDefinition(
                "tech-aspect",
                RouteNames.GetEventTechAspect,
                new { id = dto.Id },
                "GET",
                "Tech aspect details");
        }
        else
        {
            // Even if aspect doesn't exist, provide link to create it
            yield return new LinkDefinition(
                "tech-aspect:create",
                RouteNames.UpsertEventTechAspect,
                new { id = dto.Id },
                "PUT",
                "Add Tech aspect",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Event, dto);
        }

        // Actor link (owner)
        yield return new LinkDefinition(
            "actor",
            RouteNames.GetActorById,
            new { id = dto.ActorId },
            "GET",
            dto.ActorDisplayName);

        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateEvent,
            new { id = dto.Id },
            "PUT",
            "Update event",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Event, dto);

        if (dto.EventStatusId == (int)EventStatusEnum.Draft)
        {
            yield return new LinkDefinition(
                LinkRelations.PublishReadiness,
                RouteNames.GetEventPublishReadiness,
                new { id = dto.Id },
                "GET",
                "Review publish readiness",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Event, dto);

            yield return new LinkDefinition(
                LinkRelations.Publish,
                RouteNames.PublishEvent,
                new { id = dto.Id },
                "POST",
                "Publish event",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.Event, dto);
        }

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteEvent,
            new { id = dto.Id },
            "DELETE",
            "Delete event",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.Event, dto);

        // Registration link - if registration is required
        if (dto.IsRegistrationRequired)
        {
            if (!string.IsNullOrEmpty(dto.ExternalRegistrationUrl))
            {
                // External registration - we don't generate a link, but the URL is in the DTO
            }
            else
            {
                yield return new LinkDefinition(
                    "register",
                    RouteNames.CreateRegistration,
                    new { eventId = dto.Id },
                    "POST",
                    "Register for event",
                    RequiresAuth: true)
                    .RequirePermission(AuthorizationActions.Create, typeof(EventRegistrationDto), "event_registration");
            }
        }
    }
}

/// <summary>
/// Link policy for EventListDto (collection items).
/// </summary>
public sealed class EventCollectionLinkPolicy : ICollectionLinkPolicy<EventListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(EventListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventById,
            new { id = dto.Id },
            "GET",
            dto.Title);

        // Sessions link
        if (dto.SessionCount.HasValue && dto.SessionCount.Value > 0)
        {
            yield return new LinkDefinition(
                "sessions",
                RouteNames.GetEventSessions,
                new { eventId = dto.Id },
                "GET",
                $"{dto.SessionCount} sessions");
        }


        // Actor link
        yield return new LinkDefinition(
            "actor",
            RouteNames.GetActorById,
            new { id = dto.ActorId },
            "GET",
            dto.ActorDisplayName);

        // Edit link - requires authentication and permission
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateEvent,
            new { id = dto.Id },
            "PUT",
            "Update event",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventList, dto);

        // Delete link - requires authentication and permission
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteEvent,
            new { id = dto.Id },
            "DELETE",
            "Delete event",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.EventList, dto);
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateEvent,
            null,
            "POST",
            "Create new event",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(EventDto), "event");

        // My events link - requires authentication
        yield return new LinkDefinition(
            "my-events",
            RouteNames.GetMyEvents,
            null,
            "GET",
            "My events",
            RequiresAuth: true);
    }
}
