namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Location;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for LocationDto (detail view).
/// </summary>
public sealed class LocationDetailLinkPolicy : ILinkPolicy<LocationDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(LocationDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetLocationById,
            new { id = dto.Id },
            "GET",
            dto.FullName);

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetLocations,
            null,
            "GET",
            "All locations");

        // Edit link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateLocation,
            new { id = dto.Id },
            "PUT",
            "Update location",
            RequiresAuth: true)
            .RequirePermission(
                PermissionAction.Update,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["tenantId"] = dto.TenantId.ToString()
                });

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteLocation,
            new { id = dto.Id },
            "DELETE",
            "Delete location",
            RequiresAuth: true)
            .RequirePermission(
                PermissionAction.Delete,
                dto,
                dto.Id.ToString(),
                new Dictionary<string, object>
                {
                    ["tenantId"] = dto.TenantId.ToString()
                });
    }
}

/// <summary>
/// Link policy for LocationListDto (collection items).
/// </summary>
public sealed class LocationCollectionLinkPolicy : ICollectionLinkPolicy<LocationListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(LocationListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetLocationById,
            new { id = dto.Id },
            "GET",
            dto.FullName);
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateLocation,
            null,
            "POST",
            "Create new location",
            RequiresAuth: true)
            .RequirePermission(PermissionAction.Create, typeof(LocationDto), "location");
    }
}
