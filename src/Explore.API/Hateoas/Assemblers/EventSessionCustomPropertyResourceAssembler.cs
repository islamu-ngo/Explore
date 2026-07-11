// ABOUTME: HATEOAS resource assembler for event-session-level custom property definition detail and list DTOs.
// ABOUTME: Uses the generic base assembler because this slice does not need custom embedded resources yet.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSessionCustomProperty;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EventSessionCustomPropertyResourceAssembler : ResourceAssemblerBase<EventSessionCustomPropertyDefinitionDto, EventSessionCustomPropertyDefinitionListDto>
{
    public EventSessionCustomPropertyResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSessionCustomPropertyDefinitionDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventSessionCustomPropertyDefinitionListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
