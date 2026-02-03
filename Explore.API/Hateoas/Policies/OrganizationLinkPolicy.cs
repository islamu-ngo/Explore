namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Organization;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for OrganizationDto (detail view).
/// Determines which links should be included based on resource state and user authorization.
/// </summary>
public sealed class OrganizationDetailLinkPolicy : ILinkPolicy<OrganizationDto>
{
    public IEnumerable<LinkDefinition> GetLinks(OrganizationDto dto, ClaimsPrincipal? user)
    {
        // Self link - always included
        yield return LinkDefinition.Self(
            RouteNames.GetOrganizationById,
            new { id = dto.Id });

        // Collection link
        yield return LinkDefinition.Collection(RouteNames.GetOrganizations);

        // Related resources - always included
        yield return LinkDefinition.Related(
            LinkRelations.Events,
            RouteNames.GetOrganizationEvents,
            new { id = dto.Id });

        yield return LinkDefinition.Related(
            LinkRelations.Members,
            RouteNames.GetOrganizationMembers,
            new { id = dto.Id });

        // Actor link (if organization has an actor)
        if (dto.ActorId.HasValue)
        {
            yield return LinkDefinition.Related(
                LinkRelations.Actor,
                RouteNames.GetActorById,
                new { id = dto.ActorId.Value });
        }

        // Edit link - requires authentication
        // In a real implementation, you'd check if user is a member of this organization
        yield return LinkDefinition.Edit(
            RouteNames.UpdateOrganization,
            new { id = dto.Id });

        // Delete link - requires admin role
        yield return LinkDefinition.Delete(
            RouteNames.DeleteOrganization,
            new { id = dto.Id },
            roles: new[] { "Admin" });
    }
}

/// <summary>
/// Link policy for OrganizationListDto (collection items).
/// </summary>
public sealed class OrganizationCollectionLinkPolicy : ICollectionLinkPolicy<OrganizationListDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(OrganizationListDto dto, ClaimsPrincipal? user)
    {
        // Self link for the item
        yield return LinkDefinition.Self(
            RouteNames.GetOrganizationById,
            new { id = dto.Id });

        // Events link
        yield return LinkDefinition.Related(
            LinkRelations.Events,
            RouteNames.GetOrganizationEvents,
            new { id = dto.Id });

        // Members link
        yield return LinkDefinition.Related(
            LinkRelations.Members,
            RouteNames.GetOrganizationMembers,
            new { id = dto.Id });
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return LinkDefinition.Create(RouteNames.CreateOrganization);
    }
}
