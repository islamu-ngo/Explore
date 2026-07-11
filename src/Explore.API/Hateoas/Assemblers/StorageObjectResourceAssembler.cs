namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.StorageObject;

/// <summary>
/// Resource assembler for StorageObject entities.
/// Converts StorageObjectDto and StorageObjectListDto to HAL resources with appropriate links.
/// </summary>
public sealed class StorageObjectResourceAssembler : ResourceAssemblerBase<StorageObjectDto, StorageObjectListDto>
{
    public StorageObjectResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<StorageObjectDto> detailLinkPolicy,
        ICollectionLinkPolicy<StorageObjectListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for storage object details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        StorageObjectDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Storage objects are standalone resources
        return null;
    }
}
