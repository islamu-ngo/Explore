// ABOUTME: HATEOAS resource assembler for event session template detail and list DTOs.
// ABOUTME: Uses the generic base assembler because this slice does not need custom embedded resources yet.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSessionTemplate;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EventSessionTemplateResourceAssembler : ResourceAssemblerBase<EventSessionTemplateDto, EventSessionTemplateListDto>
{
    public EventSessionTemplateResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSessionTemplateDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventSessionTemplateListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
