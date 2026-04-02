namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.CustomPropertyDefinition;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for CustomPropertyDefinitionDto (detail view).
/// Determines which links should be included based on resource state and user authorization.
/// </summary>
public sealed class CustomPropertyDefinitionDetailLinkPolicy : ILinkPolicy<CustomPropertyDefinitionDto>
{
    public IEnumerable<LinkDefinition> GetLinks(CustomPropertyDefinitionDto dto, ClaimsPrincipal? user)
    {
        // Self link - always included
        yield return LinkDefinition.Self(
            RouteNames.GetCustomPropertyDefinitionById,
            new { id = dto.Id });

        // Collection link
        yield return LinkDefinition.Collection(
            RouteNames.GetCustomPropertyDefinitions,
            new { entityTypeName = dto.EntityTypeName });

        // Edit link - requires admin role
        yield return LinkDefinition.Edit(
            RouteNames.UpdateCustomPropertyDefinition,
            new { id = dto.Id })
            .RequirePermission(AuthorizationActions.Update, ResourceDescriptors.CustomPropertyDefinition, dto);

        // Delete link - requires admin role
        yield return LinkDefinition.Delete(
            RouteNames.DeleteCustomPropertyDefinition,
            new { id = dto.Id })
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.CustomPropertyDefinition, dto);
    }
}

/// <summary>
/// Link policy for CustomPropertyDefinitionListDto (collection items).
/// </summary>
public sealed class CustomPropertyDefinitionCollectionLinkPolicy : ICollectionLinkPolicy<CustomPropertyDefinitionListDto>
{
    public IEnumerable<LinkDefinition> GetItemLinks(CustomPropertyDefinitionListDto dto, ClaimsPrincipal? user)
    {
        // Self link for the item
        yield return LinkDefinition.Self(
            RouteNames.GetCustomPropertyDefinitionById,
            new { id = dto.Id });
    }

    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires admin role
        yield return LinkDefinition.Create(RouteNames.CreateCustomPropertyDefinition)
            .RequirePermission(AuthorizationActions.Create, typeof(CustomPropertyDefinitionDto), "customPropertyDefinition");
    }
}
