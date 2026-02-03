namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Actor;
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
    }
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
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Actors are typically read-only (created via user/organization registration)
        yield break;
    }
}
