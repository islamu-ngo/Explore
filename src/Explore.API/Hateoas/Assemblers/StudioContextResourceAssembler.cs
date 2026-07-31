// ABOUTME: Wraps the private Studio context in a HAL resource.
// ABOUTME: Delegates link materialization to the capability-derived Studio context policy.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Studio;

public sealed class StudioContextResourceAssembler
    : ResourceAssemblerBase<StudioContextDto, StudioContextDto>
{
    public StudioContextResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<StudioContextDto> detailLinkPolicy,
        ICollectionLinkPolicy<StudioContextDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
