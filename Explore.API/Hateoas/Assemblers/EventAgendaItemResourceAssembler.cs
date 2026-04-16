// ABOUTME: Resource assembler for EventAgendaItem entities.
// ABOUTME: Converts EventAgendaItemDto and EventAgendaItemListDto to HAL resources with HATEOAS links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventAgendaItem;

/// <summary>
/// Resource assembler for EventAgendaItem entities.
/// Converts EventAgendaItemDto and EventAgendaItemListDto to HAL resources.
/// </summary>
public sealed class EventAgendaItemResourceAssembler : ResourceAssemblerBase<EventAgendaItemDto, EventAgendaItemListDto>
{
    public EventAgendaItemResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventAgendaItemDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventAgendaItemListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
