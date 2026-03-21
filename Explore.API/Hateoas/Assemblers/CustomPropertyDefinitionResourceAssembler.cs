// ABOUTME: HATEOAS resource assembler for shared custom-property definition detail and list DTOs.
// ABOUTME: Uses the generic base assembler because this slice does not need custom embedded resources yet.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.CustomPropertyDefinition;

namespace Explore.API.Hateoas.Assemblers;

public sealed class CustomPropertyDefinitionResourceAssembler : ResourceAssemblerBase<CustomPropertyDefinitionDto, CustomPropertyDefinitionListDto>
{
    public CustomPropertyDefinitionResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<CustomPropertyDefinitionDto> detailLinkPolicy,
        ICollectionLinkPolicy<CustomPropertyDefinitionListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
