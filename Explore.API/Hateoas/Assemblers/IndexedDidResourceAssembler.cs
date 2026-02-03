namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.IndexedDid;

/// <summary>
/// Resource assembler for IndexedDid entities.
/// Converts IndexedDidDto and IndexedDidListDto to HAL resources with appropriate links.
/// Part of ATProto federation support - represents federated identity.
/// </summary>
public sealed class IndexedDidResourceAssembler : ResourceAssemblerBase<IndexedDidDto, IndexedDidListDto>
{
    public IndexedDidResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<IndexedDidDto> detailLinkPolicy,
        ICollectionLinkPolicy<IndexedDidListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for indexed DID details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        IndexedDidDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // IndexedDids link to local Actors via _links
        return null;
    }
}
