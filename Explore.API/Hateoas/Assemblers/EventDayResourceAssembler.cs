// ABOUTME: Resource assembler for EventDay entities.
// ABOUTME: Converts EventDayDto and EventDayListDto to HAL resources with HATEOAS links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventDay;

/// <summary>
/// Resource assembler for EventDay entities.
/// Converts EventDayDto and EventDayListDto to HAL resources.
/// </summary>
public sealed class EventDayResourceAssembler : ResourceAssemblerBase<EventDayDto, EventDayListDto>
{
    public EventDayResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventDayDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventDayListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
