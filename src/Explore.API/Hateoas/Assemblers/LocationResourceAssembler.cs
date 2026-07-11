namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Location;

/// <summary>
/// Resource assembler for Location entities.
/// Converts LocationDto and LocationListDto to HAL resources.
/// </summary>
public sealed class LocationResourceAssembler : ResourceAssemblerBase<LocationDto, LocationListDto>
{
    public LocationResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<LocationDto> detailLinkPolicy,
        ICollectionLinkPolicy<LocationListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
