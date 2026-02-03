namespace Explore.API.Hateoas.Policies;

using System.Security.Claims;
using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.IndexedDid;
using Explore.Application.Hateoas;

/// <summary>
/// Link policy for IndexedDidDto (detail view).
/// Provides links for indexed DID operations (federation identity).
/// </summary>
public sealed class IndexedDidDetailLinkPolicy : ILinkPolicy<IndexedDidDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetLinks(IndexedDidDto dto, ClaimsPrincipal? user)
    {
        // Self link (by DID - primary identifier)
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetIndexedDidByDid,
            new { did = dto.Did },
            "GET",
            dto.Handle ?? dto.Did);

        // Collection link
        yield return new LinkDefinition(
            LinkRelations.Collection,
            RouteNames.GetIndexedDids,
            null,
            "GET",
            "All indexed DIDs");

        // Actor link (if there's a local actor for this DID)
        yield return new LinkDefinition(
            "actor",
            RouteNames.GetActorByDid,
            new { did = dto.Did },
            "GET",
            "Local actor profile");

        // Update link - requires authentication
        yield return new LinkDefinition(
            LinkRelations.Edit,
            RouteNames.UpdateIndexedDid,
            new { did = dto.Did },
            "PUT",
            "Update indexed DID",
            RequiresAuth: true);
    }
}

/// <summary>
/// Link policy for IndexedDidListDto (collection items).
/// </summary>
public sealed class IndexedDidCollectionLinkPolicy : ICollectionLinkPolicy<IndexedDidListDto>
{
    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetItemLinks(IndexedDidListDto dto, ClaimsPrincipal? user)
    {
        // Self link for item
        yield return new LinkDefinition(
            LinkRelations.Self,
            RouteNames.GetIndexedDidByDid,
            new { did = dto.Did },
            "GET",
            dto.Handle ?? dto.Did);

        // Actor link
        yield return new LinkDefinition(
            "actor",
            RouteNames.GetActorByDid,
            new { did = dto.Did },
            "GET",
            "Local actor profile");
    }

    /// <inheritdoc />
    public IEnumerable<LinkDefinition> GetCollectionLinks(ClaimsPrincipal? user)
    {
        // Create link - requires authentication
        yield return new LinkDefinition(
            "create",
            RouteNames.CreateIndexedDid,
            null,
            "POST",
            "Index new DID",
            RequiresAuth: true);
    }
}
