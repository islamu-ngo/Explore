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
            RouteNames.GetRegistrationById,
            new { id = dto.Id },
            "GET",
            $"Registration: {dto.UserFullName}");

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetRegistrations,
            null,
            "GET",
            "All registrations");

        // User link
        yield return new LinkDefinition(
            "user",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);

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
                RouteNames.GetAtprotoRecordById,
                new { id = dto.AtprotoRecordId },
                "GET",
                "ATProto record");
        }

        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateRegistration,
            new { id = dto.Id },
            "PUT",
            "Update registration",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.EventRegistration, dto);

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteRegistration,
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
            RouteNames.GetRegistrationById,
            new { id = dto.Id },
            "GET",
            $"Registration: {dto.UserFullName}");

        // User link
        yield return new LinkDefinition(
            "user",
            RouteNames.GetUserById,
            new { id = dto.UserId },
            "GET",
            dto.UserFullName);

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
            RouteNames.CreateRegistration,
            null,
            "POST",
            "Register for event",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(EventRegistrationDto), "event_registration");
    }
}
