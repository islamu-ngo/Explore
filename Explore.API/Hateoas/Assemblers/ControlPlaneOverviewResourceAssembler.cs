// ABOUTME: HAL assembler for the Control Plane overview resource.
// ABOUTME: Reuses the shared link authorization pipeline for instance-console affordances.

namespace Explore.API.Hateoas.Assemblers;

using Explore.Application.Contracts.Hateoas;
using Explore.Application.DTOs.ControlPlane;

public sealed class ControlPlaneOverviewResourceAssembler
    : ResourceAssemblerBase<ControlPlaneOverviewDto, ControlPlaneOverviewDto>
{
    public ControlPlaneOverviewResourceAssembler(
        IHateoasLinkGenerator linkGenerator,
        ILinkPolicy<ControlPlaneOverviewDto> detailLinkPolicy,
        ICollectionLinkPolicy<ControlPlaneOverviewDto> collectionLinkPolicy)
        : base(linkGenerator, detailLinkPolicy, collectionLinkPolicy)
    {
    }
}
