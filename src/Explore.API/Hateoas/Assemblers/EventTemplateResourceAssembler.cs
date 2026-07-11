// ABOUTME: HATEOAS resource assembler for event template detail and list DTOs.
// ABOUTME: Uses the generic base assembler because this slice does not need custom embedded resources yet.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventTemplate;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EventTemplateResourceAssembler : ResourceAssemblerBase<EventTemplateDto, EventTemplateListDto>
{
    public EventTemplateResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventTemplateDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventTemplateListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
