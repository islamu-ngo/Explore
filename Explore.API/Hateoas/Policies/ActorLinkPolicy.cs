// ABOUTME: HATEOAS link policies for actor detail and collection resources.
// ABOUTME: Adds public navigation plus authenticated subscription affordances for organization and group actors.

namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.API.Hateoas;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Actor;
using Explore.Domain.Enums;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for ActorDto (detail view).
/// </summary>
public sealed class ActorDetailLinkPolicy : ILinkPolicy<ActorDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(ActorDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetActorById,
            new { id = dto.Id },
            "GET",
            dto.DisplayName);

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetActors,
            null,
            "GET",
            "All actors");

        // Events by actor link
        yield return new LinkDefinition(
            "events",
            RouteNames.GetActorEvents,
            new { actorId = dto.Id },
            "GET",
            "Events by this actor");

        // Organization link (if organization actor)
        if (dto.OrganizationId.HasValue)
        {
            yield return new LinkDefinition(
                "organization",
                RouteNames.GetOrganizationById,
                new { id = dto.OrganizationId },
                "GET",
                "Organization");
        }

        if (CanSubscribe(dto.ActorTypeId))
        {
            yield return new LinkDefinition(
                "subscription",
                RouteNames.GetActorSubscriptionByActor,
                new { targetActorId = dto.Id },
                "GET",
                "My subscription to this actor",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.ActorSubscriptions.View,
                    ResourceKinds.ActorSubscription,
                    dto.Id.ToString(),
                    SubscriptionAttributes(dto.Id));

            yield return new LinkDefinition(
                "subscribe",
                RouteNames.SubscribeToActor,
                null,
                "POST",
                "Subscribe to this actor",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.ActorSubscriptions.Create,
                    ResourceKinds.ActorSubscription,
                    dto.Id.ToString(),
                    SubscriptionAttributes(dto.Id));
        }
    }

    private static bool CanSubscribe(int actorTypeId) => actorTypeId is (int)ActorTypeEnum.Organization or (int)ActorTypeEnum.Group;

    private static IReadOnlyDictionary<string, object> SubscriptionAttributes(Guid targetActorId) => new Dictionary<string, object>
    {
        ["targetActorId"] = targetActorId.ToString()
    };
}

/// <summary>
/// Link policy for ActorListDto (collection items).
/// </summary>
public sealed class ActorCollectionLinkPolicy : ICollectionLinkPolicy<ActorListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(ActorListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetActorById,
            new { id = dto.Id },
            "GET",
            dto.DisplayName);

        // Events by actor
        yield return new LinkDefinition(
            "events",
            RouteNames.GetActorEvents,
            new { actorId = dto.Id },
            "GET",
            "Events");

        if (CanSubscribe(dto.ActorTypeId))
        {
            yield return new LinkDefinition(
                "subscription",
                RouteNames.GetActorSubscriptionByActor,
                new { targetActorId = dto.Id },
                "GET",
                "My subscription to this actor",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.ActorSubscriptions.View,
                    ResourceKinds.ActorSubscription,
                    dto.Id.ToString(),
                    SubscriptionAttributes(dto.Id));

            yield return new LinkDefinition(
                "subscribe",
                RouteNames.SubscribeToActor,
                null,
                "POST",
                "Subscribe to this actor",
                RequiresAuth: true)
                .RequirePermission(AuthorizationActions.ActorSubscriptions.Create,
                    ResourceKinds.ActorSubscription,
                    dto.Id.ToString(),
                    SubscriptionAttributes(dto.Id));
        }
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Actors are typically read-only (created via user/organization registration)
        yield break;
    }

    private static bool CanSubscribe(int actorTypeId) => actorTypeId is (int)ActorTypeEnum.Organization or (int)ActorTypeEnum.Group;

    private static IReadOnlyDictionary<string, object> SubscriptionAttributes(Guid targetActorId) => new Dictionary<string, object>
    {
        ["targetActorId"] = targetActorId.ToString()
    };
}
