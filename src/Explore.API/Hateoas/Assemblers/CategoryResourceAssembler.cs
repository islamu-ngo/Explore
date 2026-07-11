namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Category;

/// <summary>
/// Resource assembler for Category entities.
/// Converts CategoryDto and CategoryListDto to HAL resources.
/// </summary>
public sealed class CategoryResourceAssembler : ResourceAssemblerBase<CategoryDto, CategoryListDto>
{
    public CategoryResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<CategoryDto> detailLinkPolicy,
        ICollectionLinkPolicy<CategoryListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
