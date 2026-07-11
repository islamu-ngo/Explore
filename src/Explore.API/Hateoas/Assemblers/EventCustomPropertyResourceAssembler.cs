// ABOUTME: HATEOAS resource assembler for event-level custom property definition detail and list DTOs.
// ABOUTME: Uses the generic base assembler because this slice does not need custom embedded resources yet.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventCustomProperty;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EventCustomPropertyResourceAssembler : ResourceAssemblerBase<EventCustomPropertyDefinitionDto, EventCustomPropertyDefinitionListDto>
{
    public EventCustomPropertyResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventCustomPropertyDefinitionDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventCustomPropertyDefinitionListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
