namespace Explore.API.Hateoas.Policies;

using System.Collections.Generic;
using System.Security.Claims;
using Explore.Application.Authorization;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.StorageObject;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for StorageObjectDto (detail view).
/// Provides links for storage object operations.
/// </summary>
public sealed class StorageObjectDetailLinkPolicy : ILinkPolicy<StorageObjectDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(StorageObjectDto dto, ClaimsPrincipal? user)
    {
        // Self link
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetStorageObjectById,
            new { id = dto.Id },
            "GET",
            dto.FullName);

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetStorageObjects,
            null,
            "GET",
            "All storage objects");

        // Delete link - requires authentication
        yield return new LinkDefinition(
            "delete",
            RouteNames.DeleteStorageObject,
            new { id = dto.Id },
            "DELETE",
            "Delete storage object",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Delete, ResourceDescriptors.StorageObject, dto);
    }
}

/// <summary>
/// Link policy for StorageObjectListDto (collection items).
/// </summary>
public sealed class StorageObjectCollectionLinkPolicy : ICollectionLinkPolicy<StorageObjectListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(StorageObjectListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetStorageObjectById,
            new { id = dto.Id },
            "GET",
            dto.FullName);
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create/upload link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateStorageObject,
            null,
            "POST",
            "Upload storage object",
            RequiresAuth: true)
            .RequirePermission(AuthorizationActions.Create, typeof(StorageObjectDto), "storage_object");
    }
}
