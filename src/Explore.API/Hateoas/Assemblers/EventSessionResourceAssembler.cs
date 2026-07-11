namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSession;

/// <summary>
/// Resource assembler for EventSession entities.
/// Converts EventSessionDto and EventSessionListDto to HAL resources.
/// </summary>
public sealed class EventSessionResourceAssembler : ResourceAssemblerBase<EventSessionDto, EventSessionListDto>
{
    public EventSessionResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSessionDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventSessionListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
