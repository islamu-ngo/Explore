// ABOUTME: Converts event-series detail and list DTOs into HAL resources.
// ABOUTME: Uses the shared authorization-aware resource assembler pipeline.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.EventSeries;

public sealed class EventSeriesResourceAssembler : ResourceAssemblerBase<EventSeriesDto, EventSeriesListDto>
{
    public EventSeriesResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventSeriesDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventSeriesListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
