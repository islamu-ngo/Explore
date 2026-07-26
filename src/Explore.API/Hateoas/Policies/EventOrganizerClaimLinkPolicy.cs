// ABOUTME: HAL policies for organizer-claim detail and collection resources.
// ABOUTME: Filters claimant withdrawal and curator review affordances through event authorization.

using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventOrganizerClaim;
using Explore.Application.Hateoas;

namespace Explore.API.Hateoas.Policies;

public sealed class EventOrganizerClaimDetailLinkPolicy : ILinkPolicy<EventOrganizerClaimDto>
{
    public IEnumerable<LinkDefinition> GetLinks(EventOrganizerClaimDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventOrganizerClaim,
            new { eventId = dto.EventId, claimId = dto.Id },
            HttpMethods.Get,
            "Organizer claim",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Events.ViewOrganizerClaims, ResourceKinds.Event, dto.EventId.ToString());
        yield return new LinkDefinition(
            LinkRelations.Event,
            RouteNames.GetEventById,
            new { id = dto.EventId },
            HttpMethods.Get,
            "Event");
        yield return new LinkDefinition(
            LinkRelations.WithdrawClaim,
            RouteNames.WithdrawEventOrganizerClaim,
            new { eventId = dto.EventId, claimId = dto.Id },
            HttpMethods.Post,
            "Withdraw organizer claim",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Events.ClaimOrganizer, ResourceKinds.Event, dto.EventId.ToString());
        yield return new LinkDefinition(
            LinkRelations.ReviewClaim,
            RouteNames.ReviewEventOrganizerClaim,
            new { eventId = dto.EventId, claimId = dto.Id },
            HttpMethods.Post,
            "Review organizer claim",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Events.ReviewOrganizerClaim, ResourceKinds.Event, dto.EventId.ToString());
    }
}

public sealed class EventOrganizerClaimCollectionLinkPolicy : ICollectionLinkPolicy<EventOrganizerClaimDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(EventOrganizerClaimDto dto, ClaimsPrincipal? user)
    {
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetEventOrganizerClaim,
            new { eventId = dto.EventId, claimId = dto.Id },
            HttpMethods.Get,
            "Organizer claim",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Events.ViewOrganizerClaims, ResourceKinds.Event, dto.EventId.ToString());
        yield return new LinkDefinition(
            LinkRelations.Event,
            RouteNames.GetEventById,
            new { id = dto.EventId },
            HttpMethods.Get,
            "Event");
        yield return new LinkDefinition(
            LinkRelations.WithdrawClaim,
            RouteNames.WithdrawEventOrganizerClaim,
            new { eventId = dto.EventId, claimId = dto.Id },
            HttpMethods.Post,
            "Withdraw organizer claim",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Events.ClaimOrganizer, ResourceKinds.Event, dto.EventId.ToString());
        yield return new LinkDefinition(
            LinkRelations.ReviewClaim,
            RouteNames.ReviewEventOrganizerClaim,
            new { eventId = dto.EventId, claimId = dto.Id },
            HttpMethods.Post,
            "Review organizer claim",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Events.ReviewOrganizerClaim, ResourceKinds.Event, dto.EventId.ToString());
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        yield break;
    }
}
