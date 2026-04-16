// ABOUTME: Resource assembler for LocationRoom entities.
// ABOUTME: Converts LocationRoomDto and LocationRoomListDto to HAL resources with HATEOAS links.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.LocationRoom;

/// <summary>
/// Resource assembler for LocationRoom entities.
/// Converts LocationRoomDto and LocationRoomListDto to HAL resources.
/// </summary>
public sealed class LocationRoomResourceAssembler : ResourceAssemblerBase<LocationRoomDto, LocationRoomListDto>
{
    public LocationRoomResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<LocationRoomDto> detailLinkPolicy,
        ICollectionLinkPolicy<LocationRoomListDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
