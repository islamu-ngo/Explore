namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Tag;

/// <summary>
/// Resource assembler for Tag entities.
/// Converts TagDto and TagListDto to HAL resources.
/// </summary>
public sealed class TagResourceAssembler : ResourceAssemblerBase<TagDto, TagListDto>
{
    public TagResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<TagDto> detailLinkPolicy,
        ICollectionLinkPolicy<TagListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
