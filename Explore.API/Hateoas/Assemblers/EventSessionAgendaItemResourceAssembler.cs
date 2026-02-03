namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSessionAgendaItem;

/// <summary>
/// Resource assembler for EventSessionAgendaItem entities.
/// Converts EventSessionAgendaItemDto and EventSessionAgendaItemListDto to HAL resources with appropriate links.
/// </summary>
public sealed class EventSessionAgendaItemResourceAssembler : ResourceAssemblerBase<EventSessionAgendaItemDto, EventSessionAgendaItemListDto>
{
    public EventSessionAgendaItemResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSessionAgendaItemDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventSessionAgendaItemListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }

    /// <summary>
    /// Override to provide embedded resources for agenda item details.
    /// </summary>
    protected override Dictionary<string, object>? GetEmbeddedResources(
        EventSessionAgendaItemDto dto,
        Microsoft.AspNetCore.Http.HttpContext httpContext)
    {
        // Agenda items link to EventSession, Location via _links
        return null;
    }
}
