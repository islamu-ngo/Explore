// ABOUTME: Assembles source-aware public event discovery items into HAL resources and collections.
// ABOUTME: Uses the standard batched authorization pipeline for delegated local actions and source links.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.PublicExperience;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EventDiscoveryResourceAssembler : ResourceAssemblerBase<EventDiscoveryItemDto>
{
    public EventDiscoveryResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<EventDiscoveryItemDto> detailLinkPolicy,
        ICollectionLinkPolicy<EventDiscoveryItemDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
