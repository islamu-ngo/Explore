// ABOUTME: Wraps EventLocation management DTOs in authorization-filtered HAL resources.
// ABOUTME: Reuses the shared candidate, normalize, batch, and materialize pipeline.

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.Location;

namespace Explore.API.Hateoas.Assemblers;

public sealed class EventLocationResourceAssembler(
    IHateoasLinkGenerator linkGenerator,
    ILinkPolicy<EventLocationManagementDto> detailPolicy,
    ICollectionLinkPolicy<EventLocationManagementDto> collectionPolicy)
    : ResourceAssemblerBase<EventLocationManagementDto, EventLocationManagementDto>(
        linkGenerator,
        detailPolicy,
        collectionPolicy);
