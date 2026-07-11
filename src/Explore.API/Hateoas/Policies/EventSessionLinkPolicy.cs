// ABOUTME: HATEOAS link policies for event session detail and collection resources.
// ABOUTME: Emits only event session affordances backed by registered API route names.

namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSession;
using Explore.Application.Hateoas;
using Explore.Domain.Enums;

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

        yield return new LinkDefinition(
            "speakers",
            RouteNames.GetEventSessionSpeakersBySession,
            new { eventSessionId = dto.Id },
            "GET",
            "Session speakers",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSession, dto);

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
            "PATCH",
            "Update session",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSession, dto);

        if (EventSessionLifecycleAffordancePolicy.CanSchedule(dto.EventSessionStatusId))
        {
            yield return new LinkDefinition(
                LinkRelations.Schedule,
                RouteNames.ScheduleEventSession,
                new { id = dto.Id },
                "POST",
                "Schedule session",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSession, dto);
        }

        if (EventSessionLifecycleAffordancePolicy.CanPublish(dto.EventSessionStatusId, dto.IsScheduled))
        {
            yield return new LinkDefinition(
                LinkRelations.Publish,
                RouteNames.PublishEventSession,
                new { id = dto.Id },
                "POST",
                "Publish session",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSession, dto);
        }

        if (EventSessionLifecycleAffordancePolicy.CanCancel(dto.EventSessionStatusId))
        {
            yield return EventSessionLifecycleAffordancePolicy.CreateExplicitLifecycleLink(LinkRelations.Cancel, dto, "Cancel session", RouteNames.CancelEventSession);
        }

        if (EventSessionLifecycleAffordancePolicy.CanComplete(dto.EventSessionStatusId))
        {
            yield return EventSessionLifecycleAffordancePolicy.CreateExplicitLifecycleLink(LinkRelations.Complete, dto, "Complete session", RouteNames.CompleteEventSession);
        }

        if (EventSessionLifecycleAffordancePolicy.CanArchive(dto.EventSessionStatusId))
        {
            yield return EventSessionLifecycleAffordancePolicy.CreateExplicitLifecycleLink(LinkRelations.Archive, dto, "Archive session", RouteNames.ArchiveEventSession);
        }

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

        yield return new LinkDefinition(
            "speakers",
            RouteNames.GetEventSessionSpeakersBySession,
            new { eventSessionId = dto.Id },
            "GET",
            "Session speakers",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSessionList, dto);

        if (EventSessionLifecycleAffordancePolicy.CanSchedule(dto.EventSessionStatusId))
        {
            yield return new LinkDefinition(
                LinkRelations.Schedule,
                RouteNames.ScheduleEventSession,
                new { id = dto.Id },
                "POST",
                "Schedule session",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSessionList, dto);
        }

        if (EventSessionLifecycleAffordancePolicy.CanPublish(dto.EventSessionStatusId, dto.IsScheduled))
        {
            yield return new LinkDefinition(
                LinkRelations.Publish,
                RouteNames.PublishEventSession,
                new { id = dto.Id },
                "POST",
                "Publish session",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSessionList, dto);
        }

        if (EventSessionLifecycleAffordancePolicy.CanCancel(dto.EventSessionStatusId))
        {
            yield return EventSessionLifecycleAffordancePolicy.CreateExplicitLifecycleLink(LinkRelations.Cancel, dto, "Cancel session", RouteNames.CancelEventSession);
        }

        if (EventSessionLifecycleAffordancePolicy.CanComplete(dto.EventSessionStatusId))
        {
            yield return EventSessionLifecycleAffordancePolicy.CreateExplicitLifecycleLink(LinkRelations.Complete, dto, "Complete session", RouteNames.CompleteEventSession);
        }

        if (EventSessionLifecycleAffordancePolicy.CanArchive(dto.EventSessionStatusId))
        {
            yield return EventSessionLifecycleAffordancePolicy.CreateExplicitLifecycleLink(LinkRelations.Archive, dto, "Archive session", RouteNames.ArchiveEventSession);
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

internal static class EventSessionLifecycleAffordancePolicy
{
    public static bool CanSchedule(int statusId)
        => statusId is not ((int)EventSessionStatusEnum.Rejected or
            (int)EventSessionStatusEnum.Cancelled or
            (int)EventSessionStatusEnum.Archived or
            (int)EventSessionStatusEnum.Completed or
            (int)EventSessionStatusEnum.Moderated);

    public static bool CanPublish(int statusId, bool isScheduled)
        => isScheduled && statusId is not ((int)EventSessionStatusEnum.Published or
            (int)EventSessionStatusEnum.Rejected or
            (int)EventSessionStatusEnum.Cancelled or
            (int)EventSessionStatusEnum.Archived or
            (int)EventSessionStatusEnum.Completed or
            (int)EventSessionStatusEnum.Moderated);

    public static bool CanCancel(int statusId)
        => statusId is (int)EventSessionStatusEnum.Draft
            or (int)EventSessionStatusEnum.Submitted
            or (int)EventSessionStatusEnum.UnderReview
            or (int)EventSessionStatusEnum.Approved
            or (int)EventSessionStatusEnum.Published;

    public static bool CanComplete(int statusId)
        => statusId == (int)EventSessionStatusEnum.Published;

    public static bool CanArchive(int statusId)
        => statusId is (int)EventSessionStatusEnum.Draft
            or (int)EventSessionStatusEnum.Cancelled
            or (int)EventSessionStatusEnum.Completed;

    public static LinkDefinition CreateExplicitLifecycleLink(
        string relation,
        EventSessionDto dto,
        string title,
        string routeName) =>
        new LinkDefinition(relation, routeName, new { id = dto.Id }, "POST", title, RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSession, dto);

    public static LinkDefinition CreateExplicitLifecycleLink(
        string relation,
        EventSessionListDto dto,
        string title,
        string routeName) =>
        new LinkDefinition(relation, routeName, new { id = dto.Id }, "POST", title, RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventSessionList, dto);
}
